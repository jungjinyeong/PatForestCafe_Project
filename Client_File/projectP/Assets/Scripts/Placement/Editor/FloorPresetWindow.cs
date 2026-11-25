using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 층 기본 배치 제작 창. Play 모드에서 실제 배치 시스템으로 가구/바닥타일/벽지를 놓은 뒤, 지금 보고 있는 층의 배치를
// FloorPresetData 에셋으로 저장한다. 저장된 배치는 첫 실행(처음부터 열린 층)과 층 해금 때 그 층에 깔린다.
// - 창이 열려 있는 동안 Play 중이면 실제 세이브(save.json)에 쓰지 않도록 저장을 끈다.
// - 고정(Fixed) 여부는 저장 후 에셋 인스펙터에서 항목마다 켠다. 다시 저장해도 같은 칸·같은 가구의 체크는 유지된다.
public class FloorPresetWindow : EditorWindow
{
    private FloorPresetData mPreset;
    private bool mSaveDisabledThisPlay;
    private Vector2 mScroll;

    [MenuItem("Tools/Furniture/층 기본 배치")]
    private static void OpenWindow()
    {
        var window = GetWindow<FloorPresetWindow>("층 기본 배치");
        window.minSize = new Vector2(360, 260);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    // Play 중 층 이동/배치 변화를 계속 보여주고, 세이브 매니저가 준비되면 바로 저장을 끈다.
    private void OnInspectorUpdate()
    {
        if (!EditorApplication.isPlaying)
            return;

        DisableSaveIfReady();
        Repaint();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredEditMode)
            mSaveDisabledThisPlay = false;
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Play 모드에서 가구를 실제로 배치한 뒤, 지금 보고 있는 층의 배치를 에셋으로 저장합니다.\n" +
            "에셋 이름은 FP_{층Tid}여야 런타임이 찾습니다. 고정 여부는 에셋 인스펙터에서 항목마다 켭니다.", MessageType.Info);

        mPreset = (FloorPresetData)EditorGUILayout.ObjectField("기본 배치 에셋", mPreset, typeof(FloorPresetData), false);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Play 모드에서만 저장/불러오기를 할 수 있습니다.", MessageType.Warning);
            DrawPresetSummary();
            return;
        }

        var model = GameInstance.Model;
        if (model == null || model.Placement == null || model.Floor == null || GameInstance.Table == null)
        {
            EditorGUILayout.HelpBox("GameInstance 초기화를 기다리는 중입니다.", MessageType.None);
            return;
        }

        int floor = model.Floor.ViewingFloor.Value;
        EditorGUILayout.LabelField("지금 보고 있는 층", $"{model.Floor.GetName(floor)} (Tid {floor})");
        EditorGUILayout.LabelField("실제 세이브 저장", mSaveDisabledThisPlay ? "꺼짐(이번 Play)" : "켜짐 — 곧 끕니다");

        if (mPreset != null && mPreset.Floor != floor)
            EditorGUILayout.HelpBox($"에셋의 Floor({mPreset.Floor})와 보고 있는 층({floor})이 다릅니다. 저장하면 에셋의 Floor가 {floor}로 바뀝니다.", MessageType.Warning);

        EditorGUILayout.Space();

        if (GUILayout.Button(mPreset == null ? $"새 에셋 FP_{floor} 만들고 현재 층 저장" : "현재 층 → 기본 배치 저장", GUILayout.Height(28)))
            SaveCurrentFloor(floor);

        using (new EditorGUI.DisabledScope(mPreset == null))
        {
            if (GUILayout.Button("기본 배치 → 현재 층 불러오기 (층 비우고 깔기)"))
                LoadIntoCurrentFloor(floor);
        }

        if (GUILayout.Button("현재 층 비우기"))
            model.Placement.ClearFloorPlacements(floor, includeFixed: true);

        DrawPresetSummary();
    }

    private void DrawPresetSummary()
    {
        if (mPreset == null)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"에셋 내용: 가구 {mPreset.Furniture.Count}개 (고정 {mPreset.Furniture.Count(e => e.Fixed)}), 바닥 칸 {mPreset.FloorTiles.Count}개", EditorStyles.boldLabel);

        mScroll = EditorGUILayout.BeginScrollView(mScroll);
        foreach (var entry in mPreset.Furniture)
            EditorGUILayout.LabelField($"{(entry.IsSub ? "Sub" : "Base")} {entry.Tid}  칸 {entry.Cell}  {entry.Direction}{(entry.FlipX ? " 반전" : "")}{(entry.Fixed ? "  [고정]" : "")}");
        EditorGUILayout.EndScrollView();
    }

    private void DisableSaveIfReady()
    {
        if (mSaveDisabledThisPlay || GameInstance.Save == null)
            return;

        GameInstance.Save.SetSaveEnabled(false);
        mSaveDisabledThisPlay = true;
        Debug.Log("[FloorPresetWindow] 층 기본 배치 작업 중이라 이번 Play의 세이브 저장을 껐습니다.");
    }

    private void SaveCurrentFloor(int floor)
    {
        if (mPreset == null)
        {
            string path = EditorUtility.SaveFilePanelInProject("층 기본 배치 저장", FloorPresetData.ASSET_PREFIX + floor, "asset",
                "층 기본 배치 에셋을 저장할 위치를 고르세요.");
            if (string.IsNullOrEmpty(path))
                return;

            mPreset = CreateInstance<FloorPresetData>();
            AssetDatabase.CreateAsset(mPreset, path);
        }

        var placement = GameInstance.Model.Placement;

        // 다시 저장할 때 인스펙터에서 켜 둔 고정 체크를 잃지 않도록 같은 가구·같은 칸이면 이어받는다.
        var fixedKeys = new HashSet<(int tid, bool isSub, Vector2Int cell)>(
            mPreset.Furniture.Where(e => e.Fixed).Select(e => (e.Tid, e.IsSub, e.Cell)));

        Undo.RecordObject(mPreset, "층 기본 배치 저장");
        mPreset.Floor = floor;
        mPreset.Furniture = placement.GetAllPlacements()
            .Where(pair => pair.Value.Floor == floor)
            .OrderBy(pair => pair.Key)
            .Select(pair => new FloorPresetData.FurnitureEntry
            {
                Tid = pair.Value.Tid,
                IsSub = pair.Value.IsSub,
                Cell = pair.Value.Cell,
                Direction = pair.Value.Direction,
                FlipX = pair.Value.FlipX,
                AssignedDessertTid = pair.Value.AssignedDessertTid,
                Fixed = pair.Value.Fixed || fixedKeys.Contains((pair.Value.Tid, pair.Value.IsSub, pair.Value.Cell)),
            })
            .ToList();
        mPreset.FloorTiles = placement.GetAllFloorTiles()
            .Where(t => t.floor == floor)
            .Select(t => new FloorPresetData.FloorTileEntry { Cell = t.cell, Tid = t.tid })
            .ToList();

        EditorUtility.SetDirty(mPreset);
        AssetDatabase.SaveAssets();

        string expectedName = FloorPresetData.ASSET_PREFIX + floor;
        if (mPreset.name != expectedName)
            Debug.LogWarning($"[FloorPresetWindow] 에셋 이름이 '{mPreset.name}'입니다. 런타임은 '{expectedName}'을 찾으니 이름을 바꾸세요.", mPreset);

        Debug.Log($"[FloorPresetWindow] {floor}층 기본 배치 저장: 가구 {mPreset.Furniture.Count}개, 바닥 칸 {mPreset.FloorTiles.Count}개.", mPreset);
    }

    private void LoadIntoCurrentFloor(int floor)
    {
        var placement = GameInstance.Model.Placement;
        placement.ClearFloorPlacements(floor, includeFixed: true);
        int applied = placement.ApplyFloorPreset(mPreset, floor, grant: true);
        Debug.Log($"[FloorPresetWindow] '{mPreset.name}'을 {floor}층에 불러왔습니다({applied}개).");
    }
}
