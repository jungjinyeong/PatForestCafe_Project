using System.Collections.Generic;
using UnityEngine;

// 빵(및 빵 재료) 품질. 값은 BreadIngredient.csv의 Grade와 같다(1=하급 2=중급 3=고급).
public enum eBreadQuality
{
    Low = 1,
    Mid = 2,
    High = 3,
}

public static class BreadQuality
{
    public const int COUNT = 3;

    // TODO(기획): 임시 판매가 배율. 확정 시 Config/테이블로 이동.
    private static readonly float[] SALE_MULTIPLIERS = { 1f, 1.2f, 1.5f };
    private static readonly string[] NAMES = { "하급", "중급", "고급" };
    private static readonly string[] COLORS = { "#8A8A8A", "#4E8FCF", "#D9952D" };

    // 높은 품질부터 — 진열대가 생산 재고를 꺼내는 순서.
    public static readonly eBreadQuality[] HIGH_TO_LOW = { eBreadQuality.High, eBreadQuality.Mid, eBreadQuality.Low };

    public static int ToIndex(eBreadQuality quality) => Mathf.Clamp((int)quality - 1, 0, COUNT - 1);
    public static eBreadQuality FromGrade(int grade) => (eBreadQuality)Mathf.Clamp(grade, 1, COUNT);

    public static float GetSaleMultiplier(eBreadQuality quality) => SALE_MULTIPLIERS[ToIndex(quality)];
    public static string GetName(eBreadQuality quality) => NAMES[ToIndex(quality)];
    public static string GetColorHex(eBreadQuality quality) => COLORS[ToIndex(quality)];

    // 재료 등급 뱃지용 컬러 텍스트. 등급을 쓰지 않는 재료(Grade 0)는 빈 문자열.
    public static string GetGradeText(int grade)
    {
        if (grade <= 0) return string.Empty;

        var quality = FromGrade(grade);
        return $"<color={GetColorHex(quality)}>{GetName(quality)}</color>";
    }

    // 예: "고급 2 중급 1 하급 5" — 창고(생산 재고)에 수량이 있는 품질만.
    public static string BuildProducedText(BreadData bread, string separator = " ")
    {
        if (bread == null) return string.Empty;

        var parts = new List<string>();
        foreach (var quality in HIGH_TO_LOW)
        {
            int count = bread.GetProduced(quality);
            if (count > 0)
                parts.Add($"<color={GetColorHex(quality)}>{GetName(quality)}</color> {count}");
        }
        return string.Join(separator, parts);
    }

    // 기본 재료 등급의 평균을 반올림(.5는 올림)한 품질. 재료가 없으면 하급.
    // Mathf.RoundToInt는 .5를 짝수 쪽으로 보내(2.5 → 2) 중·고급 경계가 어긋나므로 쓰지 않는다.
    public static eBreadQuality FromIngredientGrades(IEnumerable<int> grades)
    {
        int sum = 0, count = 0;
        foreach (int grade in grades)
        {
            sum += Mathf.Clamp(grade, 1, COUNT);
            count++;
        }

        if (count == 0)
            return eBreadQuality.Low;

        return FromGrade(Mathf.FloorToInt((float)sum / count + 0.5f));
    }
}
