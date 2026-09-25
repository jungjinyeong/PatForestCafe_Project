using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 커피머신 재료 슬롯을 눌렀을 때 뜨는 재료 선택 모달. 재료를 누르면 바로 슬롯에 들어간다.
public class UIDrinkMaterialPicker : UIBase
{
    [SerializeField] private TextMeshProUGUI mTextTitle;
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mMaterialRowPrefab;
    [SerializeField] private UIButtonEx mBtnRemove;
    [SerializeField] private UIButtonEx mBtnClose;
    [SerializeField] private UIButtonEx mBtnShade;

    private Action<int> mOnPick;

    public void Init(Action<int> onPick, Action onRemove)
    {
        mOnPick = onPick;

        mScrollEx.Init(mMaterialRowPrefab);
        mScrollEx.SetOnSelect(OnSelectMaterial);

        mBtnRemove.OnSubscribeOnClick(onRemove).AddTo(this);
        mBtnClose.OnSubscribeOnClick(Close).AddTo(this);
        if (mBtnShade != null)
            mBtnShade.OnSubscribeOnClick(Close).AddTo(this);

        Deative();
    }

    public void Open(string title, IList<UIScrollDrinkMaterialData> materials, bool canRemove)
    {
        Active();

        mTextTitle.SetTextEx(title);
        mBtnRemove.interactable = canRemove;
        mScrollEx.SetData((System.Collections.IList)materials);
        mScrollEx.ScrollToTop();
    }

    public void Close()
    {
        Deative();
    }

    private void OnSelectMaterial(UIScrollRow row)
    {
        if (row is not UIScrollDrinkMaterial materialRow || materialRow.CurrentData == null)
            return;

        if (materialRow.CurrentData.RemainCount <= 0)
            return;

        mOnPick?.Invoke(materialRow.CurrentData.Tid);
    }
}
