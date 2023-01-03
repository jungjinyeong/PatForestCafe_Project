using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Framework.UI;

public enum eUIType
{
    Min = 0,

    UIRootIntro = eUILayerType.Menu << 0,
    UIRootLobby,

    Max,
}

public enum eUILayerType
{
    Min = 0,
    Menu,
    Popup,
}

public class UIManager : MonoBehaviour
{
    List<UIBase> m_uiStack = new List<UIBase>();
    Dictionary<eUIType, UIBase> m_uiContains = new Dictionary<eUIType, UIBase>();
    Dictionary<eUIType, string> m_uiPaths;

    [SerializeField]
    Canvas mMainCanvas = null;


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

    public T Open<T, T1>(eUIType uiType, T1 param) where T : UIBase
    {
        UIBase ui = null;
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
                ui = Instantiate(uiPrefab).GetComponent<UIBase>();
                DontDestroyOnLoad(ui);
                RectTransform rect = ui.gameObject.GetComponent<RectTransform>();
                rect.SetParent(mMainCanvas.gameObject.transform);
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

    void SetUIPath()
    {
        m_uiPaths = new Dictionary<eUIType, string>()
        {
            { eUIType.UIRootIntro, "Prefabs/UI/ui_root_intro"},
            { eUIType.UIRootLobby, "Prefabs/UI/ui_ingameInfo"},
        };
    }

    string GetUIPath(eUIType uiType)
    {
        if (m_uiPaths != null && m_uiPaths.TryGetValue(uiType, out string path))
            return path;

        return null;
    }
}