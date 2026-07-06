using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Extension;

public class BreadTable : MonoBehaviour
{
    [Header("ID")]
    [SerializeField] private int tableId;

    [Header("UI")]
    [SerializeField] private UIButtonEx btnAddBread;

    [Header("Bread")]
    [SerializeField] private Transform rootTransform;
    [SerializeField] private GameObject breadPrefab;

    private readonly Queue<Bread> mActiveBreadQueue = new Queue<Bread>();

    private void Start()
    {
        if (breadPrefab != null)
            GameInstance.Pool?.RegisterPool(Bread.PoolName, breadPrefab);

        GameInstance.Model.Bread.Register(tableId);

        btnAddBread.OnSubscribeOnClick(OnClickAddBread).AddTo(this);

        MessageBroker.Default
            .Receive<CEvent.BreadPickup>()
            .Where(e => e.tableId == tableId)
            .Subscribe(e => HandleBreadPickupAsync(e.npc).Forget())
            .AddTo(this);
    }

    private void OnClickAddBread()
    {
        SpawnBread();
        GameInstance.Model.Bread.Add(tableId);
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

        GameInstance.Model.Bread.Consume(tableId);
    }

    private void SpawnBread()
    {
        if (GameInstance.Pool == null || rootTransform == null) return;

        var go = GameInstance.Pool.Spawn(Bread.PoolName, rootTransform.position, Quaternion.identity);
        if (go == null) return;

        go.transform.SetParent(rootTransform);
        go.transform.localScale = Vector3.one;

        var bread = go.GetComponent<Bread>();
        if (bread != null)
            mActiveBreadQueue.Enqueue(bread);
    }
}
