using UnityEngine;

public partial class PlacementModel
{
    public void BeginPlacement(Transform target, PlacementGridArea area)
    {
        if (target == null || area == null)
            return;

        mTarget = target;
        mArea = area;
        mOriginPosition = target.position;

        mIsValidPosition.Value = false;
        mIsPlacing.Value = true;
    }

    public void UpdatePreview(Vector3 worldPos)
    {
        if (!mIsPlacing.Value || mTarget == null || mArea == null)
            return;

        bool isValid = mArea.TryGetSnappedPosition(worldPos, out Vector3 snappedPos);

        mIsValidPosition.Value = isValid;
        mTarget.position = isValid ? snappedPos : worldPos;
    }

    public void Confirm()
    {
        if (!mIsPlacing.Value || !mIsValidPosition.Value)
            return;

        mTarget = null;
        mArea = null;
        mIsPlacing.Value = false;
    }

    public void Cancel()
    {
        if (!mIsPlacing.Value)
            return;

        if (mTarget != null)
            mTarget.position = mOriginPosition;

        mTarget = null;
        mArea = null;
        mIsPlacing.Value = false;
    }
}
