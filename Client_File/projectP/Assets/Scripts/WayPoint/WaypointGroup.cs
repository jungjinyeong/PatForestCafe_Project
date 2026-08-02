using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// TODO
// npc들이 겹치는 범위도 설정하면 좋을 것 같음.

public class WaypointGroup : MonoBehaviour
{
    [SerializeField] private int mOrder;
    [Header("Waypoints")]
    [SerializeField] private Waypoint[] mWaypoints;

    [Header("Option")]
    [SerializeField] private bool mIsSpecialOrderZone = false;
    [SerializeField] private bool mIsBreadFreeRoamZone = false;

    public int Order => mOrder;
    public Waypoint[] Waypoints => mWaypoints;

    public bool IsSpecialOrderZone => mIsSpecialOrderZone;
    public bool IsBreadFreeRoamZone => mIsBreadFreeRoamZone;

    public Waypoint GetSpawnPoint()
    {
        if (mWaypoints == null) return null;

        foreach (var wp in mWaypoints)
        {
            if (wp != null && wp.GetCategoryType() == Waypoint.eWaypointCategoryType.SpwanPoint)
                return wp;
        }
        return null;
    }

    // 빵 자유 배회 존은 여러 Trigger_Bread 중 하나를 랜덤으로만 방문하고 바로 Exit로 향하도록
    // 경로를 줄여서 반환한다. (일반 존은 모든 경로 웨이포인트를 순서대로 반환)
    public Waypoint[] GetPathWaypoints()
    {
        if (mWaypoints == null) return null;

        if (mIsBreadFreeRoamZone)
            return GetBreadFreeRoamPath();

        var path = new List<Waypoint>();
        foreach (var wp in mWaypoints)
        {
            if (wp == null || wp.GetCategoryType() == Waypoint.eWaypointCategoryType.SpwanPoint)
                continue;

            path.Add(wp);
        }
        return path.ToArray();
    }

    private Waypoint[] GetBreadFreeRoamPath()
    {
        var breadTriggers = new List<Waypoint>();
        Waypoint exitWaypoint = null;

        foreach (var wp in mWaypoints)
        {
            if (wp == null) continue;

            if (wp.WaypointType == Waypoint.eWaypointType.Trigger_Bread)
                breadTriggers.Add(wp);
            else if (exitWaypoint == null && wp.GetCategoryType() == Waypoint.eWaypointCategoryType.Exit)
                exitWaypoint = wp;
        }

        if (breadTriggers.Count == 0)
            return exitWaypoint != null ? new[] { exitWaypoint } : new Waypoint[0];

        var chosen = breadTriggers[UnityEngine.Random.Range(0, breadTriggers.Count)];

        return exitWaypoint != null ? new[] { chosen, exitWaypoint } : new[] { chosen };
    }

#if UNITY_EDITOR
    [Button("FindWaypoint")]
    private void FindWaypoint()
    {
        mWaypoints = GetComponentsInChildren<Waypoint>();
    }
#endif
}
