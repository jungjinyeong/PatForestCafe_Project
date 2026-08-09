using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Extension;

public class UIRootMaterialIsland : UIWndBase
{
    public struct Param { }

    [Header("Scroll")]
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mMaterialGatherRowPrefab;

    [Header("Buttons")]
    [SerializeField] private UIButtonEx mBtnOpenBreadMinigame;
    [SerializeField] private UIButtonEx mBtnOpenBreadProduction;
    [SerializeField] private UIButtonEx mBtnOpenMaterialShop;
    [SerializeField] private UIButtonEx mBtnOpenWorkshop;

    public override eUIType GetUIType() => eUIType.UIRootMaterialIsland;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mMaterialGatherRowPrefab);
        mScrollEx.SetOnSelect(OnSelectMaterial);

        mBtnOpenBreadMinigame.OnSubscribeOnClick(OnClickOpenBreadMinigame).AddTo(this);
        mBtnOpenBreadProduction.OnSubscribeOnClick(OnClickOpenBreadProduction).AddTo(this);
        mBtnOpenMaterialShop.OnSubscribeOnClick(OnClickOpenMaterialShop).AddTo(this);
        mBtnOpenWorkshop.OnSubscribeOnClick(OnClickOpenWorkshop).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        RefreshMaterialList();
    }

    public override void Destroy()
    {
        base.Destroy();
    }

    private void OnSelectMaterial(UIScrollRow row)
    {
        if (row is not UIScrollMaterialGather materialRow || materialRow.CurrentData == null)
            return;

        GameInstance.Model.Material.Gather(materialRow.CurrentData.Tid);
        RefreshMaterialList();
    }

    private void OnClickOpenBreadMinigame()
    {
        GameInstance.UI.Open<UIPopupBreadMinigame, UIPopupBreadMinigame.Param>(eUIType.UIPopupBreadMinigame, new UIPopupBreadMinigame.Param());
    }

    private void OnClickOpenBreadProduction()
    {
        GameInstance.UI.Open<UIPopupBreadProduction, UIPopupBreadProduction.Param>(eUIType.UIPopupBreadProduction, new UIPopupBreadProduction.Param());
    }

    private void OnClickOpenMaterialShop()
    {
        GameInstance.UI.Open<UIPopupMaterialShop, UIPopupMaterialShop.Param>(eUIType.UIPopupMaterialShop, new UIPopupMaterialShop.Param());
    }

    private void OnClickOpenWorkshop()
    {
        GameInstance.UI.Open<UIPopupWorkshop, UIPopupWorkshop.Param>(eUIType.UIPopupWorkshop, new UIPopupWorkshop.Param());
    }

    // 클릭 채집 목록은 음료 재료 전용이다. 빵 재료는 UIPopupBreadMinigame으로만 획득한다.
    private void RefreshMaterialList()
    {
        var group = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>();
        if (group == null)
        {
            Logger.Warning("[UIRootMaterialIsland] DrinkMaterialGroup을 찾을 수 없습니다.");
            return;
        }

        var dataList = new List<UIScrollMaterialGatherData>();
        foreach (var row in group.All.Values)
        {
            var material = GameInstance.Model.Material.Get(row.Tid);
            dataList.Add(new UIScrollMaterialGatherData
            {
                Tid = row.Tid,
                Name = row.Name,
                Count = material?.Count.Value ?? 0,
            });
        }

        mScrollEx.SetData(dataList);
    }
}
