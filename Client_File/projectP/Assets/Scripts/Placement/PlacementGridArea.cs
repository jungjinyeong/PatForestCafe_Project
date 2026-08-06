using UnityEngine;

public class PlacementGridArea : MonoBehaviour
{
    [SerializeField] private SpriteRenderer mAreaSprite;
    [SerializeField] private float mCellSize = 1f;

    public Bounds Bounds => mAreaSprite != null ? mAreaSprite.bounds : default;

    public bool Contains(Vector3 worldPos)
    {
        return mAreaSprite != null && mAreaSprite.bounds.Contains(worldPos);
    }

    public bool TryGetSnappedPosition(Vector3 worldPos, out Vector3 snappedPos)
    {
        snappedPos = worldPos;

        if (mAreaSprite == null)
            return false;

        Bounds bounds = mAreaSprite.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        if (worldPos.x < min.x || worldPos.x > max.x || worldPos.y < min.y || worldPos.y > max.y)
            return false;

        float snappedX = min.x + Mathf.Round((worldPos.x - min.x) / mCellSize) * mCellSize;
        float snappedY = min.y + Mathf.Round((worldPos.y - min.y) / mCellSize) * mCellSize;

        snappedPos = new Vector3(
            Mathf.Clamp(snappedX, min.x, max.x),
            Mathf.Clamp(snappedY, min.y, max.y),
            worldPos.z);

        return true;
    }
}
