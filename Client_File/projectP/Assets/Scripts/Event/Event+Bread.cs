namespace CEvent
{
    public class BreadPickup
    {
        public int tableId;
        public CharNpc npc;

        public BreadPickup(int tableId, CharNpc npc)
        {
            this.tableId = tableId;
            this.npc = npc;
        }
    }
}
