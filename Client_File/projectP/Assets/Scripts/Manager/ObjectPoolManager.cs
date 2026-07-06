using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour, IManager
{
    [System.Serializable]
    public class PoolInfo
    {
        public string poolName;
        public GameObject prefab;
        public int initialSize;
    }

    [SerializeField] private List<PoolInfo> poolSettings;
    private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> _prefabLookup = new Dictionary<string, GameObject>();

    public void Init()
    {
        InitializePools();
    }

    public void Subscribe() { }

    public void Clear()
    {
        _pools.Clear();
        _prefabLookup.Clear();
    }

    public void Destory()
    {
        Clear();
    }

    private void InitializePools()
    {
        if (poolSettings == null) return;
        foreach (var info in poolSettings)
        {
            _pools[info.poolName] = new Queue<GameObject>();
            _prefabLookup[info.poolName] = info.prefab;

            for (int i = 0; i < info.initialSize; i++)
                CreateNewObject(info.poolName);
        }
    }

    private GameObject CreateNewObject(string poolName)
    {
        GameObject obj = Instantiate(_prefabLookup[poolName], transform);
        obj.name = poolName;
        obj.SetActive(false);
        _pools[poolName].Enqueue(obj);
        return obj;
    }

    public GameObject Spawn(string poolName, Vector3 position, Quaternion rotation)
    {
        if (!_pools.ContainsKey(poolName)) return null;

        if (_pools[poolName].Count == 0)
            CreateNewObject(poolName);

        GameObject obj = _pools[poolName].Dequeue();
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        return obj;
    }

    public void RegisterPool(string poolName, GameObject prefab)
    {
        if (_pools.ContainsKey(poolName)) return;
        _pools[poolName] = new Queue<GameObject>();
        _prefabLookup[poolName] = prefab;
    }

    public void Despawn(string poolName, GameObject obj)
    {
        obj.SetActive(false);
        _pools[poolName].Enqueue(obj);
    }
}
