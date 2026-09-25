using UnityEngine;
using TMPro;
using UniRx;
using DG.Tweening;
using Extension;

// 탭 버튼으로 패널(mBody)을 화면 밖으로 밀어 넣었다 꺼냈다 하는 하이드 인/아웃 패널.
// 탭은 mBody의 자식이라 같이 움직이고, 숨겨진 상태에서도 화면 가장자리에 탭만 남는다.
// 로비의 알림/층 이동 패널처럼 GameInstance에 의존하지 않는 순수 UI라 Awake에서 바로 구독한다.
public class UISlidePanel : MonoBehaviour
{
    public enum eSlideDirection
    {
        Right,
        Left,
        Down,
    }

    private const string ARROW_RIGHT = "▶";
    private const string ARROW_LEFT = "◀";
    private const string ARROW_DOWN = "▼";
    private const string ARROW_UP = "▲";

    [SerializeField] private RectTransform mBody;
    [SerializeField] private UIButtonEx mBtnToggle;
    [SerializeField] private TextMeshProUGUI mTextArrow;
    [SerializeField] private eSlideDirection mDirection = eSlideDirection.Right;
    // 패널 크기에 더해 밀어낼 거리(패널과 화면 가장자리 사이 간격). 숨기면 탭만 화면 가장자리에 남는다.
    [SerializeField] private float mExtraOffset = 0f;
    [SerializeField] private float mDuration = 0.25f;
    [SerializeField] private bool mStartOpen = true;

    private Vector2 mOpenPosition;
    private Tween mTween;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        mOpenPosition = mBody.anchoredPosition;
        SetOpen(mStartOpen, true);

        mBtnToggle.OnSubscribeOnClick(Toggle).AddTo(this);
    }

    private void OnDestroy()
    {
        mTween?.Kill();
    }

    public void Toggle()
    {
        SetOpen(!IsOpen, false);
    }

    public void SetOpen(bool open, bool instant)
    {
        IsOpen = open;
        RefreshArrow();

        Vector2 target = open ? mOpenPosition : GetHiddenPosition();

        mTween?.Kill();
        if (instant)
        {
            mBody.anchoredPosition = target;
            return;
        }

        mTween = DOTween.To(() => mBody.anchoredPosition, v => mBody.anchoredPosition = v, target, mDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(gameObject);
    }

    private Vector2 GetHiddenPosition()
    {
        var size = mBody.rect.size;
        switch (mDirection)
        {
            case eSlideDirection.Left: return mOpenPosition + new Vector2(-(size.x + mExtraOffset), 0f);
            case eSlideDirection.Down: return mOpenPosition + new Vector2(0f, -(size.y + mExtraOffset));
            default: return mOpenPosition + new Vector2(size.x + mExtraOffset, 0f);
        }
    }

    // 화살표는 "누르면 패널이 움직일 방향"을 가리킨다.
    private void RefreshArrow()
    {
        if (mTextArrow == null) return;

        switch (mDirection)
        {
            case eSlideDirection.Left: mTextArrow.SetTextEx(IsOpen ? ARROW_LEFT : ARROW_RIGHT); break;
            case eSlideDirection.Down: mTextArrow.SetTextEx(IsOpen ? ARROW_DOWN : ARROW_UP); break;
            default: mTextArrow.SetTextEx(IsOpen ? ARROW_RIGHT : ARROW_LEFT); break;
        }
    }
}
