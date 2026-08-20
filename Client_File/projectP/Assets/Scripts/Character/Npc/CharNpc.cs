using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.U2D;

public class CharNpc : CharBase, IBreadPickup
{
    [Header("Movement")]
    [SerializeField] private float mMoveSpeed = 3f;
    [SerializeField] private float mArrivalThreshold = 0.1f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask mObstacleLayerMask;
    [SerializeField] private float mAvoidanceLookAhead = 0.5f;
    [SerializeField] private float mAvoidanceAngle = 45f;

    [Header("Terrace")]
    [SerializeField] private float mTerraceLingerMinSeconds = 3f;
    [SerializeField] private float mTerraceLingerMaxSeconds = 6f;

    [Header("Bread Unavailable")]
    [SerializeField] private float mSweatDisplaySeconds = 1.2f;

    private bool mIsMoving = true;
    private bool mHasLingeredInTerrace;

    private WaypointGroup mCurrentGroup;
    private Waypoint mCurrentWaypoint;
    private Waypoint mPausedWaypoint;
    private IDisposable mPauseDisposable;

    private List<Waypoint> mSubPath;
    private int mSubPathIndex;

    private Queue<eNpcBehaviorStepType> mBehaviorQueue;

    public Waypoint CurrentWaypoint => mCurrentWaypoint;
    public bool IsMoving => mIsMoving;

    public LobbyCharUI GetLobbyCharUI => GetComponentInChildren<LobbyCharUI>();

    private void Update()
    {
        if (!mIsMoving || mSubPath == null || mSubPathIndex >= mSubPath.Count) return;

        MoveTowardsSubTarget();
    }

    public void Init(WaypointGroup group, Waypoint spawnPoint)
    {
        mBehaviorQueue = NpcBehaviorRuleSet.GetRandomQueue();
        mHasLingeredInTerrace = false;
        EnterGroup(group, spawnPoint);
    }

    private void EnterGroup(WaypointGroup group, Waypoint entryPoint)
    {
        mCurrentGroup = group;

        transform.position = entryPoint.transform.position;
        mCurrentWaypoint = entryPoint;
        mIsMoving = true;
        mSubPath = null;

        if (mAnimator2D != null)
            mAnimator2D.PlayAnimation("Run");

        OnReachedRouteWaypoint(entryPoint);
    }

    private void MoveTowardsSubTarget()
    {
        Transform target = mSubPath[mSubPathIndex].transform;
        Vector3 desiredDirection = (target.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= mArrivalThreshold)
        {
            transform.position = target.position;
            mSubPathIndex++;

            if (mSubPathIndex >= mSubPath.Count)
                OnReachedRouteWaypoint(mSubPath[mSubPath.Count - 1]);

            return;
        }

        Vector3 moveDirection = GetAvoidanceDirection(desiredDirection);
        if (moveDirection == Vector3.zero)
            return;

        if (mSpriteRenderer != null && moveDirection.x != 0)
            mSpriteRenderer.flipX = moveDirection.x < 0;

        transform.position += moveDirection * (mMoveSpeed * Time.deltaTime);
    }

    private Vector3 GetAvoidanceDirection(Vector3 desiredDirection)
    {
        if (!Physics2D.Raycast(transform.position, desiredDirection, mAvoidanceLookAhead, mObstacleLayerMask))
            return desiredDirection;

        Vector3 right = Quaternion.Euler(0f, 0f, -mAvoidanceAngle) * desiredDirection;
        Vector3 left = Quaternion.Euler(0f, 0f, mAvoidanceAngle) * desiredDirection;

        bool rightBlocked = Physics2D.Raycast(transform.position, right, mAvoidanceLookAhead, mObstacleLayerMask);
        if (!rightBlocked)
            return right.normalized;

        bool leftBlocked = Physics2D.Raycast(transform.position, left, mAvoidanceLookAhead, mObstacleLayerMask);
        if (!leftBlocked)
            return left.normalized;

        return Vector3.zero;
    }

    private void BeginSegmentTo(Waypoint target)
    {
        var path = WaypointPathfinder.FindPath(mCurrentWaypoint, target);

        mSubPath = (path == null || path.Count <= 1)
            ? new List<Waypoint> { mCurrentWaypoint, target }
            : path;

        mSubPathIndex = 1;
    }

    private void OnReachedRouteWaypoint(Waypoint arrived)
    {
        mCurrentWaypoint = arrived;

        bool isPaused = ProcessArrivalCategoryLogic(arrived);
        if (!isPaused)
            AdvanceBehaviorQueue();
    }

    private bool ProcessArrivalCategoryLogic(Waypoint arrived)
    {
        if (arrived == null)
            return false;

        var category = arrived.GetCategoryType();

        if (category == Waypoint.eWaypointCategoryType.Exit)
        {
            if (!TryMoveToNextGroup())
                DespawnToPool();
            return true;
        }

        if (category == Waypoint.eWaypointCategoryType.Trigger)
        {
            TriggerPause(arrived);
            return true;
        }

        return false;
    }

    // 손님 동선: 층(비-테라스) 방문을 마치면 항상 테라스로 향한다(순차 Order 체인이 아님).
    // 이미 테라스에 있다면 더 갈 곳이 없어 false(호출 측에서 디스폰 처리).
    private bool TryMoveToNextGroup()
    {
        if (mCurrentGroup == null || GameInstance.WayPoint == null || mCurrentGroup.IsTerraceZone)
            return false;

        var nextGroup = GameInstance.WayPoint.GetTerraceGroup();
        if (nextGroup == null)
            return false;

        var spawnPoint = nextGroup.GetSpawnPoint();
        if (spawnPoint == null)
            return false;

        EnterGroup(nextGroup, spawnPoint);
        return true;
    }

    private void DespawnToPool()
    {
        mIsMoving = false;

        mPauseDisposable?.Dispose();
        mPauseDisposable = null;
        mPausedWaypoint = null;

        GetComponentInChildren<LobbyCharUI>()?.ClearDrink();

        if (GameInstance.Spawn != null)
            GameInstance.Spawn.ReturnToPool(this);
        else
            gameObject.SetActive(false);
    }

    private void TriggerPause(Waypoint triggerWaypoint)
    {
        Logger.Log($"[CharNpc] TriggerPause: {triggerWaypoint.name} ({triggerWaypoint.WaypointType})");

        mIsMoving = false;
        mPausedWaypoint = triggerWaypoint;

        if (mAnimator2D != null)
            mAnimator2D.PlayAnimation("Idle");

        MessageBroker.Default.Publish(new CEvent.Waypoint(triggerWaypoint.WaypointType));

        if (triggerWaypoint.WaypointType == Waypoint.eWaypointType.Trigger_Bread)
        {
            bool hasStock = (GameInstance.Model.Bread.GetCount(triggerWaypoint.TableId)?.Value ?? 0) > 0;
            if (hasStock)
            {
                MessageBroker.Default.Publish(new CEvent.BreadPickup(triggerWaypoint.TableId, this));
            }
            else
            {
                BeginBreadUnavailableFlow();
                return;
            }
        }
        else if (triggerWaypoint.WaypointType == Waypoint.eWaypointType.Trigger_Order)
        {
            var staff = triggerWaypoint.GetComponentInParent<CharStaff>();
            if (staff != null)
                staff.BeginPickup(ProcessOrderPayment);
            else
                ProcessOrderPayment();
            return;
        }

        mPauseDisposable = Observable.Timer(TimeSpan.FromSeconds(1.5f))
            .Subscribe(_ => ResumeFromPause())
            .AddTo(this);
    }

    // 진열대에 빵 재고가 없으면(BreadModel.GetCount == 0) 잠시 더 둘러보다가(Idle 대기, eConfigType.BreadUnavailablePauseMs)
    // 땀방울 아이콘을 띄운 뒤, 남은 행동 큐(예: 음료 주문)를 포기하고 곧장 매장을 퇴장한다.
    private void BeginBreadUnavailableFlow()
    {
        float lookAroundSeconds = GameInstance.Config.GetValue(eConfigType.BreadUnavailablePauseMs) / 1000f;

        mPauseDisposable = Observable.Timer(TimeSpan.FromSeconds(lookAroundSeconds))
            .Subscribe(_ => ShowSweatThenExit())
            .AddTo(this);
    }

    private void ShowSweatThenExit()
    {
        var lobbyCharUI = GetComponentInChildren<LobbyCharUI>();
        lobbyCharUI?.SetSweatIconActive(true);

        mPauseDisposable = Observable.Timer(TimeSpan.FromSeconds(mSweatDisplaySeconds))
            .Subscribe(_ => ExitEarlyDueToNoBread(lobbyCharUI))
            .AddTo(this);
    }

    private void ExitEarlyDueToNoBread(LobbyCharUI lobbyCharUI)
    {
        lobbyCharUI?.SetSweatIconActive(false);

        mBehaviorQueue?.Clear();
        ResumeFromPause();
    }

    private void ProcessOrderPayment()
    {
        TryReceiveBreadGold();
        ReceiveRandomUnlockedDrinkGold();

        var lobbyCharUI = GetComponentInChildren<LobbyCharUI>();
        if (lobbyCharUI == null)
        {
            ResumeFromPause();
            return;
        }

        lobbyCharUI.PlayPaymentEffect(ResumeFromPause);
    }

    private void ResumeFromPause()
    {
        mPauseDisposable?.Dispose();
        mPauseDisposable = null;

        mPausedWaypoint = null;
        mIsMoving = true;
        AdvanceBehaviorQueue();
    }

    private void TryReceiveBreadGold()
    {
        var lobbyUI = GetComponentInChildren<LobbyCharUI>();
        if (lobbyUI == null || !lobbyUI.HasBread) return;

        foreach (var bread in lobbyUI.DetachAllBreads())
        {
            var breadData = GameInstance.Model.Bread.Get(bread.TableId);
            if (breadData?.MenuItemRow != null)
            {
                int gold = GameInstance.Model.Upgrade.ApplyGoldIncomeMultiplier((int)breadData.MenuItemRow.Price);
                GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Add(gold);
            }

            bread.Despawn();
        }
    }

    // 레시피북으로 해금된 레시피(+기본 음료) 중 하나를 DrinkRow.Weight 가중치에 따라 무작위로
    // "구매"한 것으로 취급해 정산한다. Weight가 클수록 더 자주 선택된다(0 이하는 추첨 제외).
    private void ReceiveRandomUnlockedDrinkGold()
    {
        var drinkModel = GameInstance.Model.Drink;
        var unlockedTids = new List<int>(GameInstance.Model.RecipeBook.GetDiscoveredTids());

        if (drinkModel.DefaultDrink != null && !unlockedTids.Contains(drinkModel.DefaultDrink.TId))
            unlockedTids.Add(drinkModel.DefaultDrink.TId);

        DrinkData purchasedDrink = null;
        int totalWeight = 0;

        foreach (var tid in unlockedTids)
        {
            var drinkData = drinkModel.Get(tid);
            int weight = drinkData?.Row?.Weight ?? 0;
            if (weight <= 0) continue;

            totalWeight += weight;
            if (UnityEngine.Random.Range(0, totalWeight) < weight)
                purchasedDrink = drinkData;
        }

        if (purchasedDrink?.MenuItemRow == null) return;

        int gold = GameInstance.Model.Upgrade.ApplyGoldIncomeMultiplier((int)purchasedDrink.MenuItemRow.Price);
        GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Add(gold);

        var lobbyCharUI = GetComponentInChildren<LobbyCharUI>();
        lobbyCharUI?.SetDrinkSprite(LoadDrinkSprite(purchasedDrink.Row));
    }

    // DrinkRow.Atlas/Icon(스프라이트 아틀라스 주소 + 아틀라스 내 스프라이트 이름)로 손에 들 음료 스프라이트를 조회한다.
    private static Sprite LoadDrinkSprite(CTable.DrinkRow drinkRow)
    {
        if (drinkRow == null || string.IsNullOrEmpty(drinkRow.Atlas) || string.IsNullOrEmpty(drinkRow.Icon))
            return null;

        var atlas = GameInstance.Resource.LoadSync<SpriteAtlas>(drinkRow.Atlas);
        return atlas != null ? atlas.GetSprite(drinkRow.Icon) : null;
    }

    // 큐에서 다음 행동 규칙을 하나씩 꺼내 목표 웨이포인트를 찾고 그쪽으로 이동을 시작한다.
    // 목표를 찾지 못하면(해당 가구가 아직 존에 배치되지 않은 경우 등) 다음 규칙으로 넘어가고,
    // 큐가 모두 비면 존의 Exit 웨이포인트로 향해 퇴장한다.
    private void AdvanceBehaviorQueue()
    {
        if (mAnimator2D != null)
            mAnimator2D.PlayAnimation("Run");

        while (mBehaviorQueue != null && mBehaviorQueue.Count > 0)
        {
            var step = mBehaviorQueue.Dequeue();

            if (TryGetTargetForStep(step, out var target))
            {
                BeginSegmentTo(target);
                return;
            }
        }

        if (mCurrentGroup != null && mCurrentGroup.IsTerraceZone && !mHasLingeredInTerrace)
        {
            LingerInTerrace();
            return;
        }

        if (mCurrentGroup != null && mCurrentGroup.TryGetRandomWaypoint(Waypoint.eWaypointCategoryType.Exit, null, out var exit))
        {
            BeginSegmentTo(exit);
            return;
        }

        DespawnToPool();
    }

    // 테라스 존(IsTerraceZone) 도착 시 행동 큐가 소진되면 곧장 Exit로 나가지 않고 한 번만 잠시 머무른다.
    // mHasLingeredInTerrace로 재입장/재확인 시 중복 실행을 막는다.
    private void LingerInTerrace()
    {
        mHasLingeredInTerrace = true;
        mIsMoving = false;

        if (mAnimator2D != null)
            mAnimator2D.PlayAnimation("Idle");

        float lingerSeconds = UnityEngine.Random.Range(mTerraceLingerMinSeconds, mTerraceLingerMaxSeconds);

        mPauseDisposable = Observable.Timer(TimeSpan.FromSeconds(lingerSeconds))
            .Subscribe(_ => ResumeFromTerraceLinger())
            .AddTo(this);
    }

    private void ResumeFromTerraceLinger()
    {
        mPauseDisposable?.Dispose();
        mPauseDisposable = null;

        mIsMoving = true;
        AdvanceBehaviorQueue();
    }

    private bool TryGetTargetForStep(eNpcBehaviorStepType step, out Waypoint target)
    {
        target = null;
        if (mCurrentGroup == null) return false;

        switch (step)
        {
            case eNpcBehaviorStepType.BuyBread:
                return mCurrentGroup.TryGetRandomWaypoint(Waypoint.eWaypointCategoryType.Trigger, Waypoint.eWaypointType.Trigger_Bread, out target);
            case eNpcBehaviorStepType.OrderDrink:
                return mCurrentGroup.TryGetRandomWaypoint(Waypoint.eWaypointCategoryType.Trigger, Waypoint.eWaypointType.Trigger_Order, out target);
            case eNpcBehaviorStepType.ExitToTerrace:
                return mCurrentGroup.TryGetRandomWaypoint(Waypoint.eWaypointCategoryType.Exit, null, out target);
            default:
                return false;
        }
    }
}
