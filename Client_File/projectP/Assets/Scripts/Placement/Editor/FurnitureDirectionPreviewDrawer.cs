using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

// [FurnitureDirectionPreview] 필드(FurnitureVisualData '옆면') 아래에 방향별 결과 그림을 나란히 그린다.
// 그림 고르는 규칙은 PlaceableObject.ApplyOrientationSprite와 같다:
// - 일반 가구: Down = 정면, Right = 옆면, Up = 뒷면, Left = 옆면 좌우반전
// - 벽가구: 정면 / 정면 좌우반전
// - 회전 없음: 정면만
// 비어 있는 그림은 정면, 정면도 없으면 프리팹의 SpriteRenderer 그림으로 대신한다(대신 쓴 칸은 라벨에 표시).
// 가구 테스트 창은 이 에셋의 Inspector를 InlineEditor로 그대로 보여 주므로 창에도 같이 나온다.
public class FurnitureDirectionPreviewDrawer : OdinAttributeDrawer<FurnitureDirectionPreviewAttribute, Sprite>
{
    private const float BOX_SIZE = 96f;
    private const float LABEL_HEIGHT = 18f;
    private const float SPACING = 6f;
    private static readonly Color BG_COLOR = new Color(0.16f, 0.16f, 0.16f);
    private static readonly Color FALLBACK_LABEL_COLOR = new Color(1f, 0.75f, 0.3f);

    private struct PreviewCell
    {
        public string Label;
        public Sprite Sprite;
        public bool FlipX;
        public string FallbackNote;
    }

    private bool mIsExpanded = true;
    private bool mWantExpanded = true;

    // 프리팹 그림은 프리팹 로드가 필요해서 프리팹이 바뀔 때만 다시 읽는다.
    private string mCachedPrefabGuid;
    private Sprite mCachedPrefabSprite;

    protected override void DrawPropertyLayout(GUIContent label)
    {
        CallNextDrawer(label);

        var visual = Property.Tree.WeakTargets.Count > 0 ? Property.Tree.WeakTargets[0] as FurnitureVisualData : null;
        if (visual == null)
            return;

        // 펼침 상태는 Layout 이벤트에서만 반영한다 — Layout과 Repaint 사이에 컨트롤 개수가 달라지면 ArgumentException이 난다.
        if (Event.current.type == EventType.Layout)
            mIsExpanded = mWantExpanded;
        mWantExpanded = SirenixEditorGUI.Foldout(mWantExpanded, "방향별 미리보기");
        if (!mIsExpanded)
            return;

        var cells = BuildCells(visual);
        float width = cells.Count * BOX_SIZE + (cells.Count - 1) * SPACING;
        var rowRect = GUILayoutUtility.GetRect(width, BOX_SIZE + LABEL_HEIGHT, GUILayout.ExpandWidth(false));
        if (Event.current.type != EventType.Repaint)
            return;

        for (int i = 0; i < cells.Count; i++)
        {
            var boxRect = new Rect(rowRect.x + i * (BOX_SIZE + SPACING), rowRect.y, BOX_SIZE, BOX_SIZE);
            var labelRect = new Rect(boxRect.x, boxRect.yMax, BOX_SIZE, LABEL_HEIGHT);
            DrawCell(boxRect, labelRect, cells[i]);
        }
    }

    private List<PreviewCell> BuildCells(FurnitureVisualData visual)
    {
        var prefabSprite = GetPrefabSprite(visual);
        var cells = new List<PreviewCell>();

        switch (visual.OrientMode)
        {
            case CTable.eFurnitureOrientMode.DirectionalSprite:
                cells.Add(MakeCell("정면 (Down)", visual.Front, false, visual, prefabSprite));
                cells.Add(MakeCell("오른쪽 (Right)", visual.Side, false, visual, prefabSprite));
                cells.Add(MakeCell("뒷면 (Up)", visual.Back, false, visual, prefabSprite));
                cells.Add(MakeCell("왼쪽 (Left)", visual.Side, true, visual, prefabSprite));
                break;
            case CTable.eFurnitureOrientMode.FlipXOnly:
                cells.Add(MakeCell("원본", visual.Front, false, visual, prefabSprite));
                cells.Add(MakeCell("좌우반전", visual.Front, true, visual, prefabSprite));
                break;
            default:
                cells.Add(MakeCell("정면", visual.Front, false, visual, prefabSprite));
                break;
        }
        return cells;
    }

    private static PreviewCell MakeCell(string label, Sprite own, bool flipX, FurnitureVisualData visual, Sprite prefabSprite)
    {
        var cell = new PreviewCell { Label = label, FlipX = flipX };
        if (own != null)
            cell.Sprite = own;
        else if (visual.Front != null)
        {
            cell.Sprite = visual.Front;
            cell.FallbackNote = "정면 대체";
        }
        else if (prefabSprite != null)
        {
            cell.Sprite = prefabSprite;
            cell.FallbackNote = "프리팹 그림";
        }
        return cell;
    }

    private Sprite GetPrefabSprite(FurnitureVisualData visual)
    {
        string guid = visual.Prefab != null ? visual.Prefab.AssetGUID : null;
        if (guid == mCachedPrefabGuid)
            return mCachedPrefabSprite;

        mCachedPrefabGuid = guid;
        mCachedPrefabSprite = null;
        if (!string.IsNullOrEmpty(guid))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            var renderer = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
            mCachedPrefabSprite = renderer != null ? renderer.sprite : null;
        }
        return mCachedPrefabSprite;
    }

    private static void DrawCell(Rect boxRect, Rect labelRect, PreviewCell cell)
    {
        EditorGUI.DrawRect(boxRect, BG_COLOR);

        if (cell.Sprite != null)
            DrawSpriteFit(boxRect, cell.Sprite, cell.FlipX);
        else
            GUI.Label(boxRect, "그림 없음", SirenixGUIStyles.CenteredGreyMiniLabel);

        string text = string.IsNullOrEmpty(cell.FallbackNote) ? cell.Label : $"{cell.Label}\n({cell.FallbackNote})";
        var style = new GUIStyle(SirenixGUIStyles.CenteredGreyMiniLabel) { wordWrap = true };
        if (!string.IsNullOrEmpty(cell.FallbackNote))
            style.normal.textColor = FALLBACK_LABEL_COLOR;
        GUI.Label(labelRect, text, style);
    }

    // 비율을 유지해 칸 안에 맞추고 아래쪽에 붙인다(바닥에 서 있는 모습). 픽셀아트라 point 필터 그대로 그린다.
    private static void DrawSpriteFit(Rect boxRect, Sprite sprite, bool flipX)
    {
        var texture = sprite.texture;
        if (texture == null)
            return;

        var textureRect = sprite.textureRect;
        var uv = new Rect(textureRect.x / texture.width, textureRect.y / texture.height,
            textureRect.width / texture.width, textureRect.height / texture.height);
        if (flipX)
            uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);

        float padding = 4f;
        float maxW = boxRect.width - padding * 2f;
        float maxH = boxRect.height - padding * 2f;
        float scale = Mathf.Min(maxW / textureRect.width, maxH / textureRect.height);
        float w = textureRect.width * scale;
        float h = textureRect.height * scale;
        var drawRect = new Rect(boxRect.center.x - w * 0.5f, boxRect.yMax - padding - h, w, h);

        GUI.DrawTextureWithTexCoords(drawRect, texture, uv);
    }
}
