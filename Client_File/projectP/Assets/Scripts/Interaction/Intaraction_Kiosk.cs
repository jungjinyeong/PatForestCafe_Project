using System;
using UnityEngine;

// 키오스크(셀프 계산대) — 매장층마다 기본으로 놓이는 철거·판매 불가 시설(층 프리팹 World_*에 고정 배치).
// 「디저트만」 손님이 이 아래의 Trigger_Kiosk 웨이포인트로 와서 결제한다. 한 번에 한 명씩, 처리 시간만큼 게이지를 채운 뒤 결제된다.
// GameInstance에 의존하지 않아 Init 체인 없이 처음 요청이 올 때 게이지를 만든다.
public class Intaraction_Kiosk : MonoBehaviour, IPaymentFacility
{
    // 기본 계산 시간. 층별 업그레이드(4초 → 3초)는 아직 연결하지 않았다(업그레이드 UI와 함께 작업 예정).
    public const float BASE_PROCESS_SECONDS = 5f;

    [Header("Line")]
    [Tooltip("키오스크 앞 대기 줄의 칸 간격·방향(결제 웨이포인트 기준).")]
    [SerializeField] private Vector3 mLineSlotStep = new Vector3(0f, -0.6f, 0f);

    [Header("Process Gauge")]
    [SerializeField] private Vector3 mGaugeOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private Vector2 mGaugeSize = new Vector2(0.8f, 0.12f);
    [SerializeField] private Color mGaugeBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color mGaugeFillColor = new Color(0.95f, 0.75f, 0.3f, 1f);

    private TimedServiceQueue mQueue;
    private ServiceLineSet mLines;

    public float ProcessSeconds => BASE_PROCESS_SECONDS;

    private void OnDestroy()
    {
        mQueue?.Dispose();
    }

    public ServiceLine GetLine(Waypoint front)
    {
        if (mLines == null)
            mLines = new ServiceLineSet(mLineSlotStep);
        return mLines.Get(front);
    }

    public void BeginService(Action onComplete, Func<bool> isValid)
    {
        BeginProcess(onComplete, isValid);
    }

    // 결제를 요청한다. 앞 손님이 처리 중이면 차례를 기다린다. isValid: 기다리는 사이 손님이 떠났는지 확인.
    public void BeginProcess(Action onComplete, Func<bool> isValid = null)
    {
        if (mQueue == null)
        {
            var gauge = new WorldGaugeBar(transform, mGaugeOffset, mGaugeSize, mGaugeBackgroundColor, mGaugeFillColor);
            mQueue = new TimedServiceQueue(gauge, () => ProcessSeconds);
        }

        mQueue.Enqueue(onComplete, isValid);
    }
}
