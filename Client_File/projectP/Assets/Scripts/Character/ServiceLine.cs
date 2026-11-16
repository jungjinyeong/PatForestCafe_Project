using System;
using System.Collections.Generic;
using UnityEngine;

// 결제 시설(카운터 직원·키오스크) 앞의 손님 줄. 0번 칸 = 시설 앞(결제 중이거나 결제하러 가는 손님), 1번부터 대기 칸.
// 대기 칸이 꽉 차면(MAX_WAITING) 더 받지 않는다 — 손님은 근처에서 기다리다 자리가 나면 다시 줄에 선다(CharNpc).
// 앞 손님이 빠지면 뒤 손님들의 칸 번호가 하나씩 당겨지고, 각 손님에게 새 칸 번호를 알려 준다.
public class ServiceLine
{
    // 결제 중인 손님을 뺀 최대 대기 인원(기획: 카운터 최대 대기 인원 — 임시 상수).
    public const int MAX_WAITING = 3;

    private struct Member
    {
        public object Owner;
        public Action<int> OnSlotChanged;
    }

    private readonly List<Member> mMembers = new List<Member>();
    private readonly Transform mFront;
    private readonly Vector3 mSlotStep;
    private Vector3 mLastFrontPosition;

    public bool HasRoom => mMembers.Count < MAX_WAITING + 1;

    // front: 시설 앞 칸(결제 웨이포인트) — 시설이 옮겨져도 칸 위치가 따라가도록 매번 현재 위치를 읽는다.
    // slotStep: 대기 칸 사이 간격·방향(보통 시설에서 멀어지는 아래쪽).
    public ServiceLine(Transform front, Vector3 slotStep)
    {
        mFront = front;
        mSlotStep = slotStep;
        mLastFrontPosition = front != null ? front.position : Vector3.zero;
    }

    // 줄에 선다. 이미 서 있으면 지금 칸을 돌려준다. onSlotChanged: 앞 손님이 빠져 칸이 당겨질 때 새 칸 번호로 불린다.
    public bool TryJoin(object owner, Action<int> onSlotChanged, out int slot)
    {
        slot = IndexOf(owner);
        if (slot >= 0)
            return true;

        if (!HasRoom)
            return false;

        mMembers.Add(new Member { Owner = owner, OnSlotChanged = onSlotChanged });
        slot = mMembers.Count - 1;
        return true;
    }

    // 결제를 마쳤거나 떠난 손님을 줄에서 뺀다. 뒤에 있던 손님들은 한 칸씩 당겨진다.
    public void Leave(object owner)
    {
        int index = IndexOf(owner);
        if (index < 0)
            return;

        mMembers.RemoveAt(index);

        // 알림 중에 다른 손님이 줄을 떠나 목록이 바뀔 수 있어 복사본으로 돈다.
        var shifted = mMembers.GetRange(index, mMembers.Count - index);
        foreach (var member in shifted)
        {
            int newSlot = IndexOf(member.Owner);
            if (newSlot >= 0)
                member.OnSlotChanged?.Invoke(newSlot);
        }
    }

    public Vector3 GetSlotPosition(int slot)
    {
        if (mFront != null)
            mLastFrontPosition = mFront.position;
        return mLastFrontPosition + mSlotStep * Mathf.Max(0, slot);
    }

    // 줄이 꽉 찼을 때 기다리는 자리: 맨 끝 대기 칸 한 칸 뒤에서 옆으로 조금씩 흩어진다.
    public Vector3 GetOverflowPosition()
    {
        var side = new Vector3(-mSlotStep.y, mSlotStep.x, 0f).normalized;
        return GetSlotPosition(MAX_WAITING + 1) + side * UnityEngine.Random.Range(-0.6f, 0.6f);
    }

    private int IndexOf(object owner)
    {
        for (int i = 0; i < mMembers.Count; i++)
        {
            if (ReferenceEquals(mMembers[i].Owner, owner))
                return i;
        }
        return -1;
    }
}

// 시설 하나가 가진 결제 줄 목록 — 결제 웨이포인트(front)마다 줄이 따로 선다(직원 한 명이 카운터 두 곳을 맡는 경우 등).
public class ServiceLineSet
{
    private readonly Dictionary<Waypoint, ServiceLine> mLines = new Dictionary<Waypoint, ServiceLine>();
    private readonly Vector3 mSlotStep;

    public ServiceLineSet(Vector3 slotStep)
    {
        mSlotStep = slotStep;
    }

    public ServiceLine Get(Waypoint front)
    {
        if (!mLines.TryGetValue(front, out var line))
        {
            line = new ServiceLine(front != null ? front.transform : null, mSlotStep);
            mLines[front] = line;
        }
        return line;
    }
}

// 줄을 세우고 한 명씩 결제를 처리하는 시설(카운터 직원 CharStaff, 키오스크 Intaraction_Kiosk).
public interface IPaymentFacility
{
    // front: 손님이 결제하러 서는 웨이포인트. 웨이포인트마다 줄이 따로 있다.
    ServiceLine GetLine(Waypoint front);

    // 0번 칸에 도착한 손님의 결제 처리(게이지). isValid: 처리 중 손님이 떠났는지 확인.
    void BeginService(Action onComplete, Func<bool> isValid);
}
