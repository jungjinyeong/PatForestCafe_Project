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

    private int mSpecialOrderOccupantCount = 0;

    public int Order => mOrder;
    public Waypoint[] Waypoints => mWaypoints;

    public bool IsSpecialOrderZone => mIsSpecialOrderZone;

    public bool TryEnterSpecialOrderSlot()
    {
        int maxSpecialOrderNpc = GameInstance.Config.GetValue(eConfigType.MaxSpecialOrderNpc);
        if (mSpecialOrderOccupantCount >= maxSpecialOrderNpc)
            return false;

        mSpecialOrderOccupantCount++;
        return true;
    }

    public void ExitSpecialOrderSlot()
    {
        mSpecialOrderOccupantCount = Mathf.Max(0, mSpecialOrderOccupantCount - 1);
    }

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
