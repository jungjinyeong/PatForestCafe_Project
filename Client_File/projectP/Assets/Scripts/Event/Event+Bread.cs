namespace CEvent
{
    public class BreadPickup
    {
        public int tableId;
        public IBreadPickup breadPickup;

        public BreadPickup(int tableId, IBreadPickup breadPickup)
        {
            this.tableId = tableId;
            this.breadPickup = breadPickup;
        }
    }
}
