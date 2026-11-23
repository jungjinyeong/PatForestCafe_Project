using UnityEngine;

public class Intaraction_Dessert : MonoBehaviour
{
    public const string PoolName = "Dessert";

    // 진열대마다 디저트 종류가 달라 풀에서 꺼낼 때마다 그림을 바꿔 끼운다. 아이콘이 없으면 프리팹에 넣어둔 기본 그림으로 되돌린다.
    private SpriteRenderer mSpriteRenderer;
    private Sprite mDefaultSprite;

    public int TableId { get; private set; }
    // 오븐에서 구울 때 정해진 품질. 손님 결제 시 판매가 배율(DessertQuality.GetSaleMultiplier)에 쓰인다.
    public eDessertQuality Quality { get; private set; } = eDessertQuality.Low;

    private void Awake()
    {
        mSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        mDefaultSprite = mSpriteRenderer != null ? mSpriteRenderer.sprite : null;
    }

    public void SetTableId(int tableId)
    {
        TableId = tableId;
    }

    public void SetQuality(eDessertQuality quality)
    {
        Quality = quality;
    }

    public void SetIcon(Sprite icon)
    {
        if (mSpriteRenderer != null)
            mSpriteRenderer.sprite = icon != null ? icon : mDefaultSprite;
    }

    public void Despawn()
    {
        GameInstance.Pool?.Despawn(PoolName, gameObject);
    }
}
