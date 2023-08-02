using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Framework.UI;
using Framework.State;

[Serializable]
public enum eUIType
{
    Min = 0,

    UIRootIntro = eUILayerType.Menu << 16,
    UIRootLobby,

    UIHudController = eUILayerType.AlwaysOnTop << 16,
    UILoading,
    UISystemMsg,

    Max,
}

public enum eUILayerType
{
    Min = 0,
    Menu,
    Popup,
    AlwaysOnTop,
}

public class UIWndStack
{
    public List<UIWndBase> m_uiStack = new List<UIWndBase>();

    public void Add(UIWndBase wnd)
    {
        if(m_uiStack.Contains(wnd))
        {

        }
        else
        {
            m_uiStack.Add(wnd);
        }
    }

    public void Remove(eUIType type)
    {
        var find = m_uiStack.Find(x => x.GetUIType() == type);
        if(find)
        {
            find.Close();
            m_uiStack.Remove(find);
        }
    }

    public bool IsContain(eUIType type)
    {
        return m_uiStack.Find(x => x.GetUIType() == type) != null;
    }

    public void Clear()
    {
        m_uiStack?.Clear();
    }
}

public class UIManager : MonoBehaviour
{
    readonly string UI_PATH_INFO_PATH = "UIPathInfo";

    UIWndStack m_UIWndStack;

    Dictionary<eUIType, UIWndBase> m_uiContains = new Dictionary<eUIType, UIWndBase>();
    UIWndBase m_curUI = null;

    private UIPathInfo m_uiPathInfo = null;
    public UIPathInfo UIPathInfo
    {
        get
        {
            if(m_uiPathInfo == null)
            {
                m_uiPathInfo = UnityEngine.Resources.Load<UIPathInfo>("UIPathInfo");
            }
            return m_uiPathInfo;
        }
    }

    [SerializeField]
    SerializableDictionary<eUILayerType, Canvas> m_targetCanvasDic = new SerializableDictionary<eUILayerType, Canvas>();

    public Canvas GetTargetCanvas(eUILayerType type)
    {
        if (m_targetCanvasDic.TryGetValue(type, out var canvas))
            return canvas;
        else
        {
            Debug.LogError($"ui layer type : {type} ::: target canvas is null");
            return null;
        }
    }

    static UIManager _instance;
    public static UIManager Instance { get { return _instance; } }

    void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this);
        m_UIWndStack = new UIWndStack();
    }

    void Start()
    {
        Initialize();
    }

    public void Initialize() 
    {
        Open<UIRootIntro, UIRootIntro.Param>(eUIType.UIRootIntro, new UIRootIntro.Param());
    }

    public void Destroy()
    {
        m_uiPathInfo = null;

        m_uiContains?.Clear();
        m_uiContains = null;

        m_UIWndStack?.Clear();
    }

    /// <summary>
    /// 씬 이동 시 ui clear
    /// </summary>
    public void Clear()
    {
        m_UIWndStack?.Clear();
        m_uiContains?.Clear();
    }

    public T Open<T, T1>(eUIType uiType, T1 param) where T : UIWndBase
    {
        if(m_curUI != null)
        {
            m_curUI.Close();
        }

        UIWndBase ui = null;
        if (m_uiContains != null && m_uiContains.TryGetValue(uiType, out ui))
        {
            m_UIWndStack.Add(ui);
            ui.Open();
        }
        else
        {
            eUILayerType layerType = GetLayerType(uiType);
            string uiPath = GetUIPath(uiType);
            GameObject uiPrefab = Resources.Load(uiPath) as GameObject;
            if (uiPrefab != null)
            {
                ui = Instantiate(uiPrefab).GetComponent<UIWndBase>();
                DontDestroyOnLoad(ui);
                ui.gameObject.name = uiPrefab.name;
                RectTransform rect = ui.gameObject.GetComponent<RectTransform>();
                rect.SetParent(GetTargetCanvas(layerType).gameObject.transform);
                rect.offsetMax = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                rect.localPosition = Vector3.zero;
                m_uiContains.Add(uiType, ui);
                m_UIWndStack.Add(ui);
                ui.Init();
                ui.Open();
            }
        }

        m_curUI = ui;

        return ui as T;
    }

    public void Close(eUIType eUIType)
    {
        if(m_UIWndStack.IsContain(eUIType))
        {
            m_UIWndStack.Remove(eUIType);
        }
    }

    string GetUIPath(eUIType uiType)
    {
        var pathInfos = UIPathInfo.m_pathInfoDic;
        if (pathInfos != null && pathInfos.TryGetValue(uiType, out string path))
            return path;

        return null;
    }

    public eUILayerType GetLayerType(eUIType type)
    {
        return (eUILayerType)((int)type >> 16);
    }
}