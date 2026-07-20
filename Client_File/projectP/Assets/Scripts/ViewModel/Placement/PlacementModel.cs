using UniRx;
using UnityEngine;

public partial class PlacementModel : IModelBase
{
    public IReadOnlyReactiveProperty<bool> IsPlacing => mIsPlacing;
    private readonly ReactiveProperty<bool> mIsPlacing = new ReactiveProperty<bool>(false);

    public IReadOnlyReactiveProperty<bool> IsValidPosition => mIsValidPosition;
    private readonly ReactiveProperty<bool> mIsValidPosition = new ReactiveProperty<bool>(false);

    private Transform mTarget;
    private PlacementGridArea mArea;
    private Vector3 mOriginPosition;

    public void Init() { }

    public void Dispose()
    {
        mIsPlacing.Dispose();
        mIsValidPosition.Dispose();
    }
}
