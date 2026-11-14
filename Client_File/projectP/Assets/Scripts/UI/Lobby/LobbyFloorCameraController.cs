using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using TMPro;
using Extension;

// 로비 카메라(매장 B3~1F + 테라스 T1~T3). 층 이동(MoveToFloor — 로비 층 이동 목록 UILobbyFloorList가 부른다)과 마우스 휠 줌을 담당한다.
// 액자(LobbyViewFrame)가 있으면 월드 중심이 액자 안쪽 가운데에 보이도록 카메라를 옮긴다(줌/해상도가 바뀌어도 매 프레임 맞춘다).
// 층 이동 목표 Y좌표는 GameInstance.WayPoint에 등록된 WaypointGroup(Order=층 ID, Floor.csv Tid)의 위치를 그대로 사용한다.
// MoveToFloor 자체는 잠긴 층도 막지 않는다. 로비 층 이동 목록은 잠긴 층을 누르면 이동 대신 층 해금 팝업을 연다(로비 UX 개편).
// 실제 스폰/구매 제한은 FloorModel.IsUnlocked 쪽에서 건다.
// 2026-09: 휠 = 층 이동 → 휠 = 줌 인/아웃으로 변경. 팝업이 떠 있거나 포인터가 UI 위에 있으면 줌하지 않는다.
public class LobbyFloorCameraController : MonoBehaviour
{
    [SerializeField] private Camera mCamera;
    [SerializeField] private float mScrollThrottleSeconds = 0.2f; // 휠 한 번(노치)당 줌 한 단계만 바뀌도록 하는 디바운스 간격

    [Header("Zoom")]
    // PixelPerfectCamera(crop 없음)는 화면을 "에셋 1픽셀 = 화면 N픽셀"의 정수 배율로만 확대하므로, 줌도 이 배율(pixel ratio)을
    // 한 단계씩 바꾼다(1080p 기준 1=0.5배, 2=기본, 3=1.5배, 4=2배). 기준 해상도만 바꾸고 에셋 PPU는 그대로라 픽셀 퍼펙트가 유지된다.
    // 최소 2 = 기본 화면이 가장 넓은 시야(1로 내리면 로비 월드 바깥 빈 공간과 윗층이 보인다).
    [SerializeField] private int mMinPixelRatio = 2;
    [SerializeField] private int mMaxPixelRatio = 4;
    // PixelPerfectCamera가 없을 때의 대체 줌(orthographicSize 배율) 범위와 한 단계 비율.
    [SerializeField] private float mFallbackMinZoom = 0.5f;
    [SerializeField] private float mFallbackMaxZoom = 2f;
    [SerializeField] private float mFallbackZoomStep = 1.25f;

    private PixelPerfectCamera mPixelPerfect;
    private float mBaseOrthographicSize;
    private int mPixelRatio;          // 0 = 아직 줌하지 않음(첫 휠 입력 때 현재 배율에서 시작)
    private float mFallbackZoom = 1f;

    // 카메라 기준 위치: x = 씬에 놓인 카메라 x, y = 보고 있는 층(WaypointGroup) y. 실제 위치 = 기준 + 액자 보정.
    private float mBaseX;
    private float mFloorY;

    // 로비 액자(UI_Root_Lobby/ViewFrame). UIRootLobby가 Init 전에 넘겨준다. 없으면 화면 가운데를 기준으로 한다.
    private LobbyViewFrame mViewFrame;

    private void Awake()
    {
        if (mCamera == null)
            mCamera = Camera.main;

        if (mCamera != null)
        {
            mPixelPerfect = mCamera.GetComponent<PixelPerfectCamera>();
            mBaseOrthographicSize = mCamera.orthographicSize;
            mBaseX = mCamera.transform.position.x;
            mFloorY = mCamera.transform.position.y;
        }
    }

    public void SetViewFrame(LobbyViewFrame viewFrame)
    {
        mViewFrame = viewFrame;
    }

    public void Init()
    {
        SubscribeZoomInput();

        // PixelPerfectCamera가 렌더 직전에 orthographicSize를 정하므로, 보정은 매 프레임 끝에 다시 계산한다(값이 같으면 위치를 건드리지 않는다).
        Observable.EveryLateUpdate()
            .Subscribe(_ => ApplyCameraPosition())
            .AddTo(this);

        var startFloor = FindGroupByFloor(GameInstance.Model.Floor.DefaultViewFloor);
        if (startFloor != null)
            SnapToFloor(startFloor);
    }

    public void MoveToFloor(int floor)
    {
        if (!GameInstance.Model.Floor.Exists(floor)) return;

        var group = FindGroupByFloor(floor);
        if (group == null) return;

        // 검은 화면(UI_Indicator)을 먼저 띄우고, 가려진 동안 카메라 좌표만 즉시 옮긴 뒤 오른쪽→왼쪽으로 걷어낸다.
        var indicator = GameInstance.UI != null ? GameInstance.UI.Indicator : null;
        if (indicator != null)
            indicator.Play(() => SnapToFloor(group));
        else
            SnapToFloor(group);
    }

    private void SubscribeZoomInput()
    {
        Observable.EveryUpdate()
            .Where(_ => Mouse.current != null)
            .Select(_ => Mouse.current.scroll.ReadValue().y)
            .Where(delta => Mathf.Abs(delta) > 0.01f)
            .Where(_ => CanZoom())
            .ThrottleFirst(TimeSpan.FromSeconds(mScrollThrottleSeconds))
            .Subscribe(delta => Zoom(delta > 0f ? 1 : -1))
            .AddTo(this);
    }

    // 팝업이 떠 있으면(팝업 안 스크롤 목록 등) 줌을 무시한다. 포인터가 UI 위(하단 메뉴, 사이드바, 가공섬 화면 등)일 때도 무시.
    private bool CanZoom()
    {
        if (GameInstance.UI != null && GameInstance.UI.HasOpenPopup())
            return false;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        return true;
    }

    // step > 0 = 확대(휠 위), step < 0 = 축소(휠 아래).
    private void Zoom(int step)
    {
        if (mPixelPerfect != null && mPixelPerfect.enabled)
        {
            if (mPixelRatio <= 0)
                mPixelRatio = Mathf.Max(1, mPixelPerfect.pixelRatio);

            mPixelRatio = Mathf.Clamp(mPixelRatio + step, Mathf.Max(1, mMinPixelRatio), Mathf.Max(1, mMaxPixelRatio));

            // 기준 해상도 = 화면 / 배율 → PixelPerfectCamera가 계산하는 정수 배율이 정확히 mPixelRatio가 된다.
            mPixelPerfect.refResolutionX = Mathf.Max(2, Screen.width / mPixelRatio / 2 * 2);
            mPixelPerfect.refResolutionY = Mathf.Max(2, Screen.height / mPixelRatio / 2 * 2);
            return;
        }

        if (mCamera != null && mCamera.orthographic)
        {
            mFallbackZoom = Mathf.Clamp(mFallbackZoom * (step > 0 ? mFallbackZoomStep : 1f / mFallbackZoomStep), mFallbackMinZoom, mFallbackMaxZoom);
            mCamera.orthographicSize = mBaseOrthographicSize / mFallbackZoom;
        }
    }

    private void SnapToFloor(WaypointGroup group)
    {
        GameInstance.Model.Floor.SetViewingFloor(group.Order);

        mFloorY = group.transform.position.y;
        ApplyCameraPosition();
    }

    // 기준 위치 + 액자 보정. 액자 안쪽 중심(u, v)에 기준 위치가 보이려면 카메라를 (0.5 - u) * 보이는 폭만큼 옮긴다.
    private void ApplyCameraPosition()
    {
        if (mCamera == null) return;

        var offset = Vector2.zero;
        if (mViewFrame != null && mCamera.orthographic)
        {
            // 로비 UI가 닫혀 있는 동안(가공섬 화면 등)은 액자를 잴 수 없으니 카메라를 그대로 둔다.
            if (!mViewFrame.TryGetInnerViewportCenter(out var center))
                return;

            float height = mCamera.orthographicSize * 2f;
            float width = height * mCamera.aspect;
            offset = new Vector2((0.5f - center.x) * width, (0.5f - center.y) * height);

            // 픽셀 퍼펙트 카메라면 에셋 1픽셀 단위로 맞춰 그림이 떨리지 않게 한다.
            if (mPixelPerfect != null && mPixelPerfect.enabled && mPixelPerfect.assetsPPU > 0)
            {
                float ppu = mPixelPerfect.assetsPPU;
                offset = new Vector2(Mathf.Round(offset.x * ppu) / ppu, Mathf.Round(offset.y * ppu) / ppu);
            }
        }

        var pos = mCamera.transform.position;
        var target = new Vector3(mBaseX + offset.x, mFloorY + offset.y, pos.z);
        if (pos != target)
            mCamera.transform.position = target;
    }

    private WaypointGroup FindGroupByFloor(int floor)
    {
        if (GameInstance.WayPoint == null) return null;

        foreach (var group in GameInstance.WayPoint.WaypointGroups)
        {
            if (group != null && group.Order == floor)
                return group;
        }
        return null;
    }
}
