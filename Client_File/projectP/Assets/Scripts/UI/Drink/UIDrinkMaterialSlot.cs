using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Extension;

// 커피머신 기본/커스텀 재료 슬롯 1칸. 클릭하면 부모(UIPopupDrinkRecipeProduction)가 재료 선택 모달을 연다.
public class UIDrinkMaterialSlot : UIBase
{
    private const string EMPTY_NAME = "재료 선택";

    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private Image mImgIcon;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private GameObject mFilledRoot;
    [SerializeField] private GameObject mEmptyRoot;

    public int MaterialTid { get; private set; }
    public bool IsFilled => MaterialTid != 0;

    public void Init(Action onClick)
    {
        mButton.OnSubscribeOnClick(onClick).AddTo(this);
    }

    public void Set(int materialTid)
    {
        MaterialTid = materialTid;

        var row = materialTid != 0 ? GameInstance.Table.Get<CTable.DrinkMaterialRow>(materialTid) : null;

        mTextName.SetTextEx(row != null ? row.Name : EMPTY_NAME);
        mImgIcon.SetSpriteEx(row?.Atlas, row?.Icon);

        if (mFilledRoot != null) mFilledRoot.SetActive(row != null);
        if (mEmptyRoot != null) mEmptyRoot.SetActive(row == null);
    }
}
