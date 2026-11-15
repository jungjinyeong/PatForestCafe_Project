using System.Collections.Generic;
using System.Linq;
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
        int assignedDessertTid = 0, bool isSub = false)
    {
        mDicPlacedFurniture[placementId] = new PlacedFurnitureRecord { Tid = tid, Position = position, Floor = floor, Cell = cell, AssignedDessertTid = assignedDessertTid, IsSub = isSub,
            Direction = direction, FlipX = flipX };

        if (placementId >= mNextPlacementId)
            mNextPlacementId = placementId + 1;
    }

    // Intaraction_DessertStand.TryAssignDessertType()이 최초 배정 시 호출 — 다음 세이브에 그대로 실린다.
    public void SetAssignedDessertTid(int placementId, int dessertTid)
    {
        if (mDicPlacedFurniture.TryGetValue(placementId, out var record))
            record.AssignedDessertTid = dessertTid;
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

    // 바닥타일·벽지는 한 번 사면(보유 1) 골드 없이 몇 개든 깔 수 있다. 나머지 가구는 1개씩 사서 보유 수만큼 놓는다.
    public static bool IsUnlimitedFurniture(int tid, bool isSub)
    {
        if (isSub)
            return false;

        var row = GameInstance.Table.Get<CTable.FurnitureRow>(tid);
        return row != null && (row.FurnitureType == (int)CTable.eFurnitureType.Tile || row.FurnitureType == (int)CTable.eFurnitureType.Wall);
    }

    // 무제한 설치 가구(바닥타일·벽지)를 이미 샀는지.
    public bool IsUnlimitedUnlocked(int tid, bool isSub) => IsUnlimitedFurniture(tid, isSub) && GetOwnedCount(tid, isSub) > 0;

    // 보관 중(배치하지 않은) 개수. 배치 목록에서 이 수만큼은 골드 없이 꺼내 놓을 수 있다. 무제한 설치 가구는 보관 개념이 없어 0.
    public int GetStoredCount(int tid, bool isSub) => IsUnlimitedFurniture(tid, isSub)
        ? 0
        : Mathf.Max(0, GetOwnedCount(tid, isSub) - GetPlacedCount(tid, isSub));

    public IEnumerable<KeyValuePair<(bool isSub, int tid), int>> GetAllOwned() => mOwnedFurniture;

    // 세이브 복원 전용.
    public void RestoreOwned(int tid, bool isSub, int count)
    {
        if (count > 0)
            mOwnedFurniture[(isSub, tid)] = count;
    }

    // 보유 수 기록이 없던 구 세이브 — 배치된 가구는 이미 산 것이므로 보유 수가 배치 수보다 적으면 맞춰 준다.
    // 무제한 설치 가구(바닥타일·벽지)는 하나라도 깔려 있으면 산 것(보유 1)으로 본다.
    // RestorePlacement/RestoreOwned를 모두 끝낸 뒤 호출한다.
    public void EnsureOwnedCoversPlacements()
    {
        foreach (var record in mDicPlacedFurniture.Values)
        {
            var key = (record.IsSub, record.Tid);
            int placed = IsUnlimitedFurniture(record.Tid, record.IsSub) ? 1 : GetPlacedCount(record.Tid, record.IsSub);
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

    // 켜기는 영업 정산 후 ~ 영업 시작 전(BusinessModel.CanEditLayout)에만 된다. 끄기는 언제나 된다.
    public void SetEditMode(bool isEdit)
    {
        if (isEdit && !GameInstance.Model.Business.CanEditLayout)
            return;

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
        if (placeable.DessertStand != null)
            placeable.DessertStand.ReturnDisplayedDessertToStorage();
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

    // 들고 있는 것이 새로 꺼낸 대량 설치 가구(바닥타일/벽지)인지. 들고 있는 미리보기는 커서에 둔 채 지나가는 칸마다 놓는다(PlaceBulk).
    // - 바닥타일: 가구 오브젝트로 놓지 않고 층의 바닥 Tilemap에 칠한다. 칠해진 바닥은 집어서 옮기지 않는다(철거 후 다시 칠함).
    // - 벽지: 칸마다 벽지 오브젝트를 하나씩 놓는다. 다른 벽지가 붙어 있던 칸은 새 벽지로 바꿔 붙인다(이전 벽지는 그냥 치움 — 무제한 설치라 회수할 수량이 없다).
    public bool IsBulkPlacing => mIsPlacing.Value && mPlaceable != null && mPlaceable.PlacementId < 0 && mPlaceable.IsBulkPlaceable;

    // 들고 있는 가구가 벽에 붙는 가구인지(벽지 드래그 설치가 시작한 벽 줄을 유지하는 데 쓴다).
    public bool IsPlacingOnWall => mIsPlacing.Value && mPlaceable != null && mPlaceable.Surface == ePlacementSurface.Wall;

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

    #region Wallpaper

    // 벽지(FurnitureType=벽)인지.
    public static bool IsWallpaperFurniture(int tid, bool isSub)
    {
        if (isSub)
            return false;

        var row = GameInstance.Table.Get<CTable.FurnitureRow>(tid);
        return row != null && row.FurnitureType == (int)CTable.eFurnitureType.Wall;
    }

    // 새로 꺼낸 벽지를 들고 있는 동안에는 다른 벽지가 붙은 칸도 놓을 수 있는 칸으로 본다(바꿔 붙이기). 그 밖에는 null(겹치면 무효).
    private System.Func<int, bool> GetPlacingReplacePredicate()
    {
        if (!IsBulkPlacing || !IsWallpaperFurniture(mPlaceable.FurnitureTid, false))
            return null;

        return placementId => mDicPlacedFurniture.TryGetValue(placementId, out var record) && IsWallpaperFurniture(record.Tid, record.IsSub);
    }

    // origins마다 들고 있는 벽지를 하나씩 놓는다. 같은 칸에 같은 벽지가 이미 있으면 건너뛰고, 다른 벽지는 치운 뒤 놓는다.
    private int PlaceWallpapers(IReadOnlyList<Vector3Int> origins)
    {
        var prefab = LoadFurniturePrefab(mPlaceable.FurnitureTid, false);
        if (prefab == null)
            return 0;

        int tid = mPlaceable.FurnitureTid;
        var canReplace = GetPlacingReplacePredicate();
        int placed = 0;
        foreach (var origin in origins)
        {
            if (!mArea.IsBasePlacementValidAt(origin, mFootprint, mPlaceable.Surface, mPlaceable.LayoutOrder, canReplace))
                continue;

            var occupantIds = mArea.GetBaseOccupantIds(origin, mFootprint, mPlaceable.LayoutOrder);
            var cell = new Vector2Int(origin.x, origin.y);
            // 같은 칸에 같은 벽지가 같은 모양(좌우반전 포함)으로 이미 있으면 건너뛴다. 반전만 다르면 새 모양으로 바꿔 붙인다.
            if (occupantIds.Count == 1 && mDicPlacedFurniture.TryGetValue(occupantIds[0], out var existing) &&
                existing.Tid == tid && existing.Cell == cell && existing.Direction == mPlaceable.Direction && existing.FlipX == mPlaceable.FlipX)
                continue;

            foreach (int occupantId in occupantIds)
                RemovePlacedObject(occupantId);

            var position = mArea.GetFootprintCenterWorld(origin, mFootprint, mTarget.position.z);
            var instance = Object.Instantiate(prefab, position, Quaternion.identity, mArea.transform);
            var placeable = instance.GetComponent<PlaceableObject>();
            if (placeable == null)
            {
                Object.Destroy(instance);
                continue;
            }

            placeable.SetFurnitureTid(tid);
            placeable.SetOrientation(mPlaceable.Direction, mPlaceable.FlipX);
            placeable.SetArea(mArea);
            placeable.SetPlacementId(RegisterPlacement(tid, position, mArea.Floor, cell, placeable.Direction, placeable.FlipX, notify: false));
            mArea.OccupyBase(position, placeable.Footprint, placeable.PlacementId, placeable.GroupId, placeable.LayoutOrder,
                placeable.AccessOffsets, placeable.ClearanceOffsets);
            placed++;
        }

        // 벽지는 손님 동선과 무관하고(PlacedLayoutChanged 불필요), 무제한 설치라 목록의 보관 수량도 바뀌지 않는다(InventoryVersion 불필요).
        // 드래그 중 매 프레임 씬 전체 동선 재수집·목록 갱신이 일어나지 않도록 둘 다 알리지 않는다.
        if (placed > 0)
            Physics2D.SyncTransforms();
        return placed;
    }

    // 배치된 가구 하나를 씬과 배치 기록에서 지운다(벽지 바꿔 붙이기). 오브젝트를 못 찾아도 기록과 칸 점유는 지운다.
    private void RemovePlacedObject(int placementId)
    {
        var target = PlaceableObject.FindByPlacementId(mArea, placementId);
        if (target != null)
            ReleasePlacedObject(target);
        else
            mArea.ReleaseByPlacementId(placementId);

        mDicPlacedFurniture.Remove(placementId);
    }

    #endregion

    #region Floor Tiles

    // 바닥타일(FurnitureType=타일)인지. 바닥타일은 가구 배치 기록 대신 층별 바닥 칸 기록(mFloorTiles)으로 다룬다.
    public static bool IsFloorTileFurniture(int tid, bool isSub)
    {
        if (isSub)
            return false;

        var row = GameInstance.Table.Get<CTable.FurnitureRow>(tid);
        return row != null && row.FurnitureType == (int)CTable.eFurnitureType.Tile;
    }

    // 들고 있는 대량 설치 가구를 cells(footprint 기준 셀)마다 놓는다. 놓을 수 없는 칸과 이미 같은 종류인 칸은 건너뛰고,
    // 다른 종류가 있던 칸은 덮어쓴다. 꺼낼 때 산 무제한 설치 가구라 골드가 들지 않는다. 반환값 = 새로 놓은 칸 수.
    public int PlaceBulk(IReadOnlyList<Vector3Int> cells)
    {
        if (!IsBulkPlacing || mArea == null || cells == null || cells.Count == 0)
            return 0;

        return IsFloorTileFurniture(mPlaceable.FurnitureTid, false)
            ? PaintFloorTiles(mArea, mPlaceable.FurnitureTid, cells, mFootprint)
            : PlaceWallpapers(cells);
    }

    // 철거 모드에서 커서 아래의 칠해진 바닥 칸. 바닥타일은 오브젝트가 없어 PlaceableObject 대신 칸으로 찾는다.
    public bool TryGetFloorTileAt(Vector3 worldPos, out TilePlacementArea area, out Vector3Int cell)
    {
        cell = default;
        area = TilePlacementArea.FindContaining(worldPos);
        if (area == null)
            return false;

        cell = area.WorldToCell(worldPos);
        return HasFloorTileRecord(area.Floor, cell);
    }

    // 칠해진 바닥 칸 하나를 지운다. 무제한 설치 가구라 보관함으로 회수할 수량은 없다. 들고 있는 동안에는 하지 않는다.
    // 그림이 아니라 기록으로 판단한다 — 아트 설정이 빠져 그려지지 않은 칸도 지울 수 있게.
    public bool EraseFloorTile(TilePlacementArea area, Vector3Int cell)
    {
        if (area == null || mIsPlacing.Value || !mFloorTiles.TryGetValue(area.Floor, out var floorTiles) ||
            !floorTiles.Remove(new Vector2Int(cell.x, cell.y)))
            return false;

        area.EraseFloorTile(cell);
        return true;
    }

    private bool HasFloorTileRecord(int floor, Vector3Int cell)
    {
        return mFloorTiles.TryGetValue(floor, out var floorTiles) && floorTiles.ContainsKey(new Vector2Int(cell.x, cell.y));
    }

    public IEnumerable<(int floor, Vector2Int cell, int tid)> GetAllFloorTiles()
    {
        foreach (var floorPair in mFloorTiles)
        {
            foreach (var cellPair in floorPair.Value)
                yield return (floorPair.Key, cellPair.Key, cellPair.Value);
        }
    }

    // 세이브 복원 전용. 그림은 RespawnSavedFurniture에서 층 영역을 찾은 뒤 칠한다.
    public void RestoreFloorTile(int floor, Vector2Int cell, int tid)
    {
        GetOrCreateFloorTiles(floor)[cell] = tid;
    }

    // origins = footprint 기준 셀(좌하단). 1×1보다 큰 바닥타일이면 footprint 칸을 모두 칠한다.
    private int PaintFloorTiles(TilePlacementArea area, int tid, IReadOnlyList<Vector3Int> origins, Vector2Int footprint)
    {
        var tile = GetFurnitureVisual(tid, false)?.FloorTile;
        if (tile == null)
        {
            Logger.Warning($"[PlacementModel] 바닥타일 아트 설정에 칠할 타일이 없습니다. Tid={tid}");
            return 0;
        }

        var floorTiles = GetOrCreateFloorTiles(area.Floor);
        var painted = new List<Vector3Int>();
        foreach (var origin in origins)
        {
            for (int x = 0; x < footprint.x; x++)
            {
                for (int y = 0; y < footprint.y; y++)
                {
                    var cell = new Vector3Int(origin.x + x, origin.y + y, 0);
                    var key = new Vector2Int(cell.x, cell.y);
                    // 같은 종류가 기록돼 있어도 그려져 있지 않으면(아트 설정이 늦게 채워진 경우 등) 다시 칠한다.
                    if (!area.IsFloorPaintable(cell) || (floorTiles.TryGetValue(key, out int current) && current == tid && area.HasFloorTile(cell)))
                        continue;

                    floorTiles[key] = tid;
                    painted.Add(cell);
                }
            }
        }

        area.SetFloorTiles(painted, tile);
        return painted.Count;
    }

    // 저장된 바닥 칸을 층 영역마다 종류별로 한 번에 칠한다. 영역이나 칠할 타일을 못 찾은 칸은 기록만 남는다.
    private void RespawnFloorTiles(TilePlacementArea[] allAreas)
    {
        foreach (var floorPair in mFloorTiles)
        {
            var area = System.Array.Find(allAreas, a => a != null && a.Floor == floorPair.Key);
            if (area == null)
            {
                Logger.Warning($"[PlacementModel] {floorPair.Key}층의 TilePlacementArea를 찾지 못해 바닥타일을 그리지 못했습니다.");
                continue;
            }

            foreach (var group in floorPair.Value.GroupBy(pair => pair.Value))
            {
                var tile = GetFurnitureVisual(group.Key, false)?.FloorTile;
                if (tile == null)
                {
                    Logger.Warning($"[PlacementModel] 바닥타일 아트 설정에 칠할 타일이 없습니다. Tid={group.Key}");
                    continue;
                }

                area.SetFloorTiles(group.Select(pair => new Vector3Int(pair.Key.x, pair.Key.y, 0)).ToList(), tile);
            }
        }
    }

    private Dictionary<Vector2Int, int> GetOrCreateFloorTiles(int floor)
    {
        if (!mFloorTiles.TryGetValue(floor, out var floorTiles))
        {
            floorTiles = new Dictionary<Vector2Int, int>();
            mFloorTiles[floor] = floorTiles;
        }
        return floorTiles;
    }

    #endregion

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
            mArea.ShowBaseHighlight(mFootprint, mPlaceable.Surface, mPlaceable.LayoutOrder, mPlaceable.PlacementId, mPlaceable.AccessOffsets, mPlaceable.ClearanceOffsets,
                GetPlacingReplacePredicate());

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
                mPlaceable != null ? mPlaceable.AccessOffsets : null, mPlaceable != null ? mPlaceable.ClearanceOffsets : null, out snappedPos,
                GetPlacingReplacePredicate());

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

            // 새로 꺼낸 대량 설치 가구(확정 버튼)는 들고 있는 미리보기 대신 그 칸에 놓은 뒤(바닥타일은 칠함) 미리보기를 치운다.
            if (IsBulkPlacing)
            {
                PlaceBulk(new[] { origin });
                Cancel();
                return;
            }

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

    // 가구 구매 목록 — 로비 패널의 가구 배치 화면(UISidePlacementView)과 상점가 가구 구입(UIPopupShopFurniture)이 공유한다.
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
                    StoredCount = GetStoredCount(row.Tid, false), IsUnlimitedUnlocked = IsUnlimitedUnlocked(row.Tid, false) });
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
    // 이미 산 무제한 설치 가구(바닥타일·벽지)는 다시 사지 않고 골드 없이 꺼낸다.
    public bool TryPurchaseAndBeginPlacement(UIScrollFurnitureData data) => TryBeginPlacement(data, useStored: false);

    // 로비 가구배치 목록 — 보관 중인 가구(또는 이미 산 바닥타일·벽지)는 골드 없이 꺼내 놓고, 없으면 골드로 산다.
    public bool TryPlaceOrPurchase(UIScrollFurnitureData data) => TryBeginPlacement(data, useStored: true);

    // 상점가 [가구 구입]을 영업 중에 누른 경우 — 배치는 못 하니 골드로 사서 보관함에만 넣는다(보유 +1).
    // 이미 산 무제한 설치 가구(바닥타일·벽지)는 더 살 것이 없어 false.
    public bool TryPurchaseToStorage(UIScrollFurnitureData data)
    {
        if (data == null || IsUnlimitedUnlocked(data.Tid, data.IsSub))
            return false;

        return TryPurchase(data.Tid, data.IsSub, data.Price);
    }

    private bool TryBeginPlacement(UIScrollFurnitureData data, bool useStored)
    {
        // 가구 배치는 영업 정산 후 ~ 영업 시작 전에만 할 수 있다.
        if (data == null || !GameInstance.Model.Business.CanEditLayout)
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

        bool hasUnit = useStored || IsUnlimitedFurniture(data.Tid, data.IsSub)
            ? TryConsumeForPlacement(data.Tid, data.IsSub, data.Price, reservedStoredCount: 0)
            : TryPurchase(data.Tid, data.IsSub, data.Price);
        if (!hasUnit)
        {
            Logger.Log("[PlacementModel] 골드가 부족합니다.");
            return false;
        }

        // 다른 가구를 들고 있으면 그 가구를 취소하고 새 가구로 바꿔 든다(새로 꺼낸 것 → 보관함, 옮기던 것 → 원위치).
        // 새 가구를 꺼낼 수 있는지(영역/프리팹/골드)를 먼저 확인한 뒤에 취소해야 실패했을 때 들고 있던 가구가 사라지지 않는다.
        // Cancel은 보유 수·골드를 바꾸지 않으므로 위에서 확보한 몫에 영향이 없다.
        if (mIsPlacing.Value)
            Cancel();

        // 층별 TilePlacementArea 오브젝트 아래에 생성해서 씬 계층 구조에서 어느 층 소속인지 바로 보이게 한다.
        var instance = Object.Instantiate(prefab, area.Bounds.center, Quaternion.identity, area.transform);
        var placeable = instance.GetComponent<PlaceableObject>();

        if (data.IsSub)
            placeable.SetSubFurnitureTid(data.Tid);
        else
            placeable.SetFurnitureTid(data.Tid);

        placeable.BeginPlacementFromSpawn(area);

        // Table.prefab처럼 가구에 Intaraction_DessertStand가 같이 붙어 있으면(다른 가구는 없음) 여기서 Init() —
        // 새로 놓는 가구는 mTableId==0(미지정)으로 시작해, 이후 UIPopupDessertSelect에서 처음 넣는 빵으로 종류가 정해진다.
        instance.GetComponent<Intaraction_DessertStand>()?.Init();
        return true;
    }

    // 가구 1개를 놓을 몫을 확보한다: 보관 수량(reservedStoredCount개는 남겨 둠)이 있으면 그걸 쓰고(보유/골드 변화 없음),
    // 없으면 골드로 산다(보유 +1). 골드가 부족하면 false. 무제한 설치 가구는 한 번 샀으면 항상 true.
    private bool TryConsumeForPlacement(int tid, bool isSub, long price, int reservedStoredCount, bool notify = true)
    {
        if (IsUnlimitedUnlocked(tid, isSub) || GetStoredCount(tid, isSub) > reservedStoredCount)
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

            // GameModeLobby+FSM.InitDessertStands()는 이 시점보다 먼저 끝나 있어 DessertModel 재고 복원(ApplyPendingDessertData)이
            // 이미 끝난 뒤다 — 종류 재지정만 반영하고, 진열 수량(SyncDisplayToSavedCount)도 여기서 직접 채워준다.
            var dessertStand = instance.GetComponent<Intaraction_DessertStand>();
            if (dessertStand != null)
            {
                dessertStand.Init();
                dessertStand.RestoreAssignedDessertType(record.AssignedDessertTid);
                dessertStand.SyncDisplayToSavedCount();
            }
        }

        RespawnFloorTiles(allAreas);
    }

    // 가구배치 리셋 — 지금까지 배치한 가구를 전부 씬에서 제거하고 배치 기록도 함께 비운다.
    // 가구를 팔지 않는다: 보유 수는 그대로라 치운 가구는 전부 보관 중으로 돌아가고, 배치 목록에서 골드 없이 다시 놓을 수 있다.
    // 칠해진 바닥타일도 모두 지운다(무제한 설치라 다시 칠하면 된다).
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
        {
            area.ClearAllOccupants();
            area.ClearFloorTiles();
        }

        mFloorTiles.Clear();
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

    // 목록 미리보기 그림. 아트 설정의 앞모습(Front), 없으면 프리팹의 첫 SpriteRenderer 그림. 둘 다 없으면 null.
    public Sprite GetFurnitureIcon(int tid, bool isSub)
    {
        var visual = GetFurnitureVisual(tid, isSub);
        if (visual == null)
            return null;
        if (visual.Front != null)
            return visual.Front;

        var prefab = LoadVisualPrefab(visual);
        var renderer = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
        return renderer != null ? renderer.sprite : null;
    }

    // 아트 설정에 연결된 배치 프리팹(AssetReferenceGameObject → Addressables GUID 키로 로드).
    private GameObject LoadFurniturePrefab(int tid, bool isSub)
    {
        var visual = GetFurnitureVisual(tid, isSub);
        if (visual == null)
            return null;

        return LoadVisualPrefab(visual);
    }

    // 아트 설정에 지정된 프리팹을 직접 로드한다. CSV에 연결되지 않은 아트 설정도 로드할 수 있다(가구 테스트 씬).
    public GameObject LoadVisualPrefab(FurnitureVisualData visual)
    {
        if (visual == null)
            return null;

        if (visual.Prefab == null || !visual.Prefab.RuntimeKeyIsValid())
        {
            Logger.Warning($"[PlacementModel] 아트 설정 '{visual.name}'에 프리팹이 지정되지 않았습니다.");
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
