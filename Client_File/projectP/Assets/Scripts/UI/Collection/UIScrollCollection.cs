using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Extension;

public class UIScrollCollectionData
{
    public int Tid;
    public int Number;          // 도감 번호(전체 목록 기준 1부터)
    public bool IsDrink;        // true = 음료(DrinkRow), false = 디저트(BreadRow)
    public bool Discovered;
}

// 도감 왼쪽 페이지의 레시피 1행: 번호 / 썸네일 / 분류·이름 / 발견 상태.
public class UIScrollCollection : UIScrollRow<UIScrollCollectionData>
{
    private const string UNDISCOVERED_NAME = "???";

    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private TextMeshProUGUI mTextNumber;
    [SerializeField] private Image mImageThumb;
    [SerializeField] private TextMeshProUGUI mTextCategory;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextStatus;
    [SerializeField] private GameObject mSelectedMark;

    public UIScrollCollectionData CurrentData { get; private set; }

    private void Awake()
    {
        mButton.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollCollectionData data)
    {
        CurrentData = data;
        SetSelected(false);
        if (data == null) return;

        var menuRow = GameInstance.Table.Get<CTable.MenuItemRow>(data.Tid);

        mTextNumber.SetTextEx(data.Number.ToString("00"));
        mTextCategory.SetTextEx(UIPopupCollection.GetCategoryName(data.IsDrink));
        mTextName.SetTextEx(data.Discovered ? menuRow?.Name ?? data.Tid.ToString() : UNDISCOVERED_NAME);
        mTextStatus.SetTextEx(data.Discovered ? "발견 완료" : "미발견");

        if (data.Discovered && menuRow != null)
            mImageThumb.SetSpriteEx(menuRow.Atlas, menuRow.Icon);
        else
            mImageThumb.enabled = false;
    }

    public void SetSelected(bool selected)
    {
        if (mSelectedMark != null)
            mSelectedMark.SetActive(selected);
    }
}
