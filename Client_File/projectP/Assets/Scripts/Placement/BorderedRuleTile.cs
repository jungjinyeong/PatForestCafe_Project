using UnityEngine;
using UnityEngine.Tilemaps;

// 테두리 + 안쪽이 다른 그림인 바닥 세트(숲 바닥(봄): 테두리 = 흙길 조각, 안쪽 = 풀밭 변형).
// - 안쪽 칸 = 8방향 이웃이 모두 같은 세트인 칸. 이웃 조건이 전부 This인 규칙(첫 규칙, 무작위 출력)이 맡는다.
// - 테두리 칸 = 나머지. 테두리 규칙의 이웃은 "테두리 칸이면 This, 안쪽 칸/다른 타일/빈 칸이면 NotThis"로 본다.
//   그래서 흙길 조각이 안쪽 풀밭 쪽에도 풀 띠를 그려 풀밭과 이어진다.
// 이웃의 안쪽 여부를 알려면 2칸 밖까지 봐야 하므로 칸이 바뀌면 5×5 범위를 다시 그린다.
// 에셋은 Tools/Furniture/Build Floor Tile Sets(FloorTileSetBuilder)가 만든다.
[CreateAssetMenu(fileName = "BorderedRuleTile", menuName = "2D/Tiles/Bordered Rule Tile")]
public class BorderedRuleTile : RuleTile
{
    private const int REFRESH_RADIUS = 2;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int y = -REFRESH_RADIUS; y <= REFRESH_RADIUS; y++)
        {
            for (int x = -REFRESH_RADIUS; x <= REFRESH_RADIUS; x++)
                tilemap.RefreshTile(position + new Vector3Int(x, y, 0));
        }
    }

    public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
    {
        if (IsInteriorRule(rule))
            return base.RuleMatches(rule, position, tilemap, ref transform);

        if (IsInterior(position, tilemap))
            return false;

        if (BorderRuleMatches(rule, position, tilemap, 0))
        {
            transform = Matrix4x4.identity;
            return true;
        }

        if (rule.m_RuleTransform != TilingRuleOutput.Transform.Rotated)
            return false;

        // RuleTile 기본 회전 규칙과 같은 방향/부호.
        for (int angle = m_RotationAngle; angle < 360; angle += m_RotationAngle)
        {
            if (BorderRuleMatches(rule, position, tilemap, angle))
            {
                transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle), Vector3.one);
                return true;
            }
        }
        return false;
    }

    private bool BorderRuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, int angle)
    {
        int count = Mathf.Min(rule.m_Neighbors.Count, rule.m_NeighborPositions.Count);
        for (int i = 0; i < count; i++)
        {
            int neighbor = rule.m_Neighbors[i];
            if (neighbor != TilingRuleOutput.Neighbor.This && neighbor != TilingRuleOutput.Neighbor.NotThis)
                continue;

            var cell = GetOffsetPosition(position, GetRotatedPosition(rule.m_NeighborPositions[i], angle));
            bool isBorder = tilemap.GetTile(cell) == this && !IsInterior(cell, tilemap);
            if (isBorder != (neighbor == TilingRuleOutput.Neighbor.This))
                return false;
        }
        return true;
    }

    private static bool IsInteriorRule(TilingRule rule)
    {
        if (rule.m_Neighbors.Count == 0)
            return false;
        foreach (int neighbor in rule.m_Neighbors)
        {
            if (neighbor != TilingRuleOutput.Neighbor.This)
                return false;
        }
        return true;
    }

    // 같은 세트가 칠해진 칸 기준으로, 8방향 이웃이 모두 같은 세트면 안쪽.
    private bool IsInterior(Vector3Int position, ITilemap tilemap)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if ((x != 0 || y != 0) && tilemap.GetTile(position + new Vector3Int(x, y, 0)) != this)
                    return false;
            }
        }
        return true;
    }
}
