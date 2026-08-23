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

    [Header("UI")]
    [SerializeField] private UIButtonEx mBtnAddBread;

    [Header("Bread")]
    [SerializeField] private Transform mRootTransform;
    [SerializeField] private GameObject mBreadPrefab;

    private readonly Queue<Intaraction_Bread> mActiveBreadQueue = new Queue<Intaraction_Bread>();

    public int TableId => mTableId;

    public void Init()
    {
        if (mBreadPrefab != null)
            GameInstance.Pool?.RegisterPool(Intaraction_Bread.PoolName, mBreadPrefab);

        GameInstance.Model.Bread.Register(mTableId);

        // mBtnAddBread는 UI 버튼(Canvas 필요)이라 월드 스페이스로 배치된 인스턴스는 비워둘 수 있음 — 디버그용 진열 버튼일 뿐, 실제 진열은 AddBread()가 다른 경로(빵 공장 등)에서도 호출됨.
        if (mBtnAddBread != null)
            mBtnAddBread.OnSubscribeOnClick(AddBread).AddTo(this);

        MessageBroker.Default
            .Receive<CEvent.BreadPickup>()
            .Where(e => e.tableId == mTableId)
            .Subscribe(e => HandleBreadPickupAsync(e.breadPickup).Forget())
            .AddTo(this);
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

    // mBtnAddBread(진열 버튼)와 UIPopupBreadSelect(월드 탭 → 빵 선택 팝업 확정) 양쪽에서 공용으로 쓴다.
    // 빵 공장(UIPopupBreadProduction)에서 만든 생산 재고를 소비해야만 실제로 진열된다 — 재고가 없으면 아무 일도 일어나지 않는다.
    public void AddBread()
    {
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
