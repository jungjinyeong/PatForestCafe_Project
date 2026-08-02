using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Extension;
using UniRx;

public class UIScrollBreadData
{
    public int Tid;
    public string Name;
}

public class UIScrollBread : UIScrollRow<UIScrollBreadData>
{
    [SerializeField] private TextMeshProUGUI mName;
    [SerializeField] private UIButtonEx mButton;

    public UIScrollBreadData CurrentData { get; private set; }

    private void Awake()
    {
        mButton.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollBreadData data)
    {
        CurrentData = data;
        if (data == null) return;

        mName.SetTextEx(data.Name);
    }
}
