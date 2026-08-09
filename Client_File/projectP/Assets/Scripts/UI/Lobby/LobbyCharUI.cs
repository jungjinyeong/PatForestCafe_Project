using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class LobbyCharUI : MonoBehaviour
{
    private const int SortingOrder = 0;

    [Header("Payment Effect")]
    [SerializeField] private GameObject mCoinIconObj;
    [SerializeField] private GameObject mSatisfactionIconObj;
    [SerializeField] private float mCoinIconDuration = 0.6f;
    [SerializeField] private float mSatisfactionIconDuration = 0.6f;

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

    public void Init()
    {
    }

    public void PlayPaymentEffect(Action onComplete)
    {
        SetCoinIconActive(true);

        Observable.Timer(TimeSpan.FromSeconds(mCoinIconDuration))
            .Subscribe(_ =>
            {
                SetCoinIconActive(false);
                SetSatisfactionIconActive(true);

                Observable.Timer(TimeSpan.FromSeconds(mSatisfactionIconDuration))
                    .Subscribe(__ =>
                    {
                        SetSatisfactionIconActive(false);
                        onComplete?.Invoke();
                    })
                    .AddTo(this);
            })
            .AddTo(this);
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
        if (breadRt != null)
        {
            breadRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0.3f);
            breadRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0.2f);
        }

        mBreads.Add(bread);
    }

    public List<Intaraction_Bread> DetachAllBreads()
    {
        var breads = new List<Intaraction_Bread>(mBreads);
        mBreads.Clear();
        return breads;
    }

    private void SetCoinIconActive(bool isActive)
    {
        if (mCoinIconObj != null)
            mCoinIconObj.SetActive(isActive);
    }

    private void SetSatisfactionIconActive(bool isActive)
    {
        if (mSatisfactionIconObj != null)
            mSatisfactionIconObj.SetActive(isActive);
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