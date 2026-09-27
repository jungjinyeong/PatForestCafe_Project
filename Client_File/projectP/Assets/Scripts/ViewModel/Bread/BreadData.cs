using CTable;
using Extension;
using UniRx;
using UnityEngine;

public class BreadData
{
    public int TId { get; private set; }

    // 진열 수량(진열대에 놓인 빵) 합계. 품질별 수량은 GetCount(quality).
    public IReadOnlyReactiveProperty<int> Count => mCount;
    private readonly ReactiveProperty<int> mCount = new ReactiveProperty<int>(0);

    // 오븐에서 구워 창고에 보관 중인(아직 진열하지 않은) 생산 재고 합계. 품질별 수량은 GetProduced(quality).
    public IReadOnlyReactiveProperty<int> ProducedCount => mProducedCount;
    private readonly ReactiveProperty<int> mProducedCount = new ReactiveProperty<int>(0);

    // 품질별(인덱스 = BreadQuality.ToIndex) 수량. 합계 프로퍼티는 이 배열이 바뀔 때마다 다시 계산한다.
    private readonly int[] mCountByQuality = new int[BreadQuality.COUNT];
    private readonly int[] mProducedByQuality = new int[BreadQuality.COUNT];

    public BreadRow Row { get; private set; }
    public MenuItemRow MenuItemRow { get; private set; }

    public static BreadData Create(MenuItemRow menuItemRow)
    {
        if(null == menuItemRow)
        {
            return null;
        }

        return new BreadData
        {
            Row = menuItemRow.GetBreadRow(),
            MenuItemRow = menuItemRow,
            TId = menuItemRow.Tid
        };
    }

    public static BreadData Create(BreadRow breadRow)
    {
        if(null == breadRow)
        {
            return null;
        }
        return new BreadData
        {
            Row = breadRow,
            MenuItemRow = GameInstance.Table.Get<MenuItemRow>(breadRow.Tid),
            TId = breadRow.Tid
        };
    }

    public int GetCount(eBreadQuality quality) => mCountByQuality[BreadQuality.ToIndex(quality)];
    public int GetProduced(eBreadQuality quality) => mProducedByQuality[BreadQuality.ToIndex(quality)];

    #region 진열 수량

    public void Add(eBreadQuality quality, int count = 1)
    {
        mCountByQuality[BreadQuality.ToIndex(quality)] += Mathf.Max(0, count);
        RefreshTotals();
    }

    // 지정한 품질에서 먼저 빼고, 모자라면 나머지 품질에서 낮은 품질부터 뺀다.
    // 진열대 여러 개가 같은 빵을 진열하면 품질별 수량이 실제 오브젝트와 어긋날 수 있어서, 이때도 합계는 줄어야 한다.
    public void Consume(eBreadQuality quality, int count = 1)
    {
        int remain = Mathf.Max(0, count);
        int index = BreadQuality.ToIndex(quality);
        remain = ConsumeAt(index, remain);

        for (int i = 0; i < BreadQuality.COUNT && remain > 0; i++)
        {
            if (i != index)
                remain = ConsumeAt(i, remain);
        }

        RefreshTotals();
    }

    public void SetCount(eBreadQuality quality, int count)
    {
        mCountByQuality[BreadQuality.ToIndex(quality)] = Mathf.Max(0, count);
        RefreshTotals();
    }

    #endregion

    #region 생산 재고

    public void AddProduced(eBreadQuality quality, int count = 1)
    {
        mProducedByQuality[BreadQuality.ToIndex(quality)] += Mathf.Max(0, count);
        RefreshTotals();
    }

    public void SetProduced(eBreadQuality quality, int count)
    {
        mProducedByQuality[BreadQuality.ToIndex(quality)] = Mathf.Max(0, count);
        RefreshTotals();
    }

    // 높은 품질부터 1개를 꺼낸다(진열대에 올릴 때).
    public bool TryConsumeProducedBest(out eBreadQuality quality)
    {
        foreach (var candidate in BreadQuality.HIGH_TO_LOW)
        {
            int index = BreadQuality.ToIndex(candidate);
            if (mProducedByQuality[index] <= 0)
                continue;

            mProducedByQuality[index]--;
            RefreshTotals();
            quality = candidate;
            return true;
        }

        quality = eBreadQuality.Low;
        return false;
    }

    #endregion

    // index 품질에서 최대 count개를 빼고, 빼지 못한 나머지 개수를 돌려준다.
    private int ConsumeAt(int index, int count)
    {
        int taken = Mathf.Min(mCountByQuality[index], count);
        mCountByQuality[index] -= taken;
        return count - taken;
    }

    private void RefreshTotals()
    {
        int count = 0, produced = 0;
        for (int i = 0; i < BreadQuality.COUNT; i++)
        {
            count += mCountByQuality[i];
            produced += mProducedByQuality[i];
        }

        mCount.Value = count;
        mProducedCount.Value = produced;
    }

    public void Dispose()
    {
        mCount.Dispose();
        mProducedCount.Dispose();
    }
}
