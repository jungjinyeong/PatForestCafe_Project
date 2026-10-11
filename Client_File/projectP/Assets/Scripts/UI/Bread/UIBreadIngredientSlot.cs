using System;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 오븐 반죽 설정의 빵 재료 슬롯 1칸(기본/추가 재료). 등급(하/중/고급) 뱃지를 함께 보여준다.
public class UIBreadIngredientSlot : UIBase
{
    private const string EMPTY_NAME = "재료 선택";

    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextGrade;
    [SerializeField] private GameObject mFilledRoot;
    [SerializeField] private GameObject mEmptyRoot;

    public int IngredientTid { get; private set; }
    public bool IsFilled => IngredientTid != 0;

    public void Init(Action onClick)
    {
        mButton.OnSubscribeOnClick(onClick).AddTo(this);
    }

    public void SetInteractable(bool interactable)
    {
        mButton.interactable = interactable;
    }

    public void Set(int ingredientTid)
    {
        IngredientTid = ingredientTid;

        var row = ingredientTid != 0 ? GameInstance.Table.Get<CTable.BreadIngredientRow>(ingredientTid) : null;
        mTextName.SetTextEx(row != null ? row.Name : EMPTY_NAME);

        if (row != null)
        {
            var quality = BreadQuality.FromGrade(row.Grade);
            mTextGrade.SetTextEx($"<color={BreadQuality.GetColorHex(quality)}>{BreadQuality.GetName(quality)}</color>");
        }
        else
        {
            mTextGrade.SetTextEx(string.Empty);
        }

        if (mFilledRoot != null) mFilledRoot.SetActive(row != null);
        if (mEmptyRoot != null) mEmptyRoot.SetActive(row == null);
    }
}
