using System.Collections.Generic;
using UnityEngine;

// 가구 테스트 씬(Scenes/FurnitureTest) 전용 GameMode. 아트 설정(FurnitureVisualData)을 실제 배치 시스템(R 회전, 들기/놓기)으로 확인한다.
// 조작은 에디터 창 Tools/Furniture/가구 테스트 창(FurnitureTestWindow)에서 한다.
// - 실제 세이브를 덮어쓰지 않도록 저장을 끈다(로드한 데이터는 그대로 쓴다).
// - 골드/보유 수 처리 없이 가구를 바로 꺼내 들고 있게 한다.
// GameInstance.Init() → SceneManager.Init() → Init() 순서로 호출된다(GameInstance 의존 초기화 규칙).
public class GameModeFurnitureTest : GameModeBase
{
    // 4방향 보기 복사본 사이 간격(칸).
    private const float SHOWCASE_GAP = 1f;
    private const float SHOWCASE_LABEL_OFFSET_Y = 0.3f;
    private const int SHOWCASE_LABEL_SORTING_ORDER = 100;
    // 바닥타일 연속 설치(PlaceBulkCopies)는 칸마다 골드로 산다. 테스트 중 골드 부족으로 멈추지 않게 메모리에서만 채워 둔다(저장 안 함).
    private const int TEST_GOLD = 100000000;

    private static readonly CTable.eFurnitureDirection[] SHOWCASE_DIRECTIONS =
    {
        CTable.eFurnitureDirection.Down,
        CTable.eFurnitureDirection.Right,
        CTable.eFurnitureDirection.Up,
        CTable.eFurnitureDirection.Left,
    };

    [Tooltip("가구를 놓을 배치 영역.")]
    [SerializeField] private TilePlacementArea mArea;
    [Tooltip("4방향 보기 복사본을 왼쪽부터 늘어놓을 기준 위치(첫 복사본의 왼쪽 아래 칸).")]
    [SerializeField] private Transform mShowcaseRoot;

    // 4방향 보기 중인 아트 설정. Inspector에서 값을 바꾸면 이 줄을 다시 만든다.
    private FurnitureVisualData mShowcaseVisual;
    private readonly List<GameObject> mShowcaseObjects = new List<GameObject>();
    // CSV에 연결되지 않아 칸 점유 없이 띄운 미리보기(모두 지우기에서 같이 치운다).
    private readonly List<GameObject> mPreviewObjects = new List<GameObject>();

    public static GameModeFurnitureTest Current { get; private set; }

    public bool IsReady { get; private set; }

#if UNITY_EDITOR
    // Inspector 편집이 연달아 들어와도(드래그 등) 다음 에디터 틱에 한 번만 새로고침한다.
    private bool mIsRefreshQueued;
#endif

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
#if UNITY_EDITOR
        FurnitureVisualData.EditorValidated -= OnVisualEdited;
#endif
    }

    public override void Init()
    {
        base.Init();
        Current = this;

        GameInstance.Save.SetSaveEnabled(false);
        GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Set(TEST_GOLD);

        if (mArea == null)
        {
            Logger.Error("[GameModeFurnitureTest] 배치 영역(TilePlacementArea)이 지정되지 않았습니다.");
            return;
        }

        GameInstance.Model.Placement.SetEditMode(true);
        gameObject.AddComponent<PlacementInputController>().Init();
        mArea.SetGridOverlayVisible(true);

#if UNITY_EDITOR
        FurnitureVisualData.EditorValidated += OnVisualEdited;
#endif
        IsReady = true;
    }

    // 아트 설정을 꺼내 들고 있게 한다. CSV(VisualPath)에 연결된 가구면 실제와 똑같이(칸 수/벽·바닥/레이어) 동작하고,
    // 연결되지 않았으면 그림/회전만 확인할 수 있는 미리보기로 띄운다(칸 점유 없음).
    public void Spawn(FurnitureVisualData visual)
    {
        if (!IsReady || visual == null)
            return;

        var placement = GameInstance.Model.Placement;
        if (placement.IsPlacing.Value)
            placement.Cancel();
        placement.SetDemolishMode(false);

        var prefab = placement.LoadVisualPrefab(visual);
        if (prefab == null)
            return;

        var instance = Instantiate(prefab, mArea.Bounds.center, Quaternion.identity, mArea.transform);
        var placeable = instance.GetComponent<PlaceableObject>();
        if (placeable == null)
        {
            Logger.Warning($"[GameModeFurnitureTest] '{prefab.name}' 프리팹에 PlaceableObject가 없습니다.");
            Destroy(instance);
            return;
        }

        var link = FindLinkedRow(visual);
        if (!ApplyTid(placeable, link.tid, link.isSub, visual))
        {
            Logger.Warning($"[GameModeFurnitureTest] '{visual.name}'은 CSV(VisualPath)에 연결되지 않았습니다. 그림/회전만 확인할 수 있습니다(칸 점유 없음).");
            placeable.SetPreviewVisual(visual);
            mPreviewObjects.Add(instance);
        }

        placeable.BeginPlacementFromSpawn(mArea);

        // PlacementModel.TryBeginPlacement와 같이 빵 진열대(Table.prefab)는 여기서 Init한다.
        instance.GetComponent<Intaraction_BreadStand>()?.Init();
    }

    // 회전 방식에서 나올 수 있는 모든 방향(바닥타일 + 좌우반전 포함이면 반전까지)을 진열 줄에 나란히 띄운다. 배치 등록은 하지 않는다.
    public void ShowAllDirections(FurnitureVisualData visual)
    {
        if (!IsReady || visual == null || mShowcaseRoot == null)
            return;

        ClearShowcase();

        var prefab = GameInstance.Model.Placement.LoadVisualPrefab(visual);
        if (prefab == null)
            return;

        mShowcaseVisual = visual;

        // sub가구는 회전하지 않는다(PlaceableObject.OrientMode와 같은 규칙).
        var link = FindLinkedRow(visual);
        var mode = link.isSub ? CTable.eFurnitureOrientMode.None : visual.OrientMode;
        var states = new List<(CTable.eFurnitureDirection direction, bool flipX)>();
        switch (mode)
        {
            case CTable.eFurnitureOrientMode.None:
                states.Add((CTable.eFurnitureDirection.Down, false));
                break;
            case CTable.eFurnitureOrientMode.FlipXOnly:
                states.Add((CTable.eFurnitureDirection.Down, false));
                states.Add((CTable.eFurnitureDirection.Down, true));
                break;
            default:
                foreach (var direction in SHOWCASE_DIRECTIONS)
                    states.Add((direction, false));
                if (mode == CTable.eFurnitureOrientMode.RotateTile && visual.TileIncludeFlipX)
                {
                    foreach (var direction in SHOWCASE_DIRECTIONS)
                        states.Add((direction, true));
                }
                break;
        }

        float left = mShowcaseRoot.position.x;
        foreach (var (direction, flipX) in states)
        {
            var instance = Instantiate(prefab, mShowcaseRoot);
            var placeable = instance.GetComponent<PlaceableObject>();
            if (placeable == null)
            {
                Destroy(instance);
                break;
            }

            if (!ApplyTid(placeable, link.tid, link.isSub, visual))
                placeable.SetPreviewVisual(visual);
            placeable.SetOrientation(direction, flipX);

            // footprint 중심이 가구 루트이므로 왼쪽 아래 칸이 기준 위치에 오도록 반 칸씩 민다.
            var footprint = placeable.Footprint;
            instance.transform.position = new Vector3(left + footprint.x * 0.5f, mShowcaseRoot.position.y + footprint.y * 0.5f, mShowcaseRoot.position.z);
            CreateLabel(instance.transform, direction, flipX, footprint);

            mShowcaseObjects.Add(instance);
            left += footprint.x + SHOWCASE_GAP;
        }
    }

    // 배치된 가구의 그림을 아트 설정에서 다시 읽어 적용하고 진열 줄을 다시 만든다.
    // 배치된 가구는 방향/칸을 그대로 둔다(칸 점유와 어긋나지 않게) — 회전 방식을 바꿨으면 새로 꺼낸 가구부터 반영된다.
    public void RefreshVisuals()
    {
        if (!IsReady)
            return;

        foreach (var placeable in FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None))
        {
            if (placeable != null)
                placeable.RefreshOrientationSprite();
        }

        if (mShowcaseVisual != null)
            ShowAllDirections(mShowcaseVisual);
    }

    // 테스트로 놓은 가구, 미리보기, 진열 줄을 모두 치운다.
    public void ClearAll()
    {
        if (!IsReady)
            return;

        GameInstance.Model.Placement.ResetAllPlacedFurniture();

        foreach (var preview in mPreviewObjects)
        {
            if (preview != null)
                Destroy(preview);
        }
        mPreviewObjects.Clear();

        ClearShowcase();
    }

    // 아트 설정의 Addressables 주소를 VisualPath로 쓰는 표 행을 찾는다(일반 가구 → sub가구 순서). 없으면 (0, false, null).
    public static (int tid, bool isSub, string name) FindLinkedRow(FurnitureVisualData visual)
    {
        if (visual == null || GameInstance.Table == null)
            return (0, false, null);

        string address = FurnitureVisualData.ADDRESS_PREFIX + visual.name;

        var furnitureTable = GameInstance.Table.GetTable<CTable.FurnitureRow>();
        if (furnitureTable != null)
        {
            foreach (var row in furnitureTable.All.Values)
            {
                if (row.VisualPath == address)
                    return (row.Tid, false, row.Name);
            }
        }

        var subTable = GameInstance.Table.GetTable<CTable.SubFurnitureRow>();
        if (subTable != null)
        {
            foreach (var row in subTable.All.Values)
            {
                if (row.VisualPath == address)
                    return (row.Tid, true, row.Name);
            }
        }

        return (0, false, null);
    }

    // 연결된 표 행의 Tid를 지정한다. 아트 설정은 Addressables 캐시 대신 편집 중인 visual을 그대로 쓴다. tid가 없으면 false.
    private static bool ApplyTid(PlaceableObject placeable, int tid, bool isSub, FurnitureVisualData visual)
    {
        if (tid <= 0)
            return false;

        if (isSub)
            placeable.SetSubFurnitureTid(tid);
        else
            placeable.SetFurnitureTid(tid, visual);
        return true;
    }

    private void ClearShowcase()
    {
        foreach (var obj in mShowcaseObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        mShowcaseObjects.Clear();
        mShowcaseVisual = null;
    }

    // 복사본 위에 방향 이름을 띄운다. TextMesh 기본 폰트는 한글이 없어 영문으로 쓴다.
    private static void CreateLabel(Transform parent, CTable.eFurnitureDirection direction, bool flipX, Vector2Int footprint)
    {
        var label = new GameObject("DirectionLabel");
        label.transform.SetParent(parent, false);
        label.transform.position = parent.position + new Vector3(0f, footprint.y * 0.5f + SHOWCASE_LABEL_OFFSET_Y, 0f);

        var text = label.AddComponent<TextMesh>();
        text.text = flipX ? $"{direction} (Flip)" : direction.ToString();
        text.anchor = TextAnchor.LowerCenter;
        text.characterSize = 0.08f;
        text.fontSize = 48;
        text.color = Color.white;
        label.GetComponent<MeshRenderer>().sortingOrder = SHOWCASE_LABEL_SORTING_ORDER;
    }

#if UNITY_EDITOR
    // OnValidate 안에서는 오브젝트를 만들거나 지우지 않는다 — 다음 에디터 틱으로 미루고 연속 편집은 한 번으로 합친다.
    private void OnVisualEdited(FurnitureVisualData visual)
    {
        if (mIsRefreshQueued)
            return;

        mIsRefreshQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null)
                return;

            mIsRefreshQueued = false;
            RefreshVisuals();
        };
    }
#endif
}
