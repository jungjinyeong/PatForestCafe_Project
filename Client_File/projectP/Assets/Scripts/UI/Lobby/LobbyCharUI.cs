using System;
using UniRx;
using UnityEngine;
using TMPro;

public class LobbyCharUI : MonoBehaviour
{
    private const int SortingOrder = 0;
    // 머리 위 디저트 표시는 캐릭터·가구보다 위에 그린다.
    private const int CART_SORTING_ORDER = 20;

    [Header("Payment Effect")]
    [SerializeField] private GameObject mCoinIconObj;
    [SerializeField] private GameObject mSatisfactionIconObj;
    [SerializeField] private float mCoinIconDuration = 0.6f;
    [SerializeField] private float mSatisfactionIconDuration = 0.6f;

    [Header("Dessert Unavailable")]
    [SerializeField] private GameObject mSweatIconObj;

    [Header("Dessert")]
    // 머리 위 디저트 표시(고른 디저트 아이콘 + "xN"). 손에 든 음료처럼 SpriteRenderer/TextMeshPro로 만들어 캐릭터보다 위에 그린다.
    [SerializeField] private Vector3 mDessertCartOffset = new Vector3(-0.2f, 1.3f, 0f);
    [SerializeField] private float mDessertIconWidth = 0.6f;
    [SerializeField] private float mDessertCountFontSize = 5f;
    [Tooltip("수량 글꼴(픽셀 글꼴 Galmuri11 권장). 비우면 TMP 기본 글꼴.")]
    [SerializeField] private TMP_FontAsset mDessertCountFont;

    [Header("Drink")]
    [SerializeField] private Vector3 mDrinkOffset = new Vector3(0.25f, -0.1f, 0f);
    [SerializeField] private Vector2 mDrinkSize = new Vector2(0.25f, 0.25f);

    private GameObject mDessertCartObj;
    private SpriteRenderer mDessertIconRenderer;
    private TextMeshPro mDessertCountText;

    private GameObject mDrinkIconObj;
    private SpriteRenderer mDrinkSpriteRenderer;

    private Camera mMainCamera;
    private IDisposable mPaymentEffectDisposable;

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
        StopPaymentEffect();
        SetCoinIconActive(true);

        mPaymentEffectDisposable = Observable.Timer(TimeSpan.FromSeconds(mCoinIconDuration))
            .Subscribe(_ =>
            {
                SetCoinIconActive(false);
                SetSatisfactionIconActive(true);

                mPaymentEffectDisposable = Observable.Timer(TimeSpan.FromSeconds(mSatisfactionIconDuration))
                    .Subscribe(__ =>
                    {
                        mPaymentEffectDisposable = null;
                        SetSatisfactionIconActive(false);
                        onComplete?.Invoke();
                    })
                    .AddTo(this);
            })
            .AddTo(this);
    }

    // 손님이 풀로 돌아갈 때(강제 퇴장 등) 진행 중인 결제 연출을 멈추고 아이콘을 끈다 — 재사용된 다음 손님에게 남지 않게.
    public void StopPaymentEffect()
    {
        mPaymentEffectDisposable?.Dispose();
        mPaymentEffectDisposable = null;
        SetCoinIconActive(false);
        SetSatisfactionIconActive(false);
    }

    public void SetSweatIconActive(bool isActive)
    {
        if (mSweatIconObj != null)
            mSweatIconObj.SetActive(isActive);
    }

    // 고른 디저트를 머리 위에 아이콘 + "xN"으로 보여 준다. count가 0 이하이면 숨긴다.
    public void SetDessertCart(Sprite icon, int count)
    {
        if (count <= 0)
        {
            ClearDessertCart();
            return;
        }

        if (mDessertCartObj == null)
            BuildDessertCartObj();

        mDessertIconRenderer.sprite = icon;
        float spriteWidth = icon != null ? icon.bounds.size.x : 0f;
        float scale = spriteWidth > 0f ? mDessertIconWidth / spriteWidth : 1f;
        mDessertIconRenderer.transform.localScale = new Vector3(scale, scale, 1f);

        mDessertCountText.text = "x" + count;
        mDessertCartObj.SetActive(true);
    }

    public void ClearDessertCart()
    {
        if (mDessertCartObj != null)
            mDessertCartObj.SetActive(false);
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

    // 전용 프리팹 없이 절차적으로 만든다(손에 든 음료 아이콘과 같은 방식). 정렬 순서를 캐릭터보다 높여 머리 위에 보이게 한다.
    private void BuildDessertCartObj()
    {
        mDessertCartObj = new GameObject("DessertCart");
        mDessertCartObj.transform.SetParent(transform, false);
        mDessertCartObj.transform.localPosition = mDessertCartOffset;

        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(mDessertCartObj.transform, false);
        mDessertIconRenderer = iconObj.AddComponent<SpriteRenderer>();
        mDessertIconRenderer.sortingOrder = CART_SORTING_ORDER;

        var countObj = new GameObject("Count");
        countObj.transform.SetParent(mDessertCartObj.transform, false);
        countObj.transform.localPosition = new Vector3(mDessertIconWidth * 0.5f + 0.05f, 0f, 0f);
        mDessertCountText = countObj.AddComponent<TextMeshPro>();
        if (mDessertCountFont != null)
            mDessertCountText.font = mDessertCountFont;
        mDessertCountText.fontSize = mDessertCountFontSize;
        mDessertCountText.alignment = TextAlignmentOptions.MidlineLeft;
        mDessertCountText.enableWordWrapping = false;
        mDessertCountText.overflowMode = TextOverflowModes.Overflow;
        mDessertCountText.rectTransform.pivot = new Vector2(0f, 0.5f);
        mDessertCountText.rectTransform.sizeDelta = new Vector2(1f, 0.4f);
        mDessertCountText.color = new Color(0.3f, 0.18f, 0.1f, 1f);
        mDessertCountText.sortingOrder = CART_SORTING_ORDER;
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