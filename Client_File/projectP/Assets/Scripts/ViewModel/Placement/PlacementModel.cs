using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class PlacedFurnitureRecord
{
    public int Tid;
    // 배치된 월드 좌표. 로드 시에는 Floor/Cell로 다시 계산하고, Floor==0인 구 세이브만 이 값으로 층/셀을 역산한다.
    public Vector3 Position;
    // 배치된 층(WaypointGroup.Order)과 footprint 기준 셀(좌하단). 0층은 미기록(구 세이브).
    public int Floor;
    public Vector2Int Cell;

    // Intaraction_BreadStand처럼 배치 후 첫 사용 시점에 종류가 정해지는 가구 전용 필드. 0이면 미지정.
    public int AssignedBreadTid;

    // true면 Tid가 CTable.SubFurnitureRow, false면 CTable.FurnitureRow를 가리킨다.
    public bool IsSub;

    // 가구가 바라보는 방향(바닥타일은 그림 회전 단계). 구 세이브는 Down(0).
    public CTable.eFurnitureDirection Direction;
    // 좌우반전(벽가구, 반전 허용 바닥타일). 구 세이브는 false.
    public bool FlipX;
}

public partial class PlacementModel : IModelBase
{
    public IReadOnlyReactiveProperty<bool> IsPlacing => mIsPlacing;
    private readonly ReactiveProperty<bool> mIsPlacing = new ReactiveProperty<bool>(false);

    public IReadOnlyReactiveProperty<bool> IsValidPosition => mIsValidPosition;
    private readonly ReactiveProperty<bool> mIsValidPosition = new ReactiveProperty<bool>(false);

    public IReadOnlyReactiveProperty<bool> IsEditMode => mIsEditMode;
    private readonly ReactiveProperty<bool> mIsEditMode = new ReactiveProperty<bool>(false);

    // G키 그리드 오버레이 켜짐 여부(사용자 설정). 실제 표시는 배치 모드일 때만(PlacementGridOverlay).
    public IReadOnlyReactiveProperty<bool> IsGridOverlayOn => mIsGridOverlayOn;
    private readonly ReactiveProperty<bool> mIsGridOverlayOn = new ReactiveProperty<bool>(false);

    // 철거 모드(X/Delete). 배치 모드 안에서만 켜지고, 가구를 들고 있는 동안에는 켜지지 않는다.
    public IReadOnlyReactiveProperty<bool> IsDemolishMode => mIsDemolishMode;
    private readonly ReactiveProperty<bool> mIsDemolishMode = new ReactiveProperty<bool>(false);

    private Transform mTarget;
    private PlaceableObject mPlaceable;
    private TilePlacementArea mArea;
    private Vector3 mOriginPosition;
    // 재드래그를 취소하면 되돌릴 시작 방향/좌우반전.
    private CTable.eFurnitureDirection mOriginDirection;
    private bool mOriginFlipX;
    private Vector2Int mFootprint = Vector2Int.one;

    // 배치 확정된 가구(Tid+위치) 레지스트리. placementId는 최초 배치 시 발급되고,
    // 이후 같은 가구를 다시 옮겨도 같은 id의 위치(Position/Floor/Cell)만 갱신된다(세이브 시 이 딕셔너리를 그대로 직렬화).
    private readonly Dictionary<int, PlacedFurnitureRecord> mDicPlacedFurniture = new();
    private int mNextPlacementId = 1;

    // 보유 가구 수(구매한 총 개수, 배치된 것 포함). 보관 중(미배치) = 보유 − 배치. 키 = (IsSub, Tid).
    // 가구는 팔 수 없다 — 리셋/취소해도 보유 수는 그대로이고 보관 중으로 돌아간다.
    private readonly Dictionary<(bool isSub, int tid), int> mOwnedFurniture = new();

    // 들고 있기(IsPlacing)를 거치지 않고 배치가 바뀌었을 때(철거/대량 설치/리셋) 발행. 손님 동선 재수집(WaypointGroup)용.
    public System.IObservable<Unit> PlacedLayoutChanged => mPlacedLayoutChanged;
    private readonly Subject<Unit> mPlacedLayoutChanged = new Subject<Unit>();

    // 보유/배치 수가 바뀔 때마다 증가(가구 목록 UI 갱신용).
    public IReadOnlyReactiveProperty<int> InventoryVersion => mInventoryVersion;
    private readonly ReactiveProperty<int> mInventoryVersion = new ReactiveProperty<int>(0);

    // (IsSub, Tid) -> 아트 설정(CSV VisualPath로 로드). 로드 실패(null)도 기록해 경고를 한 번만 남긴다.
    private readonly Dictionary<(bool isSub, int tid), FurnitureVisualData> mDicVisual = new();

    public void Init() { }

    public void Dispose()
    {
        mIsPlacing.Dispose();
        mIsValidPosition.Dispose();
        mIsEditMode.Dispose();
        mIsDemolishMode.Dispose();
        mIsGridOverlayOn.Dispose();
        mPlacedLayoutChanged.Dispose();
        mInventoryVersion.Dispose();
        mDicPlacedFurniture.Clear();
        mOwnedFurniture.Clear();
        mDicVisual.Clear();
    }
}
