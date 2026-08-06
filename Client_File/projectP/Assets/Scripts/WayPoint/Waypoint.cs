using UnityEngine;
using Sirenix.OdinInspector;

public class Waypoint : MonoBehaviour
{
    public enum eWaypointCategoryType
    {
        Common,
        SpwanPoint,
        Exit,
        Trigger,
        Max,
    }

    public enum eWaypointType
    {
        Normal = eWaypointCategoryType.Common,

        SpwanPoint_Min = eWaypointCategoryType.SpwanPoint << 16,

        Exit_Min = eWaypointCategoryType.Exit << 16,

        Trigger_Order = eWaypointCategoryType.Trigger << 16,
        Trigger_Bread,

        Max = eWaypointCategoryType.Max << 16,
    }

    [Header("Option")]
    [SerializeField] private eWaypointType mWaypointType = eWaypointType.Normal;
    [SerializeField] private float mScale = 1f;

    [Header("Trigger Option")]
    [SerializeField] private int mTableId;

    [Header("Pathfinding")]
    [SerializeField] private Waypoint[] mNeighbors;

    public eWaypointType WaypointType => mWaypointType;
    public float Scale => mScale;
    public int TableId => mTableId;
    public Waypoint[] Neighbors => mNeighbors;

    public eWaypointCategoryType GetCategoryType()
    {
        return (eWaypointCategoryType)((int)mWaypointType >> 16);
    }

    #region Gizmos

    private void OnDrawGizmos()
    {
        Gizmos.color = GetGizmoColor();
        Gizmos.DrawWireSphere(transform.position, GetGizmoRadius());
        Gizmos.DrawSphere(transform.position, GetGizmoRadius() * 0.4f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GetGizmoRadius() * 1.2f);
    }

    private float GetGizmoRadius()
    {
        return mScale * 0.5f;
    }

    private Color GetGizmoColor()
    {
        switch (GetCategoryType())
        {
            case eWaypointCategoryType.Common:
                return Color.green;
            case eWaypointCategoryType.SpwanPoint:
                return Color.blue;
            case eWaypointCategoryType.Exit:
                return Color.red;
            case eWaypointCategoryType.Trigger:
                return Color.yellow;
            default:
                return Color.white;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        gameObject.name = "Waypoint_" + transform.GetSiblingIndex();
    }
#endif

    #endregion
}
