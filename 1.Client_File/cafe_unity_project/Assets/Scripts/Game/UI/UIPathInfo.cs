using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "UIPathInfo/Create")]
[Serializable]
public class UIPathInfo : ScriptableObject
{
    [SerializeField] UIPathInfoDictionary m_pathInfoDic = new UIPathInfoDictionary();

    public Dictionary<eUIType, string> GetUIPathInfos()
    {
        return m_pathInfoDic;
    }
}