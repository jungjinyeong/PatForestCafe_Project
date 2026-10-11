using System;
using TMPro;
using UniRx;
using UnityEngine;
using Extension;

// 가구 구입 목록의 카테고리 탭 묶음. 로비 가구배치 팝업(UIPopupFurnitureList)과 상점가 가구구입 팝업(UIPopupShopFurniture)이 같이 쓴다.
// 탭 순서 = CTable.eFurnitureCategory 값 순서(전체/진열대/테이블/의자/스탠드/장식/파티션). 창고/도감 탭과 같은 버튼+밑줄 구성.
public class UIFurnitureCategoryTabs : MonoBehaviour
{
    private static readonly string[] TAB_NAMES = { "전체", "진열대", "테이블", "의자", "스탠드", "장식", "파티션" };

    [SerializeField] private UIButtonEx[] mTabButtons;
    [SerializeField] private TextMeshProUGUI[] mTabTexts;
    [SerializeField] private GameObject[] mTabSelectedMarks;

    private Action<CTable.eFurnitureCategory> mOnSelect;

    public CTable.eFurnitureCategory Selected { get; private set; } = CTable.eFurnitureCategory.All;

    // 탭 클릭 시 onSelect를 부른다. 버튼 구독은 한 번만 한다(팝업 Init에서 호출).
    public void Init(Action<CTable.eFurnitureCategory> onSelect)
    {
        mOnSelect = onSelect;

        for (int i = 0; i < mTabButtons.Length; i++)
        {
            var category = (CTable.eFurnitureCategory)i;
            mTabButtons[i].OnSubscribeOnClick(() => Select(category)).AddTo(this);
        }
    }

    public void Select(CTable.eFurnitureCategory category)
    {
        Selected = category;
        Refresh();
        mOnSelect?.Invoke(category);
    }

    private void Refresh()
    {
        for (int i = 0; i < mTabTexts.Length && i < TAB_NAMES.Length; i++)
            mTabTexts[i].SetTextEx(TAB_NAMES[i]);

        for (int i = 0; i < mTabSelectedMarks.Length; i++)
            mTabSelectedMarks[i].SetActive(i == (int)Selected);
    }
}
