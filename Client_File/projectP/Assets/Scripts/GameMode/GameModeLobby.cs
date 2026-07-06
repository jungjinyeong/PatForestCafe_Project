
using System;
using System.Collections.Generic;

using UnityEngine;


public class GameModeLobby : GameModeBase
{
    // TODO : AreaOrder를 ScriptableObject로 만들어서 관리하도록 수정 필요
    // TODO : WaypointMgr로 코드 모두 옮길 예정
    [SerializeField] GameObject mAreaOrder;
    [SerializeField] GameObject mAreaBread;

    [SerializeField] GameObject[] npcPrefabs;

    [SerializeField] GameObject lobbyCharUIPrefab;

    public override void Init()
    {
        base.Init();

        var order = mAreaOrder.GetComponent<WaypointGroup>();
        var bread = mAreaBread.GetComponent<WaypointGroup>();
        var waypointGroups = new List<WaypointGroup> { order, bread };
        waypointGroups.Sort((a, b) => a.Order.CompareTo(b.Order));

        GameInstance.Spawn.SetInfo(lobbyCharUIPrefab, npcPrefabs, waypointGroups.ToArray());
        GameInstance.Spawn.StartAutoSpawn();
    }
}