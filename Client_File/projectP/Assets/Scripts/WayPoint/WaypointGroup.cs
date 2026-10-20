using System.Collections.Generic;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;

public class WaypointGroup : MonoBehaviour
{
    // 층 ID = Floor.csv Tid(아래→위 1~7: B3 B2 B1 1F T1 T2 T3). 해금·세이브·카메라 층 이동이 이 값으로 층을 찾는다.
    [SerializeField] private int mOrder;
    [Header("Static Waypoints (SpwanPoint / Exit)")]
    [SerializeField] private Waypoint[] mStaticWaypoints;

    [Header("Zone Areas (가구 배치 영역, 이 존에 속한 Trigger 웨이포인트를 스캔할 범위)")]
    [SerializeField] private TilePlacementArea[] mZoneAreas;

    [Header("Option")]
    // 테라스층(T1~T3)이면 체크. 손님은 매장층 방문 후 열린 테라스 중 무작위 한 곳으로 간다.
    [SerializeField] private bool mIsTerraceZone = false;

    private readonly List<Waypoint> mDynamicTriggerWaypoints = new List<Waypoint>();
    private CompositeDisposable mDisposables;

    public int Order => mOrder;
    public Waypoint[] StaticWaypoints => mStaticWaypoints;
    public TilePlacementArea[] ZoneAreas => mZoneAreas;

    public bool IsTerraceZone => mIsTerraceZone;

    public void Init()
    {
        RescanDynamicWaypoints();

        mDisposables?.Dispose();
        mDisposables = new CompositeDisposable();

        GameInstance.Model.Placement.IsPlacing
            .Where(isPlacing => !isPlacing)
            .Subscribe(_ => RescanDynamicWaypoints())
            .AddTo(mDisposables);

        // 철거/대량 설치/리셋처럼 들고 있기(IsPlacing)를 거치지 않고 배치가 바뀌는 경우에도 손님 목표 후보를 다시 모은다.
        // 철거된 가구는 파괴 전에 비활성화되므로 같은 프레임의 재스캔(FindObjectsByType)에 걸리지 않는다.
        GameInstance.Model.Placement.PlacedLayoutChanged
            .Subscribe(_ => RescanDynamicWaypoints())
            .AddTo(mDisposables);
    }

    public void RescanDynamicWaypoints()
    {
        mDynamicTriggerWaypoints.Clear();

        if (mZoneAreas == null || mZoneAreas.Length == 0)
            return;

        foreach (var wp in FindObjectsByType<Waypoint>(FindObjectsSortMode.None))
        {
            if (wp == null || wp.GetCategoryType() != Waypoint.eWaypointCategoryType.Trigger)
                continue;

            if (IsInsideAnyZoneArea(wp.transform.position))
                mDynamicTriggerWaypoints.Add(wp);
        }
    }

    public Waypoint GetSpawnPoint()
    {
        if (mStaticWaypoints == null) return null;

        foreach (var wp in mStaticWaypoints)
        {
            if (wp != null && wp.GetCategoryType() == Waypoint.eWaypointCategoryType.SpwanPoint)
                return wp;
        }
        return null;
    }

    public Waypoint[] GetSpawnPoints()
    {
        var spawnPoints = new List<Waypoint>();
        if (mStaticWaypoints == null) return spawnPoints.ToArray();

        foreach (var wp in mStaticWaypoints)
        {
            if (wp != null && wp.GetCategoryType() == Waypoint.eWaypointCategoryType.SpwanPoint)
                spawnPoints.Add(wp);
        }
        return spawnPoints.ToArray();
    }

    // 자유 배회용 목표 탐색: SpwanPoint/Exit는 씬 고정(mStaticWaypoints)에서,
    // Trigger(가구/주문받는 NPC에 배치된 웨이포인트)는 런타임 동적 스캔 풀(mDynamicTriggerWaypoints)에서 찾는다.
    public bool TryGetRandomWaypoint(Waypoint.eWaypointCategoryType category, Waypoint.eWaypointType? specificType, out Waypoint result)
    {
        result = null;

        var pool = category == Waypoint.eWaypointCategoryType.Trigger ? mDynamicTriggerWaypoints : (IEnumerable<Waypoint>)mStaticWaypoints;
        if (pool == null) return false;

        var candidates = new List<Waypoint>();
        foreach (var wp in pool)
        {
            if (wp == null || wp.GetCategoryType() != category)
                continue;

            if (specificType.HasValue && wp.WaypointType != specificType.Value)
                continue;

            candidates.Add(wp);
        }

        if (candidates.Count == 0)
            return false;

        result = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    private bool IsInsideAnyZoneArea(Vector3 worldPos)
    {
        foreach (var area in mZoneAreas)
        {
            if (area != null && area.Contains(worldPos))
                return true;
        }
        return false;
    }

#if UNITY_EDITOR
    // Trigger 웨이포인트는 가구/주문받는 NPC 프리팹으로 이동했으므로, 이 버튼은 남은 고정 웨이포인트(SpwanPoint/Exit)만 수집한다.
    [Button("FindWaypoint")]
    private void FindWaypoint()
    {
        var found = new List<Waypoint>();
        foreach (var wp in GetComponentsInChildren<Waypoint>())
        {
            if (wp != null && wp.GetCategoryType() != Waypoint.eWaypointCategoryType.Trigger)
                found.Add(wp);
        }
        mStaticWaypoints = found.ToArray();
    }
#endif
}
