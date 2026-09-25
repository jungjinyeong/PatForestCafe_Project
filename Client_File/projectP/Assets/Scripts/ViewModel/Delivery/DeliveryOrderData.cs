// 둘기딜리버리 주문서 1장. DrinkTid(온도 포함)가 맞아야 배달 가능하고,
// 토핑/포함/제외 요청은 충족 시 보상 보너스만 붙는다(0/빈 문자열 = 요청 없음).
public class DeliveryOrderData
{
    public int OrderNo { get; private set; }
    public int DrinkTid { get; private set; }
    public int ToppingMaterialTid { get; private set; }
    public string IncludeTag { get; private set; }
    public string ExcludeTag { get; private set; }

    public bool HasTopping => ToppingMaterialTid != 0;
    public bool HasIncludeTag => !string.IsNullOrEmpty(IncludeTag);
    public bool HasExcludeTag => !string.IsNullOrEmpty(ExcludeTag);

    public static DeliveryOrderData Create(int orderNo, int drinkTid, int toppingMaterialTid, string includeTag, string excludeTag)
    {
        return new DeliveryOrderData
        {
            OrderNo = orderNo,
            DrinkTid = drinkTid,
            ToppingMaterialTid = toppingMaterialTid,
            IncludeTag = includeTag ?? string.Empty,
            ExcludeTag = excludeTag ?? string.Empty,
        };
    }
}
