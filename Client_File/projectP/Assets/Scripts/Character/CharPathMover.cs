using System.Collections.Generic;
using UnityEngine;

public class CharPathMover : CharBase
{
    [Header("Movement")]
    [SerializeField] private float mMoveSpeed = 3f;
    [SerializeField] private float mArrivalThreshold = 0.1f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask mObstacleLayerMask;
    [SerializeField] private float mAvoidanceLookAhead = 0.5f;
    [SerializeField] private float mAvoidanceAngle = 45f;

    private List<Waypoint> mPath;
    private int mPathIndex;
    private bool mIsMoving;

    private Waypoint mCurrentWaypoint;
    public Waypoint CurrentWaypoint => mCurrentWaypoint;
    public bool IsMoving => mIsMoving;

    private void Update()
    {
        if (!mIsMoving || mPath == null || mPathIndex >= mPath.Count)
            return;

        MoveTowardsCurrentTarget();
    }

    public void Init(Waypoint startWaypoint)
    {
        mCurrentWaypoint = startWaypoint;
        mIsMoving = false;
        mPath = null;

        if (startWaypoint != null)
            transform.position = startWaypoint.transform.position;
    }

    public bool MoveTo(Waypoint destination)
    {
        if (mCurrentWaypoint == null || destination == null)
            return false;

        var path = WaypointPathfinder.FindPath(mCurrentWaypoint, destination);
        if (path == null)
        {
            mIsMoving = false;
            return false;
        }

        if (path.Count <= 1)
        {
            mIsMoving = false;
            return true;
        }

        mPath = path;
        mPathIndex = 1;
        mIsMoving = true;

        if (mAnimator2D != null)
            mAnimator2D.PlayAnimation("Run");

        return true;
    }

    public void Stop()
    {
        mIsMoving = false;
        mPath = null;
    }

    private void MoveTowardsCurrentTarget()
    {
        Transform target = mPath[mPathIndex].transform;
        Vector3 desiredDirection = (target.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= mArrivalThreshold)
        {
            transform.position = target.position;
            mCurrentWaypoint = mPath[mPathIndex];
            mPathIndex++;

            if (mPathIndex >= mPath.Count)
                OnReachedDestination();

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

    private void OnReachedDestination()
    {
        mIsMoving = false;
        mPath = null;

        if (mAnimator2D != null)
            mAnimator2D.PlayAnimation("Idle");
    }
}
