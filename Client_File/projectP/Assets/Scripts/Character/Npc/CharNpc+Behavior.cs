using System.Collections.Generic;
using UnityEngine;

// 손님 행동 단계(손님 로직 기획서 — 손님 매장 이용 루트). 단계마다 목표로 걸어가고, 끝나면 AdvanceBehaviorQueue로 다음 단계.
// 할 수 없는 단계(해당 시설이 층에 없음 등)는 건너뛰고, 큐가 비면 퇴장한다.
public partial class CharNpc
{
    // 진열대 도착 후 구매 확률. 사지 않으면 다른 진열대로 가서 반드시 산다(다른 진열대가 없으면 그 자리에서 산다).
    private const float DESSERT_BUY_CHANCE = 0.8f;
    private const float DESSERT_BROWSE_SECONDS = 0.8f;
    private const float DESSERT_PICK_SECONDS = 0.5f;
    // 결제 후 테라스에 가고 싶어 할 확률(나머지는 퇴장). 실제 입장은 남은 테라스 좌석이 있어야 한다.
    private const float TERRACE_CHANCE = 0.5f;
    // 결제 줄이 꽉 찼을 때 근처에서 기다리는 최대 시간과 자리 확인 간격(임시 상수).
    private const float LINE_WAIT_MAX_SECONDS = 8f;
    private const float LINE_RECHECK_SECONDS = 1f;
    // 시설 앞에서 결제가 이 시간 안에 끝나지 않으면(시설이 사라짐 등) 포기한다.
    private const float SERVICE_TIMEOUT_SECONDS = 20f;

    // 디저트가 없을 때(디저트만 세트): 키오스크 이벤트 확률(나머지는 배회 후 퇴장), 키오스크 근처에서 머무는 최대 시간(임시 값).
    private const float KIOSK_EVENT_CHANCE = 0.5f;
    private const float KIOSK_EVENT_SECONDS = 8f;
    // 키오스크 이벤트 자리: 키오스크 결제 칸 옆으로 이만큼 떨어진 곳(결제 줄과 겹치지 않게).
    private const float KIOSK_EVENT_SIDE_MIN = 0.8f;
    private const float KIOSK_EVENT_SIDE_MAX = 1.2f;
    // 배회: 빈 진열대를 이 수만큼 둘러보고 나간다.
    private const int WANDER_MAX_POINTS = 2;
    private const float WANDER_PAUSE_SECONDS = 0.8f;

    // 테라스 상호작용 가구 이용 시간(임시 값). 같은 종류를 n번 쓴 뒤 다시 쓸 확률 = 0.5^n.
    private const float TERRACE_USE_MIN_SECONDS = 3f;
    private const float TERRACE_USE_MAX_SECONDS = 5f;
    private const float TERRACE_REUSE_RATE = 0.5f;

    // 진열대 웨이포인트 → 진열대 컴포넌트(진열대 고를 때마다 GetComponentInParent를 부르지 않게). 철거된 키는 가끔 지운다.
    private const int STAND_CACHE_PURGE_COUNT = 128;
    private static readonly Dictionary<Waypoint, Intaraction_DessertStand> mStandCache = new Dictionary<Waypoint, Intaraction_DessertStand>();

    private bool mIsInKioskEvent;
    // 스폰 때 고른 진열대(빵이 있는 곳). 그 층에 도착해 디저트 단계를 시작할 때 먼저 간다.
    private Waypoint mPreferredStand;

    private Waypoint mTerraceUseTarget;
    private readonly HashSet<Waypoint> mUsedTerraceFurniture = new HashSet<Waypoint>();
    private readonly Dictionary<int, int> mTerraceUseCounts = new Dictionary<int, int>();
    private readonly List<Waypoint> mWaypointBuffer = new List<Waypoint>();

    private ServiceLine mPaymentLine;
    private Waypoint mPaymentFront;
    private IPaymentFacility mPaymentFacility;
    private System.Action mOnPaid;
    private float mLineWaitSeconds;
    private bool mIsWaitingForLine;

    private Waypoint mReservedSeat;
    private WaypointGroup mReservedSeatGroup;

    private void AdvanceBehaviorQueue()
    {
        StopWait();
        // 결제하러 가던 중 이동이 끊긴 경우(웨이포인트 파괴 등) 줄에 유령 자리로 남지 않게 한다.
        LeavePaymentLine();

        while (mBehaviorQueue != null && mBehaviorQueue.Count > 0)
        {
            var step = mBehaviorQueue.Dequeue();
            if (TryBeginStep(step))
                return;
        }

        LeaveStore();
    }

    private bool TryBeginStep(eNpcBehaviorStepType step)
    {
        if (mCurrentGroup == null)
            return false;

        switch (step)
        {
            case eNpcBehaviorStepType.BuyDessert:
                BeginBuyDessert();
                return true;
            case eNpcBehaviorStepType.PayCounter:
                return TryBeginPayCounter();
            case eNpcBehaviorStepType.PayKiosk:
                return TryBeginPayKiosk();
            case eNpcBehaviorStepType.GoTerraceOrExit:
                BeginGoTerraceOrExit();
                return true;
            default:
                return false;
        }
    }

    #region Dessert

    private void BeginBuyDessert()
    {
        // 스폰 때 고른 진열대가 아직 이 층에 있고 빵이 남아 있으면 그곳으로, 아니면 이 층에서 다시 고른다.
        var stand = mPreferredStand;
        mPreferredStand = null;
        if (stand == null || GetDisplayedCount(stand) <= 0 || !IsInCurrentGroup(stand))
        {
            if (!TryPickDessertStand(null, out stand))
            {
                OnDessertUnavailable();
                return;
            }
        }

        MoveTo(stand, () => OnArriveDessertStand(stand, false));
    }

    private bool IsInCurrentGroup(Waypoint waypoint)
    {
        if (mCurrentGroup == null)
            return false;

        mWaypointBuffer.Clear();
        mCurrentGroup.CollectTriggerWaypoints(Waypoint.eWaypointType.Trigger_Dessert, mWaypointBuffer);
        return mWaypointBuffer.Contains(waypoint);
    }

    // 열린 매장층(테라스 제외) 전체에서 빵이 진열된 진열대 하나를 무작위로 고른다(진열대마다 같은 확률).
    private bool TryPickStockedStandOnAnyFloor(out Waypoint stand, out WaypointGroup group)
    {
        stand = null;
        group = null;
        var waypointManager = GameInstance.WayPoint;
        if (waypointManager == null)
            return false;

        var candidates = new List<(Waypoint stand, WaypointGroup group)>();
        foreach (var floor in waypointManager.WaypointGroups)
        {
            if (floor == null || floor.IsTerraceZone || !GameInstance.Model.Floor.IsUnlocked(floor.Order))
                continue;

            mWaypointBuffer.Clear();
            floor.CollectTriggerWaypoints(Waypoint.eWaypointType.Trigger_Dessert, mWaypointBuffer);
            foreach (var wp in mWaypointBuffer)
            {
                if (GetDisplayedCount(wp) > 0)
                    candidates.Add((wp, floor));
            }
        }

        if (candidates.Count == 0)
            return false;

        var picked = candidates[Random.Range(0, candidates.Count)];
        stand = picked.stand;
        group = picked.group;
        return true;
    }

    // exclude: 방금 들렀다 사지 않은 진열대(다시 고르지 않음).
    private bool TryPickDessertStand(Waypoint exclude, out Waypoint result)
    {
        result = null;
        return mCurrentGroup != null && mCurrentGroup.TryGetRandomWaypoint(
            Waypoint.eWaypointCategoryType.Trigger,
            Waypoint.eWaypointType.Trigger_Dessert,
            wp => wp != exclude && GetDisplayedCount(wp) > 0,
            out result);
    }

    private static Intaraction_DessertStand GetStand(Waypoint waypoint)
    {
        if (waypoint == null)
            return null;

        if (mStandCache.TryGetValue(waypoint, out var cached) && cached != null)
            return cached;

        var stand = waypoint.GetComponentInParent<Intaraction_DessertStand>();
        if (stand == null)
            return null;

        // 철거된 진열대 키가 쌓이는 것을 막기 위해 일정 수를 넘으면 통째로 비운다(다시 채우는 비용은 조회 한 번씩).
        if (mStandCache.Count >= STAND_CACHE_PURGE_COUNT)
            mStandCache.Clear();
        mStandCache[waypoint] = stand;
        return stand;
    }

    private static int GetDisplayedCount(Waypoint waypoint)
    {
        var stand = GetStand(waypoint);
        return stand != null && stand.IsAssigned ? stand.DisplayedCount : 0;
    }

    // 진열대 앞에서 멈춰 잠시 둘러본 뒤 살지 정한다. 첫 진열대에서만 20% 확률로 사지 않고 다른 진열대로 간다.
    private void OnArriveDessertStand(Waypoint standWaypoint, bool isRetry)
    {
        PlayAnimation("Idle");

        Wait(DESSERT_BROWSE_SECONDS, () =>
        {
            if (!isRetry && Random.value >= DESSERT_BUY_CHANCE && TryPickDessertStand(standWaypoint, out var other))
            {
                MoveTo(other, () => OnArriveDessertStand(other, true));
                return;
            }

            BuyAtStand(standWaypoint);
        });
    }

    // 1~최대(UpgradeModel.DessertBuyMax)개 중 무작위 수량을 집는다. 둘러보는 사이 다 팔렸으면 다른 진열대를 찾는다.
    private void BuyAtStand(Waypoint standWaypoint)
    {
        var stand = GetStand(standWaypoint);
        int wantCount = Random.Range(1, GameInstance.Model.Upgrade.DessertBuyMax + 1);
        var taken = stand != null ? stand.TakeDesserts(wantCount) : null;

        if (taken == null || taken.Count == 0)
        {
            if (TryPickDessertStand(standWaypoint, out var other))
            {
                MoveTo(other, () => OnArriveDessertStand(other, true));
                return;
            }

            OnDessertUnavailable();
            return;
        }

        SetCart(stand, taken);
        Wait(DESSERT_PICK_SECONDS, AdvanceBehaviorQueue);
    }

    // 고를 디저트가 없을 때.
    // - 다음 단계가 카운터(디저트 + 음료 세트): 땀방울 없이 바로 카운터로 가서 음료만 산다.
    // - 그 밖(디저트만 세트): 잠시 둘러보다 땀방울을 띄운 뒤 KIOSK_EVENT_CHANCE로 키오스크 이벤트, 아니면 배회 후 퇴장.
    private void OnDessertUnavailable()
    {
        if (mBehaviorQueue != null && mBehaviorQueue.Count > 0 && mBehaviorQueue.Peek() == eNpcBehaviorStepType.PayCounter)
        {
            AdvanceBehaviorQueue();
            return;
        }

        PlayAnimation("Idle");
        float lookAroundSeconds = GameInstance.Config.GetValue(eConfigType.DessertUnavailablePauseMs) / 1000f;

        Wait(lookAroundSeconds, () =>
        {
            LobbyUI?.SetSweatIconActive(true);

            Wait(mSweatDisplaySeconds, () =>
            {
                LobbyUI?.SetSweatIconActive(false);
                mBehaviorQueue?.Clear();

                if (Random.value < KIOSK_EVENT_CHANCE && TryGetKiosk(out var kiosk))
                    BeginKioskEvent(kiosk);
                else
                    BeginWander(Random.Range(1, WANDER_MAX_POINTS + 1), mCurrentWaypoint);
            });
        });
    }

    #endregion

    #region Kiosk Event / Wander

    // 키오스크 이벤트: 키오스크 결제 칸 옆에서 우물쭈물(땀방울 + 말풍선). 손님을 누르거나 시간이 지나면 테라스 or 퇴장.
    // 말풍선을 눌러 음료를 추천하는 UI는 추후 기획 — 지금은 누르면 행복 아이콘만 띄운다.
    private void BeginKioskEvent(Waypoint kiosk)
    {
        float side = Random.Range(KIOSK_EVENT_SIDE_MIN, KIOSK_EVENT_SIDE_MAX) * (Random.value < 0.5f ? -1f : 1f);
        var point = kiosk.transform.position + new Vector3(side, Random.Range(-0.3f, 0.3f), 0f);

        MoveToPoint(point, () =>
        {
            PlayAnimation("Idle");
            mIsInKioskEvent = true;
            LobbyUI?.SetSweatIconActive(true);
            LobbyUI?.SetKioskBubbleActive(true);
            Wait(KIOSK_EVENT_SECONDS, () => EndKioskEvent(false));
        });
    }

    private void EndKioskEvent(bool isClicked)
    {
        if (!mIsInKioskEvent)
            return;

        mIsInKioskEvent = false;
        StopWait();

        var lobbyUI = LobbyUI;
        if (lobbyUI != null)
        {
            lobbyUI.SetSweatIconActive(false);
            lobbyUI.SetKioskBubbleActive(false);
            if (isClicked)
                lobbyUI.PlaySatisfactionEffect();
        }

        // 결제는 없지만 기획대로 「테라스 or 퇴장」 — 지금 좌석을 예약해 본다.
        mBehaviorQueue = new Queue<eNpcBehaviorStepType>();
        mBehaviorQueue.Enqueue(eNpcBehaviorStepType.GoTerraceOrExit);
        TryReserveTerraceSeat();
        AdvanceBehaviorQueue();
    }

    // InputManager가 손님 클릭 시 부른다. 키오스크 이벤트 중일 때만 반응한다.
    public void OnClicked()
    {
        if (mIsInKioskEvent)
            EndKioskEvent(true);
    }

    // 배회: 이 층의 다른 진열대(previous = 방금 있던 곳 제외)를 remaining곳 둘러본 뒤 퇴장한다. 둘러볼 곳이 없으면 바로 퇴장.
    private void BeginWander(int remaining, Waypoint previous)
    {
        if (remaining <= 0 || mCurrentGroup == null || !mCurrentGroup.TryGetRandomWaypoint(
                Waypoint.eWaypointCategoryType.Trigger, Waypoint.eWaypointType.Trigger_Dessert, wp => wp != previous, out var target))
        {
            LeaveStore();
            return;
        }

        MoveTo(target, () =>
        {
            PlayAnimation("Idle");
            Wait(WANDER_PAUSE_SECONDS, () => BeginWander(remaining - 1, target));
        });
    }

    #endregion

    #region Payment Facilities

    // 카운터(직원): 음료 1잔 주문 + 고른 디저트 결제.
    // 이 층에 직원이 있는 카운터가 없으면 장바구니를 든 채 1층(메인 카운터)으로 간다. 1층에도 직원이 없으면 카운터에서 바로 결제한다.
    private bool TryBeginPayCounter()
    {
        bool hasCounter = mCurrentGroup.TryGetRandomWaypoint(Waypoint.eWaypointCategoryType.Trigger, Waypoint.eWaypointType.Trigger_Order, out var counter);
        var staff = hasCounter ? FindCounterStaff(counter) : null;
        if (staff != null)
        {
            JoinPaymentLine(counter, staff, () => CompleteCounterPayment(true));
            return true;
        }

        if (TryPayOnEntranceFloor(TryBeginPayCounter))
            return true;

        // 1층에도 직원이 없을 때: 카운터에서 바로 결제(카운터도 없으면 디저트라도 키오스크에서).
        if (hasCounter)
        {
            MoveTo(counter, () => CompleteCounterPayment(true));
            return true;
        }

        if (HasCart && TryGetKiosk(out var kiosk))
        {
            JoinPaymentLine(kiosk, GetKiosk(kiosk), CompleteKioskPayment);
            return true;
        }

        return false;
    }

    // 키오스크: 고른 디저트만 결제. 고른 게 없으면 건너뛴다. 키오스크가 없는 층은 직원 있는 카운터에서 디저트만 결제하고,
    // 둘 다 없으면 1층으로 올라가 결제한다.
    private bool TryBeginPayKiosk()
    {
        if (!HasCart)
            return false;

        if (TryGetKiosk(out var kiosk))
        {
            JoinPaymentLine(kiosk, GetKiosk(kiosk), CompleteKioskPayment);
            return true;
        }

        if (mCurrentGroup.TryGetRandomWaypoint(Waypoint.eWaypointCategoryType.Trigger, Waypoint.eWaypointType.Trigger_Order, out var counter))
        {
            var staff = FindCounterStaff(counter);
            if (staff != null || mCurrentGroup == mEntranceGroup)
            {
                JoinPaymentLine(counter, staff, () => CompleteCounterPayment(false));
                return true;
            }
        }

        return TryPayOnEntranceFloor(TryBeginPayKiosk);
    }

    // 결제할 시설이 없는 층: 1층으로 옮겨 가서 같은 결제 단계를 다시 시도한다. 이미 1층이면 false.
    private bool TryPayOnEntranceFloor(System.Func<bool> retryPayStep)
    {
        if (mEntranceGroup == null || mCurrentGroup == mEntranceGroup)
            return false;

        TravelTo(mEntranceGroup, () =>
        {
            if (!retryPayStep())
                AdvanceBehaviorQueue();
        });
        return true;
    }

    private bool TryGetKiosk(out Waypoint kiosk)
    {
        return mCurrentGroup.TryGetRandomWaypoint(Waypoint.eWaypointCategoryType.Trigger, Waypoint.eWaypointType.Trigger_Kiosk, out kiosk);
    }

    private static Intaraction_Kiosk GetKiosk(Waypoint kioskWaypoint)
    {
        return kioskWaypoint != null ? kioskWaypoint.GetComponentInParent<Intaraction_Kiosk>() : null;
    }

    // 층 프리팹(World_*)에서 Trigger_Order 웨이포인트는 PlacementGridArea 아래에, 직원은 층 루트 아래에 따로 있다.
    // 웨이포인트 부모에 직원이 없으면 같은 층 루트에서 찾는다.
    private static CharStaff FindCounterStaff(Waypoint counter)
    {
        if (counter == null)
            return null;

        var staff = counter.GetComponentInParent<CharStaff>();
        return staff != null ? staff : counter.transform.root.GetComponentInChildren<CharStaff>();
    }

    #endregion

    #region Payment Line

    // 결제 줄에 선다. 자리가 있으면 받은 칸으로 가고(0번 칸 = 시설 앞이면 도착해서 결제), 꽉 찼으면 근처에서 기다린다.
    // facility가 없으면(직원 없는 1층 카운터 등) 줄 없이 front로 가서 바로 결제한다.
    private void JoinPaymentLine(Waypoint front, IPaymentFacility facility, System.Action onPaid)
    {
        if (!IsFacilityAlive(facility) || front == null)
        {
            MoveTo(front, onPaid);
            return;
        }

        var line = facility.GetLine(front);
        if (line.TryJoin(this, Guarded<int>(GoToLineSlot), out int slot))
        {
            mPaymentLine = line;
            mPaymentFront = front;
            mPaymentFacility = facility;
            mOnPaid = onPaid;
            mLineWaitSeconds = 0f;
            GoToLineSlot(slot);
            return;
        }

        if (!mIsWaitingForLine)
            mLineWaitSeconds = 0f;
        WaitForLineRoom(front, facility, onPaid);
    }

    // 0번 칸이면 시설 앞으로 가서 결제를 시작하고, 아니면 대기 칸에 서서 기다린다(앞 손님이 빠지면 ServiceLine이 다시 부른다).
    private void GoToLineSlot(int slot)
    {
        if (mPaymentLine == null)
            return;

        if (slot == 0)
        {
            MoveTo(mPaymentFront, BeginServiceAtFront);
            return;
        }

        MoveToPoint(mPaymentLine.GetSlotPosition(slot), () => PlayAnimation("Idle"));
    }

    // 시설 앞(0번 칸)에서 결제 게이지를 기다린다. 시설이 사라지는 등으로 SERVICE_TIMEOUT_SECONDS 안에 끝나지 않으면 결제를 포기한다.
    private void BeginServiceAtFront()
    {
        PlayAnimation("Idle");

        var facility = mPaymentFacility;
        var onPaid = mOnPaid;
        if (!IsFacilityAlive(facility))
        {
            LeavePaymentLine();
            onPaid?.Invoke();
            return;
        }

        facility.BeginService(Guarded(() =>
        {
            StopWait();
            LeavePaymentLine();
            onPaid?.Invoke();
        }), MakeVisitGuard());

        Wait(SERVICE_TIMEOUT_SECONDS, () =>
        {
            LeavePaymentLine();
            GiveUpPayment();
        });
    }

    // 파괴된 시설(Unity 오브젝트)은 인터페이스 참조로는 null로 보이지 않아 따로 확인한다.
    private static bool IsFacilityAlive(IPaymentFacility facility)
    {
        if (facility is Object unityObject)
            return unityObject != null;
        return facility != null;
    }

    // 줄이 꽉 찼을 때: 근처에서 서성이며 1초마다 자리를 확인한다. LINE_WAIT_MAX_SECONDS가 지나면 결제를 포기하고 나간다.
    private void WaitForLineRoom(Waypoint front, IPaymentFacility facility, System.Action onPaid)
    {
        var line = facility.GetLine(front);
        if (!mHasPointTarget && !mIsWaitingForLine)
        {
            mIsWaitingForLine = true;
            MoveToPoint(line.GetOverflowPosition(), () => PlayAnimation("Idle"));
        }

        // MoveToPoint가 대기 타이머를 지우므로 이동을 시작한 뒤에 타이머를 건다.
        Wait(LINE_RECHECK_SECONDS, () =>
        {
            mLineWaitSeconds += LINE_RECHECK_SECONDS;
            if (line.HasRoom)
            {
                mIsWaitingForLine = false;
                JoinPaymentLine(front, facility, onPaid);
                return;
            }

            if (mLineWaitSeconds >= LINE_WAIT_MAX_SECONDS)
            {
                mIsWaitingForLine = false;
                GiveUpPayment();
                return;
            }

            WaitForLineRoom(front, facility, onPaid);
        });
    }

    // 결제를 포기한 손님: 고른 디저트는 진열대로 돌려놓고(진열대가 사라졌으면 창고로) 결제 없이 퇴장한다.
    private void GiveUpPayment()
    {
        mLineWaitSeconds = 0f;
        ReturnCartToStand();
        LeaveStore();
    }

    private void LeavePaymentLine()
    {
        mPaymentLine?.Leave(this);
        mPaymentLine = null;
        mPaymentFront = null;
        mPaymentFacility = null;
        mOnPaid = null;
        mIsWaitingForLine = false;
        mLineWaitSeconds = 0f;
    }

    #endregion

    #region Leave

    // 결제 때 테라스 좌석을 예약했으면 그 테라스층으로 가서 자리에 앉았다가 나가고, 예약이 없으면 1층 출구로 퇴장한다.
    private void BeginGoTerraceOrExit()
    {
        if (mReservedSeat != null && mReservedSeatGroup != null)
        {
            var seat = mReservedSeat;
            TravelTo(mReservedSeatGroup, () => MoveTo(seat, SitOnSeat));
            return;
        }

        ReleaseTerraceSeat();
        LeaveStore();
    }

    // 예약한 좌석에 잠시 앉았다가 테라스 가구를 돌아다니며 이용한다. 좌석은 테라스를 떠날 때까지 잡아 둔다(좌석 수 = 수용 인원).
    private void SitOnSeat()
    {
        PlayAnimation("Idle");
        float lingerSeconds = Random.Range(mTerraceLingerMinSeconds, mTerraceLingerMaxSeconds);
        Wait(lingerSeconds, ContinueTerraceVisit);
    }

    // 다음에 이용할 테라스 가구를 고른다. 더 이용할 가구가 없으면 퇴장한다(손님 소지금 개념은 없음).
    private void ContinueTerraceVisit()
    {
        ReleaseTerraceUse();

        if (TryPickTerraceFurniture(out var target))
        {
            mTerraceUseTarget = target;
            // 가는 도중 가구가 철거되면 좌석을 쥔 채 나가지 않고 다른 가구를 다시 고른다.
            MoveTo(target, UseTerraceFurniture, ContinueTerraceVisit);
            return;
        }

        ReleaseTerraceSeat();
        ExitFromCurrentGroup();
    }

    private void UseTerraceFurniture()
    {
        PlayAnimation("Idle");

        var row = GetTerraceFurnitureRow(mTerraceUseTarget);
        int kind = row != null ? row.FurnitureType : -1;
        mTerraceUseCounts.TryGetValue(kind, out int count);
        mTerraceUseCounts[kind] = count + 1;

        // 이용을 마치면 수입 가구(자판기·포장마차)는 이용 수입을 정산하고 다음 가구로 간다.
        Wait(Random.Range(TERRACE_USE_MIN_SECONDS, TERRACE_USE_MAX_SECONDS), () =>
        {
            // 이용 중에 가구가 철거됐으면 수입은 없다.
            if (mTerraceUseTarget != null)
                ReceiveTerraceIncome(row);
            ContinueTerraceVisit();
        });
    }

    // 테라스 수입: FurnitureRow.UseIncome × 결제 수익 업그레이드 배율. 오늘 매출에 더하고 동전·만족 아이콘을 띄운다.
    private void ReceiveTerraceIncome(CTable.FurnitureRow row)
    {
        if (row == null || row.UseIncome <= 0)
            return;

        int gold = GameInstance.Model.Upgrade.ApplyGoldIncomeMultiplier(row.UseIncome);
        GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Add(gold);
        GameInstance.Model.Business.RecordTerraceSale(gold);
        LobbyUI?.PlayPaymentEffect(null);
    }

    // 후보: 이 테라스층의 상호작용 가구 중 이번 방문에 아직 안 쓴 것 + 다른 손님이 쓰고 있지 않은 것.
    // 무작위로 하나 골라, 그 종류를 n번 썼으면 TERRACE_REUSE_RATE^n 확률로 이용한다. 실패하면 그 종류를 빼고 다시 고른다.
    private bool TryPickTerraceFurniture(out Waypoint result)
    {
        result = null;
        var waypointManager = GameInstance.WayPoint;
        if (mCurrentGroup == null || !mCurrentGroup.IsTerraceZone || waypointManager == null)
            return false;

        mWaypointBuffer.Clear();
        mCurrentGroup.CollectTriggerWaypoints(Waypoint.eWaypointType.Trigger_TerraceUse, mWaypointBuffer);
        mWaypointBuffer.RemoveAll(wp => mUsedTerraceFurniture.Contains(wp) || waypointManager.IsWaypointReserved(wp));

        // 가구 종류별로 묶는다(종류 조회는 후보마다 한 번만).
        var byKind = new Dictionary<int, List<Waypoint>>();
        foreach (var wp in mWaypointBuffer)
        {
            int kind = GetTerraceFurnitureKind(wp);
            if (!byKind.TryGetValue(kind, out var list))
                byKind[kind] = list = new List<Waypoint>();
            list.Add(wp);
        }

        // 후보 하나를 고르는 것과 같은 비율이 되도록, 남은 후보 수에 비례해 종류를 고른다.
        while (byKind.Count > 0)
        {
            int total = 0;
            foreach (var list in byKind.Values)
                total += list.Count;

            int roll = Random.Range(0, total);
            int kind = 0;
            List<Waypoint> kindList = null;
            foreach (var pair in byKind)
            {
                if (roll < pair.Value.Count)
                {
                    kind = pair.Key;
                    kindList = pair.Value;
                    break;
                }
                roll -= pair.Value.Count;
            }

            mTerraceUseCounts.TryGetValue(kind, out int usedCount);
            if (Random.value < Mathf.Pow(TERRACE_REUSE_RATE, usedCount))
            {
                var pick = kindList[Random.Range(0, kindList.Count)];
                if (waypointManager.TryReserveWaypoint(pick, this))
                {
                    mUsedTerraceFurniture.Add(pick);
                    result = pick;
                    return true;
                }
            }

            byKind.Remove(kind);
        }

        return false;
    }

    // 가구 종류 = FurnitureRow.FurnitureType(테이블·자판기 등). 가구 정보를 못 찾으면 -1.
    private static int GetTerraceFurnitureKind(Waypoint waypoint)
    {
        var row = GetTerraceFurnitureRow(waypoint);
        return row != null ? row.FurnitureType : -1;
    }

    private static CTable.FurnitureRow GetTerraceFurnitureRow(Waypoint waypoint)
    {
        var placeable = waypoint != null ? waypoint.GetComponentInParent<PlaceableObject>() : null;
        return placeable != null ? GameInstance.Table.Get<CTable.FurnitureRow>(placeable.FurnitureTid) : null;
    }

    private void ReleaseTerraceUse()
    {
        // 철거된 가구(Unity에서 == null)여도 예약표에서 지워지도록 참조 자체로 확인한다(ReleaseWaypoint가 파괴된 키를 정리).
        if (!ReferenceEquals(mTerraceUseTarget, null) && GameInstance.WayPoint != null)
            GameInstance.WayPoint.ReleaseWaypoint(mTerraceUseTarget, this);
        mTerraceUseTarget = null;
    }

    private void ResetTerraceVisit()
    {
        ReleaseTerraceUse();
        mUsedTerraceFurniture.Clear();
        mTerraceUseCounts.Clear();
    }

    // 결제 시점(줄 맨 앞 손님이 결제를 마칠 때)에 테라스 입장 여부를 정하고 좌석을 예약한다.
    // 다음 단계가 테라스 or 퇴장일 때만, 일정 확률로 테라스를 원하고, 남은 좌석이 있어야 들어간다.
    private void TryReserveTerraceSeat()
    {
        if (mReservedSeat != null || GameInstance.WayPoint == null)
            return;

        if (mBehaviorQueue == null || mBehaviorQueue.Count == 0 || mBehaviorQueue.Peek() != eNpcBehaviorStepType.GoTerraceOrExit)
            return;

        if (Random.value >= TERRACE_CHANCE)
            return;

        if (GameInstance.WayPoint.TryReserveTerraceSeat(this, out var seat, out var group))
        {
            mReservedSeat = seat;
            mReservedSeatGroup = group;
        }
    }

    private void ReleaseTerraceSeat()
    {
        if (mReservedSeat != null && GameInstance.WayPoint != null)
            GameInstance.WayPoint.ReleaseTerraceSeat(mReservedSeat, this);
        mReservedSeat = null;
        mReservedSeatGroup = null;
    }

    // 매장층 손님은 1층(출입구 층)으로 돌아가 그 층 출구로 나간다.
    private void LeaveStore()
    {
        mBehaviorQueue?.Clear();

        if (mCurrentGroup != null && mCurrentGroup.IsTerraceZone)
        {
            ExitFromCurrentGroup();
            return;
        }

        TravelTo(mEntranceGroup, ExitFromCurrentGroup);
    }

    private void ExitFromCurrentGroup()
    {
        // 나가는 길에는 테라스 좌석·가구를 잡고 있지 않는다(다른 손님이 바로 쓸 수 있게).
        ReleaseTerraceSeat();
        ReleaseTerraceUse();

        if (mCurrentGroup != null && mCurrentGroup.TryGetRandomWaypoint(Waypoint.eWaypointCategoryType.Exit, Waypoint.eWaypointType.Exit_Min, out var exit))
        {
            MoveTo(exit, DespawnToPool);
            return;
        }

        DespawnToPool();
    }

    #endregion
}
