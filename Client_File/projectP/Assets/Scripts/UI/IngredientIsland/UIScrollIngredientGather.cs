using UnityEngine;
using TMPro;
using UniRx;
using Extension;

public class UIScrollIngredientGatherData
{
    public int Tid;
    public string Name;
    public int Count;
}

public class UIScrollIngredientGather : UIScrollRow<UIScrollIngredientGatherData>
{
    [SerializeField] private TextMeshProUGUI mName;
    [SerializeField] private TextMeshProUGUI mCount;
    [SerializeField] private UIButtonEx mBtnGather;

    public UIScrollIngredientGatherData CurrentData { get; private set; }

    private void Awake()
    {
        mBtnGather.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollIngredientGatherData data)
    {
        CurrentData = data;
        if (data == null) return;

        mName.SetTextEx(data.Name);
        mCount.SetTextEx(data.Count.ToString());
    }
}
