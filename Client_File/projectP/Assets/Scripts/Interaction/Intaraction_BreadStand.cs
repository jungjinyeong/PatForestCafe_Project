using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Extension;

// 월드 탭으로 UIPopupBreadSelect를 여는 입력 경로(InputManager)를 지원하려면 BoxCollider2D가 필요하다.
[RequireComponent(typeof(BoxCollider2D))]
public class Intaraction_BreadStand : MonoBehaviour
{
    // 진열된 빵끼리 겹치지 않도록 mActiveBreadQueue 순번 기준으로 격자 배치한다(러프 버전 — 픽업으로 앞쪽이 비면 뒤쪽이 당겨지진 않음).
    private const float BreadSpawnSpacing = 0.35f;
    private const int BreadSpawnPerRow = 4;

    [Header("ID")]
    [SerializeField] private int mTableId;

    [Header("Bread")]
    [SerializeField] private Transform mRootTransform;
    [SerializeField] private GameObject mBreadPrefab;

    private readonly Queue<Intaraction_Bread> mActiveBreadQueue = new Queue<Intaraction_Bread>();
    private PlaceableObject mPlaceable;
    private Waypoint mBreadTriggerWaypoint;

    public int TableId => mTableId;
    public bool IsAssigned => mTableId != 0;

    public void Init()
    {
        if (mBreadPrefab != null)
            GameInstance.Pool?.RegisterPool(Intaraction_Bread.PoolName, mBreadPrefab);

        // 가구(Table.prefab)로 배치되는 진열대는 PlaceableObject와 같은 GameObject에 붙어 시작 시 mTableId==0(미지정)이다.
        // TryAssignBreadType()이 최초 배치 시 발급된 PlacementId를 PlacementModel에 기록해야 해서 필요하다.
        mPlaceable = GetComponent<PlaceableObject>();

        // 이 진열대 밑의 Trigger_Bread 웨이포인트(CharNpc가 재고 확인/CEvent.BreadPickup 발행에 쓰는 TableId)를
        // 찾아둔다 — mTableId가 나중에 배정/복원될 때 이 웨이포인트에도 같이 반영해야 NPC가 올바른 재고를 본다.
        mBreadTriggerWaypoint = FindBreadTriggerWaypoint();

        // mTableId==0(미지정)이면 아직 어떤 빵도 취급하지 않으므로 등록할 대상이 없다 — TryAssignBreadType()에서 등록한다.
        if (mTableId != 0)
        {
            GameInstance.Model.Bread.Register(mTableId);
            mBreadTriggerWaypoint?.SetTableId(mTableId);
        }

        MessageBroker.Default
            .Receive<CEvent.BreadPickup>()
            .Where(e => e.tableId == mTableId)
            .Subscribe(e => HandleBreadPickupAsync(e.breadPickup).Forget())
            .AddTo(this);
    }

    private Waypoint FindBreadTriggerWaypoint()
    {
        var waypoints = GetComponentsInChildren<Waypoint>(true);
        foreach (var waypoint in waypoints)
        {
            if (waypoint.WaypointType == Waypoint.eWaypointType.Trigger_Bread)
                return waypoint;
        }
        return null;
    }

    // 빈 테이블(mTableId==0)에 처음 넣는 빵 종류로 고정한다. 이미 다른 종류로 고정된 테이블이면 실패(false)를 반환해
    // 호출부(UIPopupBreadSelect)가 "다른 종류는 추가 불가"를 그대로 지킬 수 있게 한다. 이미 같은 종류면 그대로 성공 처리.
    public bool TryAssignBreadType(int breadTid)
    {
        if (mTableId == breadTid)
            return true;

        if (mTableId != 0)
            return false;

        mTableId = breadTid;
        GameInstance.Model.Bread.Register(mTableId);
        mBreadTriggerWaypoint?.SetTableId(mTableId);

        if (mPlaceable != null && mPlaceable.PlacementId >= 0)
            GameInstance.Model.Placement.SetAssignedBreadTid(mPlaceable.PlacementId, breadTid);

        return true;
    }

    // 세이브 복원 전용 — PlacementModel에 이미 기록된 값을 그대로 반영만 한다(다시 기록할 필요 없음).
    public void RestoreAssignedBreadType(int breadTid)
    {
        if (breadTid == 0 || mTableId != 0)
            return;

        mTableId = breadTid;
        GameInstance.Model.Bread.Register(mTableId);
        mBreadTriggerWaypoint?.SetTableId(mTableId);
    }

    // 세이브 로드로 Count(진열 수량)만 복원되면 mActiveBreadQueue(실제 진열된 빵 오브젝트)가 비어 있어
    // 수치와 실물이 어긋난다 — GameModeLobby+FSM.InitBreadStands()가 SaveManager.ApplyPendingBreadData() 직후 호출해
    // 저장된 개수만큼 빵 오브젝트를 다시 채워 넣는다. Count 자체는 이미 세이브 값으로 설정돼 있으므로 SpawnBread()만
    // 반복 호출한다(Add()를 쓰면 Count가 중복 증가함).
    public void SyncDisplayToSavedCount()
    {
        int count = GameInstance.Model.Bread.GetCount(mTableId)?.Value ?? 0;
        for (int i = 0; i < count; i++)
            SpawnBread();
    }

    // 월드 오브젝트 탭(InputManager → UIPopupBreadSelect 확정)에서만 호출된다.
    // 빵 공장(UIPopupBreadProduction)에서 만든 생산 재고를 소비해야만 실제로 진열된다 — 재고가 없으면 아무 일도 일어나지 않는다.
    public void AddBread()
    {
        if (mTableId == 0)
        {
            Logger.Log("[Intaraction_BreadStand] 아직 빵 종류가 지정되지 않은 테이블입니다.");
            return;
        }

        if (!GameInstance.Model.Bread.TryConsumeProduced(mTableId))
        {
            Logger.Log($"[Intaraction_BreadStand] 생산된 빵 재고가 없습니다. Tid={mTableId}");
            return;
        }

        SpawnBread();
        GameInstance.Model.Bread.Add(mTableId);
    }

    private async UniTaskVoid HandleBreadPickupAsync(IBreadPickup breadPickup)
    {
        if (mActiveBreadQueue.Count == 0) return;

        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: this.GetCancellationTokenOnDestroy());

        if (mActiveBreadQueue.Count == 0) return;

        if(breadPickup == null) return;

        var lobbyUI = breadPickup.GetLobbyCharUI;
        if (lobbyUI == null || !lobbyUI.CanAttachBread()) return;

        var bread = mActiveBreadQueue.Dequeue();
        if (bread == null) return;

        lobbyUI.AttachBread(bread);

        GameInstance.Model.Bread.Consume(mTableId);
    }

    private void SpawnBread()
    {
        if (GameInstance.Pool == null || mRootTransform == null) return;

        int index = mActiveBreadQueue.Count;
        int col = index % BreadSpawnPerRow;
        int row = index / BreadSpawnPerRow;
        Vector3 offset = new Vector3((col - (BreadSpawnPerRow - 1) * 0.5f) * BreadSpawnSpacing, row * BreadSpawnSpacing, 0f);

        var go = GameInstance.Pool.Spawn(Intaraction_Bread.PoolName, mRootTransform.position + offset, Quaternion.identity);
        if (go == null) return;

        // 부모의 스케일과 무관하게 프리팹이 authoring한 월드 스케일을 그대로 유지한다(SetParent(worldPositionStays: true) 기본 동작).
        go.transform.SetParent(mRootTransform);

        var bread = go.GetComponent<Intaraction_Bread>();
        if (bread != null)
        {
            bread.SetTableId(mTableId);
            mActiveBreadQueue.Enqueue(bread);
        }
    }
}
