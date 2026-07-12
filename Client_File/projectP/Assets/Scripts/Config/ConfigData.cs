using System;
using UnityEngine;

public enum eConfigType
{
    BreadMaxCount,
    DefaultDrinkTid,
}

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "ConfigData/Create")]
#endif
[Serializable]
public class ConfigData : ScriptableObject
{
    public SerializableDictionary<eConfigType, int> m_configDic = new SerializableDictionary<eConfigType, int>();

    public int GetValue(eConfigType type) => m_configDic.TryGetValue(type, out var value) ? value : 0;
}
