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

        var firstGroup = GameInstance.WayPoint?.GetFirstGroup();
        if (firstGroup == null)
        {
            Logger.Warning("[SpawnManager] No WaypointGroup registered.");
            return;
        }

        SpawnInGroup(firstGroup);
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

        var group = GameInstance.WayPoint?.GetFirstGroup();
        if (group == null) return;

        var spawnPoints = group.GetSpawnPoints();
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        SpawnNPC(spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)], group);
    }

    public void DespawnAll()
    {
        foreach (var npc in mSpawnedNPCs)
        {
            if (npc == null) continue;
            if (GameInstance.Pool != null)
                GameInstance.Pool.Despawn(npc.gameObject.name, npc.gameObject);
            else
                Destroy(npc.gameObject);
        }
        mSpawnedNPCs.Clear();
    }
}
