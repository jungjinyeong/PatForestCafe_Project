using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// [FloorTilePreview] 필드(FurnitureVisualData '칠할 타일') 아래에 샘플 모양으로 칠한 결과를 그린다.
// 숨긴 임시 Tilemap에 실제로 칠해서 RuleTile이 고른 그림/회전을 그대로 읽어 오므로 게임에서 칠한 모습과 같다
// (무작위 변형은 칸 위치마다 정해지므로 같은 세트라도 게임 속 위치에 따라 섞이는 모양은 다르다).
// Odin은 실제 값 타입(RuleTile, Tile 등)으로 드로어를 고르므로 TileBase 하위 타입 전부에 걸리게 제네릭으로 둔다.
public class FloorTilePreviewDrawer<T> : OdinAttributeDrawer<FloorTilePreviewAttribute, T> where T : TileBase
{
    // '#' = 칠한 칸. 위 줄이 화면 위. 넓은 면(안쪽 칸)·안쪽 구멍·1칸 폭 길·고리·T자·십자를 한 번에 본다.
    private static readonly string[] SAMPLE =
    {
        "..............",
        ".########.....",
        ".########.....",
        ".########.....",
        ".########.###.",
        ".#####..###.#.",
        ".#####..#.###.",
        ".########..#..",
        ".########.###.",
        "...........#..",
    };

    private const float CELL_SIZE = 24f;
    private static readonly Color EMPTY_COLOR = new Color(0.16f, 0.16f, 0.16f);

    private TileBase mBuiltTile;
    private Sprite[,] mSprites;
    private Matrix4x4[,] mMatrices;
    private bool mIsExpanded = true;

    protected override void DrawPropertyLayout(GUIContent label)
    {
        CallNextDrawer(label);

        var tile = ValueEntry.SmartValue;
        if (tile == null)
            return;

        mIsExpanded = SirenixEditorGUI.Foldout(mIsExpanded, "칠한 모양 미리보기");
        if (!mIsExpanded)
            return;

        // 타일 에셋(RuleTile 규칙 등)을 고친 뒤에는 [다시 그리기]로 갱신한다.
        if (mBuiltTile != tile || mSprites == null || GUILayout.Button("다시 그리기", GUILayout.Width(100f)))
            Build(tile);

        int rows = SAMPLE.Length;
        int cols = SAMPLE[0].Length;
        var gridRect = GUILayoutUtility.GetRect(cols * CELL_SIZE, rows * CELL_SIZE, GUILayout.ExpandWidth(false));
        if (Event.current.type != EventType.Repaint)
            return;

        EditorGUI.DrawRect(gridRect, EMPTY_COLOR);
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                var sprite = mSprites[x, y];
                if (sprite == null)
                    continue;

                // 셀 좌표는 아래가 0, 화면은 위가 0이다.
                var cellRect = new Rect(gridRect.x + x * CELL_SIZE, gridRect.y + (rows - 1 - y) * CELL_SIZE, CELL_SIZE, CELL_SIZE);
                DrawSprite(cellRect, sprite, mMatrices[x, y]);
            }
        }
    }

    // 숨긴 임시 Tilemap에 샘플 모양을 칠하고 칸마다 결정된 그림/변환을 읽어 둔다.
    private void Build(TileBase tile)
    {
        int rows = SAMPLE.Length;
        int cols = SAMPLE[0].Length;
        mSprites = new Sprite[cols, rows];
        mMatrices = new Matrix4x4[cols, rows];
        mBuiltTile = tile;

        var gridObject = new GameObject("FloorTilePreview", typeof(Grid)) { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var tilemapObject = new GameObject("Tilemap", typeof(Tilemap)) { hideFlags = HideFlags.HideAndDontSave };
            tilemapObject.transform.SetParent(gridObject.transform, false);
            var tilemap = tilemapObject.GetComponent<Tilemap>();

            for (int row = 0; row < rows; row++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (SAMPLE[row][x] == '#')
                        tilemap.SetTile(new Vector3Int(x, rows - 1 - row, 0), tile);
                }
            }
            tilemap.RefreshAllTiles();

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    mSprites[x, y] = tilemap.GetSprite(cell);
                    mMatrices[x, y] = tilemap.GetTransformMatrix(cell);
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
        }
    }

    // 타일 변환(회전/반전)을 칸 중심 기준으로 적용해 그린다. 화면 y가 아래로 커지므로 y 반전으로 감싼다.
    private static void DrawSprite(Rect rect, Sprite sprite, Matrix4x4 tileMatrix)
    {
        var texture = sprite.texture;
        if (texture == null)
            return;

        var textureRect = sprite.textureRect;
        var uv = new Rect(textureRect.x / texture.width, textureRect.y / texture.height,
            textureRect.width / texture.width, textureRect.height / texture.height);

        var linear = Matrix4x4.identity;
        linear.m00 = tileMatrix.m00;
        linear.m01 = tileMatrix.m01;
        linear.m10 = tileMatrix.m10;
        linear.m11 = tileMatrix.m11;

        var flipY = Matrix4x4.Scale(new Vector3(1f, -1f, 1f));
        var center = (Vector3)rect.center;
        var previous = GUI.matrix;
        GUI.matrix = previous * Matrix4x4.Translate(center) * flipY * linear * flipY * Matrix4x4.Translate(-center);
        GUI.DrawTextureWithTexCoords(rect, texture, uv);
        GUI.matrix = previous;
    }
}
