using UnityEngine;

// 층 잠금 진행도. WaypointGroup.Order를 그대로 층 번호로 사용한다(1~5=일반 층, 6=테라스).
// 언락 비용은 UpgradeModel/재배형 공방과 동일하게 기획 확정 전까지 코드 내 고정값으로 관리한다(사전 협의된 패턴).
public class FloorModel : IModelBase
{
    private static readonly int[] mUnlockCosts = { 500, 1000, 2000, 4000, 8000 };

    public const int FirstFloor = 1;
    public const int LastFloor = 6;

    public int HighestUnlockedFloor { get; private set; }

    public void Init()
    {
        HighestUnlockedFloor = FirstFloor;
    }

    public bool IsUnlocked(int floor)
    {
        return floor <= HighestUnlockedFloor;
    }

    public void SetHighestUnlockedFloor(int floor)
    {
        HighestUnlockedFloor = Mathf.Clamp(floor, FirstFloor, LastFloor);
    }

    // 다음으로 언락 가능한 층. 이미 전부 언락됐으면 null.
    public int? GetNextLockedFloor()
    {
        int next = HighestUnlockedFloor + 1;
        return next <= LastFloor ? next : null;
    }

    public int GetUnlockCost(int floor)
    {
        int index = floor - FirstFloor - 1;
        if (index < 0 || index >= mUnlockCosts.Length)
            return 0;

        return mUnlockCosts[index];
    }

    public bool TryUnlockNextFloor()
    {
        var next = GetNextLockedFloor();
        if (next == null)
            return false;

        int cost = GetUnlockCost(next.Value);
        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        if (gold == null || gold.Count.Value < cost)
            return false;

        gold.Consume(cost);
        SetHighestUnlockedFloor(next.Value);
        return true;
    }

    public void Dispose()
    {
        HighestUnlockedFloor = 0;
    }
}
