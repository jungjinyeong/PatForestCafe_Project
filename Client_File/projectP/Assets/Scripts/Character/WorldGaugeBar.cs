using UnityEngine;

// 월드 오브젝트 머리 위 진행 게이지(카운터 직원 픽업, 키오스크 결제 처리).
// 전용 프리팹·스프라이트 에셋 없이 SpriteRenderer 2장(배경/채움)을 런타임에 만든다.
public class WorldGaugeBar
{
    private static Sprite mSolidSprite;

    private readonly GameObject mRoot;
    private readonly Transform mFillTr;

    public WorldGaugeBar(Transform parent, Vector3 offset, Vector2 size, Color backgroundColor, Color fillColor)
    {
        mRoot = new GameObject("GaugeBg");
        mRoot.transform.SetParent(parent, false);
        mRoot.transform.localPosition = offset;
        mRoot.transform.localScale = new Vector3(size.x, size.y, 1f);

        var backSr = mRoot.AddComponent<SpriteRenderer>();
        backSr.sprite = GetSolidSprite();
        backSr.color = backgroundColor;
        backSr.sortingOrder = 10;

        var fillGo = new GameObject("GaugeFill");
        fillGo.transform.SetParent(mRoot.transform, false);

        var fillSr = fillGo.AddComponent<SpriteRenderer>();
        fillSr.sprite = GetSolidSprite();
        fillSr.color = fillColor;
        fillSr.sortingOrder = 11;

        mFillTr = fillGo.transform;

        SetRatio(0f);
        SetVisible(false);
    }

    // 배경 기준 스케일 안에서 채움 스프라이트를 가로 방향으로만 늘린다(중앙 기준 — 러프 버전).
    public void SetRatio(float ratio)
    {
        if (mFillTr != null)
            mFillTr.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
    }

    public void SetVisible(bool isVisible)
    {
        if (mRoot != null)
            mRoot.SetActive(isVisible);
    }

    private static Sprite GetSolidSprite()
    {
        if (mSolidSprite != null) return mSolidSprite;

        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        mSolidSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return mSolidSprite;
    }
}
