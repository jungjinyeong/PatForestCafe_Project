using System;
using UnityEngine;

public enum eConfigType
{
    BreadMaxCount,
    DefaultDrinkTid,
    MaxSpecialOrderNpc,
    BreadUnavailablePauseMs,
    DefaultBreadRecipeTid,  // 처음부터 발견된 빵 레시피(오븐 제작에서 바로 구울 수 있음)
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
