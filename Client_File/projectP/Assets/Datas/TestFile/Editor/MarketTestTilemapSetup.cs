using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace ForestCafe.Tests
{
    [InitializeOnLoad]
    public static class MarketTestTilemapSetup
    {
        private const string ScenePath = "Assets/Scenes/MarketLayoutTest.unity";
        private const string RootPath = "Assets/Datas/TestFile";
        private const string TilePath = RootPath + "/TileAssets";
        private const string PalettePath = RootPath + "/TilePalette";

        static MarketTestTilemapSetup()
        {
            EditorApplication.delayCall += Setup;
        }

        [MenuItem("Forest Cafe/Tests/Setup Market Test Tilemaps")]
        public static void Setup()
        {
            Directory.CreateDirectory(TilePath);
            Directory.CreateDirectory(PalettePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            TileBase[] tiles =
            {
                CreateTile("Grass", "tile_grass_32_v01.png"),
                CreateTile("SandPath", "tile_sand_path_32_v01.png"),
                CreateTile("WallPlain", "tile_wall_plain_32x96_v01.png"),
                CreateTile("WallTimber", "tile_wall_timber_32x96_v01.png"),
                CreateTile("WallVine", "tile_wall_vine_32x96_v01.png")
            };

            CreatePalette(tiles);
            SetupSceneTilemaps();
            AssetDatabase.SaveAssets();
            Debug.Log("MarketLayoutTest Tilemap setup complete.");
        }

        private static Tile CreateTile(string assetName, string spriteFileName)
        {
            string assetPath = TilePath + "/" + assetName + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, assetPath);
            }

            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RootPath + "/Tiles/" + spriteFileName);
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void CreatePalette(TileBase[] tiles)
        {
            string prefabPath = PalettePath + "/MarketTestPalette.prefab";
            GameObject paletteRoot = new GameObject("MarketTestPalette", typeof(Grid));
            Grid grid = paletteRoot.GetComponent<Grid>();
            grid.cellSize = Vector3.one;

            GameObject layerObject = new GameObject("Layer1", typeof(Tilemap), typeof(TilemapRenderer));
            layerObject.transform.SetParent(paletteRoot.transform);
            Tilemap tilemap = layerObject.GetComponent<Tilemap>();

            for (int i = 0; i < tiles.Length; i++)
                tilemap.SetTile(new Vector3Int(i, 0, 0), tiles[i]);

            PrefabUtility.SaveAsPrefabAsset(paletteRoot, prefabPath);
            Object.DestroyImmediate(paletteRoot);
        }

        private static void SetupSceneTilemaps()
        {
            Scene targetScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !targetScene.IsValid() || !targetScene.isLoaded;
            if (openedTemporarily)
                targetScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            GameObject gridObject = FindRoot(targetScene, "EnvironmentGrid");
            if (gridObject == null)
            {
                gridObject = new GameObject("EnvironmentGrid", typeof(Grid));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gridObject, targetScene);
            }

            Grid grid = gridObject.GetComponent<Grid>();
            if (grid == null)
                grid = gridObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            EnsureTilemap(gridObject.transform, "FloorTilemap", 0);
            EnsureTilemap(gridObject.transform, "WallTilemap", 10);

            GameObject placeableRoot = FindRoot(targetScene, "PlaceableObjects");
            if (placeableRoot == null)
            {
                placeableRoot = new GameObject("PlaceableObjects");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(placeableRoot, targetScene);
            }

            EnsureContainer(placeableRoot.transform, "FloorFurniture");
            EnsureContainer(placeableRoot.transform, "WallObjects");

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
            if (openedTemporarily)
                EditorSceneManager.CloseScene(targetScene, true);
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == objectName)
                    return root;
            return null;
        }

        private static void EnsureTilemap(Transform parent, string objectName, int sortingOrder)
        {
            Transform existing = parent.Find(objectName);
            GameObject tilemapObject = existing != null
                ? existing.gameObject
                : new GameObject(objectName, typeof(Tilemap), typeof(TilemapRenderer));

            tilemapObject.transform.SetParent(parent, false);
            TilemapRenderer renderer = tilemapObject.GetComponent<TilemapRenderer>();
            if (renderer == null)
                renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
        }

        private static void EnsureContainer(Transform parent, string objectName)
        {
            Transform existing = parent.Find(objectName);
            GameObject container = existing != null ? existing.gameObject : new GameObject(objectName);
            container.transform.SetParent(parent, false);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;
        }
    }
}
