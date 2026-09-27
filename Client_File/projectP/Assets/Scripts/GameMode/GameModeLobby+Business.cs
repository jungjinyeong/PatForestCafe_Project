using System;
using UniRx;
using UnityEngine;

// 하루 영업 흐름(BusinessModel.State)에 맞춰 손님 스폰·자동 마감·정산을 진행한다.
// 영업 중 → 스폰 / 마감 중 → 스폰 중단 + 남은 손님 퇴장 대기 / 영업 종료 → 저장 + 정산 팝업.
public partial class GameModeLobby
{
    private readonly CompositeDisposable mBusinessDisposables = new CompositeDisposable();
    private readonly SerialDisposable mClosingDisposable = new SerialDisposable();

    private void BindBusiness()
    {
        mBusinessDisposables.Clear();

        var business = GameInstance.Model.Business;

        business.State
            .Subscribe(OnBusinessStateChanged)
            .AddTo(mBusinessDisposables);

        // 게임 시계가 마감 시각을 넘는 순간 영업 중이면 자동 마감. 마감 24시는 자정에 0으로 돌아가는 순간이다
        // ([다음 날 영업 시작]의 SetHour처럼 시계를 되돌리는 경우와 구분하려고 자정 부근만 본다).
        int closeHour = BusinessModel.CloseHour;
        GameInstance.Time.CurrentHour
            .Pairwise()
            .Where(p => closeHour >= 24
                ? p.Previous >= 23f && p.Current < 1f
                : p.Previous < closeHour && p.Current >= closeHour)
            .Subscribe(_ => business.RequestClose())
            .AddTo(mBusinessDisposables);

        mBusinessDisposables.AddTo(this);
        mClosingDisposable.AddTo(this);
    }

    private void OnBusinessStateChanged(eBusinessState state)
    {
        switch (state)
        {
            case eBusinessState.Open:
                mClosingDisposable.Disposable = null;
                GameInstance.Spawn.StartAutoSpawn();
                break;

            case eBusinessState.Closing:
                GameInstance.Spawn.StopAutoSpawn();
                WaitGuestsLeave();
                break;

            case eBusinessState.Closed:
                mClosingDisposable.Disposable = null;
                GameInstance.Save.Save();
                GameInstance.UI.Open<UIPopupSettlement, UIPopupSettlement.Param>(eUIType.PopupSettlement, new UIPopupSettlement.Param());
                break;
        }
    }

    private void WaitGuestsLeave()
    {
        var allLeft = GameInstance.Spawn.ActiveCount
            .Where(count => count <= 0)
            .Take(1)
            .AsUnitObservable();

        // 손님이 길에 끼여 퇴장하지 못해도 마감이 끝나도록 하는 안전장치(GameTime.csv ClosingTimeoutSeconds).
        var timeout = Observable.Timer(TimeSpan.FromSeconds(BusinessModel.ClosingTimeoutSeconds))
            .Do(_ =>
            {
                Logger.Warning("[GameModeLobby] 마감 대기 시간 초과 — 남은 손님을 정리합니다.");
                GameInstance.Spawn.DespawnAll();
            })
            .AsUnitObservable();

        mClosingDisposable.Disposable = allLeft.Amb(timeout)
            .Take(1)
            .Subscribe(_ => GameInstance.Model.Business.CompleteClose());
    }

    // 정산 팝업 [다음 날 영업 시작]에서 호출.
    public static void StartNextBusinessDay()
    {
        if (!GameInstance.Model.Business.StartNextDay())
            return;

        GameInstance.Time.SetHour(BusinessModel.OpenHour);
    }
}
