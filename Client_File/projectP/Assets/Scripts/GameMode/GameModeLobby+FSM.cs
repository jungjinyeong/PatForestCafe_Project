
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

        mStateMachine.ChangeState(eGameModeLobbyState.OpenLobbyUI);
    }

    private void OnEnterOpenLobbyUI()
    {
        GameInstance.UI.Open<UIRootLobby, UIRootLobby.Param>(eUIType.UIRootLobby, new UIRootLobby.Param());
    }

    private void OnEnterWaitWaypointGroup()
    {
        // WayPointManager에 이미 등록된 WaypointGroup이 있을 수 있으므로 즉시 확인
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
