using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 가구가 붙을 수 있는 면. 벽 셀 = 이 층 배경(BackgroundGrid)의 WallTilemap에 타일이 있는 배치 셀, 나머지 배치 셀 = 바닥.
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

    [SerializeField] private Tilemap mTilemap;
    // 이 층 배경의 벽 타일맵(BackgroundGrid/World_N/WallTilemap). 비어 있으면 모든 셀을 바닥으로 취급한다.
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

    private Tilemap mHighlightTilemap;
    private Tile mHighlightTile;

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

    public bool Contains(Vector3 worldPos)
    {
        return mTilemap != null && mTilemap.HasTile(mTilemap.WorldToCell(worldPos));
    }

    // 일반(베이스) 가구용 유효 위치 판정: footprint 전체 셀이 타일이 있고, 가구가 붙는 면(벽/바닥)과 일치하며,
    // 같은 레이어를 다른 베이스가 점유하지 않은(또는 selfPlacementId 자신) 경우만 유효. 벽과 바닥에 걸치면 무효.
    public bool TryGetSnappedPositionForBase(Vector3 worldPos, Vector2Int footprintCells, int selfPlacementId, ePlacementSurface surface, int layer, out Vector3 snappedPos)
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

    public void OccupyBase(Vector3 worldPos, Vector2Int footprintCells, int placementId, int groupId, int layer)
    {
        ReleaseByPlacementId(placementId);

        Vector3Int origin = GetFootprintOrigin(worldPos, footprintCells);
        var cells = GetOrCreateCellList(placementId);

        foreach (var cell in IterateFootprintCells(origin, footprintCells))
        {
            GetOrCreateOccupant(cell).Bases[layer] = new BaseSlot { PlacementId = placementId, GroupId = groupId };
            cells.Add(cell);
        }
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
        HideHighlight();
    }

    // 배치 셀이 벽인지 바닥인지. 벽 타일맵은 다른 Grid(BackgroundGrid)에 있어 월드 좌표로 변환해 조회한다.
    public ePlacementSurface GetSurface(Vector3Int cell)
    {
        if (mWallTilemap == null || mTilemap == null)
            return ePlacementSurface.Floor;

        Vector3Int wallCell = mWallTilemap.WorldToCell(mTilemap.GetCellCenterWorld(cell));
        return mWallTilemap.HasTile(wallCell) ? ePlacementSurface.Wall : ePlacementSurface.Floor;
    }

    #region Highlight

    // 드래그 중 베이스 가구를 놓을 수 있는 칸(면 일치 + 같은 레이어 비어 있음)을 강조한다.
    public void ShowBaseHighlight(ePlacementSurface surface, int layer, int selfPlacementId)
    {
        ShowHighlight(cell => IsBaseCellAvailable(cell, surface, layer, selfPlacementId));
    }

    // 드래그 중 sub가구를 놓을 수 있는 칸(허용 그룹 베이스 위 + sub 비어 있음)을 강조한다.
    public void ShowSubHighlight(int allowedGroupId, int selfPlacementId)
    {
        ShowHighlight(cell => allowedGroupId > 0 && IsSubCellAvailable(cell, allowedGroupId, selfPlacementId));
    }

    public void HideHighlight()
    {
        if (mHighlightTilemap != null)
            mHighlightTilemap.ClearAllTiles();
    }

    private void ShowHighlight(System.Func<Vector3Int, bool> isAvailable)
    {
        if (mTilemap == null || !EnsureHighlightTilemap())
            return;

        mHighlightTilemap.ClearAllTiles();

        BoundsInt bounds = mTilemap.cellBounds;
        foreach (var cell in bounds.allPositionsWithin)
        {
            if (mTilemap.HasTile(cell) && isAvailable(cell))
                mHighlightTilemap.SetTile(cell, mHighlightTile);
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

        var texture = CreateHighlightTexture();
        mHighlightTile = ScriptableObject.CreateInstance<Tile>();
        mHighlightTile.hideFlags = HideFlags.DontSave;
        mHighlightTile.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        mHighlightTile.colliderType = Tile.ColliderType.None;
        return true;
    }

    private static Texture2D CreateHighlightTexture()
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
                bool border = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                pixels[y * size + x] = border ? HIGHLIGHT_BORDER : HIGHLIGHT_FILL;
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

        return !mOccupants.TryGetValue(cell, out CellOccupant occupant) ||
               !occupant.Bases.TryGetValue(layer, out BaseSlot slot) ||
               slot.PlacementId == selfPlacementId;
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

    private Vector3Int GetFootprintOrigin(Vector3 worldPos, Vector2Int footprintCells)
    {
        Vector3Int center = mTilemap.WorldToCell(worldPos);
        return new Vector3Int(center.x - footprintCells.x / 2, center.y - footprintCells.y / 2, center.z);
    }

    private Vector3 GetFootprintCenterWorld(Vector3Int origin, Vector2Int footprintCells, float z)
    {
        Vector3 min = mTilemap.GetCellCenterWorld(origin);
        Vector3 max = mTilemap.GetCellCenterWorld(new Vector3Int(origin.x + footprintCells.x - 1, origin.y + footprintCells.y - 1, origin.z));
        return new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, z);
    }
}
