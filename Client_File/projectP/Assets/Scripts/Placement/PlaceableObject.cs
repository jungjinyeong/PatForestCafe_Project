using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UniRx;

[RequireComponent(typeof(BoxCollider2D))]
public class PlaceableObject : MonoBehaviour
{
    // 레이아웃 순서(FurnitureRow.LayoutOrder) 기준 레이어. 0=바닥타일/벽지, 1=카펫, 2 이상=일반 가구(값이 클수록 위).
    // TilePlacementArea도 LAYER_FURNITURE_MIN으로 "손님 칸/비워둘 칸을 막는 가구"를 판정한다.
    public const int LAYER_TILE = 0;
    public const int LAYER_CARPET = 1;
    public const int LAYER_FURNITURE_MIN = LAYER_CARPET + 1;
    // 배치 칸 1칸의 월드 크기(배치 Grid cellSize). 손님 칸/회전 계산은 가구 루트 스케일과 무관하게 월드 거리로 칸을 센다.
    private const float CELL_WORLD_SIZE = 1f;
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

    [Header("Clearance")]
    [Tooltip("다른 가구를 놓지 못하게 비워둘 칸. 가구 정면이 아래(Down)일 때 기준, 가구 좌하단 칸이 (0,0). 예: 왼쪽 (-1,0), 아래 (0,-1).\n" +
             "벽/배치 영역 밖에 걸려도 배치는 되고, 바닥타일·카펫은 이 칸에 깔 수 있다. 회전하면 같이 돈다. 선택하면 Scene 뷰에 빨간 칸으로 보인다.")]
    [SerializeField] private List<Vector2Int> mClearanceOffsets = new List<Vector2Int>();

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
    // 손님이 서는 접근 칸(footprint 기준 셀 기준 오프셋). 프리팹 자식 Waypoint 위치에서 계산한다(빵 테이블의 Waypoint_1 등).
    private readonly List<Vector2Int> mAccessOffsets = new List<Vector2Int>();
    // 현재 방향으로 돌린 비워둘 칸(mClearanceOffsets는 Down 기준 원본).
    private readonly List<Vector2Int> mRotatedClearanceOffsets = new List<Vector2Int>();

    // 회전 전(Down) 기준값. 회전할 때마다 여기서 다시 계산해 오차가 쌓이지 않게 한다.
    private bool mIsBaseCached;
    private Vector2Int mBaseFootprint = Vector2Int.one;
    private Waypoint[] mWaypoints;
    // 가구 루트 -> Waypoint 월드 거리(회전 전). 루트 스케일이 1이 아니어도 칸 계산이 틀어지지 않게 월드 기준으로 둔다.
    private Vector3[] mWaypointBaseDeltas;
    private Vector2 mBaseColliderOffset;
    private Vector2 mBaseColliderSize;
    private Sprite mBaseSprite;

    private CTable.eFurnitureDirection mDirection = CTable.eFurnitureDirection.Down;
    private bool mCanRotate;
    private string mAtlas;
    private string mFrontResourceName;
    private string mSideResourceName;

    public int FurnitureTid => mFurnitureTid;
    public int PlacementId => mPlacementId;
    public bool IsSubFurniture => mIsSubFurniture;
    public int GroupId => mGroupId;
    public int AllowedGroupId => mAllowedGroupId;
    public Vector2Int Footprint => mFootprint;
    public IReadOnlyList<Vector2Int> AccessOffsets => mAccessOffsets;
    public IReadOnlyList<Vector2Int> ClearanceOffsets => mRotatedClearanceOffsets;
    public CTable.eFurnitureDirection Direction => mDirection;
    // 표의 CanRotate. sub가구는 회전하지 않는다.
    public bool CanRotate => mCanRotate && !mIsSubFurniture;

    // 벽 가구(FurnitureType=벽 또는 FixedType=벽고정)는 벽 셀에만, 나머지 베이스 가구는 바닥 셀에만 배치된다.
    public ePlacementSurface Surface { get; private set; } = ePlacementSurface.Floor;
    // 같은 레이어끼리만 칸을 두고 겹칠 수 없다(TilePlacementArea). sub가구에는 의미 없음.
    public int LayoutOrder { get; private set; } = LAYER_FURNITURE_MIN;

    private int PickRank => mIsSubFurniture ? SUB_PICK_RANK : LayoutOrder;

    public void SetFurnitureTid(int tid)
    {
        mFurnitureTid = tid;
        mIsSubFurniture = false;

        var row = GameInstance.Table.Get<CTable.FurnitureRow>(tid);
        mBaseFootprint = row != null
            ? new Vector2Int(Mathf.Max(1, row.Width), Mathf.Max(1, row.Height))
            : Vector2Int.one;
        mGroupId = row != null ? row.GroupId : 0;
        Surface = row != null && (row.FurnitureType == (int)CTable.eFurnitureType.Wall || row.FixedType == (int)CTable.eFurnitureFixedType.WallFixed)
            ? ePlacementSurface.Wall
            : ePlacementSurface.Floor;
        LayoutOrder = row != null ? Mathf.Max(0, row.LayoutOrder) : LAYER_FURNITURE_MIN;
        mCanRotate = row != null && row.CanRotate;
        mAtlas = row?.Atlas;
        mFrontResourceName = row?.FrontResourceName;
        mSideResourceName = row?.SideResourceName;

        CacheBaseLayout();
        SetDirection(CTable.eFurnitureDirection.Down);
        ApplySortingOrder();
    }

    // 방향을 바꾼다: footprint 가로/세로, 손님 칸(자식 Waypoint 위치), 비워둘 칸, 콜라이더, 그림(앞면/옆면/좌우반전)을 함께 돌린다.
    // 픽셀아트라 스프라이트 자체는 돌리지 않는다.
    public void SetDirection(CTable.eFurnitureDirection direction)
    {
        if (mIsSubFurniture)
            return;

        CacheBaseLayout();

        mDirection = direction;
        int steps = (int)direction;
        mFootprint = steps % 2 == 0 ? mBaseFootprint : new Vector2Int(mBaseFootprint.y, mBaseFootprint.x);

        // 가구 루트 = footprint 중심이라, 자식 로컬 위치는 원점 기준으로 돌리면 된다.
        for (int i = 0; i < mWaypoints.Length; i++)
        {
            if (mWaypoints[i] == null)
                continue;

            Vector2 rotated = RotateCounterClockwise(mWaypointBaseDeltas[i], steps);
            mWaypoints[i].transform.position = transform.position + new Vector3(rotated.x, rotated.y, mWaypointBaseDeltas[i].z);
        }

        if (mCollider != null)
        {
            mCollider.offset = RotateCounterClockwise(mBaseColliderOffset, steps);
            mCollider.size = steps % 2 == 0 ? mBaseColliderSize : new Vector2(mBaseColliderSize.y, mBaseColliderSize.x);
        }

        mRotatedClearanceOffsets.Clear();
        foreach (var offset in mClearanceOffsets)
            mRotatedClearanceOffsets.Add(RotateCellOffset(offset, mBaseFootprint, steps));

        RefreshAccessOffsets();
        ApplyDirectionSprite();
    }

    // 다음 방향(Down→Right→Up→Left→Down).
    public CTable.eFurnitureDirection GetNextDirection()
    {
        return (CTable.eFurnitureDirection)(((int)mDirection + 1) % 4);
    }

    // 회전 기준값은 프리팹 원래 상태(Down)에서 한 번만 잡는다(Awake). 이후 회전은 항상 이 값에서 다시 계산한다.
    private void CacheBaseLayout()
    {
        if (mIsBaseCached)
            return;

        mIsBaseCached = true;
        mWaypoints = GetComponentsInChildren<Waypoint>(true);
        mWaypointBaseDeltas = new Vector3[mWaypoints.Length];
        for (int i = 0; i < mWaypoints.Length; i++)
            mWaypointBaseDeltas[i] = mWaypoints[i].transform.position - transform.position;

        mBaseColliderOffset = mCollider != null ? mCollider.offset : Vector2.zero;
        mBaseColliderSize = mCollider != null ? mCollider.size : Vector2.one;
        mBaseSprite = mSpriteRenderer != null ? mSpriteRenderer.sprite : null;
    }

    private static Vector2 RotateCounterClockwise(Vector2 v, int steps)
    {
        for (int i = 0; i < steps; i++)
            v = new Vector2(-v.y, v.x);
        return v;
    }

    // footprint 좌하단 기준 셀 오프셋을 돌린다: footprint 중심 기준으로 바꿔 회전한 뒤, 돌아간 footprint의 좌하단 기준으로 되돌린다.
    private static Vector2Int RotateCellOffset(Vector2Int offset, Vector2Int baseFootprint, int steps)
    {
        var center = new Vector2(offset.x - (baseFootprint.x - 1) * 0.5f, offset.y - (baseFootprint.y - 1) * 0.5f);
        Vector2 rotated = RotateCounterClockwise(center, steps);
        var footprint = steps % 2 == 0 ? baseFootprint : new Vector2Int(baseFootprint.y, baseFootprint.x);
        return new Vector2Int(
            Mathf.RoundToInt(rotated.x + (footprint.x - 1) * 0.5f),
            Mathf.RoundToInt(rotated.y + (footprint.y - 1) * 0.5f));
    }

    // 자식 Waypoint(손님이 서는 위치)가 놓인 칸을 footprint 기준 셀(좌하단) 오프셋으로 바꿔 둔다.
    // 프리팹에서 Waypoint를 옮기면 접근 칸도 따라 바뀐다. 가구 자신의 칸 안에 있는 Waypoint는 제외.
    private void RefreshAccessOffsets()
    {
        mAccessOffsets.Clear();

        foreach (var waypoint in mWaypoints)
        {
            if (waypoint == null)
                continue;

            var offset = ToCellOffset(waypoint.transform.position - transform.position, mFootprint);
            if (!IsInsideFootprint(offset, mFootprint) && !mAccessOffsets.Contains(offset))
                mAccessOffsets.Add(offset);
        }
    }

    // 가구 중심 -> 어떤 위치까지의 월드 거리를 footprint 좌하단 기준 칸 오프셋으로 바꾼다. 런타임 접근 칸과 Scene 뷰 표시가 같이 쓴다.
    private static Vector2Int ToCellOffset(Vector3 worldDelta, Vector2Int footprint)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldDelta.x / CELL_WORLD_SIZE + (footprint.x - 1) * 0.5f),
            Mathf.RoundToInt(worldDelta.y / CELL_WORLD_SIZE + (footprint.y - 1) * 0.5f));
    }

    private static bool IsInsideFootprint(Vector2Int offset, Vector2Int footprint)
    {
        return offset.x >= 0 && offset.x < footprint.x && offset.y >= 0 && offset.y < footprint.y;
    }

    // 방향별 그림: Down/Up = 표의 FrontResourceName, Right = SideResourceName, Left = SideResourceName 좌우반전.
    // 벽지처럼 여러 가구가 프리팹 하나를 같이 쓰는 경우에도 이 경로로 그림만 바꿔 끼운다.
    // 아틀라스에 해당 스프라이트가 없으면 프리팹에 지정된 그림을 그대로 쓴다(옆면 그림이 없으면 앞면 그림 + 좌우반전만).
    private void ApplyDirectionSprite()
    {
        if (mSpriteRenderer == null)
            return;

        bool isSide = mDirection == CTable.eFurnitureDirection.Right || mDirection == CTable.eFurnitureDirection.Left;
        var sprite = isSide ? Extension.ExtensionUI.GetAtlasSprite(mAtlas, mSideResourceName) : null;
        if (sprite == null)
            sprite = Extension.ExtensionUI.GetAtlasSprite(mAtlas, mFrontResourceName);

        mSpriteRenderer.sprite = sprite != null ? sprite : mBaseSprite;
        mSpriteRenderer.flipX = mDirection == CTable.eFurnitureDirection.Left;
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
        CacheBaseLayout();
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

        // PC: 배치 중 R키로 회전(모바일은 확정/취소 옆 회전 버튼).
        Observable.EveryUpdate()
            .Where(_ => mIsDragging && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            .Subscribe(_ => GameInstance.Model.Placement.RotatePlacing())
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

#if UNITY_EDITOR
    // 작업자 확인용: 선택하면 Scene 뷰에 칸을 그린다 — 가구 칸 흰색, 손님 칸(자식 Waypoint) 파란색, 비워둘 칸 빨간색.
    // 에디터에서는 표의 가로/세로를 모르므로 콜라이더의 월드 크기로 칸 수를 어림한다.
    private void OnDrawGizmosSelected()
    {
        Vector2Int footprint = mFootprint;
        IEnumerable<Vector2Int> access = mAccessOffsets;
        IEnumerable<Vector2Int> clearance = mRotatedClearanceOffsets;

        if (!Application.isPlaying || mFurnitureTid <= 0)
        {
            var box = GetComponent<BoxCollider2D>();
            Vector3 scale = transform.lossyScale;
            footprint = box != null
                ? new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(box.size.x * scale.x / CELL_WORLD_SIZE)), Mathf.Max(1, Mathf.RoundToInt(box.size.y * scale.y / CELL_WORLD_SIZE)))
                : Vector2Int.one;

            var editAccess = new List<Vector2Int>();
            foreach (var waypoint in GetComponentsInChildren<Waypoint>(true))
            {
                var offset = ToCellOffset(waypoint.transform.position - transform.position, footprint);
                if (!IsInsideFootprint(offset, footprint))
                    editAccess.Add(offset);
            }
            access = editAccess;
            clearance = mClearanceOffsets;
        }

        var originCenter = new Vector3(-(footprint.x - 1) * 0.5f, -(footprint.y - 1) * 0.5f, 0f);
        // 칸은 월드 단위라 가구 스케일은 빼고 위치만 따른다.
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one * CELL_WORLD_SIZE);

        Gizmos.color = new Color(1f, 1f, 1f, 0.9f);
        for (int x = 0; x < footprint.x; x++)
            for (int y = 0; y < footprint.y; y++)
                Gizmos.DrawWireCube(originCenter + new Vector3(x, y, 0f), new Vector3(0.95f, 0.95f, 0f));

        DrawGizmoCells(access, originCenter, new Color(0.3f, 0.6f, 1f, 0.45f));
        DrawGizmoCells(clearance, originCenter, new Color(1f, 0.25f, 0.25f, 0.45f));
    }

    private static void DrawGizmoCells(IEnumerable<Vector2Int> cells, Vector3 originCenter, Color color)
    {
        if (cells == null)
            return;

        foreach (var cell in cells)
        {
            var center = originCenter + new Vector3(cell.x, cell.y, 0f);
            Gizmos.color = color;
            Gizmos.DrawCube(center, new Vector3(0.9f, 0.9f, 0f));
            Gizmos.color = new Color(color.r, color.g, color.b, 1f);
            Gizmos.DrawWireCube(center, new Vector3(0.9f, 0.9f, 0f));
        }
    }
#endif
}
