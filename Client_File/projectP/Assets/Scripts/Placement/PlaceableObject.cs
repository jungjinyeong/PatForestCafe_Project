using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

    private static readonly List<RaycastResult> mUIRaycastResults = new List<RaycastResult>();

    private BoxCollider2D mCollider;
    private CompositeDisposable mDragDisposables;
    private bool mIsDragging;

    private void Awake()
    {
        mCollider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        Observable.EveryUpdate()
            .Where(_ => Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            .Subscribe(_ => OnPointerDown())
            .AddTo(this);
    }

    private void OnDestroy()
    {
        mDragDisposables?.Dispose();
    }

    private void OnPointerDown()
    {
        if (mArea == null || Camera.main == null)
            return;

        if (GameInstance.Model.Placement.IsPlacing.Value)
            return;

        if (IsPointerOverUI())
            return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Pointer.current.position.ReadValue());

        if (Physics2D.OverlapPoint(worldPos) != mCollider)
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

        Observable.EveryUpdate()
            .Where(_ => mIsDragging && Pointer.current != null && Pointer.current.press.wasReleasedThisFrame)
            .Subscribe(_ => OnPointerUp())
            .AddTo(mDragDisposables);
    }

    private void OnPointerUp()
    {
        if (GameInstance.Model.Placement.IsValidPosition.Value)
            GameInstance.Model.Placement.Confirm();
        else
            GameInstance.Model.Placement.Cancel();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null || Pointer.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Pointer.current.position.ReadValue()
        };

        mUIRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, mUIRaycastResults);

        return mUIRaycastResults.Count > 0;
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
