using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Framework.UI;

[Serializable]
public enum eUIType
{
    Min = 0,

    UIRootIntro = eUILayerType.Menu << 0,
    UIRootLobby,

    UIHudController = eUILayerType.OnlyAwaysTop << 0,

    Max,
}

public enum eUILayerType
{
    Min = 0,
    Menu,
    Popup,
    OnlyAwaysTop,
}

public class UIManager : MonoBehaviour
{
    readonly string UI_PATH_INFO_PATH = "UIPathInfo";

    List<UIWndBase> m_uiStack = new List<UIWndBase>();
    Dictionary<eUIType, UIWndBase> m_uiContains = new Dictionary<eUIType, UIWndBase>();
    Dictionary<eUIType, string> m_uiPaths;

    [SerializeField]
    Canvas m_mainCanvas = null;

    static UIManager _instance;
    public static UIManager Instance { get { return _instance; } }

    void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        Initialize();
    }

    public void Initialize() 
    {
        SetUIPath();

        Open<UIRootIntro, UIRootIntro.Param>(eUIType.UIRootIntro, new UIRootIntro.Param());
    }

    void SetUIPath()
    {
        var uIPathInfo = UnityEngine.Resources.Load<UIPathInfo>(UI_PATH_INFO_PATH);
        m_uiPaths = uIPathInfo.GetUIPathInfos();
    }

    public void Destroy()
    {
        m_uiPaths?.Clear();
        m_uiPaths = null;

        m_uiContains?.Clear();
        m_uiContains = null;

        m_uiStack?.Clear();
        m_uiStack = null;
    }

    /// <summary>
    /// 씬 이동 시 ui clear
    /// </summary>
    public void Release()
    {
        m_uiStack?.Clear();
        m_uiContains?.Clear();
    }

    public T Open<T, T1>(eUIType uiType, T1 param) where T : UIWndBase
    {
        UIWndBase ui = null;
        if (m_uiContains != null && m_uiContains.TryGetValue(uiType, out ui))
        {
            m_uiStack.Add(ui);
            ui.Open();
        }
        else
        {
            string uiPath = GetUIPath(uiType);
            GameObject uiPrefab = Resources.Load(uiPath) as GameObject;
            if (uiPrefab != null)
            {
                ui = Instantiate(uiPrefab).GetComponent<UIWndBase>();
                DontDestroyOnLoad(ui);
                RectTransform rect = ui.gameObject.GetComponent<RectTransform>();
                rect.SetParent(m_mainCanvas.gameObject.transform);
                rect.offsetMax = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                rect.localPosition = Vector3.zero;
                m_uiContains.Add(uiType, ui);
                m_uiStack.Add(ui);
                //ui.gameObject.SetActive(true);
                ui.Init();
                ui.Open();
            }
        }

        return ui as T;
    }

    public void Close(eUIType eUIType)
    {
        for(int i = 0; i < m_uiStack.Count; i++)
        {
            if (m_uiStack[i].GetUIType() == eUIType)
            {
                m_uiStack[i].gameObject.SetActive(false);
                m_uiStack[i].Close();
                m_uiStack.RemoveAt(i);
            }
        }
    }

    string GetUIPath(eUIType uiType)
    {
        if (m_uiPaths != null && m_uiPaths.TryGetValue(uiType, out string path))
            return path;

        return null;
    }
}