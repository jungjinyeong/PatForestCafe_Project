using UnityEngine.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using Extension;

// 월드 탭으로 UIPopupDessertSelect를 여는 입력 경로(InputManager)를 지원하려면 BoxCollider2D가 필요하다.
[RequireComponent(typeof(BoxCollider2D))]
public class Intaraction_DessertStand : MonoBehaviour
{
    // 진열된 빵을 mActiveDessertQueue 순번 기준으로 테이블 1칸 안에 격자 배치한다(러프 버전 — 픽업으로 앞쪽이 비면 뒤쪽이 당겨지진 않음).
    // 빵 1개 폭이 약 0.5라 3열 × 0.25 간격이면 칸 폭(1)에 들어간다. 3줄을 넘으면 다시 첫 줄부터 위에 겹쳐 쌓는다.
    private const float DessertSpawnSpacingX = 0.25f;
    private const float DessertSpawnSpacingY = 0.15f;
    private const int DessertSpawnPerRow = 3;
    private const int DessertSpawnRows = 3;

    [Header("ID")]
    [SerializeField] private int mTableId;

    [Header("Dessert")]
    [SerializeField] private Transform mRootTransform;
    [SerializeField, FormerlySerializedAs("mBreadPrefab")] private GameObject mDessertPrefab;

    private readonly Queue<Intaraction_Dessert> mActiveDessertQueue = new Queue<Intaraction_Dessert>();
    private PlaceableObject mPlaceable;
    private Waypoint mDessertTriggerWaypoint;

    public int TableId => mTableId;
    public bool IsAssigned => mTableId != 0;
    // 이 진열대에 실제로 놓인 디저트 수. 손님은 이 값이 1 이상인 진열대만 고른다.
    public int DisplayedCount => mActiveDessertQueue.Count;

    public void Init()
    {
        if (mDessertPrefab != null)
            GameInstance.Pool?.RegisterPool(Intaraction_Dessert.PoolName, mDessertPrefab);

        // 가구(Table.prefab)로 배치되는 진열대는 PlaceableObject와 같은 GameObject에 붙어 시작 시 mTableId==0(미지정)이다.
        // TryAssignDessertType()이 최초 배치 시 발급된 PlacementId를 PlacementModel에 기록해야 해서 필요하다.
        mPlaceable = GetComponent<PlaceableObject>();

        // 이 진열대 밑의 Trigger_Dessert 웨이포인트(CharNpc가 진열대를 찾고 재고를 확인하는 TableId)를
        // 찾아둔다 — mTableId가 나중에 배정/복원될 때 이 웨이포인트에도 같이 반영해야 NPC가 올바른 재고를 본다.
        mDessertTriggerWaypoint = FindDessertTriggerWaypoint();

        // mTableId==0(미지정)이면 아직 어떤 빵도 취급하지 않으므로 등록할 대상이 없다 — TryAssignDessertType()에서 등록한다.
        if (mTableId != 0)
        {
            GameInstance.Model.Dessert.Register(mTableId);
            mDessertTriggerWaypoint?.SetTableId(mTableId);
        }

    }

    private Waypoint FindDessertTriggerWaypoint()
    {
        var waypoints = GetComponentsInChildren<Waypoint>(true);
        foreach (var waypoint in waypoints)
        {
            if (waypoint.WaypointType == Waypoint.eWaypointType.Trigger_Dessert)
                return waypoint;
        }
        return null;
    }

    // 빈 테이블(mTableId==0)에 처음 넣는 빵 종류로 고정한다. 이미 다른 종류로 고정된 테이블이면 실패(false)를 반환해
    // 호출부(UIPopupDessertSelect)가 "다른 종류는 추가 불가"를 그대로 지킬 수 있게 한다. 이미 같은 종류면 그대로 성공 처리.
    public bool TryAssignDessertType(int dessertTid)
    {
        if (mTableId == dessertTid)
            return true;

        if (mTableId != 0)
            return false;

        mTableId = dessertTid;
        GameInstance.Model.Dessert.Register(mTableId);
        mDessertTriggerWaypoint?.SetTableId(mTableId);

        if (mPlaceable != null && mPlaceable.PlacementId >= 0)
            GameInstance.Model.Placement.SetAssignedDessertTid(mPlaceable.PlacementId, dessertTid);

        return true;
    }

    // 세이브 복원 전용 — PlacementModel에 이미 기록된 값을 그대로 반영만 한다(다시 기록할 필요 없음).
    public void RestoreAssignedDessertType(int dessertTid)
    {
        if (dessertTid == 0 || mTableId != 0)
            return;

        mTableId = dessertTid;
        GameInstance.Model.Dessert.Register(mTableId);
        mDessertTriggerWaypoint?.SetTableId(mTableId);
    }

    // 세이브 로드로 Count(진열 수량)만 복원되면 mActiveDessertQueue(실제 진열된 빵 오브젝트)가 비어 있어
    // 수치와 실물이 어긋난다 — GameModeLobby+FSM.InitDessertStands()가 SaveManager.ApplyPendingDessertData() 직후 호출해
    // 저장된 개수만큼 빵 오브젝트를 다시 채워 넣는다. Count 자체는 이미 세이브 값으로 설정돼 있으므로 SpawnDessert()만
    // 반복 호출한다(Add()를 쓰면 Count가 중복 증가함). 품질별 진열 수량대로 높은 품질부터 채운다.
    public void SyncDisplayToSavedCount()
    {
        var dessert = GameInstance.Model.Dessert.Get(mTableId);
        if (dessert == null) return;

        foreach (var quality in DessertQuality.HIGH_TO_LOW)
        {
            int count = dessert.GetCount(quality);
            for (int i = 0; i < count; i++)
                SpawnDessert(quality);
        }
    }

    // 월드 오브젝트 탭(InputManager → UIPopupDessertSelect 확정)에서만 호출된다.
    // 오븐(OvenModel)에서 구워 창고에 보관된 생산 재고를 높은 품질부터 소비해야만 실제로 진열된다 — 재고가 없으면 아무 일도 일어나지 않는다.
    public void AddDessert()
    {
        if (mTableId == 0)
        {
            Logger.Log("[Intaraction_DessertStand] 아직 빵 종류가 지정되지 않은 테이블입니다.");
            return;
        }

        if (!GameInstance.Model.Dessert.TryConsumeProducedBest(mTableId, out var quality))
        {
            Logger.Log($"[Intaraction_DessertStand] 생산된 빵 재고가 없습니다. Tid={mTableId}");
            return;
        }

        SpawnDessert(quality);
        GameInstance.Model.Dessert.Add(mTableId, quality);
    }

    // 손님이 이 진열대에서 count개를 집어 간다. 실제로 가져가는 수 = min(count, 이 진열대에 놓인 개수).
    // 집은 디저트는 진열 수량에서 빼고 진열 오브젝트는 풀로 돌려보낸다(손님 머리 위에는 아이콘+수량만 보인다).
    // 반환: 집어 간 디저트의 품질 목록(결제 때 품질별 판매가 배율에 쓴다). 비어 있으면 아무것도 못 집은 것.
    public List<eDessertQuality> TakeDesserts(int count)
    {
        var taken = new List<eDessertQuality>();
        if (mTableId == 0)
            return taken;

        while (taken.Count < count && mActiveDessertQueue.Count > 0)
        {
            var dessert = mActiveDessertQueue.Dequeue();
            if (dessert == null)
                continue;

            GameInstance.Model.Dessert.Consume(mTableId, dessert.Quality);
            taken.Add(dessert.Quality);

            if (GameInstance.Pool != null)
                dessert.transform.SetParent(GameInstance.Pool.transform, true);
            dessert.Despawn();
        }

        return taken;
    }

    // 결제를 포기한 손님이 집었던 디저트를 되돌려 놓는다 — 진열 수량을 다시 늘리고 진열 오브젝트도 다시 만든다.
    public void ReturnDesserts(IReadOnlyList<eDessertQuality> qualities)
    {
        if (mTableId == 0 || qualities == null)
            return;

        foreach (var quality in qualities)
        {
            GameInstance.Model.Dessert.Add(mTableId, quality);
            SpawnDessert(quality);
        }
    }

    // 진열대가 철거/리셋으로 사라지기 직전에 호출한다. 진열된 빵을 진열 수량에서 빼 같은 품질 그대로 생산 재고(창고)로 되돌리고,
    // 빵 오브젝트는 풀로 반납한다 — 진열대의 자식으로 붙어 있어 그대로 두면 진열대와 함께 파괴돼 풀이 죽은 오브젝트를 쥐게 된다.
    public void ReturnDisplayedDessertToStorage()
    {
        while (mActiveDessertQueue.Count > 0)
        {
            var dessert = mActiveDessertQueue.Dequeue();
            if (dessert == null)
                continue;

            // 진열 수량은 같은 빵 종류의 진열대끼리 공유하고 오브젝트 수와 어긋날 수 있으므로, 실제로 진열 수량에서 뺀 만큼만 창고로 돌린다.
            var dessertData = mTableId != 0 ? GameInstance.Model.Dessert.Get(mTableId) : null;
            if (dessertData != null && dessertData.GetCount(dessert.Quality) > 0)
            {
                GameInstance.Model.Dessert.Consume(mTableId, dessert.Quality);
                GameInstance.Model.Dessert.AddProduced(mTableId, dessert.Quality);
            }

            if (GameInstance.Pool != null)
                dessert.transform.SetParent(GameInstance.Pool.transform, true);
            dessert.Despawn();
        }
    }

    private void SpawnDessert(eDessertQuality quality)
    {
        if (GameInstance.Pool == null || mRootTransform == null) return;

        int index = mActiveDessertQueue.Count;
        int col = index % DessertSpawnPerRow;
        int row = index / DessertSpawnPerRow % DessertSpawnRows;
        Vector3 offset = new Vector3((col - (DessertSpawnPerRow - 1) * 0.5f) * DessertSpawnSpacingX, (row - (DessertSpawnRows - 1) * 0.5f) * DessertSpawnSpacingY, 0f);

        var go = GameInstance.Pool.Spawn(Intaraction_Dessert.PoolName, mRootTransform.position + offset, Quaternion.identity);
        if (go == null) return;

        // 부모의 스케일과 무관하게 프리팹이 authoring한 월드 스케일을 그대로 유지한다(SetParent(worldPositionStays: true) 기본 동작).
        go.transform.SetParent(mRootTransform);

        var dessert = go.GetComponent<Intaraction_Dessert>();
        if (dessert != null)
        {
            dessert.SetTableId(mTableId);
            dessert.SetQuality(quality);
            mActiveDessertQueue.Enqueue(dessert);
        }
    }
}
