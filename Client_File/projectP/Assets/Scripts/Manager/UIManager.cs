using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;

[Serializable]
public enum eUIType
{
    Min = 0,

    UIRootIntro = eUILayerType.Menu << 16,
    UIRootLobby,
    UIRootLogin,

    UIPopupOption = eUILayerType.Popup << 16,
    UIPopupSpecialDrinkProduction,
    PopupOrderDetail,

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

    UIWndStack mUIWndStack;

    // test code
    [SerializeField]
    SerializableDictionary<eUIType, UIWndBase> mCachedUIDic = new SerializableDictionary<eUIType, UIWndBase>();

    Dictionary<eUIType, UIWndBase> mUiContains = new Dictionary<eUIType, UIWndBase>();
    UIWndBase mCurUI = null;

    private UIPathInfo mUiPathInfo = null;
    public UIPathInfo UIPathInfo
    {
        get
        {
            if (mUiPathInfo == null)
            {
                mUiPathInfo = UnityEngine.Resources.Load<UIPathInfo>("UIPathInfo");
            }
            return mUiPathInfo;
        }
    }

    [SerializeField]
    SerializableDictionary<eUILayerType, Canvas> mTargetCanvasDic = new SerializableDictionary<eUILayerType, Canvas>();

    [SerializeField]
    UIHudController mHudController;
    public UIHudController HudController => mHudController;

    public Canvas GetTargetCanvas(eUILayerType type)
    {
        if (mTargetCanvasDic.TryGetValue(type, out var canvas))
            return canvas;
        else
        {
            Logger.Error($"ui layer type : {type} ::: target canvas is null");
            return null;
        }
    }

    static UIManager mInstance;
    public static UIManager Instance { get { return mInstance; } }

    void Awake()
    {
        Logger.Log("awake ui");
        mInstance = this;
        DontDestroyOnLoad(this);
        mUIWndStack = new UIWndStack();

        ConnectMainCamera();
    }

    private void ConnectMainCamera()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Logger.Error("UIManager::ConnectMainCamera - MainCamera를 찾을 수 없습니다.");
            return;
        }

        foreach (var canvas in mTargetCanvasDic.Values)
        {
            if (canvas == null)
                continue;

            canvas.worldCamera = mainCamera;
        }
    }

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        foreach(var ui in mCachedUIDic)
        {
            // 이미 Open()되어 mUiContains에 등록된 UI는 꺼진 상태로 되돌리지 않는다.
            if (mUiContains.ContainsKey(ui.Key))
                continue;

            ui.Value.gameObject.SetActive(false);
        }
    }

    public void Destroy()
    {
        mUiPathInfo = null;

        mUiContains?.Clear();
        mUiContains = null;

        mUIWndStack?.Clear();
    }

    /// <summary>
    /// 씬 이동 시 ui clear
    /// </summary>
    public void Clear()
    {
        foreach (var ui in mUiContains)
        {
            GameObject.Destroy(ui.Value.gameObject);
        }

        mUIWndStack?.Clear();
        mUiContains?.Clear();
    }

    public T OpenAlwaysOnTop<T, T1>(eUIType uiType, T1 param) where T : UIWndBase
    {
        UIWndBase ui = null;
        if (!mUiContains.TryGetValue(uiType, out ui))
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
                mUiContains.Add(uiType, ui);

                ui.Init();
            }
        }
        ui.transform.SetAsLastSibling();
        ui.Open();
        return ui as T;
    }

    public T Open<T, T1>(eUIType uiType, T1 param) where T : UIWndBase
        where T1 : struct
    {
        eUILayerType layerType = GetLayerType(uiType);

        if(layerType == eUILayerType.AlwaysOnTop)
        {
            var aways = OpenAlwaysOnTop<T, T1>(uiType, param);
            return aways;
        }

        if (layerType == eUILayerType.Menu && mCurUI != null)
        {
            mCurUI.Close();
        }

        UIWndBase ui = null;

        if (!mUiContains.TryGetValue(uiType, out ui))
        {
            if(mCachedUIDic.TryGetValue(uiType, out var cachedUI))
            {
                if (cachedUI != null)
                {
                    ui = cachedUI;
                }
            }
            else
            {
                string uiPath = GetUIPath(uiType);
                GameObject uiPrefab = Resources.Load(uiPath) as GameObject;
                if (uiPrefab != null)
                {
                    ui = Instantiate(uiPrefab).GetComponent<UIWndBase>();
                    ui.gameObject.name = uiPrefab.name;
                }
            }
            if (null == ui)
                return null;

            DontDestroyOnLoad(ui);
            RectTransform rect = ui.gameObject.GetComponent<RectTransform>();
            rect.SetParent(GetTargetCanvas(layerType).gameObject.transform);
            rect.offsetMax = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.localPosition = Vector3.zero;
            rect.localScale = Vector3.one;
            rect.rotation = Quaternion.identity;
            mUiContains.Add(uiType, ui);

            ui.Init();
        }

        mUIWndStack.Add(ui);
        ui.transform.SetAsLastSibling();
        if(ui is IUIParam<T1> uiParam)
            uiParam.Set(param);
        ui.Open();

        if (layerType == eUILayerType.Menu)
            mCurUI = ui;

        return ui as T;
    }

    public void Close(eUIType eUIType)
    {
        if(mUIWndStack.IsContain(eUIType))
        {
            mUIWndStack.Pop(eUIType);
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