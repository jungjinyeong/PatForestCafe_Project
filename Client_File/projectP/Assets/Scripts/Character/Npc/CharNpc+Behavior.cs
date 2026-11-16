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
        if (!TryPickDessertStand(null, out var stand))
        {
            OnDessertUnavailable();
            return;
        }

        MoveTo(stand, () => OnArriveDessertStand(stand, false));
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
        return waypoint != null ? waypoint.GetComponentInParent<Intaraction_DessertStand>() : null;
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
    // - 그 밖(디저트만 세트): 잠시 둘러보다 땀방울을 띄우고 퇴장한다. 키오스크 이벤트(3단계)가 생기면 여기서 갈린다.
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
                LeaveStore();
            });
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

    private void SitOnSeat()
    {
        PlayAnimation("Idle");
        float lingerSeconds = Random.Range(mTerraceLingerMinSeconds, mTerraceLingerMaxSeconds);
        Wait(lingerSeconds, () =>
        {
            ReleaseTerraceSeat();
            ExitFromCurrentGroup();
        });
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
        if (mCurrentGroup != null && mCurrentGroup.TryGetRandomWaypoint(Waypoint.eWaypointCategoryType.Exit, Waypoint.eWaypointType.Exit_Min, out var exit))
        {
            MoveTo(exit, DespawnToPool);
            return;
        }

        DespawnToPool();
    }

    #endregion
}
