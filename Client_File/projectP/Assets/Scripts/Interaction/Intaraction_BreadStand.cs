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

    // mBtnAddBread(빠른 재고 채우기 버튼)와 UIPopupBreadSelect(월드 탭 → 빵 선택 팝업 확정) 양쪽에서 공용으로 쓴다.
    public void AddBread()
    {
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
