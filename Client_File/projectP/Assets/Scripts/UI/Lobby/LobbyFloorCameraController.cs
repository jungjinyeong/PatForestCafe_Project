using System;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using Extension;

// 6층 온천탑 스크롤 카메라. 층 이동 목표 Y좌표는 GameInstance.WayPoint에 등록된 WaypointGroup(Order=층 번호)의
// 위치를 그대로 사용한다 — 별도 마커가 필요 없다. 잠긴 층도 미리보기 삼아 자유롭게 스크롤 가능하고,
// 실제 스폰/구매 제한은 FloorModel.IsUnlocked 쪽에서 건다(여기서는 이동만 담당).
public class LobbyFloorCameraController : MonoBehaviour
{
    [SerializeField] private Camera mCamera;
    [SerializeField] private float mMoveSpeed = 5f;
    [SerializeField] private UIButtonEx[] mFloorButtons; // index 0 = 1층 ... index 5 = 6층(테라스)
    [SerializeField] private float mScrollThrottleSeconds = 0.2f; // 휠 한 번(노치)당 한 층만 이동하도록 하는 디바운스 간격

    private IDisposable mMoveDisposable;
    private int mCurrentFloor = FloorModel.FirstFloor;

    // UIFurnitureList가 "지금 보고 있는 층"에 가구를 배치하기 위해 참조한다.
    public PlacementGridArea CurrentFloorArea { get; private set; }

    private void Awake()
    {
        if (mCamera == null)
            mCamera = Camera.main;
    }

    public void Init()
    {
        SubscribeFloorButtons();
        SubscribeScrollInput();

        var firstFloor = FindGroupByFloor(FloorModel.FirstFloor);
        if (firstFloor != null)
            SnapToFloor(firstFloor);
    }

    public void MoveToFloor(int floor)
    {
        floor = Mathf.Clamp(floor, FloorModel.FirstFloor, FloorModel.LastFloor);

        var group = FindGroupByFloor(floor);
        if (group == null) return;

        mCurrentFloor = floor;
        CurrentFloorArea = GetPrimaryArea(group);

        // 층 이동마다 새로 구독하는 자리라 AddTo(this)를 쓰면 매번 CompositeDisposable에 쌓이기만 하고
        // 절대 제거되지 않는다(직접 Dispose해도 컨테이너에서 안 빠짐) — OnDestroy에서 수동으로 정리한다.
        mMoveDisposable?.Dispose();
        mMoveDisposable = Observable.EveryUpdate()
            .Subscribe(_ => TickMove(group.transform.position.y));
    }

    private void OnDestroy()
    {
        mMoveDisposable?.Dispose();
        mMoveDisposable = null;
    }

    private void SubscribeFloorButtons()
    {
        if (mFloorButtons == null) return;

        for (int i = 0; i < mFloorButtons.Length; i++)
        {
            int floor = i + 1;
            mFloorButtons[i]?.OnSubscribeOnClick(() => MoveToFloor(floor)).AddTo(this);
        }
    }

    private void SubscribeScrollInput()
    {
        Observable.EveryUpdate()
            .Where(_ => Mouse.current != null)
            .Select(_ => Mouse.current.scroll.ReadValue().y)
            .Where(delta => Mathf.Abs(delta) > 0.01f)
            .ThrottleFirst(TimeSpan.FromSeconds(mScrollThrottleSeconds))
            .Subscribe(delta => MoveToFloor(mCurrentFloor + (delta > 0f ? 1 : -1)))
            .AddTo(this);
    }

    private void SnapToFloor(WaypointGroup group)
    {
        CurrentFloorArea = GetPrimaryArea(group);

        if (mCamera == null) return;

        var pos = mCamera.transform.position;
        mCamera.transform.position = new Vector3(pos.x, group.transform.position.y, pos.z);
    }

    private void TickMove(float targetY)
    {
        if (mCamera == null) return;

        var pos = mCamera.transform.position;
        float newY = Mathf.MoveTowards(pos.y, targetY, mMoveSpeed * Time.deltaTime);
        mCamera.transform.position = new Vector3(pos.x, newY, pos.z);

        if (Mathf.Approximately(newY, targetY))
        {
            mMoveDisposable?.Dispose();
            mMoveDisposable = null;
        }
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

    private PlacementGridArea GetPrimaryArea(WaypointGroup group)
    {
        var areas = group.ZoneAreas;
        return (areas != null && areas.Length > 0) ? areas[0] : null;
    }
}
