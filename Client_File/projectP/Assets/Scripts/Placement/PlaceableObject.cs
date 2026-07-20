using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;

[RequireComponent(typeof(BoxCollider2D))]
public class PlaceableObject : MonoBehaviour
{
    [Header("Placement Area")]
    [SerializeField] private PlacementGridArea mArea;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer mSpriteRenderer;
    [SerializeField] private Color mValidColor = Color.white;
    [SerializeField] private Color mInvalidColor = new Color(1f, 0.4f, 0.4f, 0.6f);

    private CompositeDisposable mDragDisposables;
    private bool mIsDragging;

    private void OnMouseDown()
    {
        if (mArea == null)
            return;

        if (GameInstance.Model.Placement.IsPlacing.Value)
            return;

        mDragDisposables = new CompositeDisposable();
        mIsDragging = true;

        GameInstance.Model.Placement.BeginPlacement(transform, mArea);

        GameInstance.Model.Placement.IsValidPosition
            .Subscribe(UpdateVisual)
            .AddTo(mDragDisposables);

        GameInstance.Model.Placement.IsPlacing
            .Where(isPlacing => !isPlacing)
            .Take(1)
            .Subscribe(_ => EndPlacement())
            .AddTo(mDragDisposables);

        Observable.EveryUpdate()
            .Where(_ => mIsDragging)
            .Subscribe(_ => OnDragUpdate())
            .AddTo(mDragDisposables);
    }

    private void OnMouseUp()
    {
        mIsDragging = false;
    }

    private void OnDestroy()
    {
        mDragDisposables?.Dispose();
    }

    private void OnDragUpdate()
    {
        if (Pointer.current == null || Camera.main == null)
            return;

        Vector3 screenPos = Pointer.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = transform.position.z;

        GameInstance.Model.Placement.UpdatePreview(worldPos);
    }

    private void UpdateVisual(bool isValid)
    {
        if (mSpriteRenderer == null)
            return;

        mSpriteRenderer.color = isValid ? mValidColor : mInvalidColor;
    }

    private void EndPlacement()
    {
        mIsDragging = false;
        mDragDisposables?.Dispose();

        UpdateVisual(true);
    }
}
