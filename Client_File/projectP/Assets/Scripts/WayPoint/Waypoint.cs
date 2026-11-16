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
        // 통합계단실 — 손님이 층을 옮길 때 들어가고(현재 층) 나오는(목표 층) 지점. 층에 없으면 SpawnPoint로 대신한다.
        Exit_Stairwell,

        // 씬·프리팹에 숫자로 저장되므로 새 값은 항상 맨 뒤에 붙인다.
        Trigger_Order = eWaypointCategoryType.Trigger << 16,
        Trigger_Dessert,
        Trigger_Kiosk,
        // 테라스 좌석(의자 가구) — 손님이 결제 때 예약하고 앉는 자리. 테라스층에 놓인 수 = 테라스 수용 인원.
        Trigger_Seat,

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

    // Intaraction_DessertStand가 배치 후 빵 종류를 배정/복원할 때 자기 트리거 웨이포인트에 동기화하기 위해 쓴다.
    public void SetTableId(int tableId)
    {
        mTableId = tableId;
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
