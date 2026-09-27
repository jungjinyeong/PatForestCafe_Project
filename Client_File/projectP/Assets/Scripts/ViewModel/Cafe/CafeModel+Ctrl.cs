using UnityEngine;

public partial class CafeModel
{
    private const int BASE_REQUIRED_EXP = 50;
    private const float REQUIRED_EXP_GROWTH_RATE = 1.25f;

    public bool IsMaxLevel => mLevel.Value >= MAX_LEVEL;

    // 해당 레벨에서 다음 레벨로 가기 위해 필요한 경험치.
    public static int GetRequiredExp(int level)
    {
        return Mathf.RoundToInt(BASE_REQUIRED_EXP * Mathf.Pow(REQUIRED_EXP_GROWTH_RATE, Mathf.Max(0, level - MIN_LEVEL)));
    }

    public int RequiredExp => GetRequiredExp(mLevel.Value);

    public void AddExp(int amount)
    {
        if (amount <= 0 || IsMaxLevel) return;

        GameInstance.Model.Business.RecordExp(amount);

        int level = mLevel.Value;
        int exp = mExp.Value + amount;

        while (level < MAX_LEVEL && exp >= GetRequiredExp(level))
        {
            exp -= GetRequiredExp(level);
            level++;
            // Exp보다 Level을 먼저 올려야 구독자가 새 레벨 기준 필요 경험치로 표시한다.
            mLevel.Value = level;
            mOnLevelUp.OnNext(level);
        }

        mExp.Value = level >= MAX_LEVEL ? 0 : exp;
    }

    // 세이브 복원. SaveManager.Load()에서 호출(구 세이브는 0이 들어오므로 최소 레벨로 보정).
    public void Restore(int level, int exp)
    {
        mLevel.Value = Mathf.Clamp(level, MIN_LEVEL, MAX_LEVEL);
        mExp.Value = IsMaxLevel ? 0 : Mathf.Clamp(exp, 0, RequiredExp - 1);
    }
}
