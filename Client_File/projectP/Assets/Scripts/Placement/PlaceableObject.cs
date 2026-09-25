using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UniRx;

[RequireComponent(typeof(BoxCollider2D))]
public class PlaceableObject : MonoBehaviour
{
    // 레이아웃 순서(FurnitureRow.LayoutOrder) 기준 레이어. 0=바닥타일/벽지, 1=카펫, 2 이상=일반 가구(값이 클수록 위).
    private const int LAYER_TILE = 0;
    private const int LAYER_CARPET = 1;
    // sub가구는 베이스 레이어와 별개로 항상 가장 위에서 선택된다.
    private const int SUB_PICK_RANK = 1000;

    // 면/레이어별 스프라이트 정렬 순서. 배경 바닥(-10)·장식(-5)·벽(-1) 타일맵과 캐릭터(1) 사이에 끼워 넣는다.
    // 일반 가구(레이어 2 이상)는 프리팹에 지정된 정렬(빵 진열 등 자식 스프라이트 포함)을 그대로 쓴다.
    private const int SORTING_FLOOR_TILE = -3;
    private const int SORTING_CARPET = -2;
    private const int SORTING_WALL = 0;
    // sub가구(소품)는 올라가는 베이스 가구(1)보다 위.
    private const int SORTING_SUB = 2;

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

    // 벽 가구(FurnitureType=벽 또는 FixedType=벽고정)는 벽 셀에만, 나머지 베이스 가구는 바닥 셀에만 배치된다.
    public ePlacementSurface Surface { get; private set; } = ePlacementSurface.Floor;
    // 같은 레이어끼리만 칸을 두고 겹칠 수 없다(TilePlacementArea). sub가구에는 의미 없음.
    public int LayoutOrder { get; private set; } = LAYER_CARPET + 1;

    private int PickRank => mIsSubFurniture ? SUB_PICK_RANK : LayoutOrder;

    public void SetFurnitureTid(int tid)
    {
        mFurnitureTid = tid;
        mIsSubFurniture = false;

        var row = GameInstance.Table.Get<CTable.FurnitureRow>(tid);
        mFootprint = row != null
            ? new Vector2Int(Mathf.Max(1, row.Width), Mathf.Max(1, row.Height))
            : Vector2Int.one;
        mGroupId = row != null ? row.GroupId : 0;
        Surface = row != null && (row.FurnitureType == (int)CTable.eFurnitureType.Wall || row.FixedType == (int)CTable.eFurnitureFixedType.WallFixed)
            ? ePlacementSurface.Wall
            : ePlacementSurface.Floor;
        LayoutOrder = row != null ? Mathf.Max(0, row.LayoutOrder) : LAYER_CARPET + 1;

        ApplySortingOrder();
    }

    // 벽지/바닥타일/카펫처럼 면에 깔리는 가구만 정렬 순서를 레이어에 맞춰 덮어쓴다.
    private void ApplySortingOrder()
    {
        if (mSpriteRenderer == null)
            return;

        if (Surface == ePlacementSurface.Wall)
            mSpriteRenderer.sortingOrder = SORTING_WALL;
        else if (LayoutOrder == LAYER_TILE)
            mSpriteRenderer.sortingOrder = SORTING_FLOOR_TILE;
        else if (LayoutOrder == LAYER_CARPET)
            mSpriteRenderer.sortingOrder = SORTING_CARPET;
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

        if (mSpriteRenderer != null)
            mSpriteRenderer.sortingOrder = SORTING_SUB;
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

        // 바닥타일 위 카펫 위 테이블처럼 겹쳐 있으면 가장 위 레이어 가구만 집는다.
        if (FindTopmostAt(worldPos) != this)
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

    private static PlaceableObject FindTopmostAt(Vector2 worldPos)
    {
        PlaceableObject topmost = null;
        foreach (var hit in Physics2D.OverlapPointAll(worldPos))
        {
            var placeable = hit.GetComponent<PlaceableObject>();
            if (placeable == null || placeable.mCollider != hit || placeable.mArea == null)
                continue;

            if (topmost == null || placeable.PickRank > topmost.PickRank)
                topmost = placeable;
        }
        return topmost;
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
