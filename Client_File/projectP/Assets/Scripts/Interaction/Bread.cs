using UnityEngine;

public class Bread : MonoBehaviour
{
    public const string PoolName = "Bread";

    public void Despawn()
    {
        GameInstance.Pool?.Despawn(PoolName, gameObject);
    }
}
