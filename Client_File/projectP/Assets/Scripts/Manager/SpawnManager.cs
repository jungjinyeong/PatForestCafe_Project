using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpawnManager : MonoBehaviour
{
    [Header("NPC Prefabs")]
    [SerializeField] private GameObject[] mNpcPrefabs;

    [Header("Auto Spawn")]
    [SerializeField] private float mSpawnInterval = 3f;

    [Header("UI")]
    [SerializeField] private GameObject mLobbyCharUIPrefab;

    private readonly List<CharNpc> mSpawnedNPCs = new();

    // 매장에 남아 있는 손님 수(영업 마감 시 전원 퇴장 대기용).
    public IReadOnlyReactiveProperty<int> ActiveCount => mActiveCount;
    private readonly ReactiveProperty<int> mActiveCount = new ReactiveProperty<int>(0);
    private IDisposable mAutoSpawnDisposable;

    public void SetInfo(GameObject lobbyCharUIPrefab, GameObject[] npcPrefabs)
    {
        this.mLobbyCharUIPrefab = lobbyCharUIPrefab;
        this.mNpcPrefabs = npcPrefabs;

        RegisterNPCPools();
    }

    private void RegisterNPCPools()
    {
        if (mNpcPrefabs == null || GameInstance.Pool == null) return;
        foreach (var prefab in mNpcPrefabs)
        {
            if (prefab != null)
                GameInstance.Pool.RegisterPool(prefab.name, prefab);
        }
    }

    public void SpawnAll()
    {
        if (mNpcPrefabs == null || mNpcPrefabs.Length == 0)
        {
            Logger.Warning("[SpawnManager] NPC prefabs not loaded.");
            return;
        }

        var group = GameInstance.WayPoint?.GetRandomUnlockedFloorGroup();
        if (group == null)
        {
            Logger.Warning("[SpawnManager] No unlocked floor WaypointGroup registered.");
            return;
        }

        SpawnInGroup(group);
    }

    private void SpawnInGroup(WaypointGroup group)
    {
        if (group == null) return;

        var spawnPoints = group.GetSpawnPoints();
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Logger.Warning($"[SpawnManager] '{group.name}' has no SpawnPoint waypoints.");
            return;
        }

        foreach (var spawnPoint in spawnPoints)
            SpawnNPC(spawnPoint, group);
    }

    private void SpawnNPC(Waypoint spawnPoint, WaypointGroup group)
    {
        var prefab = mNpcPrefabs[UnityEngine.Random.Range(0, mNpcPrefabs.Length)];

        GameObject npcObj;
        if (GameInstance.Pool != null)
            npcObj = GameInstance.Pool.Spawn(prefab.name, spawnPoint.transform.position, Quaternion.identity);
        else
            npcObj = Instantiate(prefab, spawnPoint.transform.position, Quaternion.identity);

        if (npcObj == null) return;

        npcObj.transform.localScale = new Vector3(3, 3, 1);

        var npc = npcObj.GetComponent<CharNpc>();
        if (npc == null)
        {
            Logger.Error($"[SpawnManager] '{prefab.name}' has no CharNpc component.");
            Destroy(npcObj);
            return;
        }

        npc.Init(group, spawnPoint);

        if (!mSpawnedNPCs.Contains(npc))
            mSpawnedNPCs.Add(npc);
        mActiveCount.Value = mSpawnedNPCs.Count;
        GameInstance.Model.Business.RecordVisitor();

        AttachLobbyCharUI(npcObj);
    }

    private void AttachLobbyCharUI(GameObject npcObj)
    {
        if (mLobbyCharUIPrefab == null) return;
        if (npcObj.GetComponentInChildren<LobbyCharUI>() != null)
            return;

        var ui = Instantiate(mLobbyCharUIPrefab, npcObj.transform);
        ui.transform.localPosition = new Vector3(0f, 0f, 0f);
        ui.transform.localScale = Vector3.one * 0.3f;

        if (ui.GetComponent<LobbyCharUI>() == null)
            ui.AddComponent<LobbyCharUI>();

        ui.GetComponent<LobbyCharUI>().Init();
    }

    public void ReturnToPool(CharNpc npc)
    {
        if (npc == null) return;
        mSpawnedNPCs.Remove(npc);
        mActiveCount.Value = mSpawnedNPCs.Count;

        if (GameInstance.Pool != null)
            GameInstance.Pool.Despawn(npc.gameObject.name, npc.gameObject);
        else
            Destroy(npc.gameObject);
    }

    public void StartAutoSpawn()
    {
        StopAutoSpawn();
        mAutoSpawnDisposable = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(mSpawnInterval))
            .Subscribe(_ => SpawnOneRandom())
            .AddTo(this);
    }

    public void StopAutoSpawn()
    {
        mAutoSpawnDisposable?.Dispose();
        mAutoSpawnDisposable = null;
    }

    private void SpawnOneRandom()
    {
        if (mNpcPrefabs == null || mNpcPrefabs.Length == 0)
            return;

        var group = GameInstance.WayPoint?.GetRandomUnlockedFloorGroup();
        if (group == null) return;

        var spawnPoints = group.GetSpawnPoints();
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        SpawnNPC(spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)], group);
    }

    // 남은 손님을 모두 즉시 내보낸다. CharNpc.ForceLeave()가 들고 있던 빵·음료·대기 타이머를 정리한 뒤
    // ReturnToPool()로 목록에서 빠지므로, 풀에서 재사용돼도 이전 상태가 남지 않는다.
    public void DespawnAll()
    {
        foreach (var npc in new List<CharNpc>(mSpawnedNPCs))
        {
            if (npc != null)
                npc.ForceLeave();
        }
        mSpawnedNPCs.Clear();
        mActiveCount.Value = 0;
    }
}
