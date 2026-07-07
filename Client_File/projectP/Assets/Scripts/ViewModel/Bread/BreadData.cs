


public class BreadData
{
    public int TId { get; private set; }
    public int Count { get; private set; }
    public BreadData(int count)
    {

        Count = count;
    }
    public void UpdateCount(int count)
    {
        Count = count;
    }
}