using UnityEngine;
using TMPro;
using UniRx;
using Extension;

public class UIScrollMaterialGatherData
{
    public int Tid;
    public string Name;
    public int Count;
}

public class UIScrollMaterialGather : UIScrollRow<UIScrollMaterialGatherData>
{
    [SerializeField] private TextMeshProUGUI mName;
    [SerializeField] private TextMeshProUGUI mCount;
    [SerializeField] private UIButtonEx mBtnGather;

    public UIScrollMaterialGatherData CurrentData { get; private set; }

    private void Awake()
    {
        mBtnGather.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollMaterialGatherData data)
    {
        CurrentData = data;
        if (data == null) return;

        mName.SetTextEx(data.Name);
        mCount.SetTextEx(data.Count.ToString());
    }
}
