using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilePlacementArea : MonoBehaviour
{
    [SerializeField] private Tilemap mTilemap;

    private class CellOccupant
    {
        public int BasePlacementId = -1;
        public int BaseGroupId;
        public int SubPlacementId = -1;
    }

    // 셀 좌표 -> 점유 정보. 베이스 가구는 BasePlacementId/BaseGroupId를, 그 위의 sub가구는 SubPlacementId를 채운다.
    private readonly Dictionary<Vector3Int, CellOccupant> mOccupants = new Dictionary<Vector3Int, CellOccupant>();
    // placementId -> 그 placement가 차지한 셀 목록. ReleaseByPlacementId에서 역조회용.
    private readonly Dictionary<int, List<Vector3Int>> mCellsByPlacementId = new Dictionary<int, List<Vector3Int>>();

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

    // 일반(베이스) 가구용 유효 위치 판정: 타일이 존재하고, 다른 베이스가 점유하지 않은(또는 selfPlacementId 자신이 점유한) 셀들만 유효.
    public bool TryGetSnappedPositionForBase(Vector3 worldPos, Vector2Int footprintCells, int selfPlacementId, out Vector3 snappedPos)
    {
        snappedPos = worldPos;

        if (mTilemap == null)
            return false;

        Vector3Int origin = GetFootprintOrigin(worldPos, footprintCells);

        foreach (var cell in IterateFootprintCells(origin, footprintCells))
        {
            if (!mTilemap.HasTile(cell))
                return false;

            if (mOccupants.TryGetValue(cell, out CellOccupant occupant) &&
                occupant.BasePlacementId >= 0 && occupant.BasePlacementId != selfPlacementId)
                return false;
        }

        snappedPos = GetFootprintCenterWorld(origin, footprintCells, worldPos.z);
        return true;
    }

    // sub가구용 유효 위치 판정: footprint 전체 셀이 이미 allowedGroupId(0 초과, 즉 실제로 지정된 그룹)의 베이스로
    // 점유되어 있고, sub가 비어있거나(또는 selfPlacementId 자신) 있어야 유효.
    // allowedGroupId<=0(미지정)은 항상 무효 — 그룹이 없는 가구(BaseGroupId==0) 위에 아무 sub나 얹히는 걸 막는다.
    public bool TryGetSnappedPositionForSub(Vector3 worldPos, Vector2Int footprintCells, int allowedGroupId, int selfPlacementId, out Vector3 snappedPos)
    {
        snappedPos = worldPos;

        if (mTilemap == null || allowedGroupId <= 0)
            return false;

        Vector3Int origin = GetFootprintOrigin(worldPos, footprintCells);

        foreach (var cell in IterateFootprintCells(origin, footprintCells))
        {
            if (!mOccupants.TryGetValue(cell, out CellOccupant occupant) ||
                occupant.BasePlacementId < 0 || occupant.BaseGroupId != allowedGroupId)
                return false;

            if (occupant.SubPlacementId >= 0 && occupant.SubPlacementId != selfPlacementId)
                return false;
        }

        snappedPos = GetFootprintCenterWorld(origin, footprintCells, worldPos.z);
        return true;
    }

    public void OccupyBase(Vector3 worldPos, Vector2Int footprintCells, int placementId, int groupId)
    {
        ReleaseByPlacementId(placementId);

        Vector3Int origin = GetFootprintOrigin(worldPos, footprintCells);
        var cells = GetOrCreateCellList(placementId);

        foreach (var cell in IterateFootprintCells(origin, footprintCells))
        {
            var occupant = GetOrCreateOccupant(cell);
            occupant.BasePlacementId = placementId;
            occupant.BaseGroupId = groupId;
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

    // placementId가 차지했던 셀들에서 자기 몫만 지운다(베이스면 BasePlacementId/BaseGroupId, sub면 SubPlacementId).
    // 베이스인지 sub인지는 셀에 실제로 기록된 값으로 판단하므로 호출부에서 구분해 넘길 필요 없다.
    public void ReleaseByPlacementId(int placementId)
    {
        if (!mCellsByPlacementId.TryGetValue(placementId, out List<Vector3Int> cells))
            return;

        foreach (var cell in cells)
        {
            if (!mOccupants.TryGetValue(cell, out CellOccupant occupant))
                continue;

            if (occupant.BasePlacementId == placementId)
            {
                occupant.BasePlacementId = -1;
                occupant.BaseGroupId = 0;
            }

            if (occupant.SubPlacementId == placementId)
                occupant.SubPlacementId = -1;
        }

        mCellsByPlacementId.Remove(placementId);
    }

    // basePlacementId가 차지한 셀 중 sub가 하나라도 올라가 있으면 true. 베이스 재드래그(이동) 차단용.
    public bool HasAnySubOccupant(int basePlacementId)
    {
        if (!mCellsByPlacementId.TryGetValue(basePlacementId, out List<Vector3Int> cells))
            return false;

        foreach (var cell in cells)
        {
            if (mOccupants.TryGetValue(cell, out CellOccupant occupant) && occupant.SubPlacementId >= 0)
                return true;
        }
        return false;
    }

    public void ClearAllOccupants()
    {
        mOccupants.Clear();
        mCellsByPlacementId.Clear();
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
