using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class UIFurnitureList : MonoBehaviour
{
    [SerializeField] private GameObject mRoot;
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mFurnitureRowPrefab;

    private LobbyFloorCameraController mFloorCamera;

    public void Init(LobbyFloorCameraController floorCamera)
    {
        mFloorCamera = floorCamera;

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

        // 층마다 별도 PlacementGridArea가 있으므로 "지금 카메라가 보고 있는 층"에 배치한다.
        var area = mFloorCamera != null ? mFloorCamera.CurrentFloorArea : null;
        if (area == null)
        {
            Logger.Warning("[UIFurnitureList] 현재 층의 PlacementGridArea를 찾을 수 없습니다.");
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
        var placeable = instance.GetComponent<PlaceableObject>();
        placeable.SetFurnitureTid(furnitureTableRow.Tid);
        placeable.BeginPlacementFromSpawn(area);
    }

    // 세이브에 저장된 배치 목록(PlacementModel.GetAllPlacements())을 그대로 씬에 재생성한다.
    // 드래그/골드 소모가 필요 없는 복원 전용 경로라 OnClickAdd와는 별도로 둔다.
    public void RespawnSavedFurniture()
    {
        // 층마다 별도 PlacementGridArea가 있어 "아무 영역이나 하나" 쓰면 안 된다 — 저장된 절대좌표를
        // 실제로 포함하는 영역을 찾아 그 층에 맞게 연결해야, 이후 재드래그 시 올바른 층 범위 안에서 스냅된다.
        var allAreas = FindObjectsByType<PlacementGridArea>(FindObjectsSortMode.None);

        foreach (var kvp in GameInstance.Model.Placement.GetAllPlacements())
        {
            int placementId = kvp.Key;
            var record = kvp.Value;

            var furnitureTableRow = GameInstance.Table.Get<CTable.FurnitureRow>(record.Tid);
            if (furnitureTableRow == null)
            {
                Logger.Warning($"[UIFurnitureList] 저장된 가구 Tid를 찾을 수 없습니다. Tid={record.Tid}");
                continue;
            }

            var prefab = GameInstance.Resource.LoadSync<GameObject>(furnitureTableRow.PrefabPath);
            if (prefab == null)
            {
                Logger.Warning($"[UIFurnitureList] 가구 프리팹을 찾을 수 없습니다. Path: {furnitureTableRow.PrefabPath}");
                continue;
            }

            var instance = Instantiate(prefab, record.Position, Quaternion.identity);
            var placeable = instance.GetComponent<PlaceableObject>();
            if (placeable == null)
                continue;

            placeable.SetFurnitureTid(record.Tid);
            placeable.SetPlacementId(placementId);
            placeable.SetArea(FindAreaContaining(allAreas, record.Position));
        }
    }

    private PlacementGridArea FindAreaContaining(PlacementGridArea[] areas, Vector3 position)
    {
        foreach (var area in areas)
        {
            if (area != null && area.Contains(position))
                return area;
        }

        Logger.Warning($"[UIFurnitureList] 위치 {position}를 포함하는 PlacementGridArea를 찾지 못했습니다. 복원된 가구는 재드래그가 불가능합니다.");
        return null;
    }
}
