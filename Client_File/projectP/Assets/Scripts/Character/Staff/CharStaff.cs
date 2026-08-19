// 주문받는 NPC(바리스타/점원) — 카운터에 고정 배치되는 정적 캐릭터.
// CharNpc와 달리 이동/AI 로직이 없다. 손님이 걸어와 상호작용하는 지점은
// 이 오브젝트의 자식으로 붙는 Trigger_Order Waypoint가 맡고, 실제 상호작용 처리는
// 기존 CharNpc/Waypoint 로직이 그대로 담당한다(웨이포인트 소유자가 가구에서 이 NPC로
// 바뀌어도 CharNpc는 웨이포인트 타입만 보고 동작하므로 별도 코드 변경이 필요 없다).
// 애니메이션은 이 오브젝트가 원래 갖고 있는 Unity Animator 컨트롤러가 자체 기본 상태로
// 재생하므로 별도 재생 로직이 필요 없다(Animator2D 래퍼 미사용).
//
// 2026-08: 음료 픽업 속도(CTable.StaffRow.WorkSpeed) + 픽업 진행 게이지바 추가.
// 씬에 직접 배치되는 캐릭터라 전용 프리팹이 없으므로, 게이지바는 별도 프리팹/스프라이트
// 에셋 없이 런타임에 SpriteRenderer 2장(배경/채움)으로 절차적으로 생성한다.
// 카운터 하나당 게이지 하나뿐이라 동시 도착한 손님은 mPickupQueue에서 FIFO로 대기했다가
// 앞 손님 게이지가 끝난 뒤 순서대로 처리된다.
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class CharStaff : CharBase
{
    private const float BaseDrinkPickupSeconds = 2f;
    private static Sprite mSolidSprite;

    [Header("ID")]
    [SerializeField] private int mTid = 1;

    [Header("Pickup Gauge")]
    [SerializeField] private Vector3 mGaugeOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Vector2 mGaugeSize = new Vector2(0.8f, 0.12f);
    [SerializeField] private Color mGaugeBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color mGaugeFillColor = new Color(0.3f, 0.85f, 0.4f, 1f);

    private CTable.StaffRow mStaffRow;
    private GameObject mGaugeRoot;
    private Transform mGaugeFillTr;
    private IDisposable mPickupDisposable;
    private readonly Queue<Action> mPickupQueue = new Queue<Action>();
    private bool mIsPickupInProgress;

    public float WorkSpeed => mStaffRow != null && mStaffRow.WorkSpeed > 0f ? mStaffRow.WorkSpeed : 1f;

    public void Init()
    {
        mStaffRow = GameInstance.Table.Get<CTable.StaffRow>(mTid);
        if (mStaffRow == null)
            Logger.Warning($"[CharStaff] StaffRow를 찾을 수 없습니다. Tid={mTid}");

        BuildGauge();
    }

    // 음료 픽업을 요청한다. 이미 픽업 중이면(동시 도착) 대기열에 넣고, 먼저 온 손님의 게이지가
    // 끝난 뒤 순서대로 처리한다(FIFO) — 카운터 하나에 게이지 하나뿐이라 동시 진행은 불가.
    // CharNpc.TriggerPause()가 Trigger_Order 도착 시 호출한다.
    public void BeginPickup(Action onComplete)
    {
        if (mIsPickupInProgress)
        {
            mPickupQueue.Enqueue(onComplete);
            return;
        }

        StartPickup(onComplete);
    }

    private void StartPickup(Action onComplete)
    {
        mIsPickupInProgress = true;
        mPickupDisposable?.Dispose();

        float duration = BaseDrinkPickupSeconds / WorkSpeed;
        float elapsed = 0f;

        SetGaugeVisible(true);
        SetGaugeFillRatio(0f);

        mPickupDisposable = Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                elapsed += Time.deltaTime;
                float ratio = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                SetGaugeFillRatio(ratio);

                if (ratio < 1f) return;

                mPickupDisposable?.Dispose();
                mPickupDisposable = null;
                mIsPickupInProgress = false;
                SetGaugeVisible(false);

                if (mPickupQueue.Count > 0)
                    StartPickup(mPickupQueue.Dequeue());

                onComplete?.Invoke();
            })
            .AddTo(this);
    }

    private void BuildGauge()
    {
        if (mGaugeRoot != null) return;

        mGaugeRoot = new GameObject("PickupGaugeBg");
        mGaugeRoot.transform.SetParent(transform, false);
        mGaugeRoot.transform.localPosition = mGaugeOffset;
        mGaugeRoot.transform.localScale = new Vector3(mGaugeSize.x, mGaugeSize.y, 1f);

        var backSr = mGaugeRoot.AddComponent<SpriteRenderer>();
        backSr.sprite = GetSolidSprite();
        backSr.color = mGaugeBackgroundColor;
        backSr.sortingOrder = 10;

        var fillGo = new GameObject("PickupGaugeFill");
        fillGo.transform.SetParent(mGaugeRoot.transform, false);
        fillGo.transform.localPosition = Vector3.zero;

        var fillSr = fillGo.AddComponent<SpriteRenderer>();
        fillSr.sprite = GetSolidSprite();
        fillSr.color = mGaugeFillColor;
        fillSr.sortingOrder = 11;

        mGaugeFillTr = fillGo.transform;

        SetGaugeFillRatio(0f);
        SetGaugeVisible(false);
    }

    // 배경 기준 스케일 안에서 채움 스프라이트를 가로 방향으로만 늘려 게이지를 표현한다(중앙 기준 스케일 — 러프 버전).
    private void SetGaugeFillRatio(float ratio)
    {
        if (mGaugeFillTr == null) return;
        mGaugeFillTr.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
    }

    private void SetGaugeVisible(bool isVisible)
    {
        if (mGaugeRoot != null)
            mGaugeRoot.SetActive(isVisible);
    }

    private static Sprite GetSolidSprite()
    {
        if (mSolidSprite != null) return mSolidSprite;

        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        mSolidSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return mSolidSprite;
    }
}
