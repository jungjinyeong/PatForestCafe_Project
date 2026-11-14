using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Extension;

// 로비 패널 도감 화면(UISideCollectionView)의 레시피 한 칸: 번호 / 그림(없으면 "?") / 이름(미발견 "???").
public class UISideCollectionCell : UIBase
{
    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private Image mImageIcon;
    [SerializeField] private TextMeshProUGUI mTextGlyph;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextNumber;
    [Tooltip("미발견 칸을 흐리게 할 그룹. 비어 있어도 된다.")]
    [SerializeField] private CanvasGroup mCanvasGroup;

    public UIScrollCollectionData Data { get; private set; }

    private Action<UISideCollectionCell> mOnSelect;
    private bool mIsBound;

    public void Bind(Action<UISideCollectionCell> onSelect)
    {
        mOnSelect = onSelect;
        if (mIsBound) return;

        mIsBound = true;
        mButton.OnSubscribeOnClick(() => mOnSelect?.Invoke(this)).AddTo(this);
    }

    public void SetData(UIScrollCollectionData data)
    {
        Data = data;
        var menuRow = data.Discovered ? GameInstance.Table.Get<CTable.MenuItemRow>(data.Tid) : null;

        mTextNumber.SetTextEx(data.Number.ToString("00"));
        mTextName.SetTextEx(data.Discovered ? menuRow?.Name ?? data.Tid.ToString() : "???");

        if (menuRow != null)
            mImageIcon.SetSpriteEx(menuRow.Atlas, menuRow.Icon);
        else
            mImageIcon.enabled = false;

        // 그림을 못 띄우면(미발견, 아틀라스 없음) 글자로 대신한다.
        bool hasIcon = mImageIcon.enabled && mImageIcon.sprite != null;
        mTextGlyph.gameObject.SetActive(!hasIcon);
        if (!hasIcon)
            mTextGlyph.SetTextEx(data.Discovered && !string.IsNullOrEmpty(menuRow?.Name) ? menuRow.Name.Substring(0, 1) : "?");

        if (mCanvasGroup != null)
            mCanvasGroup.alpha = data.Discovered ? 1f : 0.55f;
    }
}
