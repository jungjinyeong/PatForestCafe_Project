using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class WaypointGroup : MonoBehaviour
{
    [SerializeField] private int mOrder;
    [Header("Waypoints")]
    [SerializeField] private Waypoint[] mWaypoints;

    public int Order => mOrder;
    public Waypoint[] Waypoints => mWaypoints;

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

    public Waypoint[] GetPathWaypoints()
    {
        if (mWaypoints == null) return null;

        var path = new List<Waypoint>();
        foreach (var wp in mWaypoints)
        {
            if (wp == null || wp.GetCategoryType() == Waypoint.eWaypointCategoryType.SpwanPoint)
                continue;

            path.Add(wp);
        }
        return path.ToArray();
    }

#if UNITY_EDITOR
    [Button("FindWaypoint")]
    private void FindWaypoint()
    {
        mWaypoints = GetComponentsInChildren<Waypoint>();
    }
#endif
}
