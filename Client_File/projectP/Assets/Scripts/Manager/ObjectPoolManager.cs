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

    [SerializeField] private List<PoolInfo> mPoolSettings;
    private Dictionary<string, Queue<GameObject>> mPools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> mPrefabLookup = new Dictionary<string, GameObject>();

    public void Init()
    {
        InitializePools();
    }

    public void Subscribe() { }

    public void Clear()
    {
        mPools.Clear();
        mPrefabLookup.Clear();
    }

    public void Destory()
    {
        Clear();
    }

    private void InitializePools()
    {
        if (mPoolSettings == null) return;
        foreach (var info in mPoolSettings)
        {
            mPools[info.poolName] = new Queue<GameObject>();
            mPrefabLookup[info.poolName] = info.prefab;

            for (int i = 0; i < info.initialSize; i++)
                CreateNewObject(info.poolName);
        }
    }

    private GameObject CreateNewObject(string poolName)
    {
        GameObject obj = Instantiate(mPrefabLookup[poolName], transform);
        obj.name = poolName;
        obj.SetActive(false);
        mPools[poolName].Enqueue(obj);
        return obj;
    }

    public GameObject Spawn(string poolName, Vector3 position, Quaternion rotation)
    {
        if (!mPools.ContainsKey(poolName)) return null;

        if (mPools[poolName].Count == 0)
            CreateNewObject(poolName);

        GameObject obj = mPools[poolName].Dequeue();
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        return obj;
    }

    public void RegisterPool(string poolName, GameObject prefab)
    {
        if (mPools.ContainsKey(poolName)) return;
        mPools[poolName] = new Queue<GameObject>();
        mPrefabLookup[poolName] = prefab;
    }

    public void Despawn(string poolName, GameObject obj)
    {
        obj.SetActive(false);
        mPools[poolName].Enqueue(obj);
    }
}
