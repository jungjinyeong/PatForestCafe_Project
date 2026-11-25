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
    [Tooltip("디저트 아래에 깔리는 접시. 비우면 코드로 만든 임시 접시(흰 타원)를 쓴다.")]
    [SerializeField] private Sprite mPlateSprite;
    [SerializeField] private float mPlateWidth = 0.75f;
    [SerializeField] private Vector3 mPlateOffset = new Vector3(0f, -0.12f, 0f);

    [Header("Kiosk Event")]
    // 키오스크 이벤트(디저트가 없어 키오스크 근처에서 우물쭈물) 말풍선. 손님을 누르면 꺼진다.
    [SerializeField] private Vector3 mKioskBubbleOffset = new Vector3(0.35f, 1.5f, 0f);
    [SerializeField] private float mKioskBubbleWidth = 0.5f;
    [Tooltip("말풍선 그림. 비우면 코드로 만든 임시 말풍선(흰 원)을 쓴다.")]
    [SerializeField] private Sprite mKioskBubbleSprite;

    [Header("Drink")]
    [SerializeField] private Vector3 mDrinkOffset = new Vector3(0.25f, -0.1f, 0f);
    [SerializeField] private Vector2 mDrinkSize = new Vector2(0.25f, 0.25f);

    private GameObject mDessertCartObj;
    private SpriteRenderer mDessertIconRenderer;
    private TextMeshPro mDessertCountText;

    private GameObject mDrinkIconObj;
    private SpriteRenderer mDrinkSpriteRenderer;

    private GameObject mKioskBubbleObj;

    // 임시 접시·말풍선 그림(아트가 들어오면 mPlateSprite/mKioskBubbleSprite로 교체). 모든 손님이 같이 쓴다.
    private static Sprite mTempPlateSprite;
    private static Sprite mTempBubbleSprite;

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

    // 동전 없이 만족(행복) 아이콘만 잠깐 띄운다(키오스크 이벤트에서 손님을 눌렀을 때).
    public void PlaySatisfactionEffect()
    {
        StopPaymentEffect();
        SetSatisfactionIconActive(true);

        mPaymentEffectDisposable = Observable.Timer(TimeSpan.FromSeconds(mSatisfactionIconDuration))
            .Subscribe(_ =>
            {
                mPaymentEffectDisposable = null;
                SetSatisfactionIconActive(false);
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

    // 키오스크 이벤트 말풍선("?"). 처음 켤 때 만든다.
    public void SetKioskBubbleActive(bool isActive)
    {
        if (!isActive)
        {
            if (mKioskBubbleObj != null)
                mKioskBubbleObj.SetActive(false);
            return;
        }

        if (mKioskBubbleObj == null)
            BuildKioskBubbleObj();
        mKioskBubbleObj.SetActive(true);
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

        // 접시(디저트 아이콘 아래).
        var plateObj = new GameObject("Plate");
        plateObj.transform.SetParent(mDessertCartObj.transform, false);
        plateObj.transform.localPosition = mPlateOffset;
        var plateRenderer = plateObj.AddComponent<SpriteRenderer>();
        plateRenderer.sprite = mPlateSprite != null ? mPlateSprite : GetTempPlateSprite();
        plateRenderer.sortingOrder = CART_SORTING_ORDER - 1;
        float plateSpriteWidth = plateRenderer.sprite != null ? plateRenderer.sprite.bounds.size.x : 0f;
        float plateScale = plateSpriteWidth > 0f ? mPlateWidth / plateSpriteWidth : 1f;
        plateObj.transform.localScale = new Vector3(plateScale, plateScale, 1f);

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

    private void BuildKioskBubbleObj()
    {
        mKioskBubbleObj = new GameObject("KioskBubble");
        mKioskBubbleObj.transform.SetParent(transform, false);
        mKioskBubbleObj.transform.localPosition = mKioskBubbleOffset;

        var bgObj = new GameObject("Bubble");
        bgObj.transform.SetParent(mKioskBubbleObj.transform, false);
        var bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = mKioskBubbleSprite != null ? mKioskBubbleSprite : GetTempBubbleSprite();
        bgRenderer.sortingOrder = CART_SORTING_ORDER;
        float spriteWidth = bgRenderer.sprite != null ? bgRenderer.sprite.bounds.size.x : 0f;
        float scale = spriteWidth > 0f ? mKioskBubbleWidth / spriteWidth : 1f;
        bgObj.transform.localScale = new Vector3(scale, scale, 1f);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(mKioskBubbleObj.transform, false);
        var text = textObj.AddComponent<TextMeshPro>();
        if (mDessertCountFont != null)
            text.font = mDessertCountFont;
        text.text = "?";
        text.fontSize = mDessertCountFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.rectTransform.sizeDelta = new Vector2(mKioskBubbleWidth, mKioskBubbleWidth);
        text.color = new Color(0.3f, 0.18f, 0.1f, 1f);
        text.sortingOrder = CART_SORTING_ORDER + 1;
    }

    private static Sprite GetTempPlateSprite()
    {
        if (mTempPlateSprite == null)
            mTempPlateSprite = CreateEllipseSprite("TempPlate", 32, 12, new Color(0.97f, 0.97f, 0.95f, 1f), new Color(0.65f, 0.6f, 0.55f, 1f));
        return mTempPlateSprite;
    }

    private static Sprite GetTempBubbleSprite()
    {
        if (mTempBubbleSprite == null)
            mTempBubbleSprite = CreateEllipseSprite("TempBubble", 24, 20, Color.white, new Color(0.3f, 0.18f, 0.1f, 1f));
        return mTempBubbleSprite;
    }

    // 테두리가 있는 타원 스프라이트(임시 그림용). 픽셀 아트에 맞춰 Point 필터.
    private static Sprite CreateEllipseSprite(string name, int width, int height, Color fill, Color outline)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        float rx = width * 0.5f;
        float ry = height * 0.5f;
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x + 0.5f - rx) / rx;
                float dy = (y + 0.5f - ry) / ry;
                float d = dx * dx + dy * dy;
                // 바깥 테두리 두께 ≈ 1.5px.
                float inner = 1f - 3f / Mathf.Min(width, height);
                pixels[y * width + x] = d > 1f ? Color.clear : (d > inner * inner ? outline : fill);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
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