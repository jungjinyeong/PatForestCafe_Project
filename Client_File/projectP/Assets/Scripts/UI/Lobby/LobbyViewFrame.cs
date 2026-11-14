using UnityEngine;

// 로비 월드를 둘러싸는 액자(UI_Root_Lobby/ViewFrame). 카메라는 화면 전체를 그리고, 액자 UI가 가장자리를 덮는다.
// 안쪽 영역(mInner)의 화면상 중심을 알려 주면 LobbyFloorCameraController가 월드 중심을 그 자리에 맞춘다.
// 액자 테두리는 raycastTarget이 켜져 있어 테두리 위 클릭은 월드(가구 배치)로 가지 않는다.
public class LobbyViewFrame : MonoBehaviour
{
    [Tooltip("액자 안쪽(월드가 보이는 영역). 테두리 안쪽 가장자리에 맞춘 투명 RectTransform.")]
    [SerializeField] private RectTransform mInner;

    private readonly Vector3[] mCorners = new Vector3[4];
    private Canvas mRootCanvas;

    private void Awake()
    {
        CacheCanvas();
    }

    // 액자 안쪽 중심의 Viewport 좌표(0~1). 액자가 꺼져 있거나 화면 크기가 0이면 false.
    public bool TryGetInnerViewportCenter(out Vector2 viewportCenter)
    {
        viewportCenter = new Vector2(0.5f, 0.5f);
        if (mInner == null || !mInner.gameObject.activeInHierarchy || Screen.width <= 0 || Screen.height <= 0)
            return false;

        // 캔버스 렌더 모드(Overlay / Screen Space - Camera)에 상관없이 화면 픽셀 좌표로 바꾼다.
        // 매 프레임 불리므로 캔버스는 처음 한 번만 찾는다(UIManager가 캔버스 아래로 옮긴 뒤 Awake가 불린다).
        if (mRootCanvas == null)
            CacheCanvas();
        if (mRootCanvas == null)
            return false;
        var uiCamera = mRootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mRootCanvas.worldCamera;

        mInner.GetWorldCorners(mCorners);
        var min = RectTransformUtility.WorldToScreenPoint(uiCamera, mCorners[0]);
        var max = RectTransformUtility.WorldToScreenPoint(uiCamera, mCorners[2]);
        var center = (min + max) * 0.5f;
        viewportCenter = new Vector2(center.x / Screen.width, center.y / Screen.height);
        return true;
    }

    private void CacheCanvas()
    {
        var canvas = mInner != null ? mInner.GetComponentInParent<Canvas>() : null;
        mRootCanvas = canvas != null ? canvas.rootCanvas : null;
    }
}
