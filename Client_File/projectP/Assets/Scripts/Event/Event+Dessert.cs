namespace CEvent
{
    public class DessertPickup
    {
        public int tableId;
        public IDessertPickup dessertPickup;

        public DessertPickup(int tableId, IDessertPickup dessertPickup)
        {
            this.tableId = tableId;
            this.dessertPickup = dessertPickup;
        }
    }
}
