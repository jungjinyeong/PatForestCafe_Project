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
}

public partial class PlacementModel : IModelBase
{
    public IReadOnlyReactiveProperty<bool> IsPlacing => mIsPlacing;
    private readonly ReactiveProperty<bool> mIsPlacing = new ReactiveProperty<bool>(false);

    public IReadOnlyReactiveProperty<bool> IsValidPosition => mIsValidPosition;
    private readonly ReactiveProperty<bool> mIsValidPosition = new ReactiveProperty<bool>(false);

    public IReadOnlyReactiveProperty<bool> IsEditMode => mIsEditMode;
    private readonly ReactiveProperty<bool> mIsEditMode = new ReactiveProperty<bool>(false);

    private Transform mTarget;
    private PlaceableObject mPlaceable;
    private TilePlacementArea mArea;
    private Vector3 mOriginPosition;
    private Vector2Int mFootprint = Vector2Int.one;

    // 배치 확정된 가구(Tid+위치) 레지스트리. placementId는 최초 배치 시 발급되고,
    // 이후 같은 가구를 다시 옮겨도 같은 id의 위치(Position/Floor/Cell)만 갱신된다(세이브 시 이 딕셔너리를 그대로 직렬화).
    private readonly Dictionary<int, PlacedFurnitureRecord> mDicPlacedFurniture = new();
    private int mNextPlacementId = 1;

    public void Init() { }

    public void Dispose()
    {
        mIsPlacing.Dispose();
        mIsValidPosition.Dispose();
        mIsEditMode.Dispose();
        mDicPlacedFurniture.Clear();
    }
}
