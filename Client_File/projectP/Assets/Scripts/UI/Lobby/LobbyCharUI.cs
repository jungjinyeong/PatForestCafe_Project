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

    [Header("Bread Unavailable")]
    [SerializeField] private GameObject mSweatIconObj;

    [Header("Bread")]
    [SerializeField] private Transform mRootBreadTr;
    public Transform RootBreadTr => mRootBreadTr;

    [Header("Drink")]
    [SerializeField] private Vector3 mDrinkOffset = new Vector3(0.25f, -0.1f, 0f);
    [SerializeField] private Vector2 mDrinkSize = new Vector2(0.25f, 0.25f);

    private readonly List<Intaraction_Bread> mBreads = new List<Intaraction_Bread>();
    public IReadOnlyList<Intaraction_Bread> Breads => mBreads;
    public bool HasBread => mBreads.Count > 0;

    private GameObject mDrinkIconObj;
    private SpriteRenderer mDrinkSpriteRenderer;

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

    public void SetSweatIconActive(bool isActive)
    {
        if (mSweatIconObj != null)
            mSweatIconObj.SetActive(isActive);
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

    // 계산대에서 구매가 확정된 음료의 스프라이트를 손에 들려준다(MenuItemRow.Atlas/Icon 기준, CharNpc가 조회해 넘겨줌).
    // 전용 프리팹 없이 절차적으로 생성한다(CharStaff 픽업 게이지바와 동일한 이유 — 이 오브젝트에 프리팹 자체가 없음).
    public void SetDrinkSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            ClearDrink();
            return;
        }

        if (mDrinkIconObj == null)
            BuildDrinkIconObj();

        mDrinkSpriteRenderer.sprite = sprite;
        mDrinkIconObj.SetActive(true);
    }

    public void ClearDrink()
    {
        if (mDrinkIconObj != null)
            mDrinkIconObj.SetActive(false);
    }

    private void BuildDrinkIconObj()
    {
        mDrinkIconObj = new GameObject("DrinkIcon");
        mDrinkIconObj.transform.SetParent(transform, false);
        mDrinkIconObj.transform.localPosition = mDrinkOffset;
        mDrinkIconObj.transform.localScale = new Vector3(mDrinkSize.x, mDrinkSize.y, 1f);

        mDrinkSpriteRenderer = mDrinkIconObj.AddComponent<SpriteRenderer>();
        mDrinkSpriteRenderer.sortingOrder = SortingOrder;
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