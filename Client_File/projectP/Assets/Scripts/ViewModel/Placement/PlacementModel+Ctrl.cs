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
    // 씬의 실제 가구 GameObject 제거는 호출부(UIFurnitureList.ResetAllPlacedFurniture())가 함께 처리해야 한다.
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
            isValid = mArea.TryGetSnappedPositionForBase(worldPos, mFootprint, mPlaceable != null ? mPlaceable.PlacementId : -1, out snappedPos);

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
                mArea.OccupyBase(mTarget.position, mFootprint, placeable.PlacementId, placeable.GroupId);
        }

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
                mArea.OccupyBase(mOriginPosition, mFootprint, placeable.PlacementId, placeable.GroupId);
        }

        mTarget = null;
        mPlaceable = null;
        mArea = null;
        mIsPlacing.Value = false;

        Physics2D.SyncTransforms();
    }

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
