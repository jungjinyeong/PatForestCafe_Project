using UnityEngine;

public class Intaraction_Bread : MonoBehaviour
{
    public const string PoolName = "Bread";

    public int TableId { get; private set; }
    // 오븐에서 구울 때 정해진 품질. 손님 결제 시 판매가 배율(BreadQuality.GetSaleMultiplier)에 쓰인다.
    public eBreadQuality Quality { get; private set; } = eBreadQuality.Low;

    public void SetTableId(int tableId)
    {
        TableId = tableId;
    }

    public void SetQuality(eBreadQuality quality)
    {
        Quality = quality;
    }

    public void Despawn()
    {
        GameInstance.Pool?.Despawn(PoolName, gameObject);
    }
}
