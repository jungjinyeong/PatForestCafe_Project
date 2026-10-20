using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UniRx;

// 배치 모드 키/마우스 입력(에셋 Datas/Input/PlacementInput.inputactions, Addressables 주소 Input/PlacementInput)을 받아 PlacementModel로 넘긴다.
// 배치 모드(PlacementModel.IsEditMode)일 때만 액션 맵을 켠다. 키 변경은 바인딩 오버라이드 JSON을 PlayerPrefs에 저장/복원한다.
// UIRootLobby.Init()에서 AddComponent 후 Init()을 호출한다(GameInstance 의존 초기화 규칙).
public class PlacementInputController : MonoBehaviour
{
    private const string ASSET_ADDRESS = "Input/PlacementInput";
    private const string MAP_NAME = "Placement";
    private const string PREFS_KEY_BINDING_OVERRIDES = "PlacementInput.BindingOverrides";

    public const string ACTION_PLACE = "Place";
    public const string ACTION_CANCEL = "Cancel";
    public const string ACTION_ROTATE = "Rotate";
    public const string ACTION_DEMOLISH = "Demolish";
    public const string ACTION_FILL_MODIFIER = "FillModifier";
    public const string ACTION_TOGGLE_GRID = "ToggleGrid";
    public const string ACTION_POINT = "Point";

    // 조작 안내 UI(UIPlacementControlGuide)처럼 액션 바인딩을 읽어야 하는 쪽이 쓰는 현재 인스턴스.
    // HUD와 UIRootLobby의 Init 순서가 정해져 있지 않아 ReactiveProperty로 늦게 등록돼도 받을 수 있게 한다.
    public static IReadOnlyReactiveProperty<PlacementInputController> Current => mCurrent;
    private static readonly ReactiveProperty<PlacementInputController> mCurrent = new ReactiveProperty<PlacementInputController>();

    // 원본 에셋에 오버라이드/Enable 상태가 남지 않도록 복제본을 쓴다.
    private InputActionAsset mActions;
    private readonly Subject<Unit> mBindingsChanged = new Subject<Unit>();
    private InputActionMap mMap;
    private InputAction mPlace;
    private InputAction mCancel;
    private InputAction mRotate;
    private InputAction mDemolish;
    private InputAction mFillModifier;
    private InputAction mToggleGrid;
    private InputAction mPoint;

    private enum eBulkMode
    {
        None,
        Paint,  // LMB 드래그 연속 설치
        Rect,   // Shift + LMB 드래그 사각형 채우기
    }

    private static readonly List<RaycastResult> mUIRaycastResults = new List<RaycastResult>();
    // 바닥타일 대량 설치 중인 드래그 상태.
    private eBulkMode mBulkMode = eBulkMode.None;
    private Vector3Int mBulkStartCell;
    private Vector3Int mBulkLastCell;
    // 골드가 떨어져 이번 드래그의 연속 설치를 멈췄는지(버튼을 떼면 풀림).
    private bool mIsBulkStopped;
    // 철거 모드에서 커서가 올라가 붉게 표시 중인 배치물.
    private PlaceableObject mDemolishHover;
    // 바닥타일을 눌러 철거한 드래그인지(누른 채 지나가는 바닥타일도 계속 철거).
    private bool mIsDemolishDragTiles;

    // 리바인드 UI가 액션을 찾을 때 쓴다(GetAction 참고).
    public InputActionAsset Actions => mActions;
    // 바인딩 오버라이드를 저장/초기화할 때마다 발행(안내 UI 갱신용).
    public IObservable<Unit> BindingsChanged => mBindingsChanged;
    public bool IsPlaceHeld => mPlace != null && mPlace.IsPressed();
    public bool IsFillModifierHeld => mFillModifier != null && mFillModifier.IsPressed();
    public Vector2 PointerScreenPosition => mPoint != null ? mPoint.ReadValue<Vector2>() : Vector2.zero;

    private void OnDestroy()
    {
        if (mCurrent.Value == this)
            mCurrent.Value = null;

        mBindingsChanged.OnCompleted();
        mBindingsChanged.Dispose();

        if (mActions == null)
            return;

        mActions.Disable();
        Destroy(mActions);
    }

    public void Init()
    {
        var source = GameInstance.Resource.LoadSync<InputActionAsset>(ASSET_ADDRESS);
        if (source == null)
        {
            Logger.Warning($"[PlacementInputController] 입력 에셋을 찾을 수 없습니다. Address: {ASSET_ADDRESS}");
            return;
        }

        mActions = Instantiate(source);
        LoadBindingOverrides();

        mMap = mActions.FindActionMap(MAP_NAME, true);
        mPlace = mMap.FindAction(ACTION_PLACE, true);
        mCancel = mMap.FindAction(ACTION_CANCEL, true);
        mRotate = mMap.FindAction(ACTION_ROTATE, true);
        mDemolish = mMap.FindAction(ACTION_DEMOLISH, true);
        mFillModifier = mMap.FindAction(ACTION_FILL_MODIFIER, true);
        mToggleGrid = mMap.FindAction(ACTION_TOGGLE_GRID, true);
        mPoint = mMap.FindAction(ACTION_POINT, true);

        // 상점가 [가구 구입]은 배치 모드가 아니어도 곧바로 들고 있기에 들어가므로, 들고 있는 동안에도 입력을 받는다.
        GameInstance.Model.Placement.IsEditMode
            .CombineLatest(GameInstance.Model.Placement.IsPlacing, (isEditMode, isPlacing) => isEditMode || isPlacing)
            .DistinctUntilChanged()
            .Subscribe(SetMapEnabled)
            .AddTo(this);

        Observable.EveryUpdate()
            .Where(_ => mMap.enabled)
            .Subscribe(_ => Tick())
            .AddTo(this);

        mCurrent.Value = this;
    }

    public InputAction GetAction(string actionName)
    {
        return mMap?.FindAction(actionName);
    }

    // 안내 UI용 키 이름(예: "R", "LMB", "X / Delete"). 마우스/키보드 바인딩만 모아 " / "로 잇는다.
    // 오버라이드가 반영된 표시 이름이라 키를 바꾸면 안내도 같이 바뀐다.
    public string GetKeyDisplayName(string actionName)
    {
        var action = GetAction(actionName);
        if (action == null)
            return string.Empty;

        var names = new System.Collections.Generic.List<string>();
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite)
                continue;

            string path = binding.effectivePath;
            if (string.IsNullOrEmpty(path) || !(path.StartsWith("<Mouse>") || path.StartsWith("<Keyboard>")))
                continue;

            string name = action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            if (!string.IsNullOrEmpty(name) && !names.Contains(name))
                names.Add(name);
        }
        return string.Join(" / ", names);
    }

    // 리바인드(InputAction.PerformInteractiveRebinding) 완료 후 호출해 다음 실행에도 유지한다.
    public void SaveBindingOverrides()
    {
        if (mActions == null)
            return;

        PlayerPrefs.SetString(PREFS_KEY_BINDING_OVERRIDES, mActions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
        mBindingsChanged.OnNext(Unit.Default);
    }

    public void ResetBindingOverrides()
    {
        if (mActions == null)
            return;

        mActions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(PREFS_KEY_BINDING_OVERRIDES);
        PlayerPrefs.Save();
        mBindingsChanged.OnNext(Unit.Default);
    }

    private void LoadBindingOverrides()
    {
        string json = PlayerPrefs.GetString(PREFS_KEY_BINDING_OVERRIDES, string.Empty);
        if (!string.IsNullOrEmpty(json))
            mActions.LoadBindingOverridesFromJson(json);
    }

    private void SetMapEnabled(bool isEnabled)
    {
        if (isEnabled)
        {
            mMap.Enable();
        }
        else
        {
            mMap.Disable();
            // 배치 모드를 끄면 Tick이 돌지 않으므로 철거 대상 표시를 여기서 지운다.
            SetDemolishHover(null);
        }
    }

    // 들고 있기 방식: 들고 있지 않을 때 LMB/탭 = 가장 위 가구 집기, 들고 있을 때 가구가 커서를 따라오고 LMB = 놓기, RMB = 취소.
    // 새로 꺼낸 바닥타일은 LMB 드래그 연속 설치 / Shift+LMB 드래그 사각형 채우기(TickBulk).
    // X/Delete = 철거 모드 토글(들고 있지 않을 때), 철거 모드에서 LMB = 철거(TickDemolish), RMB = 철거 모드 끄기.
    // G = 그리드 오버레이 토글(표시는 PlacementGridOverlay).
    private void Tick()
    {
        var placement = GameInstance.Model.Placement;

        if (mRotate.WasPressedThisFrame())
            placement.RotatePlacing();

        if (mDemolish.WasPressedThisFrame())
            placement.ToggleDemolishMode();

        if (mCancel.WasPressedThisFrame())
        {
            if (placement.IsPlacing.Value)
                placement.Cancel();
            else if (placement.IsDemolishMode.Value)
                placement.SetDemolishMode(false);
        }

        if (mToggleGrid.WasPressedThisFrame())
            placement.ToggleGridOverlay();

        if (placement.IsDemolishMode.Value)
        {
            TickDemolish(placement);
            return;
        }

        SetDemolishHover(null);

        if (placement.IsPlacing.Value)
            TickHolding(placement);
        else
            TickIdle();
    }

    // 철거 모드: 커서 아래 가장 위 배치물을 붉게 표시하고, UI 밖에서 LMB를 누르면 철거(보관함으로 회수)한다.
    // 바닥타일을 눌러 철거했다면 버튼을 누른 채 지나가는 바닥타일도 계속 철거한다(연속 설치와 대칭). 다른 가구는 클릭마다 하나씩.
    private void TickDemolish(PlacementModel placement)
    {
        mBulkMode = eBulkMode.None;

        PlaceableObject hover = null;
        if (!IsPointerOverUI() && TryGetPointerWorld(out var worldPos))
        {
            hover = PlaceableObject.FindTopmostAt(worldPos);
            if (hover != null && hover.PlacementId < 0)
                hover = null;
        }

        if (mPlace.WasPressedThisFrame())
        {
            mIsDemolishDragTiles = hover != null && hover.IsBulkPlaceable;
            if (hover != null && placement.Demolish(hover))
                hover = null;
        }
        else if (mPlace.IsPressed())
        {
            if (mIsDemolishDragTiles && hover != null && hover.IsBulkPlaceable && placement.Demolish(hover))
                hover = null;
        }
        else
        {
            mIsDemolishDragTiles = false;
        }

        SetDemolishHover(hover);
    }

    private void SetDemolishHover(PlaceableObject hover)
    {
        if (mDemolishHover == hover)
            return;

        // 철거돼 파괴된 오브젝트는 Unity null이라 표시를 되돌릴 필요가 없다.
        if (mDemolishHover != null)
            mDemolishHover.SetDemolishHighlight(false);

        mDemolishHover = hover;

        if (mDemolishHover != null)
            mDemolishHover.SetDemolishHighlight(true);
    }

    // 들고 있지 않을 때: UI 밖을 누르면 그 위치의 가장 위 가구를 집는다. 집은 프레임에는 놓기 판정을 하지 않는다(다음 클릭부터).
    private void TickIdle()
    {
        mBulkMode = eBulkMode.None;

        if (!mPlace.WasPressedThisFrame() || IsPointerOverUI() || !TryGetPointerWorld(out var worldPos))
            return;

        var target = PlaceableObject.FindTopmostAt(worldPos);
        if (target != null)
            target.BeginPickUp();
    }

    // 들고 있을 때: 항상 커서를 따라오고, UI 밖에서 LMB를 누르면 그 칸에 놓는다(놓을 수 없는 칸이면 계속 들고 있음).
    // PC 전용 빌드라 터치 입력은 다루지 않는다.
    private void TickHolding(PlacementModel placement)
    {
        if (!TryGetPointerWorld(out var worldPos))
            return;

        placement.UpdatePreview(worldPos);

        if (placement.IsBulkPlacing)
        {
            TickBulk(placement, worldPos);
            return;
        }

        if (mPlace.WasPressedThisFrame() && !IsPointerOverUI() && placement.IsValidPosition.Value)
            placement.Confirm();
    }

    // 새로 꺼낸 바닥타일을 들고 있을 때(마우스): 들고 있는 타일은 커서에 남겨 두고 칸마다 복사본을 깐다.
    // - LMB 클릭/드래그: 지나간 칸마다 깐다(빠르게 움직여 건너뛴 칸은 직선으로 이어 채움).
    // - Shift + LMB 드래그: 시작 칸~현재 칸 사각형을 미리 보여 주고, 버튼을 떼면 한 번에 채운다.
    // 골드가 떨어지면 이번 드래그는 거기서 멈춘다. RMB(취소)로 들고 있기를 끝낸다.
    private void TickBulk(PlacementModel placement, Vector3 worldPos)
    {
        if (!placement.TryGetPlacingCell(worldPos, out var cell))
            return;

        if (mPlace.WasPressedThisFrame())
        {
            if (IsPointerOverUI())
                return;

            mBulkMode = IsFillModifierHeld ? eBulkMode.Rect : eBulkMode.Paint;
            mBulkStartCell = cell;
            mBulkLastCell = cell;
            mIsBulkStopped = false;

            if (mBulkMode == eBulkMode.Paint)
                PaintBulk(placement, FilterToFootprintGrid(new List<Vector3Int> { cell }, placement.PlacingFootprint));
        }
        else if (mBulkMode == eBulkMode.Paint && mPlace.IsPressed())
        {
            if (cell != mBulkLastCell)
            {
                var line = GetLineCells(mBulkLastCell, cell);
                line.RemoveAt(0); // 시작 칸은 이전 프레임에 이미 처리
                PaintBulk(placement, FilterToFootprintGrid(line, placement.PlacingFootprint));
                mBulkLastCell = cell;
            }
        }
        else if (mBulkMode == eBulkMode.Rect && mPlace.IsPressed())
        {
            placement.ShowBulkFillPreview(FilterToFootprintGrid(GetRectCells(mBulkStartCell, cell), placement.PlacingFootprint));
        }
        else if (mBulkMode == eBulkMode.Rect && mPlace.WasReleasedThisFrame())
        {
            placement.PlaceBulkCopies(FilterToFootprintGrid(GetRectCells(mBulkStartCell, cell), placement.PlacingFootprint), out _);
            mBulkMode = eBulkMode.None;
        }
        else if (!mPlace.IsPressed())
        {
            mBulkMode = eBulkMode.None;
        }
    }

    private void PaintBulk(PlacementModel placement, List<Vector3Int> cells)
    {
        if (mIsBulkStopped || cells.Count == 0)
            return;

        placement.PlaceBulkCopies(cells, out bool outOfGold);
        mIsBulkStopped = outOfGold;
    }

    // from→to 사이 칸(양 끝 포함, from부터 순서대로). 브레젠험 직선.
    private static List<Vector3Int> GetLineCells(Vector3Int from, Vector3Int to)
    {
        var cells = new List<Vector3Int>();
        int x = from.x, y = from.y;
        int dx = Mathf.Abs(to.x - x), dy = -Mathf.Abs(to.y - y);
        int sx = x < to.x ? 1 : -1, sy = y < to.y ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            cells.Add(new Vector3Int(x, y, 0));
            if (x == to.x && y == to.y)
                break;

            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
        }
        return cells;
    }

    // 1x1보다 큰 타일이면 드래그 시작 칸 기준 footprint 간격의 칸만 남긴다(겹치는 위치를 빼서 빈틈없이 이어 깔리게).
    private List<Vector3Int> FilterToFootprintGrid(List<Vector3Int> cells, Vector2Int footprint)
    {
        if (footprint.x <= 1 && footprint.y <= 1)
            return cells;

        cells.RemoveAll(c => Mod(c.x - mBulkStartCell.x, footprint.x) != 0 || Mod(c.y - mBulkStartCell.y, footprint.y) != 0);
        return cells;
    }

    private static int Mod(int value, int divisor)
    {
        int r = value % divisor;
        return r < 0 ? r + divisor : r;
    }

    // a, b를 두 꼭짓점으로 하는 사각형 안의 모든 칸.
    private static List<Vector3Int> GetRectCells(Vector3Int a, Vector3Int b)
    {
        var cells = new List<Vector3Int>();
        for (int x = Mathf.Min(a.x, b.x); x <= Mathf.Max(a.x, b.x); x++)
            for (int y = Mathf.Min(a.y, b.y); y <= Mathf.Max(a.y, b.y); y++)
                cells.Add(new Vector3Int(x, y, 0));
        return cells;
    }

    private bool TryGetPointerWorld(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;
        if (Camera.main == null)
            return false;

        worldPos = Camera.main.ScreenToWorldPoint(PointerScreenPosition);
        return true;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        var eventData = new PointerEventData(EventSystem.current) { position = PointerScreenPosition };
        mUIRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, mUIRaycastResults);
        return mUIRaycastResults.Count > 0;
    }
}
