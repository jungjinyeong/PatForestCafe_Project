using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class WayPointManager : MonoBehaviour
{
    private readonly List<WaypointGroup> mWaypointGroups = new List<WaypointGroup>();

    public IReadOnlyList<WaypointGroup> WaypointGroups => mWaypointGroups;

    public void Init()
    {
        mWaypointGroups.Clear();
    }

    public void RegisterWaypointGroup(WaypointGroup group)
    {
        if (group == null || mWaypointGroups.Contains(group))
            return;

        mWaypointGroups.Add(group);
        mWaypointGroups.Sort((a, b) => a.Order.CompareTo(b.Order));

        MessageBroker.Default.Publish(new CEvent.WaypointGroupRegist(mWaypointGroups.ToArray()));
    }

    // 손님 스폰 대상 선택용: 테라스가 아니고 언락된 매장층(B3~1F) 중 무작위 하나.
    public WaypointGroup GetRandomUnlockedFloorGroup()
    {
        var candidates = new List<WaypointGroup>();
        foreach (var group in mWaypointGroups)
        {
            if (group == null || group.IsTerraceZone)
                continue;

            if (GameInstance.Model.Floor.IsUnlocked(group.Order))
                candidates.Add(group);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    // 손님이 매장층 방문을 마친 뒤 향하는 목적지: 열린 테라스층(T1~T3) 중 무작위 하나.
    // 열린 테라스가 없으면 null(그 자리에서 퇴장 처리).
    public WaypointGroup GetTerraceGroup()
    {
        var candidates = new List<WaypointGroup>();
        foreach (var group in mWaypointGroups)
        {
            if (group != null && group.IsTerraceZone && GameInstance.Model.Floor.IsUnlocked(group.Order))
                candidates.Add(group);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }
}
