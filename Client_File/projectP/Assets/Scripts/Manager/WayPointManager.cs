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

        MessageBroker.Default.Publish(new CEvent.WaypointGroupRegist(mWaypointGroups.ToArray()));
    }
}
