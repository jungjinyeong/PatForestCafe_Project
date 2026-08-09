using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class UIFurnitureList : MonoBehaviour
{
    [SerializeField] private GameObject mRoot;
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mFurnitureRowPrefab;

    public void Init()
    {
        mScrollEx.Init(mFurnitureRowPrefab);
        mScrollEx.SetOnSelect(OnClickAdd);

        GameInstance.Model.Placement.IsEditMode
            .Subscribe(OnEditModeChanged)
            .AddTo(this);
    }

    private void OnEditModeChanged(bool isEditMode)
    {
        mRoot.SetActive(isEditMode);

        if (isEditMode)
            RefreshList();
    }

    private void RefreshList()
    {
        var group = GameInstance.Table.GetTable<CTable.FurnitureRow>();
        if (group == null)
        {
            Logger.Warning("[UIFurnitureList] FurnitureGroup을 찾을 수 없습니다.");
            return;
        }

        var dataList = new List<UIScrollFurnitureData>();
        foreach (var row in group.All.Values)
        {
            dataList.Add(new UIScrollFurnitureData
            {
                Tid = row.Tid,
                Name = row.Name,
                Price = row.Price,
            });
        }

        mScrollEx.SetData(dataList);
    }

    private void OnClickAdd(UIScrollRow row)
    {
        if (row is not UIScrollFurniture furnitureRow || furnitureRow.CurrentData == null)
            return;

        if (GameInstance.Model.Placement.IsPlacing.Value)
            return;

        var furnitureTableRow = GameInstance.Table.Get<CTable.FurnitureRow>(furnitureRow.CurrentData.Tid);
        if (furnitureTableRow == null)
            return;

        var area = FindFirstObjectByType<PlacementGridArea>();
        if (area == null)
        {
            Logger.Warning("[UIFurnitureList] PlacementGridArea를 씬에서 찾을 수 없습니다.");
            return;
        }

        var prefab = GameInstance.Resource.LoadSync<GameObject>(furnitureTableRow.PrefabPath);
        if (prefab == null)
        {
            Logger.Warning($"[UIFurnitureList] 가구 프리팹을 찾을 수 없습니다. Path: {furnitureTableRow.PrefabPath}");
            return;
        }

        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        if (gold == null || gold.Count.Value < furnitureTableRow.Price)
        {
            Logger.Log("[UIFurnitureList] 골드가 부족합니다.");
            return;
        }

        gold.Consume((int)furnitureTableRow.Price);

        var instance = Instantiate(prefab, area.Bounds.center, Quaternion.identity);
        instance.GetComponent<PlaceableObject>().BeginPlacementFromSpawn(area);
    }
}
