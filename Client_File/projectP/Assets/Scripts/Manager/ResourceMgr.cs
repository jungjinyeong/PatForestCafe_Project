using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;
using UnityEngine.SceneManagement;
using UniRx;



#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace ResourceInfo
{
    public class EffectPreload
    {
        public string mAddressName;
        public string mPoolKey;
        public int mPoolCount;
        
        public EffectPreload(string addressName, string poolKey, int poolCount)
        {
            mAddressName = addressName;
            mPoolKey = poolKey;
            mPoolCount = poolCount;
        }
    }
}

public class ResourceMgr : MonoBehaviour, IManager
{
    private Dictionary<string, List<IResourceLocation>> mLocationDic = new Dictionary<string, List<IResourceLocation>>();

    public class LoadeddResourcesInfo
    {
        public int RefCount = 1;
        public bool IsAddressable;
        public AsyncOperationHandle? Handle;
        public UnityEngine.Object Resource;

        public LoadeddResourcesInfo(AsyncOperationHandle handle)
        {
            IsAddressable = true;
            Handle = handle;
        }

        public LoadeddResourcesInfo(UnityEngine.Object resource)
        {
            IsAddressable = false;
            Resource = resource;
        }

        public void Release()
        {
            if (IsAddressable && Handle.HasValue && Handle.Value.IsValid())
            {
                Addressables.Release(Handle.Value);
            }
            else if (!IsAddressable && Resource != null)
            {
                Resources.UnloadAsset(Resource);
            }
        }
    }

    private Dictionary<string, LoadeddResourcesInfo> mLoadedResources = new Dictionary<string, LoadeddResourcesInfo>();

    public IReadOnlyDictionary<string, LoadeddResourcesInfo> LoadedResources => mLoadedResources;
    
    public List<IResManagement> mListResources = new List<IResManagement>();
    
    int mMaxLoadCount = 0;

    public void Init()
    {

    }

    public void Reset()
    {
        mMaxLoadCount = 0;

        for (int i = 0; i < mListResources.Count; i++)
        {
            mListResources[i].Dispose();
        }
        mListResources?.Clear();
    }

    public void Clear()
    {
        Reset();

        mListResources?.Clear();
    }

    public T ResourceLoad<T>(string name) where T : UnityEngine.Object
    {
        T resource = Resources.Load<T>(name);
        if(resource != null)
        {
            return resource;
        }
        return default(T);
    }

    #region [PreLoading]

    public bool IsExistResourceLocation(string lable)
    {
        return mLocationDic.ContainsKey(lable);
    }

    public void ClearSceneRef()
    {
        if(mLoadedResources == null || mLoadedResources.Count <= 0)
        {
            return;
        }

        List<string> keysToRemove = new List<string>();

        foreach(var item in mLoadedResources)
        {
            if(item.Key == null ||  item.Key.Equals(string.Empty))
            {
                continue;
            }

            //상주해야하는 리소스는 필터링.
            if(ChkCommonAsset(item.Key))
            {
                continue;
            }

            keysToRemove.Add(item.Key);
        }

        for(int i = 0; i < keysToRemove.Count; i++)
        {
            Unload(keysToRemove[i], bUnloadAll: true);
        }
    }

    private bool ChkCommonAsset(string key)
    {
        if(key.Contains("Common/"))
        {
            return true;
        }
        //if(key.Contains("UI/"))
        //{
        //    return true;
        //}
        //if(key.Contains("Effect/"))
        //{
        //    return true;
        //}
        return false;
    }

    public async UniTask<List<IResourceLocation>> AsyncLoadResourceLocationsAsync(string label, Type type)
    {
        if (mLocationDic.TryGetValue(label, out var locations))
        {
            return locations;
        }
        if(string.IsNullOrEmpty(label))
        {
            Debug.LogError($"ResourceMgr::AsyncLoadResourceLocationsAsync - label is null or empty.");
            return null;
        }
        var result = await Addressables.LoadResourceLocationsAsync(label, type);

        mLocationDic.Add(label, result.ToList());

        return mLocationDic[label];
    }

    void CollectLoader()
    {
        mMaxLoadCount = 0;

        mListResources.AddRange(GameInstance.Instance.GetComponentsInChildren<IResManagement>());
        GameObject[] objs = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var obj in objs)
        {
            var list = obj.GetComponentsInChildren<IResManagement>();
            foreach (var resource in list)
                mMaxLoadCount += resource.GetMaxLoad();
        }

        Debug.Log($"Loading Mgr Count : {mListResources.Count}");
    }

    public bool NeedLoading()
    {
        return mMaxLoadCount > 0;
    }

    public async UniTask ProcessLoading(IResManagement sceneRes = null)
    {
        ClearSceneRef();

        CollectLoader();

        if(sceneRes != null)
        {
            mListResources.Add(sceneRes);
            mMaxLoadCount += sceneRes.GetMaxLoad();
        }

        if(NeedLoading())
        {
            await Preload();
        }
    }    

    async UniTask Preload()
    {
        int count = 0;

        var loadingProgress = new CEvent.LoadingEvnet(0, 0);

        for(int i = 0; i < mListResources.Count; i++)
        {
            var resource = mListResources[i];
            if(resource == null)
            {
                continue;
            }

            float curTime = Time.realtimeSinceStartup;
            await resource.Preload();
            count++;

            MessageBroker.Default.Publish(new CEvent.LoadingEvnet(mMaxLoadCount, count));
        }
    }
    #endregion

    public T ResourcesLoad<T>(string name) where T : UnityEngine.Object
    {
        T resouce = Resources.Load<T>(name);
        if(resouce != null)
        {
            return resouce;
        }
        return default(T);
    }

    public bool Unload(string key, bool bUnloadAll = false)
    {
        if (!mLoadedResources.TryGetValue(key, out var info))
        {
            return false;
        }

        info.RefCount = bUnloadAll ? 0 : info.RefCount - 1;
        if(info.RefCount > 0)
        {
            return false;
        }
        mLoadedResources.Remove(key);
        info.Release();

        return true;
    }

    public static void Unlaod<T>(AsyncOperationHandle<T> handle, Action onComplete) where T : UnityEngine.Object
    {
        if (!handle.IsValid())
            return;

        if (handle.IsDone)
        {
            Addressables.Release(handle);
            onComplete?.Invoke();
        }
    }

    public async UniTask CollectAdressables(string[] lables, List<string> collectList)
    {
        collectList.Clear();

        for(int i = 0; i < lables.Length; i++)
        {
            var locations = await AsyncLoadResourceLocationsAsync(lables[i], typeof(UnityEngine.Object));
            if( locations == null || locations.Count <= 0)
            {
                continue;
            }
            foreach (var location in locations)
            {
                if(!collectList.Contains(location.PrimaryKey))
                {
                    collectList.Add(location.PrimaryKey);
                }
            }
        }
    }

    public async UniTask<long> GetDownloadSizeAsyncByLables(string[] lables)
    {
        long totalSize = 0;
        for(int i = 0; i < lables.Length; i++)
        {
            var size = await Addressables.GetDownloadSizeAsync(lables[i]);
            totalSize += size;
        }
        return totalSize;
    }

    public async UniTask DownloadSizeAsyncByKeys(string[] lables, Action<float> onProgress)
    {
        var targetList = new List<string>();
        await CollectAdressables(lables, targetList);

        var downloadHandle = Addressables.DownloadDependenciesAsync(targetList, true);

        while (!downloadHandle.IsDone)
        {
            await UniTask.WaitForEndOfFrame();

            float progress = downloadHandle.GetDownloadStatus().Percent;

            onProgress?.Invoke(progress);
        }

        Addressables.Release(downloadHandle);    
    }

    public async UniTask<bool> DeleteAddressables(string[] lables)
    {
        var targetList = new List<string>();
        await CollectAdressables(lables, targetList);
        var deleteHandle = Addressables.ClearDependencyCacheAsync(targetList, true);
        await deleteHandle.Task;

        if(deleteHandle.Status == AsyncOperationStatus.Succeeded)
        {
            return true;
        }
        else
        {
            Debug.LogError($"DeleteAddressables failed: {deleteHandle.OperationException}");
            return false;
        }
    }

    #region [ Assets ]

    public async void LoadAsync<T>(string assetAddress, Action<T> onSuccess, Action onFail) where T : UnityEngine.Object
    {
        try
        {
            var obj = await LoadAsync<T>(assetAddress);
            if(obj != null)
            {
                onSuccess?.Invoke(obj);
            }
            else
            {
                onFail?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoadAsync Exception: {ex.Message}");
            onFail?.Invoke();
        }
    }

    public async UniTask<T> LoadAsync<T>(string assetAddress) where T : UnityEngine.Object
    {
        if (mLoadedResources.TryGetValue(assetAddress, out var info))
        {
            info.RefCount++;
            return info.IsAddressable ? (T)info.Handle.Value.Result : (T)info.Resource;
        }
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(assetAddress);
        await handle.Task;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            mLoadedResources[assetAddress] = new LoadeddResourcesInfo(handle);
            return handle.Result;
        }

        if (handle.IsValid())
        {
            Addressables.Release(handle);
        }

        var resource = Resources.Load<T>(assetAddress);
        if (resource != null)
        {
            mLoadedResources[assetAddress] = new LoadeddResourcesInfo(resource);
            return resource;
        }

        Debug.LogError($"Failed to load asset at address: {assetAddress}");
        return null;
    }

    public T LoadSync<T>(string assetAddress, string fallbak = null) where T : UnityEngine.Object
    {
        if (mLoadedResources.TryGetValue(assetAddress, out var info))
        {
            info.RefCount++;
            return info.IsAddressable ? (T)info.Handle.Value.Result : (T)info.Resource;
        }

        var handle = Addressables.LoadAssetAsync<T>(assetAddress);
        if(handle.Status == AsyncOperationStatus.Failed && string.IsNullOrEmpty(fallbak) == false)
        {
            handle = Addressables.LoadAssetAsync<T>(fallbak);
        }

        T asset = handle.WaitForCompletion();

        if (asset != null)
        {
            var loadInfo = new LoadeddResourcesInfo(handle);
            mLoadedResources.Add(assetAddress, loadInfo);
            return asset;
        }

        if(handle.IsValid())
        {
            Addressables.Release(handle);
        }

        var resource = Resources.Load<T>(assetAddress);
        if (resource != null)
        {
            mLoadedResources[assetAddress] = new LoadeddResourcesInfo(resource);
            return resource;
        }
        Debug.LogError($"Failed to load asset at address: {assetAddress}");
        return null;
    }
    #endregion


    #region [ Static ]

#if UNITY_EDITOR
    public static string GetResourceAddress(UnityEngine.Object asset, bool usePath = false)
    {
        if(asset == null)
            return null;

        string path = UnityEditor.AssetDatabase.GetAssetPath(asset);

        if(usePath)
        {
            return path;
        }

        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
        var assetEntry = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);

        if (assetEntry != null)
            return assetEntry.address;

        return null;
    }


#endif

#endregion


    public void Subscribe()
    {

    }

    public void Destory()
    {

    }
}