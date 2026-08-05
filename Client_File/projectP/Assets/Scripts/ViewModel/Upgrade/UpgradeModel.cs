using UnityEngine;

// 카운터 능력치 시스템이 생기기 전까지 사용하는 임시 업그레이드: 결제 골드 수익 배율만 다룬다.
// 레벨별 비용/효과 수치는 기획 확정 전까지 코드 내 고정값으로 관리한다.
public class UpgradeModel : IModelBase
{
    private const float GoldMultiplierPerLevel = 0.1f;
    private const int BaseUpgradeCost = 100;
    private const float UpgradeCostGrowthRate = 1.5f;

    public int Level { get; private set; }

    public float GoldIncomeMultiplier => 1f + Level * GoldMultiplierPerLevel;

    public void Init() { }

    public void SetLevel(int level)
    {
        Level = Mathf.Max(0, level);
    }

    public int GetNextUpgradeCost()
    {
        return Mathf.RoundToInt(BaseUpgradeCost * Mathf.Pow(UpgradeCostGrowthRate, Level));
    }

    public int ApplyGoldIncomeMultiplier(int baseGold)
    {
        return Mathf.RoundToInt(baseGold * GoldIncomeMultiplier);
    }

    public bool TryUpgrade()
    {
        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        int cost = GetNextUpgradeCost();
        if (gold == null || gold.Count.Value < cost)
            return false;

        gold.Consume(cost);
        Level++;
        return true;
    }

    public void Dispose()
    {
        Level = 0;
    }
}
