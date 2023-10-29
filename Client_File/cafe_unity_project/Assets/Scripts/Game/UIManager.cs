using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Framework.UI;
using Framework.State;
using Cysharp.Threading.Tasks;

[Serializable]
public enum eUIType
{
    Min = 0,

    UIRootIntro = eUILayerType.Menu << 16,
    UIRootLobby,
    UIRootLogin,

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

    public void Pop(eUIType type)
    {
        var find = m_uiStack.Find(x => x.GetUIType() == type);
        if (find)
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
        Debug.Log("awake ui");
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
        foreach(var ui in m_uiContains)
        {
            GameObject.Destroy(ui.Value.gameObject);
        }

        m_UIWndStack?.Clear();
        m_uiContains?.Clear();
    }

    public T OpenAlwaysOnTop<T,T1>(eUIType uiType, T1 param) where T : UIWndBase
    {
        UIWndBase ui = null;
        if (!m_uiContains.TryGetValue(uiType, out ui))
        {
            string uiPath = GetUIPath(uiType);
            GameObject uiPrefab = Resources.Load(uiPath) as GameObject;
            if (uiPrefab != null)
            {
                ui = Instantiate(uiPrefab).GetComponent<UIWndBase>();
                DontDestroyOnLoad(ui);
                ui.gameObject.name = uiPrefab.name;
                RectTransform rect = ui.gameObject.GetComponent<RectTransform>();
                rect.SetParent(GetTargetCanvas(eUILayerType.AlwaysOnTop).gameObject.transform);
                rect.offsetMax = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                rect.localPosition = Vector3.zero;
                rect.localScale = Vector3.one;
                rect.rotation = Quaternion.identity;
                m_uiContains.Add(uiType, ui);

                ui.Init();
            }
        }
        ui.Open();
        return ui as T;
    }

    public T Open<T, T1>(eUIType uiType, T1 param) where T : UIWndBase
    {
        eUILayerType layerType = GetLayerType(uiType);

        if(layerType == eUILayerType.AlwaysOnTop)
        {
            var aways = OpenAlwaysOnTop<T, T1>(uiType, param);
            return aways;
        }

        if (m_curUI != null)
        {
            m_curUI.Close();
        }

        UIWndBase ui = null;

        if (!m_uiContains.TryGetValue(uiType, out ui))
        {
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
                rect.localScale = Vector3.one;
                rect.rotation = Quaternion.identity;
                m_uiContains.Add(uiType, ui);

                ui.Init();
            }
        }

        m_UIWndStack.Add(ui);
        ui.Open();

        m_curUI = ui;

        return ui as T;
    }

    public void Close(eUIType eUIType)
    {
        if(m_UIWndStack.IsContain(eUIType))
        {
            m_UIWndStack.Pop(eUIType);
        }
    }

    string GetUIPath(eUIType uiType)
    {
        var pathInfos = UIPathInfo.m_pathInfoDic;
        if (pathInfos != null && pathInfos.TryGetValue(uiType, out string path))
            return path;

        return null;
    }

    public static eUILayerType GetLayerType(eUIType type)
    {
        return (eUILayerType)((int)type >> 16);
    }
}