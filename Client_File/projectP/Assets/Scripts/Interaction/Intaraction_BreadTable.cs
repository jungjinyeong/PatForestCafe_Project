using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Extension;

public class Intaraction_BreadTable : MonoBehaviour
{
    [Header("ID")]
    [SerializeField] private int mTableId;

    [Header("UI")]
    [SerializeField] private UIButtonEx mBtnAddBread;

    [Header("Bread")]
    [SerializeField] private Transform mRootTransform;
    [SerializeField] private GameObject mBreadPrefab;

    private readonly Queue<Intaraction_Bread> mActiveBreadQueue = new Queue<Intaraction_Bread>();

    public void Init()
    {
        if (mBreadPrefab != null)
            GameInstance.Pool?.RegisterPool(Intaraction_Bread.PoolName, mBreadPrefab);

        GameInstance.Model.Bread.Register(mTableId);

        mBtnAddBread.OnSubscribeOnClick(OnClickAddBread).AddTo(this);

        MessageBroker.Default
            .Receive<CEvent.BreadPickup>()
            .Where(e => e.tableId == mTableId)
            .Subscribe(e => HandleBreadPickupAsync(e.breadPickup).Forget())
            .AddTo(this);
    }

    private void OnClickAddBread()
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
