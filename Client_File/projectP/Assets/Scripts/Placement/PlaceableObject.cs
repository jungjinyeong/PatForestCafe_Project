using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UniRx;

[RequireComponent(typeof(BoxCollider2D))]
public class PlaceableObject : MonoBehaviour
{
    [Header("Placement Area")]
    [SerializeField] private TilePlacementArea mArea;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer mSpriteRenderer;
    [SerializeField] private Color mValidColor = Color.white;
    [SerializeField] private Color mInvalidColor = new Color(1f, 0.4f, 0.4f, 0.6f);

    private static readonly List<RaycastResult> mUIRaycastResults = new List<RaycastResult>();

    private BoxCollider2D mCollider;
    private CompositeDisposable mDragDisposables;
    private bool mIsDragging;

    // 구매로 스폰된 가구인지 식별하는 값. 0이면 배치 영속화 대상이 아닌(세이브/로드 대상 아닌) 오브젝트로 취급한다.
    // mIsSubFurniture에 따라 CTable.FurnitureRow 또는 CTable.SubFurnitureRow 중 어느 테이블의 Tid인지 결정된다.
    private int mFurnitureTid;
    // PlacementModel.mDicPlacedFurniture의 키. 최초 배치 확정 전까지는 -1(미발급).
    private int mPlacementId = -1;
    // 이 가구가 차지하는 (가로,세로) 셀 수. FurnitureRow/SubFurnitureRow의 Width/Height, 없으면 1x1.
    private Vector2Int mFootprint = Vector2Int.one;
    // true면 sub가구(다른 가구 그룹 위에만 배치 가능), false면 일반(베이스) 가구.
    private bool mIsSubFurniture;
    // 베이스 가구 자신의 그룹(FurnitureRow.GroupId). sub가구에게는 의미 없음(0).
    private int mGroupId;
    // sub가구가 올라갈 수 있는 베이스 그룹(SubFurnitureRow.AllowedGroupId). 베이스 가구에게는 의미 없음(0).
    private int mAllowedGroupId;

    public int FurnitureTid => mFurnitureTid;
    public int PlacementId => mPlacementId;
    public bool IsSubFurniture => mIsSubFurniture;
    public int GroupId => mGroupId;
    public int AllowedGroupId => mAllowedGroupId;
    public Vector2Int Footprint => mFootprint;

    public void SetFurnitureTid(int tid)
    {
        mFurnitureTid = tid;
        mIsSubFurniture = false;

        var row = GameInstance.Table.Get<CTable.FurnitureRow>(tid);
        mFootprint = row != null
            ? new Vector2Int(Mathf.Max(1, row.Width), Mathf.Max(1, row.Height))
            : Vector2Int.one;
        mGroupId = row != null ? row.GroupId : 0;
    }

    public void SetSubFurnitureTid(int tid)
    {
        mFurnitureTid = tid;
        mIsSubFurniture = true;

        var row = GameInstance.Table.Get<CTable.SubFurnitureRow>(tid);
        mFootprint = row != null
            ? new Vector2Int(Mathf.Max(1, row.Width), Mathf.Max(1, row.Height))
            : Vector2Int.one;
        mAllowedGroupId = row != null ? row.AllowedGroupId : 0;
    }

    public void SetPlacementId(int placementId)
    {
        mPlacementId = placementId;
    }

    // 세이브 복원 시 드래그를 시작하지 않고 영역만 채운다. mArea는 원래 BeginPlacementFromSpawn(구매 스폰)에서만
    // 런타임으로 채워지는데, 복원된 가구는 이 경로를 타지 않아 mArea가 비어있으면 이후 재배치 드래그가 막힌다.
    public void SetArea(TilePlacementArea area)
    {
        mArea = area;
    }

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

        if (!GameInstance.Model.Placement.IsEditMode.Value)
            return;

        if (GameInstance.Model.Placement.IsPlacing.Value)
            return;

        if (IsPointerOverUI())
            return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Pointer.current.position.ReadValue());

        if (Physics2D.OverlapPoint(worldPos) != mCollider)
            return;

        GameInstance.Model.Placement.BeginPlacement(transform, mArea, mFootprint);
        StartDragTracking();
    }

    /// <summary>
    /// 가구 목록의 + 버튼으로 새로 생성된 인스턴스를 곧바로 배치 모드로 전환한다.
    /// 포인터 클릭으로 시작되는 OnPointerDown과 달리 클릭 판정 없이 바로 드래그 추적을 건다.
    /// </summary>
    public void BeginPlacementFromSpawn(TilePlacementArea area)
    {
        if (area == null)
            return;

        mArea = area;
        GameInstance.Model.Placement.BeginPlacement(transform, mArea, mFootprint);
        StartDragTracking();
    }

    private void StartDragTracking()
    {
        mDragDisposables = new CompositeDisposable();
        mIsDragging = true;

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
