
using static Waypoint;

namespace CEvent
{
    public class Waypoint
    {
        public eWaypointType type;

        public Waypoint(eWaypointType type)
        {
            this.type = type;
        }
    }

    public class WaypointGroupRegist
    {
        public WaypointGroup[] waypointGroups;

        public WaypointGroupRegist(WaypointGroup[] waypointGroups)
        {
            this.waypointGroups = waypointGroups;
        }
    }
}
