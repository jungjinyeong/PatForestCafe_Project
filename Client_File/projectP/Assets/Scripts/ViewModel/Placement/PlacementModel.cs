using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class PlacedFurnitureRecord
{
    public int Tid;
    public Vector3 Position;
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
    private PlacementGridArea mArea;
    private Vector3 mOriginPosition;

    // 배치 확정된 가구(Tid+위치) 레지스트리. placementId는 최초 배치 시 발급되고,
    // 이후 같은 가구를 다시 옮겨도 같은 id의 Position만 갱신된다(세이브 시 이 딕셔너리를 그대로 직렬화).
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
