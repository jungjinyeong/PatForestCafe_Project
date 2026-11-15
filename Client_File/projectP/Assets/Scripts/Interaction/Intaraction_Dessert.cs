using UnityEngine;

public class Intaraction_Dessert : MonoBehaviour
{
    public const string PoolName = "Dessert";

    public int TableId { get; private set; }
    // 오븐에서 구울 때 정해진 품질. 손님 결제 시 판매가 배율(DessertQuality.GetSaleMultiplier)에 쓰인다.
    public eDessertQuality Quality { get; private set; } = eDessertQuality.Low;

    public void SetTableId(int tableId)
    {
        TableId = tableId;
    }

    public void SetQuality(eDessertQuality quality)
    {
        Quality = quality;
    }

    public void Despawn()
    {
        GameInstance.Pool?.Despawn(PoolName, gameObject);
    }
}
