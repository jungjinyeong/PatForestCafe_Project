using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CreateAssetMenu(menuName = "UIPathInfo/Create")]
#endif
[Serializable]
public class UIPathInfo : ScriptableObject
{
    public SerializableDictionary<eUIType, string> m_pathInfoDic = new SerializableDictionary<eUIType, string>();

    //public SerializableDictionary<eUIType, string> GetUIPathInfos() => m_pathInfoDic;
}