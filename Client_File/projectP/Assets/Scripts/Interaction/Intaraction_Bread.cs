using UnityEngine;

public class Intaraction_Bread : MonoBehaviour
{
    public const string PoolName = "Bread";

    public int TableId { get; private set; }

    public void SetTableId(int tableId)
    {
        TableId = tableId;
    }

    public void Despawn()
    {
        GameInstance.Pool?.Despawn(PoolName, gameObject);
    }
}
