using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 오븐 재료 선택 모달. 기본 재료 칸은 같은 분류(Category) 안에서 등급만 교체하고, 추가 재료 칸은 모든 빵 재료를 고른다.
public class UIBreadMaterialPicker : UIBase
{
    [SerializeField] private TextMeshProUGUI mTextTitle;
    [SerializeField] private TextMeshProUGUI mTextNote;
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mMaterialRowPrefab;
    [SerializeField] private UIButtonEx mBtnClear;
    [SerializeField] private UIButtonEx mBtnClose;
    [SerializeField] private UIButtonEx mBtnShade;

    private Action<int> mOnPick;

    public void Init(Action<int> onPick, Action onClear)
    {
        mOnPick = onPick;

        mScrollEx.Init(mMaterialRowPrefab);
        mScrollEx.SetOnSelect(OnSelectMaterial);

        mBtnClear.OnSubscribeOnClick(onClear).AddTo(this);
        mBtnClose.OnSubscribeOnClick(Close).AddTo(this);
        if (mBtnShade != null)
            mBtnShade.OnSubscribeOnClick(Close).AddTo(this);

        Deative();
    }

    public void Open(string title, string note, List<UIScrollBreadMaterialData> materials, bool canClear)
    {
        Active();

        mTextTitle.SetTextEx(title);
        mTextNote.SetTextEx(note);
        mBtnClear.gameObject.SetActive(canClear);
        mScrollEx.SetData(materials);
        mScrollEx.ScrollToTop();
    }

    public void Close()
    {
        Deative();
    }

    private void OnSelectMaterial(UIScrollRow row)
    {
        if (row is not UIScrollBreadMaterial materialRow || materialRow.CurrentData == null)
            return;

        mOnPick?.Invoke(materialRow.CurrentData.Tid);
    }
}
