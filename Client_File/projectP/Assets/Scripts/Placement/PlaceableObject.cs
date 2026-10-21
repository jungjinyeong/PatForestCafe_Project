using System.Collections.Generic;
using UnityEngine;
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

    // 콜라이더 → 가구. 클릭/철거 대상 판정(FindTopmostAt)이 매 프레임 GetComponent를 부르지 않도록 Awake/OnDestroy에서 등록·해제한다.
    private static readonly Dictionary<Collider2D, PlaceableObject> mPlaceablesByCollider = new Dictionary<Collider2D, PlaceableObject>();

    private BoxCollider2D mCollider;
    // 같은 오브젝트의 빵 진열대(Table.prefab만). 철거/리셋 시 진열 빵 정리에 쓴다. 없으면 null.
    private Intaraction_BreadStand mBreadStand;
    // 들고 있는 동안(IsPlacing)의 색 표시 구독. 내려놓으면 해제한다.
    private CompositeDisposable mHoldDisposables;

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
    // 그림 오브젝트(mSpriteRenderer)의 원래 회전. 바닥타일(RotateTile)은 여기에 90°씩 더해 그림을 돌린다.
    private Quaternion mBaseSpriteLocalRotation = Quaternion.identity;

    // 방향 = Down 기준 반시계 90° 회전 횟수(eFurnitureDirection 값). R은 시계 방향(Down→Left→Up→Right)으로 돈다.
    private CTable.eFurnitureDirection mDirection = CTable.eFurnitureDirection.Down;
    // 좌우반전. 벽가구(FlipXOnly)와 바닥타일(RotateTile + TileIncludeFlipX)만 쓴다 — 일반 가구의 Left 반전은 방향에서 계산한다.
    // 기하(칸/손님 칸/콜라이더)와 그림 모두 "좌우반전 후 회전" 순서로 적용한다.
    private bool mFlipX;
    // 아트 설정(방향별 스프라이트, 회전 방식). 등록되지 않은 가구면 null — 프리팹 그림 그대로, 회전 없음.
    private FurnitureVisualData mVisual;

    public int FurnitureTid => mFurnitureTid;
    public TilePlacementArea Area => mArea;
    public Intaraction_BreadStand BreadStand => mBreadStand;
    public int PlacementId => mPlacementId;
    public bool IsSubFurniture => mIsSubFurniture;
    public int GroupId => mGroupId;
    public int AllowedGroupId => mAllowedGroupId;
    public Vector2Int Footprint => mFootprint;
    public IReadOnlyList<Vector2Int> AccessOffsets => mAccessOffsets;
    public IReadOnlyList<Vector2Int> ClearanceOffsets => mRotatedClearanceOffsets;
    public CTable.eFurnitureDirection Direction => mDirection;
    public bool FlipX => mFlipX;
    // 아트 설정의 회전 방식. 아트 설정이 없거나 sub가구면 None(회전 없음).
    public CTable.eFurnitureOrientMode OrientMode => mVisual != null && !mIsSubFurniture ? mVisual.OrientMode : CTable.eFurnitureOrientMode.None;
    // R(회전 버튼)이 동작하는지. 회전 방식에 따라 회전/좌우반전 중 하나가 된다(RotateNext).
    public bool CanRotate => OrientMode != CTable.eFurnitureOrientMode.None;

    // 벽 가구(FurnitureType=벽 또는 FixedType=벽고정)는 벽 셀에만, 나머지 베이스 가구는 바닥 셀에만 배치된다.
    public ePlacementSurface Surface { get; private set; } = ePlacementSurface.Floor;
    // 같은 레이어끼리만 칸을 두고 겹칠 수 없다(TilePlacementArea). sub가구에는 의미 없음.
    public int LayoutOrder { get; private set; } = LAYER_FURNITURE_MIN;
    // 드래그 연속 설치 / Shift+드래그 사각형 채우기 대상인지. 지금은 바닥타일(FurnitureType=타일)만 — 대상은 추후 바뀔 수 있다.
    public bool IsBulkPlaceable { get; private set; }

    private int PickRank => mIsSubFurniture ? SUB_PICK_RANK : LayoutOrder;

    // visualOverride: 표의 VisualPath 대신 쓸 아트 설정. 가구 테스트 씬이 에디터에서 편집 중인 에셋을 그대로 쓰게 할 때만 넘긴다.
    public void SetFurnitureTid(int tid, FurnitureVisualData visualOverride = null)
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
        IsBulkPlaceable = row != null && row.FurnitureType == (int)CTable.eFurnitureType.Tile;
        mVisual = visualOverride != null ? visualOverride : GameInstance.Model.Placement.GetFurnitureVisual(tid, false);

        CacheBaseLayout();
        SetOrientation(CTable.eFurnitureDirection.Down, false);
        ApplySortingOrder();
    }

    // CSV에 아직 연결되지 않은 아트 설정을 가구 테스트 씬(GameModeFurnitureTest)에서 그림/회전만 확인할 때 쓴다.
    // Tid가 없어(0) 배치 확정해도 칸 점유·세이브 대상이 아니다. 칸 수는 콜라이더의 월드 크기로 어림하고 바닥 일반 가구로 취급한다.
    public void SetPreviewVisual(FurnitureVisualData visual)
    {
        mFurnitureTid = 0;
        mIsSubFurniture = false;
        mGroupId = 0;
        Surface = ePlacementSurface.Floor;
        LayoutOrder = LAYER_FURNITURE_MIN;
        IsBulkPlaceable = false;
        mVisual = visual;

        CacheBaseLayout();
        Vector3 scale = transform.lossyScale;
        mBaseFootprint = new Vector2Int(
            Mathf.Max(1, Mathf.RoundToInt(mBaseColliderSize.x * scale.x / CELL_WORLD_SIZE)),
            Mathf.Max(1, Mathf.RoundToInt(mBaseColliderSize.y * scale.y / CELL_WORLD_SIZE)));
        SetOrientation(CTable.eFurnitureDirection.Down, false);
        ApplySortingOrder();
    }

    // 아트 설정의 그림만 다시 읽어 적용한다(방향/칸/콜라이더는 그대로). 가구 테스트 씬에서 Inspector 편집을 반영할 때 쓴다.
    public void RefreshOrientationSprite()
    {
        if (!mIsSubFurniture)
            ApplyOrientationSprite();
    }

    // R 한 번: 회전 방식(아트 설정)에 따라 다음 상태로 바꾼다.
    // - DirectionalSprite(일반 가구): 시계 방향 90°(Down→Left→Up→Right)
    // - FlipXOnly(벽가구): 좌우반전 토글
    // - RotateTile(바닥타일): 그림 시계 방향 90°. TileIncludeFlipX면 한 바퀴 돌 때마다 좌우반전도 토글(8가지 상태)
    public void RotateNext()
    {
        var nextDirection = (CTable.eFurnitureDirection)(((int)mDirection + 3) % 4);

        switch (OrientMode)
        {
            case CTable.eFurnitureOrientMode.DirectionalSprite:
                SetOrientation(nextDirection, false);
                break;
            case CTable.eFurnitureOrientMode.FlipXOnly:
                SetOrientation(CTable.eFurnitureDirection.Down, !mFlipX);
                break;
            case CTable.eFurnitureOrientMode.RotateTile:
                bool wrapped = nextDirection == CTable.eFurnitureDirection.Down;
                SetOrientation(nextDirection, wrapped && mVisual.TileIncludeFlipX ? !mFlipX : mFlipX);
                break;
        }
    }

    // 방향/좌우반전을 지정한다(세이브 복원, 취소 시 되돌리기도 이 경로). 회전 방식에서 쓸 수 없는 값은 기본값으로 정리한다.
    // footprint 가로/세로, 손님 칸(자식 Waypoint 위치), 비워둘 칸, 콜라이더를 "좌우반전 후 회전"으로 함께 바꾸고 그림을 갱신한다.
    public void SetOrientation(CTable.eFurnitureDirection direction, bool flipX)
    {
        if (mIsSubFurniture)
            return;

        CacheBaseLayout();

        switch (OrientMode)
        {
            case CTable.eFurnitureOrientMode.None:
                direction = CTable.eFurnitureDirection.Down;
                flipX = false;
                break;
            case CTable.eFurnitureOrientMode.DirectionalSprite:
                flipX = false;
                break;
            case CTable.eFurnitureOrientMode.FlipXOnly:
                direction = CTable.eFurnitureDirection.Down;
                break;
            case CTable.eFurnitureOrientMode.RotateTile:
                if (!mVisual.TileIncludeFlipX)
                    flipX = false;
                break;
        }

        mDirection = direction;
        mFlipX = flipX;
        int steps = (int)direction;
        mFootprint = steps % 2 == 0 ? mBaseFootprint : new Vector2Int(mBaseFootprint.y, mBaseFootprint.x);

        // 가구 루트 = footprint 중심이라, 자식 로컬 위치는 원점 기준으로 반전/회전하면 된다.
        for (int i = 0; i < mWaypoints.Length; i++)
        {
            if (mWaypoints[i] == null)
                continue;

            Vector2 rotated = RotateCounterClockwise(MirrorX(mWaypointBaseDeltas[i], flipX), steps);
            mWaypoints[i].transform.position = transform.position + new Vector3(rotated.x, rotated.y, mWaypointBaseDeltas[i].z);
        }

        if (mCollider != null)
        {
            mCollider.offset = RotateCounterClockwise(MirrorX(mBaseColliderOffset, flipX), steps);
            mCollider.size = steps % 2 == 0 ? mBaseColliderSize : new Vector2(mBaseColliderSize.y, mBaseColliderSize.x);
        }

        mRotatedClearanceOffsets.Clear();
        foreach (var offset in mClearanceOffsets)
        {
            var mirrored = flipX ? new Vector2Int(mBaseFootprint.x - 1 - offset.x, offset.y) : offset;
            mRotatedClearanceOffsets.Add(RotateCellOffset(mirrored, mBaseFootprint, steps));
        }

        RefreshAccessOffsets();
        ApplyOrientationSprite();
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
        mBaseSpriteLocalRotation = mSpriteRenderer != null ? mSpriteRenderer.transform.localRotation : Quaternion.identity;
    }

    private static Vector2 MirrorX(Vector2 v, bool flipX)
    {
        return flipX ? new Vector2(-v.x, v.y) : v;
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

    // 회전 방식별 그림(아트 설정 FurnitureVisualData). 벽지처럼 여러 가구가 프리팹 하나를 같이 쓰는 경우에도 이 경로로 그림만 바꿔 끼운다.
    // - DirectionalSprite: Down = Front, Up = Back, Right = Side, Left = Side 좌우반전. 픽셀아트라 그림 자체는 돌리지 않는다.
    // - FlipXOnly: Front + 좌우반전
    // - RotateTile: Front + 좌우반전 후 그림 오브젝트를 반시계 (방향 값 × 90°) 회전(= 시계 방향 R 횟수 × 90°)
    // 그림이 비어 있으면 Front, Front도 없으면 프리팹에 지정된 그림을 쓴다.
    private void ApplyOrientationSprite()
    {
        if (mSpriteRenderer == null)
            return;

        var mode = OrientMode;
        Sprite sprite = null;
        bool flipX = mFlipX;

        if (mode == CTable.eFurnitureOrientMode.DirectionalSprite)
        {
            if (mDirection == CTable.eFurnitureDirection.Up)
                sprite = mVisual.Back;
            else if (mDirection == CTable.eFurnitureDirection.Right || mDirection == CTable.eFurnitureDirection.Left)
                sprite = mVisual.Side;
            flipX = mDirection == CTable.eFurnitureDirection.Left;
        }

        if (sprite == null && mVisual != null)
            sprite = mVisual.Front;

        mSpriteRenderer.sprite = sprite != null ? sprite : mBaseSprite;
        mSpriteRenderer.flipX = flipX;
        ApplySpriteRotation(mode == CTable.eFurnitureOrientMode.RotateTile ? (int)mDirection * 90f : 0f);
    }

    // 그림 오브젝트만 돌린다(루트를 돌리면 콜라이더/Waypoint까지 돌아 칸 계산이 틀어짐). 그림이 루트에 붙은 프리팹은 돌리지 않는다.
    private void ApplySpriteRotation(float counterClockwiseDegrees)
    {
        var spriteTransform = mSpriteRenderer.transform;
        if (spriteTransform == transform)
        {
            if (counterClockwiseDegrees != 0f)
                Logger.Warning($"[PlaceableObject] '{name}'은 SpriteRenderer가 루트에 있어 그림을 회전할 수 없습니다. 그림을 자식 오브젝트로 옮겨 주세요.");
            return;
        }

        spriteTransform.localRotation = mBaseSpriteLocalRotation * Quaternion.Euler(0f, 0f, counterClockwiseDegrees);
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
        mBreadStand = TryGetComponent(out Intaraction_BreadStand breadStand) ? breadStand : null;
        if (mCollider != null)
            mPlaceablesByCollider[mCollider] = this;
        CacheBaseLayout();
    }

    private void OnDestroy()
    {
        mHoldDisposables?.Dispose();
        if (mCollider != null && mPlaceablesByCollider.TryGetValue(mCollider, out var registered) && registered == this)
            mPlaceablesByCollider.Remove(mCollider);
    }

    // 배치 모드에서 이 가구를 집는다(들고 있기 시작). PlacementInputController가 LMB/탭 위치의 가장 위 가구(FindTopmostAt)를 골라 호출한다.
    // 이후 커서를 따라다니고, LMB(또는 확정 버튼)로 놓거나 RMB(또는 취소 버튼)로 원위치/보관함으로 돌린다.
    public void BeginPickUp()
    {
        if (mArea == null)
            return;

        GameInstance.Model.Placement.BeginPlacement(transform, mArea, mFootprint);

        // 위에 sub가구가 있는 베이스처럼 BeginPlacement가 거절하면 들고 있지 않은 상태 그대로다.
        if (GameInstance.Model.Placement.IsPlacing.Value)
            BeginHoldVisual();
    }

    /// <summary>
    /// 가구 목록의 + 버튼으로 새로 생성된 인스턴스를 곧바로 들고 있는 상태로 만든다.
    /// </summary>
    public void BeginPlacementFromSpawn(TilePlacementArea area)
    {
        if (area == null)
            return;

        mArea = area;
        BeginPickUp();
    }

    // 월드 좌표에서 집을 수 있는 가장 위 가구. 바닥타일 위 카펫 위 테이블처럼 겹쳐 있으면 레이어가 가장 높은 것(sub가구 최우선).
    // 영역(mArea)이 정해지지 않은 오브젝트는 배치 대상이 아니므로 제외한다.
    public static PlaceableObject FindTopmostAt(Vector2 worldPos)
    {
        PlaceableObject topmost = null;
        foreach (var hit in Physics2D.OverlapPointAll(worldPos))
        {
            if (!mPlaceablesByCollider.TryGetValue(hit, out var placeable) || placeable == null || placeable.mArea == null)
                continue;

            if (topmost == null || placeable.PickRank > topmost.PickRank)
                topmost = placeable;
        }
        return topmost;
    }

    // area에 배치된(확정된) 가구 중 placementId가 일치하는 것. 없으면 null.
    public static PlaceableObject FindByPlacementId(TilePlacementArea area, int placementId)
    {
        foreach (var placeable in mPlaceablesByCollider.Values)
        {
            if (placeable != null && placeable.mArea == area && placeable.mPlacementId == placementId)
                return placeable;
        }
        return null;
    }

    // 들고 있는 동안 놓을 수 있는지에 따라 색을 바꾸고, 내려놓으면(확정/취소) 원래 색으로 돌린다.
    private void BeginHoldVisual()
    {
        mHoldDisposables?.Dispose();
        mHoldDisposables = new CompositeDisposable();

        GameInstance.Model.Placement.IsValidPosition
            .Subscribe(UpdateVisual)
            .AddTo(mHoldDisposables);

        GameInstance.Model.Placement.IsPlacing
            .Where(isPlacing => !isPlacing)
            .Take(1)
            .Subscribe(_ => EndHoldVisual())
            .AddTo(mHoldDisposables);
    }

    private void UpdateVisual(bool isValid)
    {
        if (mSpriteRenderer == null)
            return;

        mSpriteRenderer.color = isValid ? mValidColor : mInvalidColor;
    }

    // 철거 모드에서 커서가 올라간 대상을 붉게 표시한다(PlacementInputController가 켜고 끈다).
    public void SetDemolishHighlight(bool isHighlighted)
    {
        UpdateVisual(!isHighlighted);
    }

    private void EndHoldVisual()
    {
        mHoldDisposables?.Dispose();
        mHoldDisposables = null;

        UpdateVisual(true);
    }

#if UNITY_EDITOR
    // 켜면 선택하지 않은 가구도 전부 칸을 그린다(가구 테스트 창의 "칸 표시" 토글).
    public static bool AlwaysDrawCellGizmos;

    private void OnDrawGizmos()
    {
        if (AlwaysDrawCellGizmos)
            DrawCellGizmos();
    }

    // 작업자 확인용: 선택하면 Scene 뷰에 칸을 그린다 — 가구 칸 흰색, 손님 칸(자식 Waypoint) 파란색, 비워둘 칸 빨간색.
    private void OnDrawGizmosSelected()
    {
        if (!AlwaysDrawCellGizmos)
            DrawCellGizmos();
    }

    // 에디터에서는 표의 가로/세로를 모르므로 콜라이더의 월드 크기로 칸 수를 어림한다.
    private void DrawCellGizmos()
    {
        Vector2Int footprint = mFootprint;
        IEnumerable<Vector2Int> access = mAccessOffsets;
        IEnumerable<Vector2Int> clearance = mRotatedClearanceOffsets;

        // 런타임 칸 정보는 Tid(또는 미리보기 아트 설정)를 지정한 뒤에만 있다. 그 전에는 콜라이더/Waypoint로 어림한다.
        if (!Application.isPlaying || (mFurnitureTid <= 0 && mVisual == null))
        {
            var box = mCollider != null ? mCollider : GetComponent<BoxCollider2D>();
            Vector3 scale = transform.lossyScale;
            footprint = box != null
                ? new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(box.size.x * scale.x / CELL_WORLD_SIZE)), Mathf.Max(1, Mathf.RoundToInt(box.size.y * scale.y / CELL_WORLD_SIZE)))
                : Vector2Int.one;

            var editAccess = new List<Vector2Int>();
            foreach (var waypoint in mWaypoints ?? GetComponentsInChildren<Waypoint>(true))
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
