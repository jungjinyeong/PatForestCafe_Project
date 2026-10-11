using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;
using UnityEngine.Serialization;

// 커피머신 재료 슬롯을 눌렀을 때 뜨는 재료 선택 모달. 재료를 누르면 바로 슬롯에 들어간다.
public class UIDrinkIngredientPicker : UIBase
{
    [SerializeField] private TextMeshProUGUI mTextTitle;
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField, FormerlySerializedAs("mMaterialRowPrefab")] private GameObject mIngredientRowPrefab;
    [SerializeField] private UIButtonEx mBtnRemove;
    [SerializeField] private UIButtonEx mBtnClose;
    [SerializeField] private UIButtonEx mBtnShade;

    private Action<int> mOnPick;

    public void Init(Action<int> onPick, Action onRemove)
    {
        mOnPick = onPick;

        mScrollEx.Init(mIngredientRowPrefab);
        mScrollEx.SetOnSelect(OnSelectIngredient);

        mBtnRemove.OnSubscribeOnClick(onRemove).AddTo(this);
        mBtnClose.OnSubscribeOnClick(Close).AddTo(this);
        if (mBtnShade != null)
            mBtnShade.OnSubscribeOnClick(Close).AddTo(this);

        Deative();
    }

    public void Open(string title, IList<UIScrollDrinkIngredientData> ingredients, bool canRemove)
    {
        Active();

        mTextTitle.SetTextEx(title);
        mBtnRemove.interactable = canRemove;
        mScrollEx.SetData((System.Collections.IList)ingredients);
        mScrollEx.ScrollToTop();
    }

    public void Close()
    {
        Deative();
    }

    private void OnSelectIngredient(UIScrollRow row)
    {
        if (row is not UIScrollDrinkIngredient ingredientRow || ingredientRow.CurrentData == null)
            return;

        if (ingredientRow.CurrentData.RemainCount <= 0)
            return;

        mOnPick?.Invoke(ingredientRow.CurrentData.Tid);
    }
}
