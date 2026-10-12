using System.Collections.Generic;
using UnityEngine;

public partial class PlacementModel
{
    public IEnumerable<KeyValuePair<int, PlacedFurnitureRecord>> GetAllPlacements() => mDicPlacedFurniture;

    public int RegisterPlacement(int tid, Vector3 position, int floor, Vector2Int cell, CTable.eFurnitureDirection direction, bool isSub = false)
    {
        int placementId = mNextPlacementId++;
        mDicPlacedFurniture[placementId] = new PlacedFurnitureRecord { Tid = tid, Position = position, Floor = floor, Cell = cell, IsSub = isSub, Direction = direction };
        mInventoryVersion.Value++;
        return placementId;
    }

    // 방향을 빠뜨리면 저장된 회전이 조용히 초기화되므로 기본값 없이 항상 넘긴다.
    public void UpdatePlacementPosition(int placementId, Vector3 position, int floor, Vector2Int cell, CTable.eFurnitureDirection direction)
    {
        if (!mDicPlacedFurniture.TryGetValue(placementId, out var record))
            return;

        record.Position = position;
        record.Floor = floor;
        record.Cell = cell;
        record.Direction = direction;
    }

    // 세이브 로드 복원 전용 — 저장된 id를 그대로 재사용해야 이후 이동 시 같은 레코드를 갱신한다.
    public void RestorePlacement(int placementId, int tid, Vector3 position, int floor, Vector2Int cell, CTable.eFurnitureDirection direction,
        int assignedBreadTid = 0, bool isSub = false)
    {
        mDicPlacedFurniture[placementId] = new PlacedFurnitureRecord { Tid = tid, Position = position, Floor = floor, Cell = cell, AssignedBreadTid = assignedBreadTid, IsSub = isSub, Direction = direction };

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
        mInventoryVersion.Value++;
    }

    #region Owned Furniture

    // 구매한 총 개수(배치된 것 포함).
    public int GetOwnedCount(int tid, bool isSub) => mOwnedFurniture.TryGetValue((isSub, tid), out int count) ? count : 0;

    public int GetPlacedCount(int tid, bool isSub)
    {
        int count = 0;
        foreach (var record in mDicPlacedFurniture.Values)
        {
            if (record.Tid == tid && record.IsSub == isSub)
                count++;
        }
        return count;
    }

    // 보관 중(배치하지 않은) 개수. 배치 목록에서 이 수만큼은 골드 없이 꺼내 놓을 수 있다.
    public int GetStoredCount(int tid, bool isSub) => Mathf.Max(0, GetOwnedCount(tid, isSub) - GetPlacedCount(tid, isSub));

    public IEnumerable<KeyValuePair<(bool isSub, int tid), int>> GetAllOwned() => mOwnedFurniture;

    // 세이브 복원 전용.
    public void RestoreOwned(int tid, bool isSub, int count)
    {
        if (count > 0)
            mOwnedFurniture[(isSub, tid)] = count;
    }

    // 보유 수 기록이 없던 구 세이브 — 배치된 가구는 이미 산 것이므로 보유 수가 배치 수보다 적으면 맞춰 준다.
    // RestorePlacement/RestoreOwned를 모두 끝낸 뒤 호출한다.
    public void EnsureOwnedCoversPlacements()
    {
        foreach (var record in mDicPlacedFurniture.Values)
        {
            var key = (record.IsSub, record.Tid);
            int placed = GetPlacedCount(record.Tid, record.IsSub);
            if (GetOwnedCount(record.Tid, record.IsSub) < placed)
                mOwnedFurniture[key] = placed;
        }
        mInventoryVersion.Value++;
    }

    private void AddOwned(int tid, bool isSub)
    {
        mOwnedFurniture[(isSub, tid)] = GetOwnedCount(tid, isSub) + 1;
        mInventoryVersion.Value++;
    }

    #endregion

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
        mOriginDirection = placeable != null ? placeable.Direction : CTable.eFurnitureDirection.Down;

        mIsValidPosition.Value = false;
        mIsPlacing.Value = true;

        ShowPlacingHighlight();
    }

    // 배치 중인 가구가 회전 가능한지(확정/취소 팝업의 회전 버튼 표시용).
    public bool CanRotatePlacing => mIsPlacing.Value && mPlaceable != null && mPlaceable.CanRotate;

    // 배치 중인 가구를 다음 방향(Down→Right→Up→Left)으로 돌린다. 회전 버튼 / R키.
    // footprint 가로·세로와 손님 칸/비워둘 칸이 바뀌므로 강조 칸과 유효 위치를 다시 계산한다.
    public void RotatePlacing()
    {
        if (!CanRotatePlacing || mTarget == null || mArea == null)
            return;

        mPlaceable.SetDirection(mPlaceable.GetNextDirection());
        mFootprint = mPlaceable.Footprint;

        ShowPlacingHighlight();
        UpdatePreview(mTarget.position);
    }

    // 놓을 수 있는 칸(면/레이어/그룹 조건)을 드래그 동안 강조한다.
    private void ShowPlacingHighlight()
    {
        if (mPlaceable != null && mPlaceable.IsSubFurniture)
            mArea.ShowSubHighlight(mFootprint, mPlaceable.AllowedGroupId, mPlaceable.PlacementId);
        else if (mPlaceable != null)
            mArea.ShowBaseHighlight(mFootprint, mPlaceable.Surface, mPlaceable.LayoutOrder, mPlaceable.PlacementId, mPlaceable.AccessOffsets, mPlaceable.ClearanceOffsets);

        RefreshPlacementPreview();
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
                mPlaceable != null ? mPlaceable.Surface : ePlacementSurface.Floor, mPlaceable != null ? mPlaceable.LayoutOrder : 0,
                mPlaceable != null ? mPlaceable.AccessOffsets : null, mPlaceable != null ? mPlaceable.ClearanceOffsets : null, out snappedPos);

        mIsValidPosition.Value = isValid;
        mTarget.position = isValid ? snappedPos : worldPos;

        RefreshPlacementPreview();
    }

    // 손님 칸/비워둘 칸이 있는 가구(빵 테이블 등)는 드래그하는 동안 자기 칸 + 그 칸들을 다른 테이블처럼 붉게 따라 표시한다.
    private void RefreshPlacementPreview()
    {
        if (mArea == null || mTarget == null || mPlaceable == null || mPlaceable.IsSubFurniture)
            return;

        if (mPlaceable.AccessOffsets.Count == 0 && mPlaceable.ClearanceOffsets.Count == 0)
        {
            mArea.ClearPlacementPreview();
            return;
        }

        mArea.ShowPlacementPreview(mArea.GetFootprintOrigin(mTarget.position, mFootprint), mFootprint, mPlaceable.AccessOffsets, mPlaceable.ClearanceOffsets);
    }

    public void Confirm()
    {
        if (!mIsPlacing.Value || !mIsValidPosition.Value)
            return;

        var placeable = mPlaceable;
        if (placeable != null && placeable.FurnitureTid > 0)
        {
            var origin = mArea.GetFootprintOrigin(mTarget.position, mFootprint);
            var cell = new Vector2Int(origin.x, origin.y);

            if (placeable.PlacementId < 0)
                placeable.SetPlacementId(RegisterPlacement(placeable.FurnitureTid, mTarget.position, mArea.Floor, cell, placeable.Direction, placeable.IsSubFurniture));
            else
                UpdatePlacementPosition(placeable.PlacementId, mTarget.position, mArea.Floor, cell, placeable.Direction);

            if (placeable.IsSubFurniture)
                mArea.OccupySub(mTarget.position, mFootprint, placeable.PlacementId);
            else
                mArea.OccupyBase(mTarget.position, mFootprint, placeable.PlacementId, placeable.GroupId, placeable.LayoutOrder,
                    placeable.AccessOffsets, placeable.ClearanceOffsets);
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

        // 아직 한 번도 확정된 적 없는(방금 사거나 보관함에서 꺼낸) 가구를 취소하면 오브젝트만 제거한다.
        // 보유 수는 그대로라 보관 중으로 돌아간다(가구는 팔지 않으므로 골드 환불도 없다).
        if (placeable != null && placeable.FurnitureTid > 0 && placeable.PlacementId < 0)
        {
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

        // 드래그 중 돌렸다면 시작 방향으로 되돌린다(footprint/손님 칸/비워둘 칸도 같이 복원).
        if (placeable != null && placeable.Direction != mOriginDirection)
        {
            placeable.SetDirection(mOriginDirection);
            mFootprint = placeable.Footprint;
        }

        // 이미 확정됐던 걸 재드래그하다 취소한 경우 — BeginPlacement에서 해제했던 점유를 원래 위치에 복원한다.
        if (placeable != null && placeable.PlacementId >= 0 && mArea != null)
        {
            if (placeable.IsSubFurniture)
                mArea.OccupySub(mOriginPosition, mFootprint, placeable.PlacementId);
            else
                mArea.OccupyBase(mOriginPosition, mFootprint, placeable.PlacementId, placeable.GroupId, placeable.LayoutOrder,
                    placeable.AccessOffsets, placeable.ClearanceOffsets);
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
    // category가 All이 아니면 그 탭(표의 Category)에 속한 가구만 돌려준다.
    public List<UIScrollFurnitureData> BuildFurnitureShopData(CTable.eFurnitureCategory category = CTable.eFurnitureCategory.All)
    {
        var dataList = new List<UIScrollFurnitureData>();

        var furnitureGroup = GameInstance.Table.GetTable<CTable.FurnitureRow>();
        if (furnitureGroup != null)
        {
            foreach (var row in furnitureGroup.All.Values)
                dataList.Add(new UIScrollFurnitureData { Tid = row.Tid, Name = row.Name, Price = row.Price, PrefabPath = row.PrefabPath, IsSub = false,
                    Category = (CTable.eFurnitureCategory)row.Category, Width = Mathf.Max(1, row.Width), Height = Mathf.Max(1, row.Height),
                    StoredCount = GetStoredCount(row.Tid, false) });
        }

        var subFurnitureGroup = GameInstance.Table.GetTable<CTable.SubFurnitureRow>();
        if (subFurnitureGroup != null)
        {
            foreach (var row in subFurnitureGroup.All.Values)
                dataList.Add(new UIScrollFurnitureData { Tid = row.Tid, Name = row.Name, Price = row.Price, PrefabPath = row.PrefabPath, IsSub = true,
                    Category = (CTable.eFurnitureCategory)row.Category, Width = Mathf.Max(1, row.Width), Height = Mathf.Max(1, row.Height),
                    StoredCount = GetStoredCount(row.Tid, true) });
        }

        if (category != CTable.eFurnitureCategory.All)
            dataList.RemoveAll(data => data.Category != category);

        return dataList;
    }

    // 상점가 [가구 구입] — 항상 골드로 새로 사서(보유 +1) 곧바로 드래그 배치 모드로 전환한다.
    public bool TryPurchaseAndBeginPlacement(UIScrollFurnitureData data) => TryBeginPlacement(data, useStored: false);

    // 로비 가구배치 목록 — 보관 중인 가구가 있으면 골드 없이 꺼내 놓고, 없으면 골드로 산다.
    public bool TryPlaceOrPurchase(UIScrollFurnitureData data) => TryBeginPlacement(data, useStored: true);

    private bool TryBeginPlacement(UIScrollFurnitureData data, bool useStored)
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

        bool fromStorage = useStored && GetStoredCount(data.Tid, data.IsSub) > 0;
        if (!fromStorage)
        {
            var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
            if (gold == null || gold.Count.Value < data.Price)
            {
                Logger.Log("[PlacementModel] 골드가 부족합니다.");
                return false;
            }

            gold.Consume((int)data.Price);
            AddOwned(data.Tid, data.IsSub);
        }

        // 층별 TilePlacementArea 오브젝트 아래에 생성해서 씬 계층 구조에서 어느 층 소속인지 바로 보이게 한다.
        var instance = Object.Instantiate(prefab, area.Bounds.center, Quaternion.identity, area.transform);
        var placeable = instance.GetComponent<PlaceableObject>();

        if (data.IsSub)
            placeable.SetSubFurnitureTid(data.Tid);
        else
            placeable.SetFurnitureTid(data.Tid);

        placeable.BeginPlacementFromSpawn(area);

        // Table.prefab처럼 가구에 Intaraction_BreadStand가 같이 붙어 있으면(다른 가구는 없음) 여기서 Init() —
        // 새로 놓는 가구는 mTableId==0(미지정)으로 시작해, 이후 UIPopupBreadSelect에서 처음 넣는 빵으로 종류가 정해진다.
        instance.GetComponent<Intaraction_BreadStand>()?.Init();
        return true;
    }

    // 세이브에 저장된 배치 목록을 그대로 씬에 재생성한다. 드래그/골드 소모가 필요 없는 복원 전용 경로.
    // 씬의 TilePlacementArea/WaypointGroup이 등록된 뒤(UIRootLobby.Init)에 호출해야 한다.
    public void RespawnSavedFurniture()
    {
        // 층마다 별도 TilePlacementArea가 있어 "아무 영역이나 하나" 쓰면 안 된다 — 저장된 층(Floor)의 영역에 연결하고
        // 위치는 그 영역의 셀(Cell)로 다시 계산한다. 그래야 칸 배치/Grid 위치가 바뀌어도 가구가 같은 칸에 놓인다.
        // Floor가 없는 구 세이브(월드 좌표만 저장)는 저장 좌표를 포함하는 영역을 찾아 층/셀을 역산해 채운다.
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

            var area = record.Floor > 0 ? FindAreaByFloor(allAreas, record.Floor) : null;
            if (area == null)
                area = FindAreaContaining(allAreas, record.Position);

            var instance = Object.Instantiate(prefab, record.Position, Quaternion.identity, area != null ? area.transform : null);
            var placeable = instance.GetComponent<PlaceableObject>();
            if (placeable == null)
                continue;

            if (record.IsSub)
                placeable.SetSubFurnitureTid(record.Tid);
            else
            {
                placeable.SetFurnitureTid(record.Tid);
                // 회전 불가 가구에 방향이 남아 있으면(표에서 CanRotate를 껐을 때 등) 기본 방향으로 둔다.
                placeable.SetDirection(placeable.CanRotate ? record.Direction : CTable.eFurnitureDirection.Down);
                record.Direction = placeable.Direction;
            }

            // footprint는 Tid 지정 후에야 알 수 있으므로 위치 보정은 여기서 한다.
            if (area != null)
            {
                if (record.Floor <= 0 || area.Floor != record.Floor)
                {
                    var legacyOrigin = area.GetFootprintOrigin(record.Position, placeable.Footprint);
                    record.Floor = area.Floor;
                    record.Cell = new Vector2Int(legacyOrigin.x, legacyOrigin.y);
                }

                var origin = new Vector3Int(record.Cell.x, record.Cell.y, 0);
                record.Position = area.GetFootprintCenterWorld(origin, placeable.Footprint, record.Position.z);
                instance.transform.position = record.Position;
            }

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
                    area.OccupyBase(record.Position, placeable.Footprint, placementId, placeable.GroupId, placeable.LayoutOrder,
                        placeable.AccessOffsets, placeable.ClearanceOffsets);
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
    // 가구를 팔지 않는다: 보유 수는 그대로라 치운 가구는 전부 보관 중으로 돌아가고, 배치 목록에서 골드 없이 다시 놓을 수 있다.
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

    private static TilePlacementArea FindAreaByFloor(TilePlacementArea[] areas, int floor)
    {
        foreach (var area in areas)
        {
            if (area != null && area.Floor == floor)
                return area;
        }

        Logger.Warning($"[PlacementModel] {floor}층의 TilePlacementArea를 찾지 못했습니다. 저장된 월드 좌표로 영역을 다시 찾습니다.");
        return null;
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
}
