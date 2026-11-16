// 주문받는 NPC(바리스타/점원) — 카운터에 고정 배치되는 정적 캐릭터.
// CharNpc와 달리 이동/AI 로직이 없다. 손님이 걸어와 상호작용하는 지점은
// 이 오브젝트의 자식으로 붙는 Trigger_Order Waypoint가 맡고, 실제 상호작용 처리는
// 기존 CharNpc/Waypoint 로직이 그대로 담당한다(웨이포인트 소유자가 가구에서 이 NPC로
// 바뀌어도 CharNpc는 웨이포인트 타입만 보고 동작하므로 별도 코드 변경이 필요 없다).
// 애니메이션은 이 오브젝트가 원래 갖고 있는 Unity Animator 컨트롤러가 자체 기본 상태로
// 재생하므로 별도 재생 로직이 필요 없다(Animator2D 래퍼 미사용).
//
// 2026-08: 음료 픽업 속도(CTable.StaffRow.WorkSpeed) + 픽업 진행 게이지바 추가.
// 게이지(WorldGaugeBar)와 FIFO 대기(TimedServiceQueue)는 키오스크(Intaraction_Kiosk)와 같은 공통 클래스를 쓴다.
using System;
using UnityEngine;

public class CharStaff : CharBase, IPaymentFacility
{
    private const float BaseDrinkPickupSeconds = 2f;

    [Header("ID")]
    [SerializeField] private int mTid = 1;

    [Header("Line")]
    [Tooltip("카운터 앞 대기 줄의 칸 간격·방향(결제 웨이포인트 기준).")]
    [SerializeField] private Vector3 mLineSlotStep = new Vector3(0f, -0.6f, 0f);

    [Header("Pickup Gauge")]
    [SerializeField] private Vector3 mGaugeOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Vector2 mGaugeSize = new Vector2(0.8f, 0.12f);
    [SerializeField] private Color mGaugeBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color mGaugeFillColor = new Color(0.3f, 0.85f, 0.4f, 1f);

    private CTable.StaffRow mStaffRow;
    private TimedServiceQueue mPickupQueue;
    private ServiceLineSet mLines;

    public float WorkSpeed => mStaffRow != null && mStaffRow.WorkSpeed > 0f ? mStaffRow.WorkSpeed : 1f;

    private void OnDestroy()
    {
        mPickupQueue?.Dispose();
    }

    public void Init()
    {
        mStaffRow = GameInstance.Table.Get<CTable.StaffRow>(mTid);
        if (mStaffRow == null)
            Logger.Warning($"[CharStaff] StaffRow를 찾을 수 없습니다. Tid={mTid}");

        EnsureQueue();
    }

    // 음료 픽업을 요청한다. 이미 픽업 중이면(동시 도착) 대기열에서 차례를 기다린다(FIFO).
    // isValid: 기다리는 사이 손님이 떠났는지 확인(떠났으면 건너뜀). CharNpc가 Trigger_Order 도착 시 호출한다.
    public void BeginPickup(Action onComplete, Func<bool> isValid = null)
    {
        EnsureQueue();
        mPickupQueue.Enqueue(onComplete, isValid);
    }

    public ServiceLine GetLine(Waypoint front)
    {
        if (mLines == null)
            mLines = new ServiceLineSet(mLineSlotStep);
        return mLines.Get(front);
    }

    public void BeginService(Action onComplete, Func<bool> isValid)
    {
        BeginPickup(onComplete, isValid);
    }

    private void EnsureQueue()
    {
        if (mPickupQueue != null) return;

        var gauge = new WorldGaugeBar(transform, mGaugeOffset, mGaugeSize, mGaugeBackgroundColor, mGaugeFillColor);
        mPickupQueue = new TimedServiceQueue(gauge, () => BaseDrinkPickupSeconds / WorkSpeed);
    }
}
