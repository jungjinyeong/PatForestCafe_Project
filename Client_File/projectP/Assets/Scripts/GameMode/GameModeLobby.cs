
using System;
using System.Collections.Generic;

using UnityEngine;
using UniRx;


public class GameModeLobby : GameModeBase
{
    private enum eGameModeLobbyState
    {
        Init,
        WaitWaypointGroup,
        Spawn,
    }

    [SerializeField] private GameObject[] mNpcPrefabs;

    [SerializeField] private GameObject mLobbyCharUIPrefab;

    [SerializeField] private int mWaitWaypointGroupCount = 2;

    private readonly StateMachine<eGameModeLobbyState> mStateMachine = new StateMachine<eGameModeLobbyState>();

    private IDisposable mWaypointGroupDisposable;

    public override void Init()
    {
        base.Init();

        GameInstance.UI.Open<UIRootLobby, UIRootLobby.Param>(eUIType.UIRootLobby, new UIRootLobby.Param());

        mStateMachine.RegisterState(eGameModeLobbyState.WaitWaypointGroup, OnEnterWaitWaypointGroup, OnExitWaitWaypointGroup);
        mStateMachine.RegisterState(eGameModeLobbyState.Spawn, OnEnterSpawn);

        mStateMachine.ChangeState(eGameModeLobbyState.WaitWaypointGroup);
    }

    private void OnEnterWaitWaypointGroup()
    {
        mWaypointGroupDisposable = MessageBroker.Default.Receive<CEvent.WaypointGroupRegist>()
            .Subscribe(OnWaypointGroupRegist)
            .AddTo(this);

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
        if (waypointGroups == null || waypointGroups.Count < mWaitWaypointGroupCount)
            return;

        var sortedGroups = new List<WaypointGroup>(waypointGroups);
        sortedGroups.Sort((a, b) => a.Order.CompareTo(b.Order));

        GameInstance.Spawn.SetInfo(mLobbyCharUIPrefab, mNpcPrefabs, sortedGroups.ToArray());

        mStateMachine.ChangeState(eGameModeLobbyState.Spawn);
    }

    private void OnEnterSpawn()
    {
        GameInstance.Spawn.StartAutoSpawn();
    }
}
