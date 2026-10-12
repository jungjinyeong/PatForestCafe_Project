using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;

// 층 잠금 진행도. 층 ID는 Floor.csv(CTable.FloorRow)의 Tid이고 씬의 WaypointGroup.Order와 같다(아래→위 1~7: B3 B2 B1 1F T1 T2 T3).
// 매장층(FloorType 0)과 테라스층(FloorType 1)은 각자 UnlockOrder 순서로 따로 해금한다. UnlockOrder 0 = 처음부터 열림(1F, T1).
// 테이블이 없으면 아래 기본값(Floor.csv와 같은 내용)을 쓴다.
public class FloorModel : IModelBase
{
    public class FloorInfo
    {
        public int Id;
        public string Name;
        public eFloorType Type;
        public int UnlockOrder;
        public int UnlockCost;
    }

    // Floor.csv가 없을 때만 사용하는 기본 층 구성.
    private static readonly FloorInfo[] mDefaultFloors =
    {
        new FloorInfo { Id = 1, Name = "B3", Type = eFloorType.Store, UnlockOrder = 3, UnlockCost = 2000 },
        new FloorInfo { Id = 2, Name = "B2", Type = eFloorType.Store, UnlockOrder = 2, UnlockCost = 1000 },
        new FloorInfo { Id = 3, Name = "B1", Type = eFloorType.Store, UnlockOrder = 1, UnlockCost = 500 },
        new FloorInfo { Id = 4, Name = "1F", Type = eFloorType.Store, UnlockOrder = 0, UnlockCost = 0 },
        new FloorInfo { Id = 5, Name = "T1", Type = eFloorType.Terrace, UnlockOrder = 0, UnlockCost = 0 },
        new FloorInfo { Id = 6, Name = "T2", Type = eFloorType.Terrace, UnlockOrder = 1, UnlockCost = 4000 },
        new FloorInfo { Id = 7, Name = "T3", Type = eFloorType.Terrace, UnlockOrder = 2, UnlockCost = 8000 },
    };

    private readonly List<FloorInfo> mFloors = new List<FloorInfo>();
    private readonly HashSet<int> mUnlockedFloors = new HashSet<int>();

    // 층 ID 순(아래→위).
    public IReadOnlyList<FloorInfo> Floors => mFloors;
    public IEnumerable<int> UnlockedFloors => mUnlockedFloors;

    // 처음 화면에 보여줄 층 = 처음부터 열린 매장층(1F).
    public int DefaultViewFloor { get; private set; }

    // 해금 상태가 바뀔 때마다 발행(층 해금 팝업 갱신 등).
    public IReadOnlyReactiveProperty<int> UnlockedCount => mUnlockedCount;
    private readonly ReactiveProperty<int> mUnlockedCount = new ReactiveProperty<int>(0);

    // 로비 카메라가 지금 보고 있는 층(LobbyFloorCameraController가 갱신). 가구 구매 시 이 층에 스폰한다.
    public IReadOnlyReactiveProperty<int> ViewingFloor => mViewingFloor;
    private readonly ReactiveProperty<int> mViewingFloor = new ReactiveProperty<int>(0);

    public void Init()
    {
        mFloors.Clear();

        var group = GameInstance.Table?.GetTable<CTable.FloorRow>();
        if (group != null && group.All.Count > 0)
        {
            foreach (var row in group.All.Values)
            {
                mFloors.Add(new FloorInfo
                {
                    Id = row.Tid,
                    Name = string.IsNullOrEmpty(row.Name) ? row.Tid.ToString() : row.Name,
                    Type = row.FloorType == (int)eFloorType.Terrace ? eFloorType.Terrace : eFloorType.Store,
                    UnlockOrder = row.UnlockOrder,
                    UnlockCost = (int)System.Math.Clamp(row.UnlockCost, 0L, int.MaxValue),
                });
            }
        }
        else
        {
            Logger.Warning("[FloorModel] Floor 테이블이 없어 기본 층 구성을 사용합니다.");
            mFloors.AddRange(mDefaultFloors);
        }

        mFloors.Sort((a, b) => a.Id.CompareTo(b.Id));

        var defaultView = mFloors.Where(f => f.Type == eFloorType.Store).OrderBy(f => f.UnlockOrder).FirstOrDefault();
        DefaultViewFloor = defaultView != null ? defaultView.Id : (mFloors.Count > 0 ? mFloors[0].Id : 0);
        mViewingFloor.Value = DefaultViewFloor;

        ResetUnlocks();
    }

    public FloorInfo GetFloor(int floor)
    {
        foreach (var info in mFloors)
        {
            if (info.Id == floor)
                return info;
        }
        return null;
    }

    public bool Exists(int floor) => GetFloor(floor) != null;

    public string GetName(int floor) => GetFloor(floor)?.Name ?? floor.ToString();

    public bool IsUnlocked(int floor) => mUnlockedFloors.Contains(floor);

    public void SetViewingFloor(int floor)
    {
        if (Exists(floor))
            mViewingFloor.Value = floor;
    }

    // 같은 종류에서 잠긴 층 중 UnlockOrder가 가장 작은 층. 그 종류가 전부 열렸으면 null.
    public int? GetNextLockedFloor(eFloorType type)
    {
        FloorInfo next = null;
        foreach (var info in mFloors)
        {
            if (info.Type != type || IsUnlocked(info.Id))
                continue;

            if (next == null || info.UnlockOrder < next.UnlockOrder)
                next = info;
        }
        return next?.Id;
    }

    public int GetUnlockCost(int floor) => GetFloor(floor)?.UnlockCost ?? 0;

    public bool TryUnlockNextFloor(eFloorType type)
    {
        var next = GetNextLockedFloor(type);
        if (next == null)
            return false;

        int cost = GetUnlockCost(next.Value);
        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        if (gold == null || gold.Count.Value < cost)
            return false;

        gold.Consume(cost);
        Unlock(next.Value);
        return true;
    }

    // 세이브 복원 — 기본 개방 층에 저장된 층을 더한다. 테이블에 없는 층 ID는 무시한다.
    public void RestoreUnlockedFloors(IEnumerable<int> floors)
    {
        ResetUnlocks();

        if (floors != null)
        {
            foreach (int floor in floors)
            {
                if (Exists(floor))
                    mUnlockedFloors.Add(floor);
            }
        }

        mUnlockedCount.Value = mUnlockedFloors.Count;
    }

    public void Dispose()
    {
        mFloors.Clear();
        mUnlockedFloors.Clear();
        mUnlockedCount.Dispose();
        mViewingFloor.Dispose();
    }

    private void ResetUnlocks()
    {
        mUnlockedFloors.Clear();
        foreach (var info in mFloors)
        {
            if (info.UnlockOrder <= 0)
                mUnlockedFloors.Add(info.Id);
        }
        mUnlockedCount.Value = mUnlockedFloors.Count;
    }

    private void Unlock(int floor)
    {
        if (mUnlockedFloors.Add(floor))
            mUnlockedCount.Value = mUnlockedFloors.Count;
    }
}
