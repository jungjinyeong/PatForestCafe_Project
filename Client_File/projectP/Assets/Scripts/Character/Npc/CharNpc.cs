using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Sirenix.OdinInspector;

public class CharNpc : CharBase, ISpecialOrderWaiter, IBreadPickup
{
    [Header("Waypoints")]
    [SerializeField] private bool mIsInit = false;
    [SerializeField] private Waypoint[] mWaypoints;

    [Header("Movement")]
    [SerializeField] private float mMoveSpeed = 3f;
    [SerializeField] private float mArrivalThreshold = 0.1f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask mObstacleLayerMask;
    [SerializeField] private float mAvoidanceLookAhead = 0.5f;
    [SerializeField] private float mAvoidanceAngle = 45f;

    [Header("Loop")]
    [SerializeField] private bool mLoop = true;
    [SerializeField] private bool mReverseOnEnd = false;

    [Header("Random Stop (Trigger_Bread)")]
    [SerializeField] [Range(0f, 1f)] private float mBreadStopChance = 0.5f;

    [Header("Gizmo")]
    [SerializeField] private Color mPathColor = Color.green;

    private int mCurrentIndex = 0;
    private bool mMovingForward = true;
    private bool mIsMoving = true;

    private WaypointGroup mCurrentGroup;
    private Waypoint mCurrentWaypoint;
    private Waypoint mPausedWaypoint;
    private IDisposable mPauseDisposable;

    private List<Waypoint> mSubPath;
    private int mSubPathIndex;

    public Waypoint CurrentWaypoint => mCurrentWaypoint;
    public bool IsMoving => mIsMoving;

    public bool IsWaitingSpecialOrder =>
        mPausedWaypoint != null && mPausedWaypoint.WaypointType == Waypoint.eWaypointType.Wait_SpecialOrder;

    public LobbyCharUI GetLobbyCharUI => GetComponentInChildren<LobbyCharUI>();

    private void Start()
    {
        if (mIsInit == false) return;

        Init(mWaypoints);
    }

    private void Update()
    {
        if (!mIsMoving || mSubPath == null || mSubPathIndex >= mSubPath.Count) return;

        MoveTowardsSubTarget();
    }

    public void Init(WaypointGroup group, Waypoint[] waypoints)
    {
        mCurrentGroup = group;
        Init(waypoints);
    }

    public void Init(Waypoint[] waypoints)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            mIsMoving = false;
            return;
        }

        mWaypoints = waypoints;

        transform.position = mWaypoints[0].transform.position;
        mCurrentWaypoint = mWaypoints[0];
        mCurrentIndex = 0;
        mMovingForward = true;
        mIsMoving = true;
        mSubPath = null;

        if (mAnimator2D != null)
            mAnimator2D.PlayAnimation("Run");

        OnReachedRouteWaypoint(mWaypoints[0]);
    }

    public void ResumeFromSpecialOrderWait()
    {
        if (!IsWaitingSpecialOrder)
            return;

        GetComponentInChildren<LobbyCharUI>()?.SetSpecialOrderActive(false);
        ResumeFromPause();
    }

    [Button("ReStart", ButtonSizes.Large)]
    public void ResetPath()
    {
        if (mWaypoints == null || mWaypoints.Length == 0) return;

        mCurrentIndex = 0;
        mMovingForward = true;
        mIsMoving = true;
        transform.position = mWaypoints[0].transform.position;
        mCurrentWaypoint = mWaypoints[0];
        mSubPath = null;
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
                OnReachedRouteWaypoint(mWaypoints[mCurrentIndex]);

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
            AdvanceToNextWaypoint();
    }

    private bool ProcessArrivalCategoryLogic(Waypoint arrived)
    {
        if (arrived == null)
            return false;

        var category = arrived.GetCategoryType();

        if (category == Waypoint.eWaypointCategoryType.SpwanPoint)
        {
            ApplySpecialOrderParam();
        }

        if (category == Waypoint.eWaypointCategoryType.Exit)
        {
            if (!TryMoveToNextGroup())
                mIsMoving = false;
            return true;
        }

        if (category == Waypoint.eWaypointCategoryType.Trigger)
        {
            bool shouldStop = arrived.WaypointType == Waypoint.eWaypointType.Trigger_Bread
                ? UnityEngine.Random.value < mBreadStopChance
                : true;

            if (shouldStop)
            {
                TriggerPause(arrived);
                return true;
            }
        }

        if (category == Waypoint.eWaypointCategoryType.Wait)
        {
            bool shouldStop = CanEnterWait(arrived);

            if (shouldStop)
            {
                TriggerPause(arrived);
                return true;
            }
        }

        return false;
    }

    private void ApplySpecialOrderParam()
    {
        if (GameInstance.Spawn == null) return;

        var lobbyCharUI = GetComponentInChildren<LobbyCharUI>();
        if (lobbyCharUI == null) return;

        bool isSpecialOrder = GameInstance.Spawn.DecideSpecialOrder(mCurrentGroup);
        int desiredDrinkTid = isSpecialOrder ? GameInstance.Model.Drink.GetRandomSpecialOrderTid() : -1;

        if (desiredDrinkTid < 0)
            isSpecialOrder = false;

        lobbyCharUI.SetParam(new LobbyCharUI.Param
        {
            IsSpecialOrder = isSpecialOrder,
            DesiredDrinkTid = desiredDrinkTid,
        });
    }

    private bool CanEnterWait(Waypoint waypoint)
    {
        switch (waypoint.WaypointType)
        {
            case Waypoint.eWaypointType.Wait_SpecialOrder:
                return TryEnterSpecialOrderWait();
            default:
                return false;
        }
    }

    private bool TryEnterSpecialOrderWait()
    {
        if (mCurrentGroup == null || !mCurrentGroup.IsSpecialOrderZone)
            return false;

        var lobbyCharUI = GetComponentInChildren<LobbyCharUI>();
        return lobbyCharUI != null && lobbyCharUI.IsSpecialOrderActive;
    }

    private bool TryMoveToNextGroup()
    {
        if (mCurrentGroup == null || GameInstance.WayPoint == null)
            return false;

        var nextGroup = GameInstance.WayPoint.GetNextGroup(mCurrentGroup.Order);
        if (nextGroup == null)
            return false;

        var spawnPoint = nextGroup.GetSpawnPoint();
        if (spawnPoint == null)
            return false;

        var pathWaypoints = nextGroup.GetPathWaypoints();
        var waypoints = new Waypoint[1 + pathWaypoints.Length];
        waypoints[0] = spawnPoint;
        for (int i = 0; i < pathWaypoints.Length; i++)
            waypoints[i + 1] = pathWaypoints[i];

        Init(nextGroup, waypoints);
        return true;
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
            MessageBroker.Default.Publish(new CEvent.BreadPickup(triggerWaypoint.TableId, this));
        else if (triggerWaypoint.WaypointType == Waypoint.eWaypointType.Trigger_Order)
        {
            TryReceiveBreadGold();
            ReceiveDefaultDrinkGold();
        }

        // Wait_SpecialOrder는 자동으로 재개되지 않고, 컨펌 버튼(ResumeFromSpecialOrderWait)으로만 재개된다.
        if (triggerWaypoint.WaypointType == Waypoint.eWaypointType.Wait_SpecialOrder)
            return;

        mPauseDisposable = Observable.Timer(TimeSpan.FromSeconds(1.5f))
            .Subscribe(_ => ResumeFromPause())
            .AddTo(this);
    }

    private void ResumeFromPause()
    {
        mPauseDisposable?.Dispose();
        mPauseDisposable = null;

        mPausedWaypoint = null;
        mIsMoving = true;
        AdvanceToNextWaypoint();
    }

    private void TryReceiveBreadGold()
    {
        var lobbyUI = GetComponentInChildren<LobbyCharUI>();
        if (lobbyUI == null || !lobbyUI.HasBread) return;

        foreach (var bread in lobbyUI.DetachAllBreads())
        {
            var breadData = GameInstance.Model.Bread.Get(bread.TableId);
            if (breadData?.MenuItemRow != null)
                GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Add((int)breadData.MenuItemRow.Price);

            bread.Despawn();
        }
    }

    private void ReceiveDefaultDrinkGold()
    {
        var drinkData = GameInstance.Model.Drink.DefaultDrink;
        if (drinkData?.MenuItemRow == null) return;

        GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Add((int)drinkData.MenuItemRow.Price);
    }

    private void AdvanceToNextWaypoint()
    {
        if (mAnimator2D != null)
            mAnimator2D.PlayAnimation("Run");

        if (mMovingForward)
        {
            if (mCurrentIndex < mWaypoints.Length - 1)
                mCurrentIndex++;
            else
                OnReachedEnd();
        }
        else
        {
            if (mCurrentIndex > 0)
                mCurrentIndex--;
            else
                OnReachedStart();
        }

        if (!mIsMoving) return;

        BeginSegmentTo(mWaypoints[mCurrentIndex]);
    }

    private void OnReachedEnd()
    {
        if (mReverseOnEnd)
        {
            mMovingForward = false;
            mCurrentIndex--;
        }
        else if (mLoop)
        {
            mCurrentIndex = 0;
        }
        else
        {
            mIsMoving = false;
        }
    }

    private void OnReachedStart()
    {
        if (mReverseOnEnd)
        {
            mMovingForward = true;
            mCurrentIndex++;
        }
        else if (mLoop)
        {
            mCurrentIndex = mWaypoints.Length - 1;
        }
        else
        {
            mIsMoving = false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (mWaypoints == null || mWaypoints.Length < 2) return;

        Gizmos.color = mPathColor;
        for (int i = 0; i < mWaypoints.Length - 1; i++)
        {
            if (mWaypoints[i] != null && mWaypoints[i + 1] != null)
            {
                Gizmos.DrawLine(mWaypoints[i].transform.position, mWaypoints[i + 1].transform.position);

                Vector3 mid = (mWaypoints[i].transform.position + mWaypoints[i + 1].transform.position) * 0.5f;
                Vector3 dir = (mWaypoints[i + 1].transform.position - mWaypoints[i].transform.position).normalized;
                DrawArrow(mid, dir, 0.3f);
            }
        }

        if (mLoop && mWaypoints[mWaypoints.Length - 1] != null && mWaypoints[0] != null)
        {
            Gizmos.color = new Color(mPathColor.r, mPathColor.g, mPathColor.b, 0.4f);
            Gizmos.DrawLine(mWaypoints[mWaypoints.Length - 1].transform.position, mWaypoints[0].transform.position);
        }
    }

    private void DrawArrow(Vector3 position, Vector3 direction, float size)
    {
        if (direction == Vector3.zero) return;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
        Vector3 left  = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
        Gizmos.DrawRay(position, right * size);
        Gizmos.DrawRay(position, left * size);
    }
#endif
}
