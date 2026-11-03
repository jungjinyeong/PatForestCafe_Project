using System;
using UniRx;
using UnityEngine;

// 하루 영업 흐름(BusinessModel.State)에 맞춰 손님 스폰·자동 마감·정산·시계를 진행한다.
// 영업 준비 → 시계를 개점 시각에 멈춤(가구 배치 가능) / 영업 중 → 시계 재개 + 배치 모드 종료 + 스폰
// / 마감 중 → 스폰 중단 + 남은 손님 퇴장 대기 / 영업 종료 → 시계 멈춤 + 저장 + 정산 팝업(가구 배치 가능).
// 정산 [확인] → 다음 날 영업 준비, 탑바 [영업 시작] → 영업 중. 영업 중 탑바 [조기 마감] → 마감 중.
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
        // (영업 준비 진입 시 SetHour처럼 시계를 되돌리는 경우와 구분하려고 자정 부근만 본다).
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
            case eBusinessState.Preparing:
                mClosingDisposable.Disposable = null;
                GameInstance.Time.SetHour(BusinessModel.OpenHour);
                GameInstance.Time.Pause();
                break;

            case eBusinessState.Open:
                mClosingDisposable.Disposable = null;
                // 영업 중에는 가구를 배치할 수 없다 — 들고 있던 가구는 취소(원위치/보관함)하고 배치 모드를 닫는다.
                GameInstance.Model.Placement.SetEditMode(false);
                GameInstance.Time.Resume();
                GameInstance.Spawn.StartAutoSpawn();
                break;

            case eBusinessState.Closing:
                GameInstance.Spawn.StopAutoSpawn();
                WaitGuestsLeave();
                break;

            case eBusinessState.Closed:
                mClosingDisposable.Disposable = null;
                GameInstance.Time.Pause();
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

    // 정산 팝업 [확인]에서 호출 — 다음 날 영업 준비로 넘어가 저장한다(재실행해도 같은 날 준비부터).
    public static void ConfirmSettlement()
    {
        if (GameInstance.Model.Business.StartNextDay())
            GameInstance.Save.Save();
    }

    // 탑바 [영업 시작]에서 호출. 정산 팝업을 [확인] 없이 닫은 영업 종료 상태라면 다음 날 준비를 거쳐 바로 연다.
    public static void StartBusiness()
    {
        var business = GameInstance.Model.Business;
        if (business.State.Value == eBusinessState.Closed)
            ConfirmSettlement();

        if (business.OpenBusiness())
            GameInstance.UI.Close(eUIType.PopupSettlement);
    }
}
