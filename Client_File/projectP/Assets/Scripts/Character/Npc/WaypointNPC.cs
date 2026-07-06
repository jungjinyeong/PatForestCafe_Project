using System;
using UniRx;
using UnityEngine;
using Sirenix.OdinInspector;

public class WaypointNPC : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private bool mIsInit = false;
    [SerializeField] private Waypoint[] mWaypoints;

    [Header("Movement")]
    [SerializeField] private float mMoveSpeed = 3f;
    [SerializeField] private float mArrivalThreshold = 0.1f;

    [Header("Loop")]
    [SerializeField] private bool mLoop = true;
    [SerializeField] private bool mReverseOnEnd = false;

    [Header("Random Stop (Trigger_Bread)")]
    [SerializeField] [Range(0f, 1f)] private float mBreadStopChance = 0.5f;

    [Header("NPC Separation")]
    [SerializeField] private float mSeparationDistance = 0.3f;

    [Header("Gizmo")]
    [SerializeField] private Color mPathColor = Color.green;

    private int mCurrentIndex = 0;
    private bool mMovingForward = true;
    private bool mIsMoving = true;

    private Animator2D mAnimator2D;
    private SpriteRenderer mSpriteRenderer;

    private void Start()
    {
        if (mIsInit == false) return;

        Init(mWaypoints);
    }

    private void Update()
    {
        if (!mIsMoving || mWaypoints == null || mWaypoints.Length == 0) return;

        MoveTowardsTarget();
    }

    public void Init(Waypoint[] waypoints)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            mIsMoving = false;
            return;
        }

        mAnimator2D = GetComponentInChildren<Animator2D>();
        mSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        mWaypoints = waypoints;

        transform.position = mWaypoints[0].transform.position;
        mCurrentIndex = 0;
        mMovingForward = true;
        mIsMoving = true;
        SetNextTarget();

        if (mAnimator2D != null)
            mAnimator2D.PlayAnimation("Run");
    }

    private void MoveTowardsTarget()
    {
        Transform target = mWaypoints[mCurrentIndex].transform;
        Vector3 direction = (target.position - transform.position).normalized;
        float distanceThisFrame = mMoveSpeed * Time.deltaTime;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= mArrivalThreshold)
        {
            transform.position = target.position;
            AdvanceWaypoint();
            return;
        }

        if (GameInstance.Spawn != null && GameInstance.Spawn.IsBlockedByNPC(this, direction, mSeparationDistance))
            return;

        if (mSpriteRenderer != null && direction.x != 0)
            mSpriteRenderer.flipX = direction.x < 0;

        transform.position += direction * distanceThisFrame;
    }

    private void SetNextTarget()
    {
        if (mWaypoints == null || mWaypoints.Length == 0) return;
        mCurrentIndex = Mathf.Clamp(mCurrentIndex, 0, mWaypoints.Length - 1);
    }

    private void AdvanceWaypoint()
    {
        var arrived = mWaypoints[mCurrentIndex];
        if (arrived != null)
        {
            var category = arrived.GetCategoryType();

            if (category == Waypoint.eWaypointCategoryType.Exit)
            {
                ReturnToPool();
                return;
            }

            if (category == Waypoint.eWaypointCategoryType.Trigger)
            {
                bool shouldStop = arrived.WaypointType == Waypoint.eWaypointType.Trigger_Bread
                    ? UnityEngine.Random.value < mBreadStopChance
                    : true;

                if (shouldStop)
                {
                    TriggerPause(arrived);
                    return;
                }
            }
        }

        MoveToNextWaypoint();
    }

    private void ReturnToPool()
    {
        mIsMoving = false;
        GameInstance.Spawn?.ReturnToPool(this);
    }

    private void TriggerPause(Waypoint triggerWaypoint)
    {
        mIsMoving = false;

        if (mAnimator2D != null)
            mAnimator2D.PlayAnimation("Idle");

        MessageBroker.Default.Publish(new CEvent.Waypoint(triggerWaypoint.WaypointType));

        if (triggerWaypoint.WaypointType == Waypoint.eWaypointType.Trigger_Bread)
            MessageBroker.Default.Publish(new CEvent.BreadPickup(triggerWaypoint.TableId, this));

        Observable.Timer(TimeSpan.FromSeconds(1.5f))
            .Subscribe(_ =>
            {
                mIsMoving = true;
                MoveToNextWaypoint();
            })
            .AddTo(this);
    }

    private void MoveToNextWaypoint()
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

    [Button("ReStart", ButtonSizes.Large)]
    public void ResetPath()
    {
        if (mWaypoints == null || mWaypoints.Length == 0) return;
        mCurrentIndex = 0;
        mMovingForward = true;
        mIsMoving = true;
        transform.position = mWaypoints[0].transform.position;
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
