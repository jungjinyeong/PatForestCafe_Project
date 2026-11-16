using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class WayPointManager : MonoBehaviour
{
    private readonly List<WaypointGroup> mWaypointGroups = new List<WaypointGroup>();
    // 테라스 좌석 예약: 좌석 웨이포인트 → 예약한 손님. 손님이 자리를 뜨거나 떠날 때 푼다.
    private readonly Dictionary<Waypoint, object> mSeatReservations = new Dictionary<Waypoint, object>();
    private readonly List<Waypoint> mSeatBuffer = new List<Waypoint>();

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

    // 손님 스폰 층(로비 1층 출입구): 매장층 중 가장 위층(1F). 층 Order는 아래→위로 커진다.
    public WaypointGroup GetLobbyEntranceGroup()
    {
        WaypointGroup entrance = null;
        foreach (var group in mWaypointGroups)
        {
            if (group == null || group.IsTerraceZone)
                continue;

            if (entrance == null || group.Order > entrance.Order)
                entrance = group;
        }
        return entrance;
    }

    // 손님 목표 층 선택용: 테라스가 아니고 언락된 매장층(B3~1F) 중 무작위 하나.
    public WaypointGroup GetRandomUnlockedStoreGroup()
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

    // 열린 테라스층에 놓인 좌석(Trigger_Seat) 중 비어 있는 자리 하나를 owner 이름으로 예약한다.
    // 실제로 배치된 좌석 수가 테라스 수용 인원이다 — 좌석이 없거나 모두 찼으면 false(손님은 테라스에 가지 않고 퇴장).
    public bool TryReserveTerraceSeat(object owner, out Waypoint seat, out WaypointGroup group)
    {
        seat = null;
        group = null;
        RemoveDestroyedSeatReservations();

        var candidates = new List<(Waypoint seat, WaypointGroup group)>();
        foreach (var terrace in mWaypointGroups)
        {
            if (terrace == null || !terrace.IsTerraceZone || !GameInstance.Model.Floor.IsUnlocked(terrace.Order))
                continue;

            mSeatBuffer.Clear();
            terrace.CollectTriggerWaypoints(Waypoint.eWaypointType.Trigger_Seat, mSeatBuffer);
            foreach (var wp in mSeatBuffer)
            {
                if (!mSeatReservations.ContainsKey(wp))
                    candidates.Add((wp, terrace));
            }
        }

        if (candidates.Count == 0)
            return false;

        var picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        mSeatReservations[picked.seat] = owner;
        seat = picked.seat;
        group = picked.group;
        return true;
    }

    // owner가 예약한 좌석일 때만 푼다(다른 손님이 이미 다시 예약한 자리는 건드리지 않음).
    public void ReleaseTerraceSeat(Waypoint seat, object owner)
    {
        if (seat == null)
        {
            RemoveDestroyedSeatReservations();
            return;
        }

        if (mSeatReservations.TryGetValue(seat, out var current) && ReferenceEquals(current, owner))
            mSeatReservations.Remove(seat);
    }

    // 철거된 의자의 좌석 예약을 지운다(Unity의 파괴된 오브젝트는 == null).
    private void RemoveDestroyedSeatReservations()
    {
        var destroyed = new List<Waypoint>();
        foreach (var seat in mSeatReservations.Keys)
        {
            if (seat == null)
                destroyed.Add(seat);
        }
        foreach (var seat in destroyed)
            mSeatReservations.Remove(seat);
    }
}
