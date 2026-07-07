using UnityEngine;

public class LobbyCharUI : MonoBehaviour
{
    private const int SortingOrder = 1002;

    [Header("Bread")]
    [SerializeField] private Transform mRootBreadTr;
    public Transform RootBreadTr => mRootBreadTr;

    private Camera mMainCamera;

    private void Awake()
    {
        mMainCamera = Camera.main;
        ApplySortingOrder();
    }

    public void AttachBread(Intaraction_Bread bread)
    {
        if (mRootBreadTr == null || bread == null) return;

        bread.transform.SetParent(mRootBreadTr);
        bread.transform.localPosition = Vector3.zero;
        bread.transform.localScale = Vector3.one;

        var breadRt = bread.GetComponent<RectTransform>();
        var referenceRt = GetComponent<RectTransform>();
        if (breadRt != null && referenceRt != null)
            breadRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, referenceRt.rect.width);
    }

    private void LateUpdate()
    {
        if (mMainCamera == null) return;
        transform.rotation = mMainCamera.transform.rotation;
    }

    private void ApplySortingOrder()
    {
        foreach (var canvas in GetComponentsInChildren<Canvas>(true))
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
        }

        foreach (var sr in GetComponentsInChildren<Renderer>(true))
            sr.sortingOrder = SortingOrder;
    }
}