namespace CEvent
{
    public class BreadPickup
    {
        public int tableId;
        public WaypointNPC npc;

        public BreadPickup(int tableId, WaypointNPC npc)
        {
            this.tableId = tableId;
            this.npc = npc;
        }
    }
}
