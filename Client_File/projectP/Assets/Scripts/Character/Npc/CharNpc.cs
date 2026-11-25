using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

// 손님 NPC — 이동·층 이동·생애주기(이 파일), 행동 단계(CharNpc+Behavior), 결제(CharNpc+Payment).
// 흐름: 로비 1층 출입구에서 나타남 → 계단실로 목표 매장층 이동 → 행동 세트(NpcBehaviorRuleSet) 단계를 차례로 실행
// → 테라스에 들르거나 1층 출구로 퇴장. 이동은 MoveTo(목표, 도착 콜백) 하나로 처리한다.
public partial class CharNpc : CharBase
{
    private const float BLOCKED_PASS_THROUGH_SECONDS = 1f;

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

    [Header("Dessert Unavailable")]
    [SerializeField] private float mSweatDisplaySeconds = 1.2f;

    private bool mIsMoving;
    private float mBlockedSeconds;

    // 이번 방문 번호. 풀에서 재사용되면 바뀌므로, 늦게 도착한 콜백(카운터 대기열·연출 타이머)이 다음 손님에게 실행되지 않게 막는다.
    private int mVisitId;

    private WaypointGroup mEntranceGroup;
    private WaypointGroup mCurrentGroup;
    private Waypoint mCurrentWaypoint;
    private IDisposable mWaitDisposable;

    private List<Waypoint> mSubPath;
    private int mSubPathIndex;
    private Action mArriveAction;
    private Action mLostAction;
    // 웨이포인트가 아닌 지점(결제 대기 줄 칸 등)으로 곧장 걸어갈 때.
    private bool mHasPointTarget;
    private Vector3 mPointTarget;

    private Queue<eNpcBehaviorStepType> mBehaviorQueue;
    private LobbyCharUI mLobbyCharUI;

    public Waypoint CurrentWaypoint => mCurrentWaypoint;
    public bool IsMoving => mIsMoving;

    // SpawnManager가 Init 뒤에 붙이므로 처음 필요할 때 찾는다.
    private LobbyCharUI LobbyUI
    {
        get
        {
            if (mLobbyCharUI == null)
                mLobbyCharUI = GetComponentInChildren<LobbyCharUI>();
            return mLobbyCharUI;
        }
    }

    private void Update()
    {
        if (!mIsMoving) return;

        if (mHasPointTarget)
        {
            MoveTowardsPoint();
            return;
        }

        if (mSubPath == null || mSubPathIndex >= mSubPath.Count) return;

        MoveTowardsSubTarget();
    }

    // entrance: 로비 1층(나타나는 층), spawnPoint: 1층 출입구, targetGroup: 쇼핑할 매장층(1층일 수도 있음).
    public void Init(WaypointGroup entrance, Waypoint spawnPoint, WaypointGroup targetGroup)
    {
        mVisitId++;
        mBehaviorQueue = NpcBehaviorRuleSet.GetRandomQueue();
        mIsInKioskEvent = false;
        ResetCart();
        ResetTerraceVisit();

        mEntranceGroup = entrance;
        PlaceAt(entrance, spawnPoint);

        // 디저트를 사는 세트는 기획대로 「진열된 빵 테이블 기준」 — 열린 매장층 전체의 빵이 있는 진열대 중 하나를 골라 그 층으로 간다.
        // 빵이 어디에도 없으면 SpawnManager가 고른 무작위 층으로 간다(그 층에서 디저트 없음 분기).
        mPreferredStand = null;
        if (mBehaviorQueue.Count > 0 && mBehaviorQueue.Peek() == eNpcBehaviorStepType.BuyDessert
            && TryPickStockedStandOnAnyFloor(out var stand, out var standGroup))
        {
            mPreferredStand = stand;
            targetGroup = standGroup;
        }

        TravelTo(targetGroup, AdvanceBehaviorQueue);
    }

    // 영업 마감 대기 시간 초과 등으로 손님을 즉시 내보낸다. 고른 디저트는 결제 없이 진열대(없으면 창고)로 돌아간다.
    public void ForceLeave()
    {
        mBehaviorQueue?.Clear();
        DespawnToPool();
    }

    #region Movement

    // target까지 웨이포인트 경로로 걸어가 도착하면 onArrive를 부른다. 가는 도중 목표가 사라지면(가구 철거) onLost,
    // 없으면 다음 행동으로 넘어간다.
    private void MoveTo(Waypoint target, Action onArrive, Action onLost = null)
    {
        StopWait();

        if (target == null)
        {
            (onLost ?? AdvanceBehaviorQueue).Invoke();
            return;
        }

        mArriveAction = onArrive;
        mLostAction = onLost;
        mHasPointTarget = false;
        mIsMoving = true;
        mBlockedSeconds = 0f;
        PlayAnimation("Run");
        BeginSegmentTo(target);
    }

    // 웨이포인트가 아닌 지점까지 곧장 걸어간다(장애물 회피는 웨이포인트 이동과 같다). 도착하면 onArrive.
    private void MoveToPoint(Vector3 point, Action onArrive)
    {
        StopWait();

        mSubPath = null;
        // 웨이포인트를 떠나므로 다음 경로 탐색은 지금 위치에서 곧장 시작하게 한다(BeginSegmentTo의 null 처리).
        mCurrentWaypoint = null;
        mArriveAction = onArrive;
        mLostAction = null;
        mPointTarget = new Vector3(point.x, point.y, transform.position.z);
        mHasPointTarget = true;
        mIsMoving = true;
        mBlockedSeconds = 0f;
        PlayAnimation("Run");
    }

    private void MoveTowardsPoint()
    {
        Vector3 toTarget = mPointTarget - transform.position;
        if (toTarget.magnitude <= mArrivalThreshold)
        {
            transform.position = mPointTarget;
            mHasPointTarget = false;
            mIsMoving = false;

            var action = mArriveAction;
            mArriveAction = null;
            action?.Invoke();
            return;
        }

        StepTowards(toTarget.normalized);
    }

    private void MoveTowardsSubTarget()
    {
        // 가구 철거/리셋으로 목표(또는 경유) 웨이포인트가 파괴됐으면 이번 이동을 포기하고 다음 행동으로 넘어간다.
        if (mSubPath[mSubPathIndex] == null)
        {
            var lostAction = mLostAction;
            mSubPath = null;
            mArriveAction = null;
            mLostAction = null;
            mIsMoving = false;
            (lostAction ?? AdvanceBehaviorQueue).Invoke();
            return;
        }

        Transform target = mSubPath[mSubPathIndex].transform;
        Vector3 desiredDirection = (target.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= mArrivalThreshold)
        {
            transform.position = target.position;
            mSubPathIndex++;

            if (mSubPathIndex >= mSubPath.Count)
                OnArrived(mSubPath[mSubPath.Count - 1]);

            return;
        }

        StepTowards(desiredDirection);
    }

    // 한 프레임 이동. 앞·좌·우가 모두 막히면 잠시 멈췄다가(가구 사이에 끼는 경우) 일정 시간 뒤에는 장애물을 무시하고 곧장 간다.
    private void StepTowards(Vector3 desiredDirection)
    {
        Vector3 moveDirection = GetAvoidanceDirection(desiredDirection);
        if (moveDirection == Vector3.zero)
        {
            mBlockedSeconds += Time.deltaTime;
            if (mBlockedSeconds < BLOCKED_PASS_THROUGH_SECONDS)
                return;
            moveDirection = desiredDirection;
        }
        else
        {
            mBlockedSeconds = 0f;
        }

        if (mSpriteRenderer != null && moveDirection.x != 0)
            mSpriteRenderer.flipX = moveDirection.x < 0;

        transform.position += moveDirection * (mMoveSpeed * Time.deltaTime);
    }

    private void OnArrived(Waypoint arrived)
    {
        mCurrentWaypoint = arrived;
        mSubPath = null;
        mIsMoving = false;
        mLostAction = null;

        var action = mArriveAction;
        mArriveAction = null;
        action?.Invoke();
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
        // 서 있던 웨이포인트(철거된 가구의 손님 칸 등)가 파괴됐으면 경로 탐색 없이 목표로 곧장 간다(mSubPath[0]은 읽지 않음).
        if (mCurrentWaypoint == null)
        {
            mSubPath = new List<Waypoint> { null, target };
            mSubPathIndex = 1;
            return;
        }

        var path = WaypointPathfinder.FindPath(mCurrentWaypoint, target);

        mSubPath = (path == null || path.Count <= 1)
            ? new List<Waypoint> { mCurrentWaypoint, target }
            : path;

        mSubPathIndex = 1;
    }

    #endregion

    #region Floor Travel

    // 다른 층으로 갈 때: 지금 층 계단실까지 걸어가 → 목표 층 계단실에서 나온다(층 사이는 순간 이동).
    // 이미 그 층이면 바로 onArrived. 계단실이 없는 층은 WaypointGroup이 SpawnPoint로 대신 돌려준다.
    private void TravelTo(WaypointGroup target, Action onArrived)
    {
        if (target == null || target == mCurrentGroup)
        {
            onArrived?.Invoke();
            return;
        }

        var stairwell = mCurrentGroup != null ? mCurrentGroup.GetStairwellPoint() : null;
        if (stairwell == null)
        {
            PlaceAt(target, target.GetStairwellPoint());
            onArrived?.Invoke();
            return;
        }

        MoveTo(stairwell, () =>
        {
            PlaceAt(target, target.GetStairwellPoint());
            onArrived?.Invoke();
        });
    }

    private void PlaceAt(WaypointGroup group, Waypoint point)
    {
        mCurrentGroup = group;
        mCurrentWaypoint = point;
        mSubPath = null;
        mLostAction = null;
        mHasPointTarget = false;
        mIsMoving = false;

        if (point != null)
            transform.position = point.transform.position;
    }

    #endregion

    #region Lifecycle

    private void DespawnToPool()
    {
        mVisitId++;
        StopWait();
        mIsMoving = false;
        mSubPath = null;
        mHasPointTarget = false;
        mArriveAction = null;
        mLostAction = null;

        mIsInKioskEvent = false;
        LeavePaymentLine();
        ReleaseTerraceSeat();
        ResetTerraceVisit();

        // 결제하지 못하고 떠나는 손님(결제 시설이 없는 층, 영업 마감 강제 퇴장 등)의 디저트는 진열대(없으면 창고)로 돌려놓는다.
        ReturnCartToStand();

        var lobbyUI = LobbyUI;
        if (lobbyUI != null)
        {
            // 결제한 디저트·음료는 퇴장할 때까지 들고 있다가 여기서 끈다.
            lobbyUI.ClearDessertCart();
            lobbyUI.ClearDrink();
            lobbyUI.SetSweatIconActive(false);
            lobbyUI.SetKioskBubbleActive(false);
            lobbyUI.StopPaymentEffect();
        }

        if (GameInstance.Spawn != null)
            GameInstance.Spawn.ReturnToPool(this);
        else
            gameObject.SetActive(false);
    }

    // 이번 방문이 아직 진행 중인지(풀로 돌아갔다가 재사용된 손님이 아닌지).
    private Func<bool> MakeVisitGuard()
    {
        int visitId = mVisitId;
        return () => this != null && mVisitId == visitId && gameObject.activeInHierarchy;
    }

    private Action<T> Guarded<T>(Action<T> action)
    {
        var isValid = MakeVisitGuard();
        return value =>
        {
            if (isValid())
                action?.Invoke(value);
        };
    }

    // 이번 방문 중에만 action을 실행하도록 감싼다(연출 타이머·대기열 콜백용).
    private Action Guarded(Action action)
    {
        var isValid = MakeVisitGuard();
        return () =>
        {
            if (isValid())
                action?.Invoke();
        };
    }

    private void Wait(float seconds, Action onDone)
    {
        StopWait();
        mWaitDisposable = Observable.Timer(TimeSpan.FromSeconds(Mathf.Max(0f, seconds)))
            .Subscribe(_ =>
            {
                mWaitDisposable = null;
                onDone?.Invoke();
            })
            .AddTo(this);
    }

    private void StopWait()
    {
        mWaitDisposable?.Dispose();
        mWaitDisposable = null;
    }

    private void PlayAnimation(string name)
    {
        if (mAnimator2D != null)
            mAnimator2D.PlayAnimation(name);
    }

    #endregion
}
