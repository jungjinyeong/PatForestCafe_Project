using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Extension;
using Cysharp.Threading.Tasks;
using UniRx;

public class UIScrollDrinkMaterialData
{
    public int Tid;
    public string Name;
}

public class UIScrollDrinkMaterial : UIScrollRow<UIScrollDrinkMaterialData>
{
    [SerializeField] private TextMeshProUGUI mName;
    [SerializeField] private UIButtonEx mButton;

    public UIScrollDrinkMaterialData CurrentData { get; private set; }

    private void Awake()
    {
        mButton.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollDrinkMaterialData data)
    {
        CurrentData = data;
        if (data == null) return;

        mName.SetTextEx(data.Name);
    }
}
