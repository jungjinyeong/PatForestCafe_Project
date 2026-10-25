using System;
using TMPro;
using UniRx;
using UnityEngine;
using Extension;

public class UIWarehouseCellData
{
    public int Tid;
    public bool IsDessert;
    public string Name;
    public int Count;
    // 좌상단 뱃지(재료 등급 등). 비어 있으면 숨긴다.
    public string Badge;
}

// 창고(UIPopupWarehouse) 그리드 한 칸. 아이콘 리소스가 없어 이름 첫 글자를 대신 표시한다.
// 오븐 제작 팝업의 재료 선택창·빵 재고 그리드에서도 같은 셀을 쓴다.
public class UIWarehouseCell : UIBase
{
    [SerializeField] private UIButtonEx mBtnSelect;
    [SerializeField] private TextMeshProUGUI mTextGlyph;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextCount;
    [SerializeField] private GameObject mSelectedMark;
    [SerializeField] private TextMeshProUGUI mTextBadge;

    public UIWarehouseCellData Data { get; private set; }

    private Action<UIWarehouseCell> mOnSelect;
    private bool mIsBound;

    public void Bind(Action<UIWarehouseCell> onSelect)
    {
        mOnSelect = onSelect;
        if (mIsBound) return;

        mIsBound = true;
        mBtnSelect.OnSubscribeOnClick(() => mOnSelect?.Invoke(this)).AddTo(this);
    }

    public void SetData(UIWarehouseCellData data)
    {
        Data = data;
        mTextGlyph.SetTextEx(string.IsNullOrEmpty(data.Name) ? "?" : data.Name.Substring(0, 1));
        mTextName.SetTextEx(data.Name);
        mTextCount.SetTextEx($"×{data.Count}");

        if (mTextBadge != null)
        {
            mTextBadge.gameObject.SetActive(!string.IsNullOrEmpty(data.Badge));
            mTextBadge.SetTextEx(data.Badge);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (mSelectedMark != null)
            mSelectedMark.SetActive(isSelected);
    }
}
