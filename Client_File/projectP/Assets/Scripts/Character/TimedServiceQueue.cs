using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

// 한 번에 한 손님씩 일정 시간 동안 처리하는 결제 시설(카운터 직원·키오스크) 공통 대기열.
// 동시에 도착한 손님은 FIFO로 기다렸다가 앞 손님 게이지가 끝나면 차례로 처리된다.
// isValid: 기다리는 사이 손님이 떠났으면(영업 마감 강제 퇴장 등) false — 그 요청은 건너뛴다.
public class TimedServiceQueue : IDisposable
{
    private struct Request
    {
        public Action OnComplete;
        public Func<bool> IsValid;
    }

    private readonly WorldGaugeBar mGauge;
    private readonly Func<float> mGetDuration;
    private readonly Queue<Request> mWaiting = new Queue<Request>();
    private IDisposable mProgressDisposable;
    private bool mIsBusy;

    public int WaitingCount => mWaiting.Count;
    public bool IsBusy => mIsBusy;

    // getDuration: 처리 시작 시점마다 읽는다(업그레이드·직원 숙련도가 영업 중에 바뀌어도 다음 손님부터 반영).
    public TimedServiceQueue(WorldGaugeBar gauge, Func<float> getDuration)
    {
        mGauge = gauge;
        mGetDuration = getDuration;
    }

    public void Enqueue(Action onComplete, Func<bool> isValid)
    {
        var request = new Request { OnComplete = onComplete, IsValid = isValid };
        if (mIsBusy)
        {
            mWaiting.Enqueue(request);
            return;
        }

        Start(request);
    }

    // 시설이 사라질 때(층·직원 오브젝트 파괴, 씬 종료). 결제를 대신 처리하지 않고 요청만 버린다 —
    // 파괴 도중 골드·판매 기록이 바뀌면 안 되기 때문. 기다리던 손님은 CharNpc의 결제 대기 시간 초과로 스스로 포기한다.
    public void Dispose()
    {
        mProgressDisposable?.Dispose();
        mProgressDisposable = null;
        mIsBusy = false;
        mWaiting.Clear();
        mGauge?.SetVisible(false);
    }

    private void Start(Request request)
    {
        mIsBusy = true;
        mProgressDisposable?.Dispose();

        float duration = Mathf.Max(0f, mGetDuration != null ? mGetDuration() : 0f);
        float elapsed = 0f;

        mGauge?.SetVisible(true);
        mGauge?.SetRatio(0f);

        mProgressDisposable = Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                // 처리 중인 손님이 떠났으면 게이지를 끝까지 채우지 않고 다음 손님으로 넘어간다.
                bool isValid = request.IsValid == null || request.IsValid();
                elapsed += Time.deltaTime;
                float ratio = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                mGauge?.SetRatio(ratio);

                if (isValid && ratio < 1f) return;

                Finish();
                if (isValid)
                    request.OnComplete?.Invoke();
            });
    }

    private void Finish()
    {
        mProgressDisposable?.Dispose();
        mProgressDisposable = null;
        mIsBusy = false;
        mGauge?.SetVisible(false);

        while (mWaiting.Count > 0)
        {
            var next = mWaiting.Dequeue();
            if (next.IsValid != null && !next.IsValid())
                continue;

            Start(next);
            return;
        }
    }
}
