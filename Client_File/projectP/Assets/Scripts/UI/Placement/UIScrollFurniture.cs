using UnityEngine;
using TMPro;
using Extension;
using UniRx;

public class UIScrollFurnitureData
{
    public int Tid;
    public string Name;
    public long Price;
    public string PrefabPath;
    // true면 CTable.SubFurnitureRow, false면 CTable.FurnitureRow 소속 Tid.
    public bool IsSub;
    // 구입 목록 탭 분류(표의 Category).
    public CTable.eFurnitureCategory Category;
    // 차지하는 칸 수(가로×세로). 목록에 "1×3"처럼 표시한다.
    public int Width;
    public int Height;

    public string SizeText => $"{Width}×{Height}";
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

        mName.SetTextEx($"{data.Name}\n<size=70%>{data.SizeText}</size>");
        mPrice.SetTextEx(data.Price.ToString());
    }
}
