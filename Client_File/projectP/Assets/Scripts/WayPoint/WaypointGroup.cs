using Sirenix.OdinInspector;
using UnityEngine;

public class WaypointGroup : MonoBehaviour
{
    [SerializeField] private int mOrder;
    [Header("Waypoints")]
    [SerializeField] private Waypoint[] mWaypoints;

    public int Order => mOrder;
    public Waypoint[] Waypoints => mWaypoints;

#if UNITY_EDITOR
    [Button("FindWaypoint")]
    private void FindWaypoint()
    {
        mWaypoints = GetComponentsInChildren<Waypoint>();
    }
#endif
}
