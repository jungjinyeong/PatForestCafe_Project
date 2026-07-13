using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

public class UIPopupSpecialDrinkProduction : UIWndBase, IUIParam<UIPopupSpecialDrinkProduction.Param>
{
    public struct Param { }

    [Header("Scroll")]
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mDrinkMaterialRowPrefab;

    public override eUIType GetUIType() => eUIType.UIPopupSpecialDrinkProduction;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mDrinkMaterialRowPrefab);
        mScrollEx.SetOnSelect(OnSelectMaterial);
    }

    public override void Open()
    {
        base.Open();

        SetupMaterialScroll();
    }

    public void Set(Param param) { }

    private void SetupMaterialScroll()
    {
        var group = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>();
        if (group == null)
        {
            Logger.Warning("[UIPopupSpecialDrinkProduction] DrinkMaterialGroup을 찾을 수 없습니다.");
            return;
        }

        var dataList = new List<UIScrollDrinkMaterialData>();
        foreach (var row in group.All.Values)
        {
            dataList.Add(new UIScrollDrinkMaterialData
            {
                Tid = row.Tid,
                Name = row.Name,
            });
        }

        mScrollEx.SetData(dataList);
    }

    private void OnSelectMaterial(UIScrollRow row)
    {
        if (row is UIScrollDrinkMaterial materialRow && materialRow.CurrentData != null)
            Logger.Log($"[UIPopupSpecialDrinkProduction] 선택된 재료: Tid={materialRow.CurrentData.Tid}, Name={materialRow.CurrentData.Name}");
    }
}
