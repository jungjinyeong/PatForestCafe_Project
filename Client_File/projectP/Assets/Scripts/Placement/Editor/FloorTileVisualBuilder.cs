using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Tilemaps;

// 프로젝트 창에서 고른 바닥 타일(Tile/RuleTile 등 TileBase)로 바닥타일 아트 설정(FurnitureVisualData)과 들고 있기용 프리팹을 만든다.
// 우클릭 Furniture/바닥타일 아트 설정 만들기.
// - 타일 에셋 자체(RuleTile 규칙, 그림)는 건드리지 않는다. 타일은 작업자가 직접 만들고 고친다.
// - 경로/이름 목록을 코드에 두지 않는다. 새 에셋의 위치·프리팹 모양·Addressables 그룹/주소 규칙은
//   이미 있는 바닥 아트 설정 하나(템플릿)를 찾아 그대로 따른다.
// - 고른 타일을 이미 칠하는 아트 설정이 있으면 새로 만들지 않고 연결만 갱신한다(프리팹은 그대로 둔다).
// - 이름: 아트 설정 = FV_{타일 이름}, 프리팹 = {타일 이름}. CSV의 VisualPath = FurnitureVisual/FV_{타일 이름}.
public static class FloorTileVisualBuilder
{
    private const string MENU = "Assets/Furniture/바닥타일 아트 설정 만들기";
    private const string LOG = "[FloorTileVisualBuilder]";
    private const string VISUAL_NAME_PREFIX = "FV_";

    [MenuItem(MENU, true)]
    private static bool ValidateCreate() => Selection.objects.Any(obj => obj is TileBase);

    [MenuItem(MENU, false, 2000)]
    private static void Create()
    {
        var tiles = Selection.objects.OfType<TileBase>().Distinct().ToList();
        if (tiles.Count == 0)
            return;

        var visuals = LoadAllVisuals();
        var template = FindTemplate(visuals);
        if (template == null)
        {
            Debug.LogError($"{LOG} 템플릿으로 쓸 바닥 아트 설정(칠할 타일 + 프리팹이 연결된 것)이 하나도 없습니다. 하나를 직접 만든 뒤 다시 실행하세요.");
            return;
        }

        string templatePrefabPath = AssetDatabase.GUIDToAssetPath(template.Prefab.AssetGUID);
        string visualDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(template)).Replace('\\', '/');
        string prefabDir = Path.GetDirectoryName(templatePrefabPath).Replace('\\', '/');

        var lines = new List<string>();
        foreach (var tile in tiles)
        {
            var visual = visuals.FirstOrDefault(v => v.FloorTile == tile);
            if (visual != null)
            {
                SetVisualFields(visual, tile, GetFrontSprite(tile));
                lines.Add($"갱신 {visual.name} ← {tile.name}");
                continue;
            }

            string prefabPath = $"{prefabDir}/{tile.name}.prefab";
            if (!CreatePrefab(templatePrefabPath, prefabPath, GetFrontSprite(tile)))
                continue;

            visual = CreateVisual($"{visualDir}/{VISUAL_NAME_PREFIX}{tile.name}.asset", AssetDatabase.AssetPathToGUID(prefabPath));
            if (visual == null)
                continue;

            SetVisualFields(visual, tile, GetFrontSprite(tile));
            RegisterPrefabLikeTemplate(template.Prefab.AssetGUID, AssetDatabase.AssetPathToGUID(prefabPath), tile.name);
            visuals.Add(visual);
            lines.Add($"생성 {visual.name} (VisualPath = {FurnitureVisualData.ADDRESS_PREFIX}{visual.name})");
        }

        AssetDatabase.SaveAssets();
        FurnitureVisualDataTool.RegisterAll();
        Debug.Log($"{LOG} 바닥타일 아트 설정 {lines.Count}개 처리\n{string.Join("\n", lines)}");
    }

    private static List<FurnitureVisualData> LoadAllVisuals() =>
        AssetDatabase.FindAssets("t:FurnitureVisualData")
            .Select(guid => AssetDatabase.LoadAssetAtPath<FurnitureVisualData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(v => v != null)
            .ToList();

    // 칠할 타일과 프리팹이 모두 연결된 바닥 아트 설정 중 하나(이름순 첫 번째 — 실행할 때마다 같은 것을 고른다).
    private static FurnitureVisualData FindTemplate(List<FurnitureVisualData> visuals) =>
        visuals.Where(v => v.FloorTile != null && v.Prefab != null && v.Prefab.RuntimeKeyIsValid() &&
                           !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(v.Prefab.AssetGUID)))
            .OrderBy(v => v.name)
            .FirstOrDefault();

    // 목록 아이콘/들고 있기 그림. Tile이면 그 그림, RuleTile이면 기본 그림. 그 밖의 타일은 null(기존 그림 유지).
    private static Sprite GetFrontSprite(TileBase tile) => tile switch
    {
        RuleTile ruleTile => ruleTile.m_DefaultSprite,
        Tile plainTile => plainTile.sprite,
        _ => null,
    };

    // 템플릿 프리팹을 복사해 그림만 바꾼다. 같은 이름의 프리팹이 이미 있으면 그대로 쓴다.
    private static bool CreatePrefab(string templatePath, string prefabPath, Sprite sprite)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            return true;

        if (!AssetDatabase.CopyAsset(templatePath, prefabPath))
        {
            Debug.LogError($"{LOG} 템플릿 프리팹을 복사하지 못했습니다: {templatePath} → {prefabPath}");
            return false;
        }

        if (sprite == null)
            return true;

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var renderer = root.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null)
                renderer.sprite = sprite;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return true;
    }

    private static FurnitureVisualData CreateVisual(string path, string prefabGuid)
    {
        if (AssetDatabase.LoadAssetAtPath<FurnitureVisualData>(path) != null)
        {
            Debug.LogError($"{LOG} 같은 이름의 아트 설정이 이미 있지만 다른 타일을 칠합니다. 이름을 확인하세요: {path}");
            return null;
        }

        var visual = ScriptableObject.CreateInstance<FurnitureVisualData>();
        AssetDatabase.CreateAsset(visual, path);

        var so = new SerializedObject(visual);
        so.FindProperty("mPrefab.m_AssetGUID").stringValue = prefabGuid;
        so.ApplyModifiedPropertiesWithoutUndo();
        return visual;
    }

    // 칠할 타일을 연결하고 회전을 끈다(바닥 그림은 타일이 정한다). front가 null이면 정면 그림은 그대로 둔다.
    private static void SetVisualFields(FurnitureVisualData visual, TileBase floorTile, Sprite front)
    {
        var so = new SerializedObject(visual);
        so.FindProperty("mFloorTile").objectReferenceValue = floorTile;
        so.FindProperty("mOrientMode").enumValueIndex = (int)CTable.eFurnitureOrientMode.None;
        so.FindProperty("mTileIncludeFlipX").boolValue = false;
        if (front != null)
            so.FindProperty("mFront").objectReferenceValue = front;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 새 프리팹을 템플릿 프리팹과 같은 Addressables 그룹, 같은 주소 규칙({템플릿 주소의 폴더 부분}/{프리팹명})으로 등록한다.
    private static void RegisterPrefabLikeTemplate(string templateGuid, string prefabGuid, string prefabName)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var templateEntry = settings != null ? settings.FindAssetEntry(templateGuid) : null;
        if (templateEntry == null)
        {
            Debug.LogError($"{LOG} 템플릿 프리팹이 Addressables에 등록돼 있지 않아 새 프리팹 '{prefabName}'을 등록하지 못했습니다.");
            return;
        }

        int slash = templateEntry.address.LastIndexOf('/');
        string address = (slash >= 0 ? templateEntry.address.Substring(0, slash + 1) : string.Empty) + prefabName;

        var entry = settings.CreateOrMoveEntry(prefabGuid, templateEntry.parentGroup, false, false);
        entry.SetAddress(address, false);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
    }
}
