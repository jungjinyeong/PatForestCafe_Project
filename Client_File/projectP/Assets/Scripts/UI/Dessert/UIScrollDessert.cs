using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Extension;
using UniRx;

public class UIScrollDessertData
{
    public int Tid;
    public string Name;

    // 2단계(빵 선택 후 배치할 테이블 목록)에서만 쓰인다 — 1단계(빵 종류 선택)에서는 기본값(false/null) 그대로 둔다.
    public bool ShowAddIcon;
    public Intaraction_DessertStand TargetStand;
}

public class UIScrollDessert : UIScrollRow<UIScrollDessertData>
{
    [SerializeField] private TextMeshProUGUI mName;
    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private GameObject mAddIcon;

    public UIScrollDessertData CurrentData { get; private set; }

    private void Awake()
    {
        mButton.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollDessertData data)
    {
        CurrentData = data;
        if (data == null) return;

        mName.SetTextEx(data.Name);

        if (mAddIcon != null)
            mAddIcon.SetActive(data.ShowAddIcon);
    }
}
