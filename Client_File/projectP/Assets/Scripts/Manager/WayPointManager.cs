using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class WayPointManager : MonoBehaviour
{
    private readonly List<WaypointGroup> mWaypointGroups = new List<WaypointGroup>();

    public IReadOnlyList<WaypointGroup> WaypointGroups => mWaypointGroups;

    public void Init()
    {
        mWaypointGroups.Clear();
    }

    public void RegisterWaypointGroup(WaypointGroup group)
    {
        if (group == null || mWaypointGroups.Contains(group))
            return;

        mWaypointGroups.Add(group);
        mWaypointGroups.Sort((a, b) => a.Order.CompareTo(b.Order));

        MessageBroker.Default.Publish(new CEvent.WaypointGroupRegist(mWaypointGroups.ToArray()));
    }

    public WaypointGroup GetFirstGroup()
    {
        WaypointGroup first = null;
        foreach (var group in mWaypointGroups)
        {
            if (group == null)
                continue;

            if (first == null || group.Order < first.Order)
                first = group;
        }
        return first;
    }

    public WaypointGroup GetNextGroup(int currentOrder)
    {
        WaypointGroup next = null;
        foreach (var group in mWaypointGroups)
        {
            if (group == null || group.Order <= currentOrder)
                continue;

            if (next == null || group.Order < next.Order)
                next = group;
        }
        return next;
    }
}
