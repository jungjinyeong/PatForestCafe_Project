using CTable;
using Extension;
using UniRx;
using UnityEngine;

public class DessertData
{
    public int TId { get; private set; }

    // 진열 수량(진열대에 놓인 빵) 합계. 품질별 수량은 GetCount(quality).
    public IReadOnlyReactiveProperty<int> Count => mCount;
    private readonly ReactiveProperty<int> mCount = new ReactiveProperty<int>(0);

    // 오븐에서 구워 창고에 보관 중인(아직 진열하지 않은) 생산 재고 합계. 품질별 수량은 GetProduced(quality).
    public IReadOnlyReactiveProperty<int> ProducedCount => mProducedCount;
    private readonly ReactiveProperty<int> mProducedCount = new ReactiveProperty<int>(0);

    // 품질별(인덱스 = DessertQuality.ToIndex) 수량. 합계 프로퍼티는 이 배열이 바뀔 때마다 다시 계산한다.
    private readonly int[] mCountByQuality = new int[DessertQuality.COUNT];
    private readonly int[] mProducedByQuality = new int[DessertQuality.COUNT];

    public DessertRow Row { get; private set; }
    public MenuItemRow MenuItemRow { get; private set; }

    public static DessertData Create(MenuItemRow menuItemRow)
    {
        if(null == menuItemRow)
        {
            return null;
        }

        return new DessertData
        {
            Row = menuItemRow.GetDessertRow(),
            MenuItemRow = menuItemRow,
            TId = menuItemRow.Tid
        };
    }

    public static DessertData Create(DessertRow dessertRow)
    {
        if(null == dessertRow)
        {
            return null;
        }
        return new DessertData
        {
            Row = dessertRow,
            MenuItemRow = GameInstance.Table.Get<MenuItemRow>(dessertRow.Tid),
            TId = dessertRow.Tid
        };
    }

    public int GetCount(eDessertQuality quality) => mCountByQuality[DessertQuality.ToIndex(quality)];
    public int GetProduced(eDessertQuality quality) => mProducedByQuality[DessertQuality.ToIndex(quality)];

    #region 진열 수량

    public void Add(eDessertQuality quality, int count = 1)
    {
        mCountByQuality[DessertQuality.ToIndex(quality)] += Mathf.Max(0, count);
        RefreshTotals();
    }

    // 지정한 품질에서 먼저 빼고, 모자라면 나머지 품질에서 낮은 품질부터 뺀다.
    // 진열대 여러 개가 같은 빵을 진열하면 품질별 수량이 실제 오브젝트와 어긋날 수 있어서, 이때도 합계는 줄어야 한다.
    public void Consume(eDessertQuality quality, int count = 1)
    {
        int remain = Mathf.Max(0, count);
        int index = DessertQuality.ToIndex(quality);
        remain = ConsumeAt(index, remain);

        for (int i = 0; i < DessertQuality.COUNT && remain > 0; i++)
        {
            if (i != index)
                remain = ConsumeAt(i, remain);
        }

        RefreshTotals();
    }

    public void SetCount(eDessertQuality quality, int count)
    {
        mCountByQuality[DessertQuality.ToIndex(quality)] = Mathf.Max(0, count);
        RefreshTotals();
    }

    #endregion

    #region 생산 재고

    public void AddProduced(eDessertQuality quality, int count = 1)
    {
        mProducedByQuality[DessertQuality.ToIndex(quality)] += Mathf.Max(0, count);
        RefreshTotals();
    }

    public void SetProduced(eDessertQuality quality, int count)
    {
        mProducedByQuality[DessertQuality.ToIndex(quality)] = Mathf.Max(0, count);
        RefreshTotals();
    }

    // 높은 품질부터 1개를 꺼낸다(진열대에 올릴 때).
    public bool TryConsumeProducedBest(out eDessertQuality quality)
    {
        foreach (var candidate in DessertQuality.HIGH_TO_LOW)
        {
            int index = DessertQuality.ToIndex(candidate);
            if (mProducedByQuality[index] <= 0)
                continue;

            mProducedByQuality[index]--;
            RefreshTotals();
            quality = candidate;
            return true;
        }

        quality = eDessertQuality.Low;
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
        for (int i = 0; i < DessertQuality.COUNT; i++)
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
