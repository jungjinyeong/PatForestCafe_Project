
using System;
using System.Collections.Generic;

using UnityEngine;
using UniRx;


public partial class GameModeLobby : GameModeBase
{
    private enum eGameModeLobbyState
    {
        Init,

        //준비가 필요한 상태들 - 순차 실행
        OpenLobbyUI,
        WaitWaypointGroup,

        //TODO: 매장 준비
        
        // 매장 시작
        Spawn,
    }

    [SerializeField] private GameObject[] mNpcPrefabs;

    [SerializeField] private GameObject mLobbyCharUIPrefab;

    private readonly StateMachine<eGameModeLobbyState> mStateMachine = new StateMachine<eGameModeLobbyState>();

    private IDisposable mWaypointGroupDisposable;

    public override void Init()
    {
        base.Init();

        RegisterStates();
    }
}
