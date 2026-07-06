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
    [SerializeField] private GameObject[] npcPrefabs;

    [Header("Waypoint Groups")]
    [SerializeField] private WaypointGroup[] waypointGroups;

    [Header("Auto Spawn")]
    [SerializeField] private float mSpawnInterval = 3f;

    [Header("UI")]
    [SerializeField] private GameObject lobbyCharUIPrefab;

    private readonly List<WaypointNPC> mSpawnedNPCs = new();
    private IDisposable mAutoSpawnDisposable;

    public void SetInfo(GameObject lobbyCharUIPrefab, GameObject[] npcPrefabs, WaypointGroup[] waypointGroups)
    {
        this.lobbyCharUIPrefab = lobbyCharUIPrefab;
        this.npcPrefabs = npcPrefabs;
        this.waypointGroups = waypointGroups;

        RegisterNPCPools();
    }

    private void RegisterNPCPools()
    {
        if (npcPrefabs == null || GameInstance.Pool == null) return;
        foreach (var prefab in npcPrefabs)
        {
            if (prefab != null)
                GameInstance.Pool.RegisterPool(prefab.name, prefab);
        }
    }

    public void SpawnAll()
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0)
        {
            Debug.LogWarning("[SpawnManager] NPC prefabs not loaded.");
            return;
        }

        foreach (var group in waypointGroups)
            SpawnInGroup(group);
    }

    private void SpawnInGroup(WaypointGroup group)
    {
        if (group == null || group.Waypoints == null || group.Waypoints.Length == 0) return;

        var spawnPoints = new List<Waypoint>();
        var pathWaypoints = new List<Waypoint>();

        foreach (var wp in group.Waypoints)
        {
            if (wp == null) continue;

            if (wp.GetCategoryType() == Waypoint.eWaypointCategoryType.SpwanPoint)
                spawnPoints.Add(wp);
            else
                pathWaypoints.Add(wp);
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"[SpawnManager] '{group.name}' has no SpawnPoint waypoints.");
            return;
        }

        foreach (var spawnPoint in spawnPoints)
            SpawnNPC(spawnPoint, pathWaypoints);
    }

    private void SpawnNPC(Waypoint spawnPoint, List<Waypoint> pathWaypoints)
    {
        var prefab = npcPrefabs[UnityEngine.Random.Range(0, npcPrefabs.Length)];

        GameObject npcObj;
        if (GameInstance.Pool != null)
            npcObj = GameInstance.Pool.Spawn(prefab.name, spawnPoint.transform.position, Quaternion.identity);
        else
            npcObj = Instantiate(prefab, spawnPoint.transform.position, Quaternion.identity);

        if (npcObj == null) return;

        npcObj.transform.localScale = new Vector3(3, 3, 1);

        var npc = npcObj.GetComponent<WaypointNPC>();
        if (npc == null)
        {
            Debug.LogError($"[SpawnManager] '{prefab.name}' has no WaypointNPC component.");
            Destroy(npcObj);
            return;
        }

        var initWaypoints = new Waypoint[1 + pathWaypoints.Count];
        initWaypoints[0] = spawnPoint;
        for (int i = 0; i < pathWaypoints.Count; i++)
            initWaypoints[i + 1] = pathWaypoints[i];

        npc.Init(initWaypoints);

        if (!mSpawnedNPCs.Contains(npc))
            mSpawnedNPCs.Add(npc);

        AttachLobbyCharUIRandom(npcObj);
    }

    private void AttachLobbyCharUIRandom(GameObject npcObj)
    {
        if (lobbyCharUIPrefab == null || UnityEngine.Random.value < 0.5f) return;
        if (npcObj.GetComponentInChildren<LobbyCharUI>() != null) return;

        var ui = Instantiate(lobbyCharUIPrefab, npcObj.transform);
        ui.transform.localPosition = new Vector3(0f, 0f, 0f);
        ui.transform.localScale = Vector3.one * 0.3f;

        if (ui.GetComponent<LobbyCharUI>() == null)
            ui.AddComponent<LobbyCharUI>();
    }

    public void ReturnToPool(WaypointNPC npc)
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
        mAutoSpawnDisposable = Observable.Interval(TimeSpan.FromSeconds(mSpawnInterval))
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
        if (npcPrefabs == null || npcPrefabs.Length == 0 || waypointGroups == null || waypointGroups.Length == 0)
            return;

        var group = waypointGroups[UnityEngine.Random.Range(0, waypointGroups.Length)];
        if (group == null || group.Waypoints == null || group.Waypoints.Length == 0) return;

        var spawnPoints = new List<Waypoint>();
        var pathWaypoints = new List<Waypoint>();

        foreach (var wp in group.Waypoints)
        {
            if (wp == null) continue;
            if (wp.GetCategoryType() == Waypoint.eWaypointCategoryType.SpwanPoint)
                spawnPoints.Add(wp);
            else
                pathWaypoints.Add(wp);
        }

        if (spawnPoints.Count == 0) return;

        SpawnNPC(spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)], pathWaypoints);
    }

    public bool IsBlockedByNPC(WaypointNPC self, Vector3 direction, float separationDistance)
    {
        foreach (var npc in mSpawnedNPCs)
        {
            if (npc == self || npc == null || !npc.gameObject.activeInHierarchy) continue;

            Vector3 toNpc = npc.transform.position - self.transform.position;
            float dist = toNpc.magnitude;

            if (dist > separationDistance) continue;
            if (Vector3.Dot(toNpc.normalized, direction) > 0.5f)
                return true;
        }
        return false;
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
