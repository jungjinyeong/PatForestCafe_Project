using UnityEngine.Serialization;
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

    [Header("Dessert Unavailable")]
    [SerializeField] private GameObject mSweatIconObj;

    [Header("Dessert")]
    [SerializeField, FormerlySerializedAs("mRootBreadTr")] private Transform mRootDessertTr;
    public Transform RootDessertTr => mRootDessertTr;

    [Header("Drink")]
    [SerializeField] private Vector3 mDrinkOffset = new Vector3(0.25f, -0.1f, 0f);
    [SerializeField] private Vector2 mDrinkSize = new Vector2(0.25f, 0.25f);

    private readonly List<Intaraction_Dessert> mDesserts = new List<Intaraction_Dessert>();
    public IReadOnlyList<Intaraction_Dessert> Desserts => mDesserts;
    public bool HasDessert => mDesserts.Count > 0;

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

    public bool CanAttachDessert()
    {
        return mDesserts.Count < GameInstance.Config.GetValue(eConfigType.DessertMaxCount);
    }

    public void AttachDessert(Intaraction_Dessert dessert)
    {
        if (mRootDessertTr == null || dessert == null || !CanAttachDessert()) return;

        dessert.transform.SetParent(mRootDessertTr);
        dessert.transform.localPosition = Vector3.zero;
        dessert.transform.localScale = Vector3.one;

        var dessertRt = dessert.GetComponent<RectTransform>();
        if (dessertRt != null)
        {
            dessertRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0.3f);
            dessertRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0.2f);
        }

        mDesserts.Add(dessert);
    }

    public List<Intaraction_Dessert> DetachAllDesserts()
    {
        var desserts = new List<Intaraction_Dessert>(mDesserts);
        mDesserts.Clear();
        return desserts;
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