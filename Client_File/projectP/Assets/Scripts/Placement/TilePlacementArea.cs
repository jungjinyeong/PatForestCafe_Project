using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// 가구가 붙을 수 있는 면. 벽 셀 = 이 층의 PlacementWallTilemap에 타일이 있는 배치 셀, 나머지 배치 셀 = 바닥.
public enum ePlacementSurface
{
    Floor,
    Wall,
}

public class TilePlacementArea : MonoBehaviour
{
    // 배치 가능 칸 강조 오버레이. 바닥 타일(-3)/카펫(-2)/벽 배경(-1) 위, 가구·캐릭터(1) 아래에 그린다.
    private const int HIGHLIGHT_SORTING_ORDER = 0;
    // 칸 테두리 + 옅은 채움 텍스처(32px = 1칸). 잔디처럼 밝은 바닥에서도 보이도록 채움은 흰색, 테두리는 진하게.
    private const int HIGHLIGHT_TEXTURE_SIZE = 32;
    private static readonly Color HIGHLIGHT_FILL = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color HIGHLIGHT_BORDER = new Color(1f, 1f, 0.75f, 0.85f);
    // 같은 면(바닥/벽)인데 놓을 수 없는 칸(다른 가구가 차지, 손님 접근 칸, 테이블 앞 칸이 막힘 등)은 붉게 표시한다.
    private static readonly Color HIGHLIGHT_BLOCKED_FILL = new Color(1f, 0.25f, 0.25f, 0.28f);
    private static readonly Color HIGHLIGHT_BLOCKED_BORDER = new Color(1f, 0.35f, 0.35f, 0.85f);
    // 이 레이어 이상(일반 가구)만 손님 접근 칸을 막는다. 바닥타일(0)/카펫(1)은 위에 서도 되므로 깔 수 있다.
    private const int ACCESS_BLOCKING_LAYER = 2;

    [SerializeField] private Tilemap mTilemap;
    // 벽 가구를 놓을 수 있는 셀을 칠해 둔 보이지 않는 타일맵(PlaceableObjects/PlacementWallTilemap). 비어 있으면 모든 셀을 바닥으로 취급한다.
    [SerializeField] private Tilemap mWallTilemap;

    private struct BaseSlot
    {
        public int PlacementId;
        public int GroupId;
    }

    // 셀 하나의 점유 정보. 베이스 가구는 레이아웃 순서(FurnitureRow.LayoutOrder)별로 한 칸씩 따로 점유한다 —
    // 같은 레이어끼리만 겹칠 수 없고, 다른 레이어(바닥타일 위 카펫, 카펫 위 테이블 등)는 같은 칸에 겹쳐 놓을 수 있다.
    private class CellOccupant
    {
        public readonly Dictionary<int, BaseSlot> Bases = new Dictionary<int, BaseSlot>();
        public int SubPlacementId = -1;
    }

    // 셀 좌표 -> 점유 정보.
    private readonly Dictionary<Vector3Int, CellOccupant> mOccupants = new Dictionary<Vector3Int, CellOccupant>();
    // placementId -> 그 placement가 차지한 셀 목록. ReleaseByPlacementId에서 역조회용.
    private readonly Dictionary<int, List<Vector3Int>> mCellsByPlacementId = new Dictionary<int, List<Vector3Int>>();
    // 비워둬야 하는 칸(손님 접근 칸 + 작업자가 지정한 비워둘 칸) -> 그 칸을 예약한 placementId들. 일반 가구는 이 칸에 놓을 수 없다.
    // 두 테이블이 같은 칸을 함께 비워두게 할 수 있어 칸 하나에 여러 placement가 걸릴 수 있다.
    private readonly Dictionary<Vector3Int, List<int>> mAccessReservedBy = new Dictionary<Vector3Int, List<int>>();
    // placementId -> 그 placement가 예약한 칸 목록. ReleaseByPlacementId에서 역조회용.
    private readonly Dictionary<int, List<Vector3Int>> mAccessCellsByPlacementId = new Dictionary<int, List<Vector3Int>>();

    private Tilemap mHighlightTilemap;
    private Tile mHighlightTile;
    private Tile mBlockedTile;
    // 드래그 중인 가구 자신의 칸/손님 접근 칸을 붉게 덮어 칠한 칸 -> 덮기 전 강조 타일(되돌리기용).
    private readonly Dictionary<Vector3Int, TileBase> mPreviewOverwritten = new Dictionary<Vector3Int, TileBase>();

    // 신규 구매 가구를 처음 스폰할 때 이 영역 중앙 근처에 놓기 위한 용도(드래그 시작 전 임시 위치).
    public Bounds Bounds
    {
        get
        {
            if (mTilemap == null)
                return default;

            BoundsInt cellBounds = mTilemap.cellBounds;
            Vector3 min = mTilemap.CellToWorld(cellBounds.min);
            Vector3 max = mTilemap.CellToWorld(cellBounds.max);

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }
    }

    // 이 영역이 속한 층 번호(부모 WaypointGroup.Order). 세이브에 가구의 층을 기록하는 데 쓴다.
    public int Floor
    {
        get
        {
            var group = GetComponentInParent<WaypointGroup>();
            return group != null ? group.Order : 0;
        }
    }

    public bool Contains(Vector3 worldPos)
    {
        return mTilemap != null && mTilemap.HasTile(mTilemap.WorldToCell(worldPos));
    }

    // 일반(베이스) 가구용 유효 위치 판정: footprint 전체 셀이 타일이 있고, 가구가 붙는 면(벽/바닥)과 일치하며,
    // 같은 레이어를 다른 베이스가 점유하지 않은(또는 selfPlacementId 자신) 경우만 유효. 벽과 바닥에 걸치면 무효.
    // accessOffsets(footprint 기준 셀 오프셋)가 있으면 그 접근 칸들도 손님이 설 수 있는 바닥이어야 하고,
    // clearanceOffsets(비워둘 칸)는 영역 안이면 다른 일반 가구가 없어야 한다(벽/영역 밖은 통과).
    public bool TryGetSnappedPositionForBase(Vector3 worldPos, Vector2Int footprintCells, int selfPlacementId, ePlacementSurface surface, int layer,
        IReadOnlyList<Vector2Int> accessOffsets, IReadOnlyList<Vector2Int> clearanceOffsets, out Vector3 snappedPos)
    {
        snappedPos = worldPos;

        if (mTilemap == null)
            return false;

        Vector3Int origin = GetFootprintOrigin(worldPos, footprintCells);

        foreach (var cell in IterateFootprintCells(origin, footprintCells))
        {
            if (!IsBaseCellAvailable(cell, surface, layer, selfPlacementId))
                return false;
        }

        if (!AreAccessCellsAvailable(origin, accessOffsets, selfPlacementId) || !AreClearanceCellsAvailable(origin, clearanceOffsets, selfPlacementId))
            return false;

        snappedPos = GetFootprintCenterWorld(origin, footprintCells, worldPos.z);
        return true;
    }

    // sub가구용 유효 위치 판정: footprint 전체 셀에 allowedGroupId(0 초과, 즉 실제로 지정된 그룹)의 베이스가 (어느 레이어든)
    // 있고, sub가 비어있거나(또는 selfPlacementId 자신) 있어야 유효.
    // allowedGroupId<=0(미지정)은 항상 무효 — 그룹이 없는 가구(바닥타일/카펫 등, GroupId==0) 위에 아무 sub나 얹히는 걸 막는다.
    public bool TryGetSnappedPositionForSub(Vector3 worldPos, Vector2Int footprintCells, int allowedGroupId, int selfPlacementId, out Vector3 snappedPos)
    {
        snappedPos = worldPos;

        if (mTilemap == null || allowedGroupId <= 0)
            return false;

        Vector3Int origin = GetFootprintOrigin(worldPos, footprintCells);

        foreach (var cell in IterateFootprintCells(origin, footprintCells))
        {
            if (!IsSubCellAvailable(cell, allowedGroupId, selfPlacementId))
                return false;
        }

        snappedPos = GetFootprintCenterWorld(origin, footprintCells, worldPos.z);
        return true;
    }

    public void OccupyBase(Vector3 worldPos, Vector2Int footprintCells, int placementId, int groupId, int layer,
        IReadOnlyList<Vector2Int> accessOffsets, IReadOnlyList<Vector2Int> clearanceOffsets)
    {
        ReleaseByPlacementId(placementId);

        Vector3Int origin = GetFootprintOrigin(worldPos, footprintCells);
        var cells = GetOrCreateCellList(placementId);

        foreach (var cell in IterateFootprintCells(origin, footprintCells))
        {
            GetOrCreateOccupant(cell).Bases[layer] = new BaseSlot { PlacementId = placementId, GroupId = groupId };
            cells.Add(cell);
        }

        List<Vector3Int> reservedCells = null;
        foreach (var cell in IterateAccessCells(origin, accessOffsets).Concat(IterateAccessCells(origin, clearanceOffsets)))
        {
            reservedCells ??= new List<Vector3Int>();
            if (reservedCells.Contains(cell))
                continue;

            if (!mAccessReservedBy.TryGetValue(cell, out List<int> owners))
            {
                owners = new List<int>();
                mAccessReservedBy[cell] = owners;
            }
            owners.Add(placementId);
            reservedCells.Add(cell);
        }
        if (reservedCells != null)
            mAccessCellsByPlacementId[placementId] = reservedCells;
    }

    public void OccupySub(Vector3 worldPos, Vector2Int footprintCells, int placementId)
    {
        ReleaseByPlacementId(placementId);

        Vector3Int origin = GetFootprintOrigin(worldPos, footprintCells);
        var cells = GetOrCreateCellList(placementId);

        foreach (var cell in IterateFootprintCells(origin, footprintCells))
        {
            GetOrCreateOccupant(cell).SubPlacementId = placementId;
            cells.Add(cell);
        }
    }

    // placementId가 차지했던 셀들에서 자기 몫만 지운다(베이스면 해당 레이어 슬롯, sub면 SubPlacementId).
    // 베이스인지 sub인지, 어느 레이어인지는 셀에 실제로 기록된 값으로 판단하므로 호출부에서 구분해 넘길 필요 없다.
    public void ReleaseByPlacementId(int placementId)
    {
        if (mAccessCellsByPlacementId.TryGetValue(placementId, out List<Vector3Int> accessCells))
        {
            foreach (var cell in accessCells)
            {
                if (!mAccessReservedBy.TryGetValue(cell, out List<int> owners))
                    continue;

                owners.Remove(placementId);
                if (owners.Count == 0)
                    mAccessReservedBy.Remove(cell);
            }
            mAccessCellsByPlacementId.Remove(placementId);
        }

        if (!mCellsByPlacementId.TryGetValue(placementId, out List<Vector3Int> cells))
            return;

        var removeLayers = new List<int>();
        foreach (var cell in cells)
        {
            if (!mOccupants.TryGetValue(cell, out CellOccupant occupant))
                continue;

            removeLayers.Clear();
            foreach (var pair in occupant.Bases)
            {
                if (pair.Value.PlacementId == placementId)
                    removeLayers.Add(pair.Key);
            }
            foreach (int layer in removeLayers)
                occupant.Bases.Remove(layer);

            if (occupant.SubPlacementId == placementId)
                occupant.SubPlacementId = -1;
        }

        mCellsByPlacementId.Remove(placementId);
    }

    // basePlacementId가 차지한 셀 중 sub가 하나라도 올라가 있으면 true. 베이스 재드래그(이동) 차단용.
    // sub는 그룹이 지정된 베이스(GroupId>0) 위에만 올라가므로, 그룹 없는 베이스(바닥타일/카펫)는 sub와 무관하게 옮길 수 있다.
    public bool HasAnySubOccupant(int basePlacementId)
    {
        if (!mCellsByPlacementId.TryGetValue(basePlacementId, out List<Vector3Int> cells))
            return false;

        foreach (var cell in cells)
        {
            if (!mOccupants.TryGetValue(cell, out CellOccupant occupant) || occupant.SubPlacementId < 0)
                continue;

            foreach (var slot in occupant.Bases.Values)
            {
                if (slot.PlacementId == basePlacementId && slot.GroupId > 0)
                    return true;
            }
        }
        return false;
    }

    public void ClearAllOccupants()
    {
        mOccupants.Clear();
        mCellsByPlacementId.Clear();
        mAccessReservedBy.Clear();
        mAccessCellsByPlacementId.Clear();
        HideHighlight();
    }

    // 배치 셀이 벽인지 바닥인지. 벽 타일맵이 다른 Grid에 있어도 되도록 월드 좌표로 변환해 조회한다.
    public ePlacementSurface GetSurface(Vector3Int cell)
    {
        if (mWallTilemap == null || mTilemap == null)
            return ePlacementSurface.Floor;

        Vector3Int wallCell = mWallTilemap.WorldToCell(mTilemap.GetCellCenterWorld(cell));
        return mWallTilemap.HasTile(wallCell) ? ePlacementSurface.Wall : ePlacementSurface.Floor;
    }

    #region Highlight

    // 드래그 중 베이스 가구를 놓을 수 있는 칸(면 일치 + 같은 레이어 비어 있음 + 손님 접근 칸 확보)은 밝게,
    // 같은 면인데 놓을 수 없는 칸은 붉게 강조한다. 접근 칸은 그 칸을 기준 셀로 놓았을 때 기준(1칸 가구 기준).
    public void ShowBaseHighlight(ePlacementSurface surface, int layer, int selfPlacementId,
        IReadOnlyList<Vector2Int> accessOffsets, IReadOnlyList<Vector2Int> clearanceOffsets)
    {
        ShowHighlight(
            cell => IsBaseCellAvailable(cell, surface, layer, selfPlacementId)
                    && AreAccessCellsAvailable(cell, accessOffsets, selfPlacementId)
                    && AreClearanceCellsAvailable(cell, clearanceOffsets, selfPlacementId),
            cell => GetSurface(cell) == surface);
    }

    // 드래그 중 sub가구를 놓을 수 있는 칸(허용 그룹 베이스 위 + sub 비어 있음)을 강조한다.
    public void ShowSubHighlight(int allowedGroupId, int selfPlacementId)
    {
        ShowHighlight(cell => allowedGroupId > 0 && IsSubCellAvailable(cell, allowedGroupId, selfPlacementId), null);
    }

    public void HideHighlight()
    {
        mPreviewOverwritten.Clear();
        if (mHighlightTilemap != null)
            mHighlightTilemap.ClearAllTiles();
    }

    // 드래그 중인 가구가 놓이면 막게 될 칸(자기 칸 + 손님 접근 칸)을 다른 가구처럼 붉게 표시한다. 가구를 따라 매번 갱신.
    // 이전에 덮어 칠한 칸은 원래 강조(흰/붉은/없음)로 되돌린 뒤 새 위치를 칠한다.
    public void ShowPlacementPreview(Vector3Int origin, Vector2Int footprintCells, IReadOnlyList<Vector2Int> accessOffsets, IReadOnlyList<Vector2Int> clearanceOffsets)
    {
        if (mHighlightTilemap == null)
            return;

        ClearPlacementPreview();

        foreach (var cell in IterateFootprintCells(origin, footprintCells))
            PaintPreviewCell(cell);
        foreach (var cell in IterateAccessCells(origin, accessOffsets))
            PaintPreviewCell(cell);
        foreach (var cell in IterateAccessCells(origin, clearanceOffsets))
            PaintPreviewCell(cell);
    }

    public void ClearPlacementPreview()
    {
        if (mHighlightTilemap != null)
        {
            foreach (var pair in mPreviewOverwritten)
                mHighlightTilemap.SetTile(pair.Key, pair.Value);
        }
        mPreviewOverwritten.Clear();
    }

    private void PaintPreviewCell(Vector3Int cell)
    {
        if (!mTilemap.HasTile(cell) || mPreviewOverwritten.ContainsKey(cell))
            return;

        mPreviewOverwritten[cell] = mHighlightTilemap.GetTile(cell);
        mHighlightTilemap.SetTile(cell, mBlockedTile);
    }

    // isAvailable 칸은 밝게, 그렇지 않은데 isBlockedCandidate(같은 면 등)인 칸은 붉게 칠한다. isBlockedCandidate가 null이면 붉은 칸은 없다.
    private void ShowHighlight(System.Func<Vector3Int, bool> isAvailable, System.Func<Vector3Int, bool> isBlockedCandidate)
    {
        if (mTilemap == null || !EnsureHighlightTilemap())
            return;

        mHighlightTilemap.ClearAllTiles();

        BoundsInt bounds = mTilemap.cellBounds;
        foreach (var cell in bounds.allPositionsWithin)
        {
            if (!mTilemap.HasTile(cell))
                continue;

            if (isAvailable(cell))
                mHighlightTilemap.SetTile(cell, mHighlightTile);
            else if (isBlockedCandidate != null && isBlockedCandidate(cell))
                mHighlightTilemap.SetTile(cell, mBlockedTile);
        }
    }

    // 배치 타일맵과 같은 Grid 아래에 런타임 전용 강조 타일맵을 만든다(씬/프리팹에 저장되지 않음).
    private bool EnsureHighlightTilemap()
    {
        if (mHighlightTilemap != null)
            return true;

        var grid = mTilemap.layoutGrid;
        if (grid == null)
            return false;

        var go = new GameObject("PlacementHighlight") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(grid.transform, false);

        mHighlightTilemap = go.AddComponent<Tilemap>();
        mHighlightTilemap.tileAnchor = mTilemap.tileAnchor;

        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = HIGHLIGHT_SORTING_ORDER;

        mHighlightTile = CreateHighlightTile(HIGHLIGHT_FILL, HIGHLIGHT_BORDER);
        mBlockedTile = CreateHighlightTile(HIGHLIGHT_BLOCKED_FILL, HIGHLIGHT_BLOCKED_BORDER);
        return true;
    }

    private static Tile CreateHighlightTile(Color fill, Color border)
    {
        var texture = CreateHighlightTexture(fill, border);
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.hideFlags = HideFlags.DontSave;
        tile.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        tile.colliderType = Tile.ColliderType.None;
        return tile;
    }

    private static Texture2D CreateHighlightTexture(Color fill, Color border)
    {
        int size = HIGHLIGHT_TEXTURE_SIZE;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave,
        };

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                pixels[y * size + x] = isBorder ? border : fill;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    #endregion

    private bool IsBaseCellAvailable(Vector3Int cell, ePlacementSurface surface, int layer, int selfPlacementId)
    {
        if (!mTilemap.HasTile(cell) || GetSurface(cell) != surface)
            return false;

        // 다른 가구의 손님 접근 칸에는 일반 가구를 놓을 수 없다(바닥타일/카펫은 가능).
        if (layer >= ACCESS_BLOCKING_LAYER && IsReservedByOther(cell, selfPlacementId))
            return false;

        return !mOccupants.TryGetValue(cell, out CellOccupant occupant) ||
               !occupant.Bases.TryGetValue(layer, out BaseSlot slot) ||
               slot.PlacementId == selfPlacementId;
    }

    // 손님 접근 칸: 배치 영역 안의 바닥이고, 다른 일반 가구(레이어 2 이상)가 차지하지 않은 칸이어야 손님이 설 수 있다.
    private bool IsAccessCellAvailable(Vector3Int cell, int selfPlacementId)
    {
        if (!mTilemap.HasTile(cell) || GetSurface(cell) != ePlacementSurface.Floor)
            return false;

        if (!mOccupants.TryGetValue(cell, out CellOccupant occupant))
            return true;

        foreach (var pair in occupant.Bases)
        {
            if (pair.Key >= ACCESS_BLOCKING_LAYER && pair.Value.PlacementId != selfPlacementId)
                return false;
        }
        return true;
    }

    private bool IsReservedByOther(Vector3Int cell, int selfPlacementId)
    {
        if (!mAccessReservedBy.TryGetValue(cell, out List<int> owners))
            return false;

        foreach (int owner in owners)
        {
            if (owner != selfPlacementId)
                return true;
        }
        return false;
    }

    // 비워둘 칸: 벽이거나 배치 영역 밖이면 상관없고(벽에 붙여 놓기 허용), 영역 안 바닥이면 다른 일반 가구가 없어야 한다.
    private bool IsClearanceCellAvailable(Vector3Int cell, int selfPlacementId)
    {
        if (!mTilemap.HasTile(cell) || GetSurface(cell) != ePlacementSurface.Floor)
            return true;

        return IsAccessCellAvailable(cell, selfPlacementId);
    }

    private bool AreClearanceCellsAvailable(Vector3Int origin, IReadOnlyList<Vector2Int> clearanceOffsets, int selfPlacementId)
    {
        foreach (var cell in IterateAccessCells(origin, clearanceOffsets))
        {
            if (!IsClearanceCellAvailable(cell, selfPlacementId))
                return false;
        }
        return true;
    }

    private bool AreAccessCellsAvailable(Vector3Int origin, IReadOnlyList<Vector2Int> accessOffsets, int selfPlacementId)
    {
        foreach (var cell in IterateAccessCells(origin, accessOffsets))
        {
            if (!IsAccessCellAvailable(cell, selfPlacementId))
                return false;
        }
        return true;
    }

    // footprint 기준 셀(origin)에 접근 칸 오프셋을 더한 셀들. 오프셋이 없으면 아무것도 내놓지 않는다.
    private IEnumerable<Vector3Int> IterateAccessCells(Vector3Int origin, IReadOnlyList<Vector2Int> accessOffsets)
    {
        if (accessOffsets == null)
            yield break;

        foreach (var offset in accessOffsets)
            yield return new Vector3Int(origin.x + offset.x, origin.y + offset.y, origin.z);
    }

    private bool IsSubCellAvailable(Vector3Int cell, int allowedGroupId, int selfPlacementId)
    {
        if (!mOccupants.TryGetValue(cell, out CellOccupant occupant))
            return false;

        if (occupant.SubPlacementId >= 0 && occupant.SubPlacementId != selfPlacementId)
            return false;

        foreach (var slot in occupant.Bases.Values)
        {
            if (slot.GroupId == allowedGroupId)
                return true;
        }
        return false;
    }

    // footprintCells 크기의 사각형을 origin부터 채우는 셀 좌표들을 순서대로 내놓는다.
    // TryGetSnappedPositionForBase/ForSub, OccupyBase/OccupySub이 전부 이 하나의 순회 규칙을 공유한다.
    private IEnumerable<Vector3Int> IterateFootprintCells(Vector3Int origin, Vector2Int footprintCells)
    {
        for (int x = 0; x < footprintCells.x; x++)
        {
            for (int y = 0; y < footprintCells.y; y++)
                yield return new Vector3Int(origin.x + x, origin.y + y, origin.z);
        }
    }

    private CellOccupant GetOrCreateOccupant(Vector3Int cell)
    {
        if (!mOccupants.TryGetValue(cell, out CellOccupant occupant))
        {
            occupant = new CellOccupant();
            mOccupants[cell] = occupant;
        }
        return occupant;
    }

    private List<Vector3Int> GetOrCreateCellList(int placementId)
    {
        if (!mCellsByPlacementId.TryGetValue(placementId, out List<Vector3Int> cells))
        {
            cells = new List<Vector3Int>();
            mCellsByPlacementId[placementId] = cells;
        }
        return cells;
    }

    // footprintCells 크기 가구의 기준 셀(좌하단). worldPos는 가구 중심.
    // 짝수 칸 가구는 스냅된 중심이 칸 경계에 걸려 WorldToCell이 부동소수 오차로 한 칸 밀릴 수 있다 —
    // 그 축은 반 칸 밀어서 경계가 아닌 칸 안쪽에서 셀을 구한다(GetFootprintCenterWorld와 왕복해도 같은 셀이 나오게).
    public Vector3Int GetFootprintOrigin(Vector3 worldPos, Vector2Int footprintCells)
    {
        Vector3 cellSize = mTilemap.layoutGrid.cellSize;
        worldPos.x += footprintCells.x % 2 == 0 ? cellSize.x * 0.5f : 0f;
        worldPos.y += footprintCells.y % 2 == 0 ? cellSize.y * 0.5f : 0f;

        Vector3Int center = mTilemap.WorldToCell(worldPos);
        return new Vector3Int(center.x - footprintCells.x / 2, center.y - footprintCells.y / 2, 0);
    }

    // 기준 셀(origin)에서 footprintCells만큼 차지할 때의 가구 중심 월드 좌표.
    public Vector3 GetFootprintCenterWorld(Vector3Int origin, Vector2Int footprintCells, float z)
    {
        Vector3 min = mTilemap.GetCellCenterWorld(origin);
        Vector3 max = mTilemap.GetCellCenterWorld(new Vector3Int(origin.x + footprintCells.x - 1, origin.y + footprintCells.y - 1, origin.z));
        return new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, z);
    }
}
