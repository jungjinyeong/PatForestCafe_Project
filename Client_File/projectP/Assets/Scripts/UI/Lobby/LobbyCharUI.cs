using System.Collections.Generic;
using UnityEngine;

public class LobbyCharUI : MonoBehaviour
{
    private const int SortingOrder = 1002;

    [Header("Bread")]
    [SerializeField] private Transform mRootBreadTr;
    public Transform RootBreadTr => mRootBreadTr;

    private readonly List<Intaraction_Bread> mBreads = new List<Intaraction_Bread>();
    public IReadOnlyList<Intaraction_Bread> Breads => mBreads;
    public bool HasBread => mBreads.Count > 0;

    private Camera mMainCamera;

    private void Awake()
    {
        mMainCamera = Camera.main;
        ApplySortingOrder();
    }

    public bool CanAttachBread()
    {
        return mBreads.Count < GameInstance.Config.GetValue(eConfigType.BreadMaxCount);
    }

    public void AttachBread(Intaraction_Bread bread)
    {
        if (mRootBreadTr == null || bread == null || !CanAttachBread()) return;

        bread.transform.SetParent(mRootBreadTr);
        bread.transform.localPosition = Vector3.zero;
        bread.transform.localScale = Vector3.one;

        var breadRt = bread.GetComponent<RectTransform>();
        var referenceRt = GetComponent<RectTransform>();
        if (breadRt != null && referenceRt != null)
            breadRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, referenceRt.rect.width);

        mBreads.Add(bread);
    }

    public List<Intaraction_Bread> DetachAllBreads()
    {
        var breads = new List<Intaraction_Bread>(mBreads);
        mBreads.Clear();
        return breads;
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