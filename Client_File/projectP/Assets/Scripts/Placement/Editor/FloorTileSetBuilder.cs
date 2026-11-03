using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Neighbor = UnityEngine.RuleTile.TilingRuleOutput.Neighbor;

// 바닥타일 세트(RuleTile) + 세트용 아트 설정(FurnitureVisualData) 생성 툴. Tools/Furniture/Build Floor Tile Sets.
// 다시 실행하면 같은 경로의 에셋을 덮어써 갱신한다(Furniture.csv의 VisualPath = FurnitureVisual/{세트 아트 설정 이름}).
// - 경계 세트(숲 바닥(봄), BorderedRuleTile): 시트 16장을 모두 쓴다. 안쪽 칸 = 풀밭 변형(9~15 무작위),
//   테두리 칸 = 흙길 조각(0~7, 위쪽(N)이 바깥인 기준 + 회전(Rotated)으로 4방향). 가운데 흙(8)은 대체 그림.
// - 변형 세트(정원석): 규칙 1개 + 무작위 출력(Perlin, 칸 위치마다 고정이라 다시 불러와도 같은 그림).
// - 단색 타일(숲길 돌바닥): 세트가 아니므로 기존 Tile 에셋을 아트 설정에 연결만 한다.
public static class FloorTileSetBuilder
{
    private const string ART_ROOT = "Assets/Datas/Graphic/Furniture/FurnitureArt/Tiles/Floor";
    private const string VISUAL_DIR = FurnitureVisualDataTool.VISUAL_ROOT + "/Tiles/Floor";
    private const string PREFAB_DIR = "Assets/Datas/Graphic/Furniture/FurniturePrefabs/Tiles/Floor";

    // 이웃 순서는 RuleTile 기본 m_NeighborPositions와 같다: NW, N, NE, W, E, SW, S, SE.
    // 문자: '1' = 같은 세트, '2' = 다른 타일/빈 칸, '0' = 상관없음.
    private struct EdgeRule
    {
        public string Neighbors;
        public int SpriteIndex;
    }

    // 숲 바닥(봄) 테두리 — flr_0120 시트 0~7번. 위에서부터 먼저 맞는 규칙을 쓴다(RuleTile 규칙 순서 = 우선순위).
    // '1' = 테두리 칸, '2' = 안쪽 풀밭 칸/다른 타일/빈 칸(BorderedRuleTile). 테두리 칸은 늘 '2' 이웃이 있어 가운데 흙(8)은 나오지 않는다.
    private static readonly EdgeRule[] DIRT_PATH_RULES =
    {
        new EdgeRule { Neighbors = "21211212", SpriteIndex = 3 }, // 안쪽 모서리 4개(십자)
        new EdgeRule { Neighbors = "02011212", SpriteIndex = 2 }, // 위 가장자리 + 아래 안쪽 모서리 2개(T자)
        new EdgeRule { Neighbors = "02021012", SpriteIndex = 0 }, // 바깥 모서리 + 반대쪽 안쪽 모서리
        new EdgeRule { Neighbors = "11111212", SpriteIndex = 6 }, // 아래 안쪽 모서리 2개
        new EdgeRule { Neighbors = "11111112", SpriteIndex = 7 }, // 안쪽 모서리 1개
        new EdgeRule { Neighbors = "02011020", SpriteIndex = 1 }, // 폭 1칸 직선
        new EdgeRule { Neighbors = "02021011", SpriteIndex = 4 }, // 바깥 모서리
        new EdgeRule { Neighbors = "02011111", SpriteIndex = 5 }, // 위 가장자리
        // 그림이 없는 조합의 대체 — 빠지는 모서리가 생기더라도 엉뚱한 풀 조각이 끼지 않는 쪽을 고른다.
        new EdgeRule { Neighbors = "02011010", SpriteIndex = 5 }, // 가장자리 + 안쪽 모서리 1개 → 가장자리
        new EdgeRule { Neighbors = "01011012", SpriteIndex = 7 }, // 대각선 안쪽 모서리 2개/3개 → 안쪽 모서리 1개
        new EdgeRule { Neighbors = "02021020", SpriteIndex = 1 }, // 막다른 끝 → 직선
        new EdgeRule { Neighbors = "02022020", SpriteIndex = 1 }, // 외톨이 칸 → 직선
    };

    private struct RandomSet
    {
        public string SheetPath;      // ART_ROOT 기준 스프라이트 시트
        public int[] SpriteIndices;
        public string RuleTilePath;   // ART_ROOT 기준 생성 경로
        public string VisualName;     // 세트 아트 설정 이름
        public string Prefab;         // 들고 있는 동안 커서를 따라올 바닥 프리팹(PREFAB_DIR 기준 이름)
    }

    private static readonly RandomSet[] RANDOM_SETS =
    {
        new RandomSet { SheetPath = "flr_0110_ForestGardenStone/Sprites/flr_0110_ForestGardenStone_Natural.png", SpriteIndices = new[] { 0, 1, 2 },
            RuleTilePath = "flr_0110_ForestGardenStone/RuleTiles/flr_0110_ForestGardenStone_Natural_set.asset",
            VisualName = "FV_flr_0110_ForestGardenStone_Natural_set", Prefab = "flr_0110_ForestGardenStone_Natural_0" },
        new RandomSet { SheetPath = "flr_0110_ForestGardenStone/Sprites/flr_0111_ForestGardenStone_Blossom.png", SpriteIndices = new[] { 0, 1, 2 },
            RuleTilePath = "flr_0110_ForestGardenStone/RuleTiles/flr_0111_ForestGardenStone_Blossom_set.asset",
            VisualName = "FV_flr_0111_ForestGardenStone_Blossom_set", Prefab = "flr_0111_ForestGardenStone_Blossom_0" },
        new RandomSet { SheetPath = "flr_0110_ForestGardenStone/Sprites/flr_0112_ForestGardenStone_Autumn.png", SpriteIndices = new[] { 0, 1, 2 },
            RuleTilePath = "flr_0110_ForestGardenStone/RuleTiles/flr_0112_ForestGardenStone_Autumn_set.asset",
            VisualName = "FV_flr_0112_ForestGardenStone_Autumn_set", Prefab = "flr_0112_ForestGardenStone_Autumn_0" },
        new RandomSet { SheetPath = "flr_0110_ForestGardenStone/Sprites/flr_0113_ForestGardenStone_Moss.png", SpriteIndices = new[] { 0, 1, 2 },
            RuleTilePath = "flr_0110_ForestGardenStone/RuleTiles/flr_0113_ForestGardenStone_Moss_set.asset",
            VisualName = "FV_flr_0113_ForestGardenStone_Moss_set", Prefab = "flr_0113_ForestGardenStone_Moss_0" },
        new RandomSet { SheetPath = "flr_0110_ForestGardenStone/Sprites/flr_0114_ForestGardenStone_Moonlight.png", SpriteIndices = new[] { 0, 1, 2 },
            RuleTilePath = "flr_0110_ForestGardenStone/RuleTiles/flr_0114_ForestGardenStone_Moonlight_set.asset",
            VisualName = "FV_flr_0114_ForestGardenStone_Moonlight_set", Prefab = "flr_0114_ForestGardenStone_Moonlight_0" },
    };

    private const string DIRT_SHEET = "flr_0120_Forest_Ground_Tile/Sprites/flr_0120_forest_ground_tile_spring.png";
    private const string DIRT_RULE_TILE = "flr_0120_Forest_Ground_Tile/RuleTiles/flr_0120_forest_ground_spring_path.asset";
    private const string DIRT_VISUAL = "FV_flr_0120_forest_ground_spring_path";
    private const string DIRT_PREFAB = "flr_0120_forest_ground_tile_spring_8";
    private static readonly int[] DIRT_INTERIOR_SPRITES = { 9, 10, 11, 12, 13, 14, 15 };
    // 숲 풀밭(봄)(30026)도 같은 통합 세트를 칠한다(같은 타일 에셋이라 흙길(30025)과 붙여 칠하면 한 덩어리로 이어진다).
    private const string GRASS_VISUAL = "FV_flr_0120_forest_ground_spring_grass";
    private const string GRASS_PREFAB = "flr_0120_forest_ground_tile_spring_9";
    // 예전 풀밭 전용 무작위 세트(통합 후 쓰지 않음) — 남아 있으면 지운다.
    private const string OLD_GRASS_RULE_TILE = "flr_0120_Forest_Ground_Tile/RuleTiles/flr_0120_forest_ground_spring_grass.asset";

    // 단색 타일: 기존 아트 설정 이름 접두사 → 같은 번호의 기존 Tile 에셋.
    private const string SOLID_VISUAL_PREFIX = "FV_flr_0100_forest_path_stone_tile_";
    private const string SOLID_TILE_PREFIX = "flr_0100_Forest_Path_Stone/TileAssets/flr_0100_forest_path_stone_tile_";
    private const int SOLID_COUNT = 6;

    [MenuItem("Tools/Furniture/Build Floor Tile Sets")]
    public static void Build()
    {
        int count = 0;

        var dirtSprites = LoadSheetSprites(DIRT_SHEET);
        var dirtTile = CreateOrLoadRuleTile<BorderedRuleTile>(DIRT_RULE_TILE);
        if (dirtSprites != null && dirtTile != null)
        {
            FillEdgeRules(dirtTile, dirtSprites, DIRT_PATH_RULES);
            AddInteriorRule(dirtTile, DIRT_INTERIOR_SPRITES.Select(i => dirtSprites[i]).ToArray());
            if (WriteVisual(DIRT_VISUAL, DIRT_PREFAB, dirtTile, dirtSprites[8]))
                count++;
            if (WriteVisual(GRASS_VISUAL, GRASS_PREFAB, dirtTile, dirtSprites[9]))
                count++;
            AssetDatabase.DeleteAsset($"{ART_ROOT}/{OLD_GRASS_RULE_TILE}");
        }

        foreach (var set in RANDOM_SETS)
        {
            var sprites = LoadSheetSprites(set.SheetPath);
            var tile = CreateOrLoadRuleTile<RuleTile>(set.RuleTilePath);
            if (sprites == null || tile == null)
                continue;

            FillRandomRule(tile, set.SpriteIndices.Select(i => sprites[i]).ToArray());
            if (WriteVisual(set.VisualName, set.Prefab, tile, sprites[set.SpriteIndices[0]]))
                count++;
        }

        for (int i = 0; i < SOLID_COUNT; i++)
        {
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>($"{ART_ROOT}/{SOLID_TILE_PREFIX}{i}.asset");
            var visual = LoadVisual(SOLID_VISUAL_PREFIX + i);
            if (tile == null || visual == null)
            {
                Debug.LogError($"[FloorTileSetBuilder] 단색 타일 {i}번의 Tile 에셋 또는 아트 설정을 찾지 못했습니다.");
                continue;
            }

            SetVisualFields(visual, tile, null);
            count++;
        }

        AssetDatabase.SaveAssets();
        FurnitureVisualDataTool.RegisterAll();
        Debug.Log($"[FloorTileSetBuilder] 바닥타일 아트 설정 {count}개를 만들거나 갱신했습니다.");
    }

    private static void FillEdgeRules(RuleTile tile, Dictionary<int, Sprite> sprites, EdgeRule[] rules)
    {
        tile.m_DefaultSprite = sprites[8];
        tile.m_DefaultColliderType = Tile.ColliderType.None;
        tile.m_TilingRules = new List<RuleTile.TilingRule>();

        foreach (var rule in rules)
        {
            tile.m_TilingRules.Add(new RuleTile.TilingRule
            {
                m_Id = tile.m_TilingRules.Count,
                m_Neighbors = rule.Neighbors.Select(ToNeighbor).ToList(),
                m_Sprites = new[] { sprites[rule.SpriteIndex] },
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
                m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Rotated,
                m_ColliderType = Tile.ColliderType.None,
            });
        }
        EditorUtility.SetDirty(tile);
    }

    // 안쪽 칸(8방향이 모두 같은 세트) 규칙을 맨 앞에 넣는다 — 무작위 출력(Perlin, 칸 위치마다 고정).
    private static void AddInteriorRule(RuleTile tile, Sprite[] sprites)
    {
        tile.m_TilingRules.Insert(0, new RuleTile.TilingRule
        {
            m_Neighbors = Enumerable.Repeat(Neighbor.This, 8).ToList(),
            m_Sprites = sprites,
            m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random,
            m_ColliderType = Tile.ColliderType.None,
        });
        for (int i = 0; i < tile.m_TilingRules.Count; i++)
            tile.m_TilingRules[i].m_Id = i;
        EditorUtility.SetDirty(tile);
    }

    private static void FillRandomRule(RuleTile tile, Sprite[] sprites)
    {
        tile.m_DefaultSprite = sprites[0];
        tile.m_DefaultColliderType = Tile.ColliderType.None;
        tile.m_TilingRules = new List<RuleTile.TilingRule>
        {
            new RuleTile.TilingRule
            {
                m_Neighbors = Enumerable.Repeat(0, 8).ToList(),
                m_Sprites = sprites,
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random,
                m_ColliderType = Tile.ColliderType.None,
            },
        };
        EditorUtility.SetDirty(tile);
    }

    private static int ToNeighbor(char c) => c switch
    {
        '1' => Neighbor.This,
        '2' => Neighbor.NotThis,
        _ => 0,
    };

    // 시트의 슬라이스 스프라이트(이름 끝 _번호)를 번호로 찾는다.
    private static Dictionary<int, Sprite> LoadSheetSprites(string sheetPath)
    {
        string path = $"{ART_ROOT}/{sheetPath}";
        var result = new Dictionary<int, Sprite>();
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
        {
            int underscore = sprite.name.LastIndexOf('_');
            if (underscore >= 0 && int.TryParse(sprite.name.Substring(underscore + 1), out int index))
                result[index] = sprite;
        }

        if (result.Count == 0)
        {
            Debug.LogError($"[FloorTileSetBuilder] 시트 스프라이트를 찾지 못했습니다: {path}");
            return null;
        }
        return result;
    }

    // 같은 경로에 다른 종류(예: RuleTile → BorderedRuleTile)가 있으면 지우고 새로 만든다(아트 설정 연결은 WriteVisual이 다시 한다).
    private static T CreateOrLoadRuleTile<T>(string relativePath) where T : RuleTile
    {
        string path = $"{ART_ROOT}/{relativePath}";
        var existing = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
        if (existing is T typed && existing.GetType() == typeof(T))
            return typed;
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var tile = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    // 세트 아트 설정을 만들거나 갱신한다. 프리팹은 들고 있는 동안 커서를 따라오는 미리보기로만 쓴다.
    private static bool WriteVisual(string visualName, string prefabName, TileBase floorTile, Sprite front)
    {
        string prefabGuid = AssetDatabase.AssetPathToGUID($"{PREFAB_DIR}/{prefabName}.prefab");
        if (string.IsNullOrEmpty(prefabGuid))
        {
            Debug.LogError($"[FloorTileSetBuilder] 바닥 프리팹을 찾지 못했습니다: {PREFAB_DIR}/{prefabName}.prefab");
            return false;
        }

        string path = $"{VISUAL_DIR}/{visualName}.asset";
        var visual = AssetDatabase.LoadAssetAtPath<FurnitureVisualData>(path);
        if (visual == null)
        {
            visual = ScriptableObject.CreateInstance<FurnitureVisualData>();
            AssetDatabase.CreateAsset(visual, path);
        }

        var so = new SerializedObject(visual);
        so.FindProperty("mPrefab.m_AssetGUID").stringValue = prefabGuid;
        so.ApplyModifiedPropertiesWithoutUndo();
        SetVisualFields(visual, floorTile, front);
        return true;
    }

    // 칠할 타일을 연결하고 회전을 끈다(바닥 그림은 RuleTile이 정한다). front가 null이면 정면 그림은 그대로 둔다.
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

    private static FurnitureVisualData LoadVisual(string visualName) =>
        AssetDatabase.LoadAssetAtPath<FurnitureVisualData>($"{VISUAL_DIR}/{visualName}.asset");
}
