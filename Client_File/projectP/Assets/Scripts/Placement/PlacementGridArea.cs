using UnityEngine;
using UnityEngine.UI;

public class PlacementGridArea : MonoBehaviour
{
    [SerializeField] private Image mAreaImage;
    [SerializeField] private float mCellSize = 1f;

    public bool TryGetSnappedPosition(Vector3 worldPos, out Vector3 snappedPos)
    {
        snappedPos = worldPos;

        if (mAreaImage == null)
            return false;

        Vector3[] corners = new Vector3[4];
        mAreaImage.rectTransform.GetWorldCorners(corners);

        Vector3 min = corners[0];
        Vector3 max = corners[2];

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
