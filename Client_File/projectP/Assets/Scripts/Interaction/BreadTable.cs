using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Extension;

public class BreadTable : MonoBehaviour
{
    [Header("ID")]
    [SerializeField] private int mTableId;

    [Header("UI")]
    [SerializeField] private UIButtonEx mBtnAddBread;

    [Header("Bread")]
    [SerializeField] private Transform mRootTransform;
    [SerializeField] private GameObject mBreadPrefab;

    private readonly Queue<Bread> mActiveBreadQueue = new Queue<Bread>();

    private void Start()
    {
        if (mBreadPrefab != null)
            GameInstance.Pool?.RegisterPool(Bread.PoolName, mBreadPrefab);

        GameInstance.Model.Bread.Register(mTableId);

        mBtnAddBread.OnSubscribeOnClick(OnClickAddBread).AddTo(this);

        MessageBroker.Default
            .Receive<CEvent.BreadPickup>()
            .Where(e => e.tableId == mTableId)
            .Subscribe(e => HandleBreadPickupAsync(e.npc).Forget())
            .AddTo(this);
    }

    private void OnClickAddBread()
    {
        SpawnBread();
        GameInstance.Model.Bread.Add(mTableId);
    }

    private async UniTaskVoid HandleBreadPickupAsync(WaypointNPC npc)
    {
        if (mActiveBreadQueue.Count == 0) return;

        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: this.GetCancellationTokenOnDestroy());

        if (mActiveBreadQueue.Count == 0) return;

        var bread = mActiveBreadQueue.Dequeue();
        if (bread == null) return;

        var lobbyUI = npc.GetComponentInChildren<LobbyCharUI>();
        lobbyUI?.AttachBread(bread);

        GameInstance.Model.Bread.Consume(mTableId);
    }

    private void SpawnBread()
    {
        if (GameInstance.Pool == null || mRootTransform == null) return;

        var go = GameInstance.Pool.Spawn(Bread.PoolName, mRootTransform.position, Quaternion.identity);
        if (go == null) return;

        go.transform.SetParent(mRootTransform);
        go.transform.localScale = Vector3.one;

        var bread = go.GetComponent<Bread>();
        if (bread != null)
            mActiveBreadQueue.Enqueue(bread);
    }
}
