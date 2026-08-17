
using System;
using System.Collections.Generic;

using UnityEngine;
using UniRx;


public partial class GameModeLobby
{
    void RegisterStates()
    {
        mStateMachine.RegisterState(eGameModeLobbyState.Init, OnEnterInit);
        mStateMachine.RegisterState(eGameModeLobbyState.OpenLobbyUI, OnEnterOpenLobbyUI);
        mStateMachine.RegisterState(eGameModeLobbyState.WaitWaypointGroup, OnEnterWaitWaypointGroup, OnExitWaitWaypointGroup);
        mStateMachine.RegisterState(eGameModeLobbyState.Spawn, OnEnterSpawn);

        mStateMachine.ChangeState(eGameModeLobbyState.Init);
    }

    private void OnEnterInit()
    {
        mWaypointGroupDisposable = MessageBroker.Default.Receive<CEvent.WaypointGroupRegist>()
            .Subscribe(OnWaypointGroupRegist)
            .AddTo(this);

        InitBreadStands();

        mStateMachine.ChangeState(eGameModeLobbyState.OpenLobbyUI);
    }

    private void InitBreadStands()
    {
        var breadStands = FindObjectsByType<Intaraction_BreadStand>(FindObjectsSortMode.None);

        foreach (var breadStand in breadStands)
            breadStand.Init();

        GameInstance.Save.ApplyPendingBreadData();

        // Count(진열 수량)를 세이브 값으로 되돌린 뒤에야 실제 빵 오브젝트 개수를 맞출 수 있으므로 순서 중요.
        foreach (var breadStand in breadStands)
            breadStand.SyncDisplayToSavedCount();
    }

    private void OnEnterOpenLobbyUI()
    {
        GameInstance.UI.Open<UIRootLobby, UIRootLobby.Param>(eUIType.UIRootLobby, new UIRootLobby.Param());

        GameInstance.UI.HudController.Init();

        ShowPendingOfflineIncomeIfAny();
    }

    private void ShowPendingOfflineIncomeIfAny()
    {
        if (!GameInstance.Save.TryConsumePendingOfflineIncome(out int gold, out double offlineSeconds))
            return;

        GameInstance.UI.Open<UIPopupOfflineIncome, UIPopupOfflineIncome.Param>(eUIType.UIPopupOfflineIncome,
            new UIPopupOfflineIncome.Param { Gold = gold, OfflineSeconds = offlineSeconds });
    }

    private void OnEnterWaitWaypointGroup()
    {
        // WayPointManager�� �̹� ��ϵ� WaypointGroup�� ���� �� �����Ƿ� ��� Ȯ��
        CheckWaypointGroupsReady(GameInstance.WayPoint.WaypointGroups);
    }

    private void OnExitWaitWaypointGroup()
    {
        mWaypointGroupDisposable?.Dispose();
        mWaypointGroupDisposable = null;
    }

    private void OnWaypointGroupRegist(CEvent.WaypointGroupRegist e)
    {
        CheckWaypointGroupsReady(e.waypointGroups);
    }

    private void CheckWaypointGroupsReady(IReadOnlyList<WaypointGroup> waypointGroups)
    {
        if (waypointGroups == null || waypointGroups.Count <= 0)
            return;

        GameInstance.Spawn.SetInfo(mLobbyCharUIPrefab, mNpcPrefabs);

        mStateMachine.ChangeState(eGameModeLobbyState.Spawn);
    }

    private void OnEnterSpawn()
    {
        GameInstance.Spawn.StartAutoSpawn();
    }
}
