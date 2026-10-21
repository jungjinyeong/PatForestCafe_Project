using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 가구 테스트 창. 아트 설정(FurnitureVisualData)을 고르고 바로 편집하면서, 가구 테스트 씬(Scenes/FurnitureTest)에서
// 실제 배치 시스템으로 놓아 보고 4방향/칸을 확인한다. 테스트 씬은 세이브를 저장하지 않는다(GameModeFurnitureTest).
public class FurnitureTestWindow : OdinEditorWindow
{
    private const string TEST_SCENE_PATH = "Assets/Scenes/FurnitureTest.unity";
    private const string PLAY_ONLY_GROUP = "테스트 (Play 중에만)";

    [MenuItem("Tools/Furniture/가구 테스트 창")]
    private static void OpenWindow()
    {
        var window = GetWindow<FurnitureTestWindow>();
        window.titleContent = new GUIContent("가구 테스트");
        window.Show();
    }

    #region 테스트 씬

    [Title("테스트 씬")]
    [Button("테스트 씬 열기", ButtonSizes.Medium), HideIf(nameof(IsPlaying))]
    [PropertyTooltip("가구 테스트 씬(Scenes/FurnitureTest)을 연다. 지금 씬에 저장하지 않은 변경이 있으면 먼저 저장할지 묻는다.")]
    private void OpenTestScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(TEST_SCENE_PATH);
    }

    [Button("Play 시작", ButtonSizes.Medium), HideIf(nameof(IsPlaying))]
    [PropertyTooltip("테스트 씬이 아니면 먼저 테스트 씬을 연 뒤 Play를 시작한다.")]
    private void StartPlay()
    {
        if (EditorSceneManager.GetActiveScene().path != TEST_SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(TEST_SCENE_PATH);
        }

        EditorApplication.isPlaying = true;
    }

    [Button("Play 정지", ButtonSizes.Medium), ShowIf(nameof(IsPlaying))]
    [PropertyTooltip("Play를 멈춘다. 테스트로 놓은 가구는 세이브에 남지 않는다.")]
    private void StopPlay()
    {
        EditorApplication.isPlaying = false;
    }

    #endregion

    #region 아트 설정

    [Title("아트 설정")]
    [LabelText("아트 설정"), ValueDropdown(nameof(GetVisualDropdown)), AssetsOnly]
    [PropertyTooltip("테스트할 가구 아트 설정(FurnitureVisualData). 목록은 프로젝트 안의 모든 아트 설정이다.")]
    [OnValueChanged(nameof(RefreshLinkStatus))]
    [SerializeField] private FurnitureVisualData mSelected;

    // 선택이 바뀔 때만 CSV를 다시 읽는다(창은 계속 다시 그려지므로).
    private string mLinkStatus;

    [ShowInInspector, LabelText("CSV 연결"), DisplayAsString(false), ShowIf(nameof(HasSelected))]
    [PropertyTooltip("이 아트 설정의 주소(FurnitureVisual/에셋명)를 VisualPath로 쓰는 Furniture.csv / SubFurniture.csv 행.\n" +
                     "연결되지 않으면 테스트 씬에서 그림/회전만 확인할 수 있다(칸 점유 없음).")]
    private string LinkStatus => mLinkStatus;

    [ShowInInspector, HideLabel, ShowIf(nameof(HasSelected))]
    [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
    private FurnitureVisualData SelectedEditor
    {
        get => mSelected;
        set { }
    }

    [FoldoutGroup("새 아트 설정 만들기")]
    [LabelText("이름"), PropertyTooltip("에셋 이름은 FV_{이름}이 된다. 주소 FurnitureVisual/FV_{이름}으로 자동 등록된다.")]
    [SerializeField] private string mNewName;

    [FoldoutGroup("새 아트 설정 만들기")]
    [LabelText("폴더"), ValueDropdown(nameof(GetVisualFolders))]
    [PropertyTooltip("Tiles = 바닥·벽 타일, Display = 매장 진열 가구, Dining = 테라스 테이블·의자.")]
    [SerializeField] private string mNewFolder = "Display";

    [FoldoutGroup("새 아트 설정 만들기")]
    [Button("만들기"), EnableIf(nameof(CanCreate))]
    [PropertyTooltip("새 아트 설정을 만들고 바로 선택한다. 같은 이름이 있으면 만들지 않는다.")]
    private void CreateVisual()
    {
        string folder = $"{FurnitureVisualDataTool.VISUAL_ROOT}/{mNewFolder}";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(FurnitureVisualDataTool.VISUAL_ROOT, mNewFolder);

        string path = $"{folder}/FV_{mNewName.Trim()}.asset";
        if (AssetDatabase.LoadAssetAtPath<FurnitureVisualData>(path) != null)
        {
            Debug.LogWarning($"[FurnitureTestWindow] 이미 있는 아트 설정입니다: {path}");
            return;
        }

        var visual = CreateInstance<FurnitureVisualData>();
        AssetDatabase.CreateAsset(visual, path);
        AssetDatabase.SaveAssets();

        mSelected = visual;
        mNewName = string.Empty;
        RefreshLinkStatus();
        EditorGUIUtility.PingObject(visual);
    }

    #endregion

    #region 테스트 (Play 중에만)

    [Title(PLAY_ONLY_GROUP)]
    [InfoBox("Play 중에 테스트 씬에서만 쓸 수 있습니다.", InfoMessageType.Info, nameof(IsNotTestPlaying))]
    [Button("배치해 보기", ButtonSizes.Large), EnableIf(nameof(CanTest))]
    [PropertyTooltip("고른 가구를 꺼내 마우스로 들고 있게 한다.\nLMB 놓기 · RMB 취소 · R 회전 · 놓은 가구를 LMB로 다시 집기.")]
    private void Spawn()
    {
        GameModeFurnitureTest.Current.Spawn(mSelected);
        FocusGameView();
    }

    [Button("4방향 보기", ButtonSizes.Large), EnableIf(nameof(CanTest))]
    [PropertyTooltip("회전 방식에서 나올 수 있는 모든 방향을 격자 아래에 나란히 띄운다.\n일반 가구 = Down/Right/Up/Left, 벽가구 = 원본/반전, 바닥타일 = 4방향(+좌우반전 포함이면 반전 4방향).")]
    private void ShowAllDirections()
    {
        GameModeFurnitureTest.Current.ShowAllDirections(mSelected);
    }

    [Button("그림 새로고침"), EnableIf(nameof(IsTestPlaying))]
    [PropertyTooltip("아트 설정의 그림을 다시 읽어 적용한다. Inspector에서 값을 바꾸면 자동으로 새로고침되므로 보통은 누를 필요가 없다.\n이미 놓은 가구는 방향/칸을 그대로 둔다 — 회전 방식을 바꿨으면 새로 꺼내거나 4방향 보기로 확인한다.")]
    private void RefreshVisuals()
    {
        GameModeFurnitureTest.Current.RefreshVisuals();
    }

    [ShowInInspector, LabelText("칸 표시")]
    [PropertyTooltip("모든 가구의 칸을 Scene 뷰(Game 뷰는 Gizmos를 켰을 때)에 그린다.\n흰색 = 차지하는 칸, 파란색 = 손님이 서는 칸, 빨간색 = 비워둘 칸.")]
    private bool ShowCells
    {
        get => PlaceableObject.AlwaysDrawCellGizmos;
        set
        {
            PlaceableObject.AlwaysDrawCellGizmos = value;
            SceneView.RepaintAll();
        }
    }

    [Button("모두 지우기"), EnableIf(nameof(IsTestPlaying))]
    [PropertyTooltip("테스트로 놓은 가구, 미리보기, 4방향 진열을 모두 치운다.")]
    private void ClearAll()
    {
        GameModeFurnitureTest.Current.ClearAll();
    }

    #endregion

    private bool IsPlaying => EditorApplication.isPlaying;
    private bool HasSelected => mSelected != null;
    private bool IsTestPlaying => EditorApplication.isPlaying && GameModeFurnitureTest.Current != null && GameModeFurnitureTest.Current.IsReady;
    private bool IsNotTestPlaying => !IsTestPlaying;
    private bool CanTest => IsTestPlaying && mSelected != null;
    private bool CanCreate => !string.IsNullOrWhiteSpace(mNewName) && !string.IsNullOrEmpty(mNewFolder);

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshLinkStatus();
    }

    private void RefreshLinkStatus()
    {
        var links = FurnitureVisualDataTool.FindCsvLinks(mSelected);
        mLinkStatus = links.Count > 0 ? string.Join("\n", links) : "미연결";
    }

    private void OnInspectorUpdate()
    {
        // Play 시작/정지에 따라 버튼 활성 상태가 바뀌므로 주기적으로 다시 그린다.
        Repaint();
    }

    private static IEnumerable<ValueDropdownItem<FurnitureVisualData>> GetVisualDropdown()
    {
        return AssetDatabase.FindAssets("t:FurnitureVisualData")
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .Select(path => new ValueDropdownItem<FurnitureVisualData>(ToDropdownLabel(path), AssetDatabase.LoadAssetAtPath<FurnitureVisualData>(path)));
    }

    // 하위 폴더별로 묶어 보여 준다(예: "Display/FV_Table").
    private static string ToDropdownLabel(string path)
    {
        string root = FurnitureVisualDataTool.VISUAL_ROOT + "/";
        string label = path.StartsWith(root) ? path.Substring(root.Length) : path;
        return Path.ChangeExtension(label, null);
    }

    private static IEnumerable<string> GetVisualFolders()
    {
        var folders = new List<string> { "Tiles", "Display", "Dining" };
        if (AssetDatabase.IsValidFolder(FurnitureVisualDataTool.VISUAL_ROOT))
        {
            foreach (string sub in AssetDatabase.GetSubFolders(FurnitureVisualDataTool.VISUAL_ROOT))
            {
                string name = Path.GetFileName(sub);
                if (!folders.Contains(name))
                    folders.Add(name);
            }
        }
        return folders;
    }

    // 버튼을 누른 뒤 바로 마우스로 들고 움직일 수 있게 Game 뷰로 포커스를 옮긴다.
    private static void FocusGameView()
    {
        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType != null)
            FocusWindowIfItsOpen(gameViewType);
    }
}
