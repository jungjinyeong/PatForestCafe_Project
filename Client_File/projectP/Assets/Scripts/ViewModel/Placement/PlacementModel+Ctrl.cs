using System.Collections.Generic;
using UniRx;
using UnityEngine;

public partial class PlacementModel
{
    public IEnumerable<KeyValuePair<int, PlacedFurnitureRecord>> GetAllPlacements() => mDicPlacedFurniture;

    // notify=false면 목록 UI 갱신(InventoryVersion)을 호출부가 모아서 한 번에 한다(대량 설치).
    public int RegisterPlacement(int tid, Vector3 position, int floor, Vector2Int cell, CTable.eFurnitureDirection direction, bool flipX,
        bool isSub = false, bool notify = true)
    {
        int placementId = mNextPlacementId++;
        mDicPlacedFurniture[placementId] = new PlacedFurnitureRecord { Tid = tid, Position = position, Floor = floor, Cell = cell, IsSub = isSub, Direction = direction, FlipX = flipX };
        if (notify)
            mInventoryVersion.Value++;
        return placementId;
    }

    // 방향/좌우반전을 빠뜨리면 저장된 회전이 조용히 초기화되므로 기본값 없이 항상 넘긴다.
    public void UpdatePlacementPosition(int placementId, Vector3 position, int floor, Vector2Int cell, CTable.eFurnitureDirection direction, bool flipX)
    {
        if (!mDicPlacedFurniture.TryGetValue(placementId, out var record))
            return;

        record.Position = position;
        record.Floor = floor;
        record.Cell = cell;
        record.Direction = direction;
        record.FlipX = flipX;
    }

    // 세이브 로드 복원 전용 — 저장된 id를 그대로 재사용해야 이후 이동 시 같은 레코드를 갱신한다.
    public void RestorePlacement(int placementId, int tid, Vector3 position, int floor, Vector2Int cell, CTable.eFurnitureDirection direction, bool flipX,
        int assignedBreadTid = 0, bool isSub = false)
    {
        mDicPlacedFurniture[placementId] = new PlacedFurnitureRecord { Tid = tid, Position = position, Floor = floor, Cell = cell, AssignedBreadTid = assignedBreadTid, IsSub = isSub,
            Direction = direction, FlipX = flipX };

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

    // notify=false면 목록 UI 갱신(InventoryVersion)을 호출부가 모아서 한 번에 한다(대량 설치).
    private void AddOwned(int tid, bool isSub, bool notify = true)
    {
        mOwnedFurniture[(isSub, tid)] = GetOwnedCount(tid, isSub) + 1;
        if (notify)
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

        if (!isEdit)
            mIsDemolishMode.Value = false;

        mIsEditMode.Value = isEdit;
    }

    public void ToggleGridOverlay()
    {
        mIsGridOverlayOn.Value = !mIsGridOverlayOn.Value;
    }

    public void SetGridOverlay(bool isOn)
    {
        mIsGridOverlayOn.Value = isOn;
    }

    public void ToggleDemolishMode()
    {
        SetDemolishMode(!mIsDemolishMode.Value);
    }

    // 배치 모드가 아니거나 가구를 들고 있으면 켜지 않는다.
    public void SetDemolishMode(bool isDemolish)
    {
        if (isDemolish && (!mIsEditMode.Value || mIsPlacing.Value))
            return;

        mIsDemolishMode.Value = isDemolish;
    }

    // 배치된 가구를 철거해 보관함으로 회수한다(보유 수는 그대로 → 보관 수량 +1). 베이스 위에 올라간 sub가구도 함께 회수하고,
    // 빵 진열대면 진열된 빵을 창고 재고로 되돌린다. 아직 한 번도 놓지 않은 가구(PlacementId<0)나 들고 있는 동안에는 하지 않는다.
    public bool Demolish(PlaceableObject target)
    {
        if (target == null || target.PlacementId < 0 || mIsPlacing.Value)
            return false;

        var victims = new List<PlaceableObject> { target };
        if (!target.IsSubFurniture && target.Area != null)
        {
            foreach (int subId in target.Area.GetSubPlacementIdsOnBase(target.PlacementId))
            {
                var sub = PlaceableObject.FindByPlacementId(target.Area, subId);
                if (sub != null)
                    victims.Add(sub);
            }
        }

        foreach (var victim in victims)
        {
            ReleasePlacedObject(victim);
            mDicPlacedFurniture.Remove(victim.PlacementId);
        }

        mInventoryVersion.Value++;
        Physics2D.SyncTransforms();
        mPlacedLayoutChanged.OnNext(Unit.Default);
        return true;
    }

    // 배치된 오브젝트를 씬에서 치운다(칸 점유 해제 + 빵 진열대 정리 + 파괴). 배치 기록 정리는 호출부가 한다.
    // Destroy는 프레임 끝에 일어나므로 먼저 비활성화해 같은 프레임의 클릭 판정(FindTopmostAt)에 다시 걸리지 않게 한다.
    private static void ReleasePlacedObject(PlaceableObject placeable)
    {
        if (placeable.BreadStand != null)
            placeable.BreadStand.ReturnDisplayedBreadToStorage();
        if (placeable.Area != null)
            placeable.Area.ReleaseByPlacementId(placeable.PlacementId);
        placeable.gameObject.SetActive(false);
        Object.Destroy(placeable.gameObject);
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

        // 철거 모드에서 목록으로 새 가구를 꺼내는 경우 등 — 들고 있는 동안은 철거 모드가 아니다.
        mIsDemolishMode.Value = false;

        mTarget = target;
        mPlaceable = placeable;
        mArea = area;
        mFootprint = footprint;
        mOriginPosition = target.position;
        mOriginDirection = placeable != null ? placeable.Direction : CTable.eFurnitureDirection.Down;
        mOriginFlipX = placeable != null && placeable.FlipX;

        mIsValidPosition.Value = false;
        mIsPlacing.Value = true;

        ShowPlacingHighlight();
    }

    // 들고 있는 것이 새로 꺼낸(아직 한 번도 놓지 않은) 대량 설치 대상(바닥타일)인지. 이미 깔린 타일을 다시 집은 경우는 이동이라
    // 지금처럼 하나만 놓는다. true면 PC 입력은 들고 있는 타일을 그대로 두고 칸마다 복사본을 깐다(PlaceBulkCopies).
    public bool IsBulkPlacing => mIsPlacing.Value && mPlaceable != null && mPlaceable.PlacementId < 0 && mPlaceable.IsBulkPlaceable;

    // 들고 있는 가구가 차지하는 칸 수(회전 반영). 대량 설치가 footprint 간격으로 칸을 고르는 데 쓴다.
    public Vector2Int PlacingFootprint => mFootprint;

    // 월드 좌표 → 들고 있는 가구 footprint 기준 셀(좌하단). 들고 있지 않으면 false.
    public bool TryGetPlacingCell(Vector3 worldPos, out Vector3Int cell)
    {
        cell = default;
        if (!mIsPlacing.Value || mArea == null)
            return false;

        cell = mArea.GetFootprintOrigin(worldPos, mFootprint);
        return true;
    }

    // 사각형 채우기 범위 미리보기. UpdatePreview가 매번 미리보기를 지우므로 그 다음에 호출한다.
    public void ShowBulkFillPreview(IEnumerable<Vector3Int> cells)
    {
        if (mIsPlacing.Value && mArea != null)
            mArea.ShowFillPreview(cells, mFootprint);
    }

    // 들고 있는 바닥타일과 같은 타일(같은 방향/좌우반전)을 cells(footprint 기준 셀)마다 깐다. 놓을 수 없는 칸은 건너뛴다.
    // 비용: 들고 있는 타일 몫 1개를 뺀 보관 수량을 먼저 쓰고, 모자라면 칸마다 골드로 산다. 골드가 떨어지면 거기서 멈추고 outOfGold=true.
    // 목록 UI 갱신(InventoryVersion)은 끝에 한 번만 한다. 반환값 = 실제로 깐 수.
    public int PlaceBulkCopies(IReadOnlyList<Vector3Int> cells, out bool outOfGold)
    {
        outOfGold = false;
        if (!IsBulkPlacing || mArea == null || mTarget == null || cells == null || cells.Count == 0)
            return 0;

        var ghost = mPlaceable;
        var row = GameInstance.Table.Get<CTable.FurnitureRow>(ghost.FurnitureTid);
        var prefab = LoadFurniturePrefab(ghost.FurnitureTid, false);
        if (row == null || prefab == null)
            return 0;

        int placed = 0;
        foreach (var cell in cells)
        {
            var worldPos = mArea.GetFootprintCenterWorld(cell, mFootprint, mTarget.position.z);
            if (!mArea.TryGetSnappedPositionForBase(worldPos, mFootprint, -1, ghost.Surface, ghost.LayoutOrder,
                    ghost.AccessOffsets, ghost.ClearanceOffsets, out var snappedPos))
                continue;

            // 들고 있는 타일도 보관 수량에 포함돼 있으므로 1개는 남겨 둔다(취소하면 보관함으로 돌아가는 몫).
            if (!TryConsumeForPlacement(ghost.FurnitureTid, false, row.Price, reservedStoredCount: 1, notify: false))
            {
                outOfGold = true;
                break;
            }

            var instance = Object.Instantiate(prefab, snappedPos, Quaternion.identity, mArea.transform);
            var placeable = instance.GetComponent<PlaceableObject>();
            placeable.SetFurnitureTid(ghost.FurnitureTid);
            placeable.SetArea(mArea);
            placeable.SetOrientation(ghost.Direction, ghost.FlipX);

            int placementId = RegisterPlacement(ghost.FurnitureTid, snappedPos, mArea.Floor, new Vector2Int(cell.x, cell.y),
                placeable.Direction, placeable.FlipX, isSub: false, notify: false);
            placeable.SetPlacementId(placementId);
            mArea.OccupyBase(snappedPos, placeable.Footprint, placementId, placeable.GroupId, placeable.LayoutOrder,
                placeable.AccessOffsets, placeable.ClearanceOffsets);
            placed++;
        }

        if (placed > 0)
        {
            mInventoryVersion.Value++;
            Physics2D.SyncTransforms();
            mPlacedLayoutChanged.OnNext(Unit.Default);
            // 방금 깐 칸은 더 이상 놓을 수 없으므로 강조/유효 위치를 다시 계산한다.
            ShowPlacingHighlight();
            UpdatePreview(mTarget.position);
        }

        if (outOfGold)
            Logger.Log("[PlacementModel] 골드가 부족해 연속 설치를 멈췄습니다.");

        return placed;
    }

    // 배치 중인 가구가 회전 가능한지(확정/취소 팝업의 회전 버튼, 조작 안내 표시용).
    public bool CanRotatePlacing => mIsPlacing.Value && mPlaceable != null && mPlaceable.CanRotate;
    // 배치 중인 가구의 회전 방식(조작 안내에서 "회전"/"좌우 반전" 문구를 고르는 데 쓴다). 배치 중이 아니면 None.
    public CTable.eFurnitureOrientMode PlacingOrientMode => mIsPlacing.Value && mPlaceable != null ? mPlaceable.OrientMode : CTable.eFurnitureOrientMode.None;

    // 배치 중인 가구에 R 한 번(회전 버튼 / R키) — 회전 방식에 따라 시계 방향 90° 회전 또는 좌우반전(PlaceableObject.RotateNext).
    // footprint 가로·세로와 손님 칸/비워둘 칸이 바뀔 수 있으므로 강조 칸과 유효 위치를 다시 계산한다.
    public void RotatePlacing()
    {
        if (!CanRotatePlacing || mTarget == null || mArea == null)
            return;

        mPlaceable.RotateNext();
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

    // 들고 있는 가구를 worldPos(커서 위치) 근처 칸에 맞춘다. z는 가구 원래 깊이를 유지한다(호출부는 화면→월드 좌표만 넘기면 됨).
    public void UpdatePreview(Vector3 worldPos)
    {
        if (!mIsPlacing.Value || mTarget == null || mArea == null)
            return;

        worldPos.z = mTarget.position.z;

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
                placeable.SetPlacementId(RegisterPlacement(placeable.FurnitureTid, mTarget.position, mArea.Floor, cell, placeable.Direction, placeable.FlipX, placeable.IsSubFurniture));
            else
                UpdatePlacementPosition(placeable.PlacementId, mTarget.position, mArea.Floor, cell, placeable.Direction, placeable.FlipX);

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

        // 드래그 중 돌렸거나 뒤집었다면 시작 상태로 되돌린다(footprint/손님 칸/비워둘 칸도 같이 복원).
        if (placeable != null && (placeable.Direction != mOriginDirection || placeable.FlipX != mOriginFlipX))
        {
            placeable.SetOrientation(mOriginDirection, mOriginFlipX);
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
                dataList.Add(new UIScrollFurnitureData { Tid = row.Tid, Name = row.Name, Price = row.Price, IsSub = false,
                    Category = (CTable.eFurnitureCategory)row.Category, Width = Mathf.Max(1, row.Width), Height = Mathf.Max(1, row.Height),
                    StoredCount = GetStoredCount(row.Tid, false) });
        }

        var subFurnitureGroup = GameInstance.Table.GetTable<CTable.SubFurnitureRow>();
        if (subFurnitureGroup != null)
        {
            foreach (var row in subFurnitureGroup.All.Values)
                dataList.Add(new UIScrollFurnitureData { Tid = row.Tid, Name = row.Name, Price = row.Price, IsSub = true,
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

        var prefab = LoadFurniturePrefab(data.Tid, data.IsSub);
        if (prefab == null)
            return false;

        bool hasUnit = useStored
            ? TryConsumeForPlacement(data.Tid, data.IsSub, data.Price, reservedStoredCount: 0)
            : TryPurchase(data.Tid, data.IsSub, data.Price);
        if (!hasUnit)
        {
            Logger.Log("[PlacementModel] 골드가 부족합니다.");
            return false;
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

    // 가구 1개를 놓을 몫을 확보한다: 보관 수량(reservedStoredCount개는 남겨 둠)이 있으면 그걸 쓰고(보유/골드 변화 없음),
    // 없으면 골드로 산다(보유 +1). 골드가 부족하면 false.
    private bool TryConsumeForPlacement(int tid, bool isSub, long price, int reservedStoredCount, bool notify = true)
    {
        if (GetStoredCount(tid, isSub) > reservedStoredCount)
            return true;

        return TryPurchase(tid, isSub, price, notify);
    }

    private bool TryPurchase(int tid, bool isSub, long price, bool notify = true)
    {
        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        if (gold == null || gold.Count.Value < price)
            return false;

        gold.Consume((int)price);
        AddOwned(tid, isSub, notify);
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

            var prefab = LoadFurniturePrefab(record.Tid, record.IsSub);
            if (prefab == null)
                continue;

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
                // 아트 설정의 회전 방식에서 쓸 수 없는 방향/반전이 남아 있으면(회전 방식을 바꿨을 때 등) SetOrientation이 기본값으로 정리한다.
                placeable.SetOrientation(record.Direction, record.FlipX);
                record.Direction = placeable.Direction;
                record.FlipX = placeable.FlipX;
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
    // 가구를 들고 있으면 먼저 취소해서 PlacementModel/입력이 파괴된 오브젝트를 참조하지 않게 한다.
    public void ResetAllPlacedFurniture()
    {
        if (mIsPlacing.Value)
            Cancel();

        foreach (var placeable in Object.FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None))
        {
            if (placeable == null || placeable.PlacementId < 0)
                continue;

            // 철거와 같은 정리(진열 빵 → 창고, 풀 회수, 비활성화 후 파괴). 남은 칸 점유는 아래에서 영역별로 한 번에 비운다.
            ReleasePlacedObject(placeable);
        }

        foreach (var area in Object.FindObjectsByType<TilePlacementArea>(FindObjectsSortMode.None))
            area.ClearAllOccupants();

        ClearAllPlacements();
        mPlacedLayoutChanged.OnNext(Unit.Default);
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

    // 가구/소품의 아트 설정(프리팹, 방향별 스프라이트, 회전 방식). CSV의 VisualPath(Addressables 주소)로 로드한다.
    // 표에 없거나 로드에 실패하면 null — 스폰 불가, 이미 있는 오브젝트는 프리팹 그림 그대로·회전 없음으로 취급한다.
    public FurnitureVisualData GetFurnitureVisual(int tid, bool isSub)
    {
        if (mDicVisual.TryGetValue((isSub, tid), out var cached))
            return cached;

        string visualPath = GetVisualPath(tid, isSub);
        var visual = string.IsNullOrEmpty(visualPath) ? null : GameInstance.Resource.LoadSync<FurnitureVisualData>(visualPath);
        if (visual == null && !string.IsNullOrEmpty(visualPath))
            Logger.Warning($"[PlacementModel] 가구 아트 설정을 찾을 수 없습니다. Tid={tid}, IsSub={isSub}, VisualPath={visualPath}");

        mDicVisual[(isSub, tid)] = visual;
        return visual;
    }

    // 아트 설정에 연결된 배치 프리팹(AssetReferenceGameObject → Addressables GUID 키로 로드).
    private GameObject LoadFurniturePrefab(int tid, bool isSub)
    {
        var visual = GetFurnitureVisual(tid, isSub);
        if (visual == null)
            return null;

        if (visual.Prefab == null || !visual.Prefab.RuntimeKeyIsValid())
        {
            Logger.Warning($"[PlacementModel] 아트 설정 '{visual.name}'에 프리팹이 지정되지 않았습니다. Tid={tid}, IsSub={isSub}");
            return null;
        }

        var prefab = GameInstance.Resource.LoadSync<GameObject>(visual.Prefab.AssetGUID);
        if (prefab == null)
            Logger.Warning($"[PlacementModel] 가구 프리팹을 로드하지 못했습니다. Visual={visual.name}, GUID={visual.Prefab.AssetGUID}");
        return prefab;
    }

    private static string GetVisualPath(int tid, bool isSub)
    {
        if (isSub)
        {
            var subRow = GameInstance.Table.Get<CTable.SubFurnitureRow>(tid);
            if (subRow == null)
                Logger.Warning($"[PlacementModel] sub가구 Tid를 표에서 찾을 수 없습니다. Tid={tid}");
            return subRow?.VisualPath;
        }

        var row = GameInstance.Table.Get<CTable.FurnitureRow>(tid);
        if (row == null)
            Logger.Warning($"[PlacementModel] 가구 Tid를 표에서 찾을 수 없습니다. Tid={tid}");
        return row?.VisualPath;
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
