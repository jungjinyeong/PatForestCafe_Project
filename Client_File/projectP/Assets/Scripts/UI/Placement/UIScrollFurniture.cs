using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Extension;
using UniRx;

public class UIScrollFurnitureData
{
    public int Tid;
    public string Name;
    public long Price;
    // true면 CTable.SubFurnitureRow, false면 CTable.FurnitureRow 소속 Tid.
    public bool IsSub;
    // 구입 목록 탭 분류(표의 Category).
    public CTable.eFurnitureCategory Category;
    // 차지하는 칸 수(가로×세로). 목록에 "1×3"처럼 표시한다.
    public int Width;
    public int Height;
    // 보관 중(산 뒤 배치하지 않은) 개수. 0보다 크면 배치 목록에서 골드 없이 꺼내 놓는다.
    public int StoredCount;
    // 이미 산 무제한 설치 가구(바닥타일·벽지). true면 골드 없이 몇 개든 놓는다.
    public bool IsUnlimitedUnlocked;

    public string SizeText => $"{Width}×{Height}";
}

public class UIScrollFurniture : UIScrollRow<UIScrollFurnitureData>
{
    [SerializeField] private Image mIcon;
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

        // 창고처럼 왼쪽에 가구 그림을 보여준다(비율 유지). 그림이 없으면 칸을 숨긴다.
        if (mIcon != null)
        {
            var icon = GameInstance.Model.Placement.GetFurnitureIcon(data.Tid, data.IsSub);
            mIcon.sprite = icon;
            mIcon.preserveAspect = true;
            mIcon.enabled = icon != null;
        }

        // 픽셀 글꼴은 정수 크기에서만 선명해서 <size=%>로 줄이지 않는다(두 줄 같은 크기).
        mName.SetTextEx($"{data.Name}\n{data.SizeText}");
        // 이미 산 바닥타일·벽지는 "보유", 보관 중인 가구가 있으면 보관 수를 가격 대신 보여준다(누르면 골드 없이 배치).
        if (data.IsUnlimitedUnlocked)
            mPrice.SetTextEx("보유");
        else
            mPrice.SetTextEx(data.StoredCount > 0 ? $"보관 {data.StoredCount}" : data.Price.ToString());
    }
}
