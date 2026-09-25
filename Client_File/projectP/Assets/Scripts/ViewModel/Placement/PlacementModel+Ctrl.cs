using System.Collections.Generic;
using UnityEngine;

public partial class PlacementModel
{
    public IEnumerable<KeyValuePair<int, PlacedFurnitureRecord>> GetAllPlacements() => mDicPlacedFurniture;

    public int RegisterPlacement(int tid, Vector3 position, bool isSub = false)
    {
        int placementId = mNextPlacementId++;
        mDicPlacedFurniture[placementId] = new PlacedFurnitureRecord { Tid = tid, Position = position, IsSub = isSub };
        return placementId;
    }

    public void UpdatePlacementPosition(int placementId, Vector3 position)
    {
        if (mDicPlacedFurniture.TryGetValue(placementId, out var record))
            record.Position = position;
    }

    // 세이브 로드 복원 전용 — 저장된 id를 그대로 재사용해야 이후 이동 시 같은 레코드를 갱신한다.
    public void RestorePlacement(int placementId, int tid, Vector3 position, int assignedBreadTid = 0, bool isSub = false)
    {
        mDicPlacedFurniture[placementId] = new PlacedFurnitureRecord { Tid = tid, Position = position, AssignedBreadTid = assignedBreadTid, IsSub = isSub };

        if (placementId >= mNextPlacementId)
            mNextPlacementId = placementId + 1;
    }

    // Intaraction_BreadStand.TryAssignBreadType()이 최초 배정 시 호출 — 다음 세이브에 그대로 실린다.
    public void SetAssignedBreadTid(int placementId, int breadTid)
    {
        if (mDicPlacedFurniture.TryGetValue(placementId, out var record))
            record.AssignedBreadTid = breadTid;
    }

    // 가구배치 리셋 전용 — 배치 기록을 전부 비우고 placementId 발급 카운터도 처음부터 다시 시작한다.
    // 씬의 실제 가구 GameObject 제거는 호출부(ResetAllPlacedFurniture())가 함께 처리해야 한다.
    public void ClearAllPlacements()
    {
        mDicPlacedFurniture.Clear();
        mNextPlacementId = 1;
    }

    public void ToggleEditMode()
    {
        SetEditMode(!mIsEditMode.Value);
    }

    public void SetEditMode(bool isEdit)
    {
        // 배치 모드를 끄는데 드래그 중이었다면 원래 위치로 되돌리고 정리한다.
        if (!isEdit && mIsPlacing.Value)
            Cancel();

        mIsEditMode.Value = isEdit;
    }

    public void BeginPlacement(Transform target, TilePlacementArea area, Vector2Int footprint)
    {
        if (target == null || area == null)
            return;

        var placeable = target.GetComponent<PlaceableObject>();

        // 이미 확정된 가구를 재드래그하는 경우: 베이스인데 위에 sub가구가 올라가 있으면 먼저 치우게 하고 거부한다.
        // 그 외에는 스스로와의 겹침 오탐을 막기 위해 기존 점유를 먼저 해제해둔다(Confirm/Cancel에서 다시 점유).
        if (placeable != null && placeable.PlacementId >= 0)
        {
            if (!placeable.IsSubFurniture && area.HasAnySubOccupant(placeable.PlacementId))
            {
                Logger.Warning("[PlacementModel] 위에 sub가구가 있어 재배치할 수 없습니다. 먼저 sub가구를 치워주세요.");
                return;
            }

            area.ReleaseByPlacementId(placeable.PlacementId);
        }

        mTarget = target;
        mPlaceable = placeable;
        mArea = area;
        mFootprint = footprint;
        mOriginPosition = target.position;

        mIsValidPosition.Value = false;
        mIsPlacing.Value = true;

        // 놓을 수 있는 칸(면/레이어/그룹 조건)을 드래그 동안 강조한다.
        if (placeable != null && placeable.IsSubFurniture)
            area.ShowSubHighlight(placeable.AllowedGroupId, placeable.PlacementId);
        else if (placeable != null)
            area.ShowBaseHighlight(placeable.Surface, placeable.LayoutOrder, placeable.PlacementId);
    }

    public void UpdatePreview(Vector3 worldPos)
    {
        if (!mIsPlacing.Value || mTarget == null || mArea == null)
            return;

        bool isValid;
        Vector3 snappedPos;

        if (mPlaceable != null && mPlaceable.IsSubFurniture)
            isValid = mArea.TryGetSnappedPositionForSub(worldPos, mFootprint, mPlaceable.AllowedGroupId, mPlaceable.PlacementId, out snappedPos);
        else
            isValid = mArea.TryGetSnappedPositionForBase(worldPos, mFootprint, mPlaceable != null ? mPlaceable.PlacementId : -1,
                mPlaceable != null ? mPlaceable.Surface : ePlacementSurface.Floor, mPlaceable != null ? mPlaceable.LayoutOrder : 0, out snappedPos);

        mIsValidPosition.Value = isValid;
        mTarget.position = isValid ? snappedPos : worldPos;
    }

    public void Confirm()
    {
        if (!mIsPlacing.Value || !mIsValidPosition.Value)
            return;

        var placeable = mPlaceable;
        if (placeable != null && placeable.FurnitureTid > 0)
        {
            if (placeable.PlacementId < 0)
                placeable.SetPlacementId(RegisterPlacement(placeable.FurnitureTid, mTarget.position, placeable.IsSubFurniture));
            else
                UpdatePlacementPosition(placeable.PlacementId, mTarget.position);

            if (placeable.IsSubFurniture)
                mArea.OccupySub(mTarget.position, mFootprint, placeable.PlacementId);
            else
                mArea.OccupyBase(mTarget.position, mFootprint, placeable.PlacementId, placeable.GroupId, placeable.LayoutOrder);
        }

        mArea?.HideHighlight();
        mTarget = null;
        mPlaceable = null;
        mArea = null;
        mIsPlacing.Value = false;
    }

    public void Cancel()
    {
        if (!mIsPlacing.Value)
            return;

        var placeable = mPlaceable;

        // 구매 직후(아직 한 번도 확정된 적 없는) 가구를 취소하면 배치될 곳이 없어 그대로 사라진다 —
        // 이미 소모된 골드를 환불하고 오브젝트도 제거해서, 등록되지 않은 채 씬에 남아있다가
        // 다음 Save/Load 때 조용히 사라지는(골드만 날리는) 상황을 막는다.
        if (placeable != null && placeable.FurnitureTid > 0 && placeable.PlacementId < 0)
        {
            RefundFurniture(placeable.FurnitureTid, placeable.IsSubFurniture);

            mArea?.HideHighlight();
            mTarget = null;
            mPlaceable = null;
            mArea = null;
            mIsPlacing.Value = false;

            Object.Destroy(placeable.gameObject);
            Physics2D.SyncTransforms();
            return;
        }

        if (mTarget != null)
            mTarget.position = mOriginPosition;

        // 이미 확정됐던 걸 재드래그하다 취소한 경우 — BeginPlacement에서 해제했던 점유를 원래 위치에 복원한다.
        if (placeable != null && placeable.PlacementId >= 0 && mArea != null)
        {
            if (placeable.IsSubFurniture)
                mArea.OccupySub(mOriginPosition, mFootprint, placeable.PlacementId);
            else
                mArea.OccupyBase(mOriginPosition, mFootprint, placeable.PlacementId, placeable.GroupId, placeable.LayoutOrder);
        }

        mArea?.HideHighlight();
        mTarget = null;
        mPlaceable = null;
        mArea = null;
        mIsPlacing.Value = false;

        Physics2D.SyncTransforms();
    }

    #region Furniture Spawn

    // 가구 구매 목록 — 로비의 가구배치 팝업(UIPopupFurnitureList)과 상점가 가구 구입(UIPopupShopFurniture)이 공유한다.
    public List<UIScrollFurnitureData> BuildFurnitureShopData()
    {
        var dataList = new List<UIScrollFurnitureData>();

        var furnitureGroup = GameInstance.Table.GetTable<CTable.FurnitureRow>();
        if (furnitureGroup != null)
        {
            foreach (var row in furnitureGroup.All.Values)
                dataList.Add(new UIScrollFurnitureData { Tid = row.Tid, Name = row.Name, Price = row.Price, PrefabPath = row.PrefabPath, IsSub = false });
        }

        var subFurnitureGroup = GameInstance.Table.GetTable<CTable.SubFurnitureRow>();
        if (subFurnitureGroup != null)
        {
            foreach (var row in subFurnitureGroup.All.Values)
                dataList.Add(new UIScrollFurnitureData { Tid = row.Tid, Name = row.Name, Price = row.Price, PrefabPath = row.PrefabPath, IsSub = true });
        }

        return dataList;
    }

    // 가구를 구매(골드 소모)하고 곧바로 드래그 배치 모드로 전환한다. "어디서 눌렀는지"와 무관하게 동일해야 하므로
    // 가구배치 팝업과 상점가 가구 구입이 모두 이 경로를 쓴다.
    public bool TryPurchaseAndBeginPlacement(UIScrollFurnitureData data)
    {
        if (data == null || mIsPlacing.Value)
            return false;

        // 층마다 별도 TilePlacementArea가 있으므로 "지금 카메라가 보고 있는 층"에 배치한다.
        var area = GetViewingFloorArea();
        if (area == null)
        {
            Logger.Warning("[PlacementModel] 현재 층의 TilePlacementArea를 찾을 수 없습니다.");
            return false;
        }

        var prefab = GameInstance.Resource.LoadSync<GameObject>(data.PrefabPath);
        if (prefab == null)
        {
            Logger.Warning($"[PlacementModel] 가구 프리팹을 찾을 수 없습니다. Path: {data.PrefabPath}");
            return false;
        }

        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        if (gold == null || gold.Count.Value < data.Price)
        {
            Logger.Log("[PlacementModel] 골드가 부족합니다.");
            return false;
        }

        gold.Consume((int)data.Price);

        // 층별 TilePlacementArea 오브젝트 아래에 생성해서 씬 계층 구조에서 어느 층 소속인지 바로 보이게 한다.
        var instance = Object.Instantiate(prefab, area.Bounds.center, Quaternion.identity, area.transform);
        var placeable = instance.GetComponent<PlaceableObject>();

        if (data.IsSub)
            placeable.SetSubFurnitureTid(data.Tid);
        else
            placeable.SetFurnitureTid(data.Tid);

        placeable.BeginPlacementFromSpawn(area);

        // Table.prefab처럼 가구에 Intaraction_BreadStand가 같이 붙어 있으면(다른 가구는 없음) 여기서 Init() —
        // 방금 산 새 가구는 mTableId==0(미지정)으로 시작해, 이후 UIPopupBreadSelect에서 처음 넣는 빵으로 종류가 정해진다.
        instance.GetComponent<Intaraction_BreadStand>()?.Init();
        return true;
    }

    // 세이브에 저장된 배치 목록을 그대로 씬에 재생성한다. 드래그/골드 소모가 필요 없는 복원 전용 경로.
    // 씬의 TilePlacementArea/WaypointGroup이 등록된 뒤(UIRootLobby.Init)에 호출해야 한다.
    public void RespawnSavedFurniture()
    {
        // 층마다 별도 TilePlacementArea가 있어 "아무 영역이나 하나" 쓰면 안 된다 — 저장된 절대좌표를
        // 실제로 포함하는 영역을 찾아 그 층에 맞게 연결해야, 이후 재드래그 시 올바른 층 범위 안에서 스냅된다.
        var allAreas = Object.FindObjectsByType<TilePlacementArea>(FindObjectsSortMode.None);

        foreach (var kvp in mDicPlacedFurniture)
        {
            int placementId = kvp.Key;
            var record = kvp.Value;

            string prefabPath = GetFurniturePrefabPath(record.Tid, record.IsSub);
            if (prefabPath == null)
                continue;

            var prefab = GameInstance.Resource.LoadSync<GameObject>(prefabPath);
            if (prefab == null)
            {
                Logger.Warning($"[PlacementModel] 가구 프리팹을 찾을 수 없습니다. Path: {prefabPath}");
                continue;
            }

            var area = FindAreaContaining(allAreas, record.Position);
            var instance = Object.Instantiate(prefab, record.Position, Quaternion.identity, area != null ? area.transform : null);
            var placeable = instance.GetComponent<PlaceableObject>();
            if (placeable == null)
                continue;

            if (record.IsSub)
                placeable.SetSubFurnitureTid(record.Tid);
            else
                placeable.SetFurnitureTid(record.Tid);

            placeable.SetPlacementId(placementId);
            placeable.SetArea(area);

            // 점유 상태는 런타임 전용(TilePlacementArea)이라 세이브에 없다 — 복원 시 다시 채워야
            // 이후 재드래그(겹침/그룹 판정)가 올바르게 동작한다. footprint/groupId는 방금 SetFurnitureTid/
            // SetSubFurnitureTid가 테이블에서 계산해 둔 값을 그대로 쓴다.
            if (area != null)
            {
                if (record.IsSub)
                    area.OccupySub(record.Position, placeable.Footprint, placementId);
                else
                    area.OccupyBase(record.Position, placeable.Footprint, placementId, placeable.GroupId, placeable.LayoutOrder);
            }

            // GameModeLobby+FSM.InitBreadStands()는 이 시점보다 먼저 끝나 있어 BreadModel 재고 복원(ApplyPendingBreadData)이
            // 이미 끝난 뒤다 — 종류 재지정만 반영하고, 진열 수량(SyncDisplayToSavedCount)도 여기서 직접 채워준다.
            var breadStand = instance.GetComponent<Intaraction_BreadStand>();
            if (breadStand != null)
            {
                breadStand.Init();
                breadStand.RestoreAssignedBreadType(record.AssignedBreadTid);
                breadStand.SyncDisplayToSavedCount();
            }
        }
    }

    // 가구배치 리셋 — 지금까지 배치한 가구를 전부 씬에서 제거하고 배치 기록도 함께 비운다.
    // 배치 모드/드래그 중이면 먼저 취소해서 PlaceableObject.OnPointerDown 등이 파괴된 오브젝트를 참조하지 않게 한다.
    public void ResetAllPlacedFurniture()
    {
        if (mIsPlacing.Value)
            Cancel();

        foreach (var placeable in Object.FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None))
        {
            if (placeable == null || placeable.PlacementId < 0)
                continue;

            Object.Destroy(placeable.gameObject);
        }

        foreach (var area in Object.FindObjectsByType<TilePlacementArea>(FindObjectsSortMode.None))
            area.ClearAllOccupants();

        ClearAllPlacements();
    }

    // FloorModel.ViewingFloor(로비 카메라가 보고 있는 층)의 WaypointGroup(Order=층 번호) 첫 영역.
    private TilePlacementArea GetViewingFloorArea()
    {
        if (GameInstance.WayPoint == null)
            return null;

        int floor = GameInstance.Model.Floor.ViewingFloor.Value;
        foreach (var group in GameInstance.WayPoint.WaypointGroups)
        {
            if (group == null || group.Order != floor)
                continue;

            var areas = group.ZoneAreas;
            return areas != null && areas.Length > 0 ? areas[0] : null;
        }
        return null;
    }

    private static string GetFurniturePrefabPath(int tid, bool isSub)
    {
        if (isSub)
        {
            var subRow = GameInstance.Table.Get<CTable.SubFurnitureRow>(tid);
            if (subRow == null)
                Logger.Warning($"[PlacementModel] 저장된 sub가구 Tid를 찾을 수 없습니다. Tid={tid}");
            return subRow?.PrefabPath;
        }

        var row = GameInstance.Table.Get<CTable.FurnitureRow>(tid);
        if (row == null)
            Logger.Warning($"[PlacementModel] 저장된 가구 Tid를 찾을 수 없습니다. Tid={tid}");
        return row?.PrefabPath;
    }

    private static TilePlacementArea FindAreaContaining(TilePlacementArea[] areas, Vector3 position)
    {
        foreach (var area in areas)
        {
            if (area != null && area.Contains(position))
                return area;
        }

        Logger.Warning($"[PlacementModel] 위치 {position}를 포함하는 TilePlacementArea를 찾지 못했습니다. 복원된 가구는 재드래그가 불가능합니다.");
        return null;
    }

    #endregion

    private void RefundFurniture(int tid, bool isSub)
    {
        long price;

        if (isSub)
        {
            var row = GameInstance.Table.Get<CTable.SubFurnitureRow>(tid);
            if (row == null) return;
            price = row.Price;
        }
        else
        {
            var row = GameInstance.Table.Get<CTable.FurnitureRow>(tid);
            if (row == null) return;
            price = row.Price;
        }

        GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Add((int)price);
    }
}
