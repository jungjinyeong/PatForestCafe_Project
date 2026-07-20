using System;
using System.IO;
using UnityEngine;
using UniRx;

public class SaveManager : MonoBehaviour
{
    private const string SaveFileName = "save.json";
    private const float AutoSaveIntervalSeconds = 30f;

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private IDisposable mAutoSaveDisposable;

    public void Init()
    {
        Load();
        StartAutoSave();
    }

    public void StartAutoSave()
    {
        StopAutoSave();
        mAutoSaveDisposable = Observable.Interval(TimeSpan.FromSeconds(AutoSaveIntervalSeconds))
            .Subscribe(_ => Save())
            .AddTo(this);
    }

    public void StopAutoSave()
    {
        mAutoSaveDisposable?.Dispose();
        mAutoSaveDisposable = null;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    public void Save()
    {
        var data = new SaveData();

        foreach (var item in GameInstance.Model.Item.GetAll())
            data.Items.Add(new ItemSaveEntry { Tid = item.Tid, Count = item.Count.Value });

        foreach (var wealth in GameInstance.Model.Item.GetAllWealth())
            data.Items.Add(new ItemSaveEntry { Tid = wealth.Tid, Count = wealth.Count.Value });

        File.WriteAllText(SavePath, JsonUtility.ToJson(data));
        Logger.Log($"[SaveManager] 저장 완료: {SavePath}");
    }

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Logger.Log("[SaveManager] 저장 파일이 없어 기본값으로 시작합니다.");
            return;
        }

        var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (data?.Items == null) return;

        foreach (var entry in data.Items)
            GameInstance.Model.Item.SetByTid(entry.Tid, entry.Count);

        Logger.Log($"[SaveManager] 불러오기 완료: {SavePath}");
    }
}
