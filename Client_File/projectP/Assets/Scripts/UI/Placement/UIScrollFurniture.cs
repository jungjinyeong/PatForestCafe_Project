using UnityEngine;
using TMPro;
using Extension;
using UniRx;

public class UIScrollFurnitureData
{
    public int Tid;
    public string Name;
    public long Price;
}

public class UIScrollFurniture : UIScrollRow<UIScrollFurnitureData>
{
    [SerializeField] private TextMeshProUGUI mName;
    [SerializeField] private TextMeshProUGUI mPrice;
    [SerializeField] private UIButtonEx mBtnAdd;

    public UIScrollFurnitureData CurrentData { get; private set; }

    private void Awake()
    {
        mBtnAdd.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollFurnitureData data)
    {
        CurrentData = data;
        if (data == null) return;

        mName.SetTextEx(data.Name);
        mPrice.SetTextEx(data.Price.ToString());
    }
}
