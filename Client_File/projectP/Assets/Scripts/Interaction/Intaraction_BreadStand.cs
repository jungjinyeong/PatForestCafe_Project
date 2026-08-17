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

        var go = GameInstance.Pool.Spawn(Intaraction_Bread.PoolName, mRootTransform.position, Quaternion.identity);
        if (go == null) return;

        go.transform.SetParent(mRootTransform);
        go.transform.localScale = Vector3.one;

        var bread = go.GetComponent<Intaraction_Bread>();
        if (bread != null)
        {
            bread.SetTableId(mTableId);
            mActiveBreadQueue.Enqueue(bread);
        }
    }
}
