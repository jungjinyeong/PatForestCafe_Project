using System;
using System.Collections.Generic;
using System.Linq;
using Extension;

public partial class DeliveryModel
{
    // TODO(기획): 임시 수치. 확정 시 Config 테이블로 이동.
    private const int ORDER_REFILL_SECONDS = 20;
    private const float TOPPING_REQUEST_CHANCE = 0.4f;
    private const float INCLUDE_TAG_REQUEST_CHANCE = 0.35f;
    private const float EXCLUDE_TAG_REQUEST_CHANCE = 0.3f;
    private const float BONUS_RATE_PER_REQUEST = 0.2f;

    public int FindEmptyPickupSlot()
    {
        for (int i = 0; i < PickupSlots.Length; i++)
        {
            if (PickupSlots[i].Value == null)
                return i;
        }
        return -1;
    }

    public bool TryStorePickup(PickupDrinkData drink)
    {
        int index = FindEmptyPickupSlot();
        if (drink == null || index < 0)
            return false;

        PickupSlots[index].Value = drink;
        return true;
    }

    public void DiscardPickup(int pickupIndex)
    {
        if (pickupIndex < 0 || pickupIndex >= PickupSlots.Length)
            return;

        PickupSlots[pickupIndex].Value = null;
    }

    // 음료(온도 포함 DrinkTid)가 맞아야만 배달 가능. 토핑/속성 요청은 보너스 판정에만 쓴다.
    public static bool IsDeliverable(DeliveryOrderData order, PickupDrinkData drink)
    {
        return order != null && drink != null && !drink.IsFailed && drink.DrinkTid == order.DrinkTid;
    }

    // 토핑/포함 요청은 충족 시 +보너스.
    public static int CountSatisfiedRequests(DeliveryOrderData order, PickupDrinkData drink)
    {
        int count = 0;
        if (order.HasTopping && drink.CustomMaterialTids.Contains(order.ToppingMaterialTid)) count++;
        if (order.HasIncludeTag && drink.Tags.Contains(order.IncludeTag)) count++;
        return count;
    }

    // 제외 요청은 레시피대로만 만들면 항상 지켜지므로(기본 재료 속성은 후보에서 빠짐) 보너스가 아니라 어겼을 때 -패널티.
    public static bool IsExcludeViolated(DeliveryOrderData order, PickupDrinkData drink)
    {
        return order.HasExcludeTag && drink.Tags.Contains(order.ExcludeTag);
    }

    public static long CalcReward(DeliveryOrderData order, PickupDrinkData drink)
    {
        long basePrice = GameInstance.Table.Get<CTable.MenuItemRow>(order.DrinkTid)?.Price ?? 0;
        float rate = 1f + BONUS_RATE_PER_REQUEST * CountSatisfiedRequests(order, drink);
        if (IsExcludeViolated(order, drink))
            rate -= BONUS_RATE_PER_REQUEST;
        return (long)Math.Round(basePrice * Math.Max(0f, rate));
    }

    public bool TryDeliver(int orderIndex, int pickupIndex, out long reward)
    {
        reward = 0;
        if (orderIndex < 0 || orderIndex >= Orders.Count) return false;
        if (pickupIndex < 0 || pickupIndex >= PickupSlots.Length) return false;

        var order = Orders[orderIndex];
        var drink = PickupSlots[pickupIndex].Value;
        if (!IsDeliverable(order, drink))
            return false;

        reward = CalcReward(order, drink);
        GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Add((int)reward);

        Orders.RemoveAt(orderIndex);
        PickupSlots[pickupIndex].Value = null;
        NextRefillUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ORDER_REFILL_SECONDS;
        return true;
    }

    // 세이브 복원. SaveManager.Load()에서 호출.
    public void Restore(IEnumerable<DeliveryOrderData> orders, IReadOnlyList<PickupDrinkData> pickups, long nextRefillUnixSeconds, int nextOrderNo)
    {
        Orders.Clear();
        if (orders != null)
        {
            foreach (var order in orders.Take(MAX_ORDER_COUNT))
                Orders.Add(order);
        }

        for (int i = 0; i < PickupSlots.Length; i++)
            PickupSlots[i].Value = pickups != null && i < pickups.Count ? pickups[i] : null;

        NextRefillUnixSeconds = nextRefillUnixSeconds;
        NextOrderNo = Math.Max(1, nextOrderNo);
    }

    private void TickRefill()
    {
        if (Orders.Count >= MAX_ORDER_COUNT)
            return;

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() < NextRefillUnixSeconds)
            return;

        while (Orders.Count < MAX_ORDER_COUNT)
        {
            var order = CreateRandomOrder();
            if (order == null)
                break;

            Orders.Add(order);
        }
    }

    // 도감에 발견된 음료 중 DrinkRequest 가중치로 1개를 고른다. 발견한 레시피가 없으면 null.
    private DeliveryOrderData CreateRandomOrder()
    {
        var requestTable = GameInstance.Table.GetTable<CTable.DrinkRequestRow>();
        if (requestTable == null)
            return null;

        var candidates = requestTable.All.Values
            .Where(r => r.Weight > 0 && GameInstance.Model.RecipeBook.IsDiscovered(r.DrinkTid))
            .ToList();
        if (candidates.Count == 0)
            return null;

        int drinkTid = PickWeighted(candidates).DrinkTid;
        var baseMaterialTids = GameInstance.Model.Drink.GetBaseMaterialTids(drinkTid);
        var baseTags = PickupDrinkData.CollectTags(baseMaterialTids);

        var materialTable = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>();
        var materialRows = materialTable?.All.Values
            .Where(m => m.Tid != DrinkModel.ICE_MATERIAL_TID)
            .ToList() ?? new List<CTable.DrinkMaterialRow>();

        int toppingTid = 0;
        if (UnityEngine.Random.value < TOPPING_REQUEST_CHANCE)
        {
            var toppings = materialRows.Where(m => m.IsTopping && !baseMaterialTids.Contains(m.Tid)).ToList();
            if (toppings.Count > 0)
                toppingTid = toppings[UnityEngine.Random.Range(0, toppings.Count)].Tid;
        }

        // 기본 재료로 이미 충족되는 속성은 요청할 의미가 없으므로 제외한다.
        var tagPool = materialRows.SelectMany(m => m.GetTags()).Distinct().Where(t => !baseTags.Contains(t)).ToList();

        string includeTag = null;
        if (tagPool.Count > 0 && UnityEngine.Random.value < INCLUDE_TAG_REQUEST_CHANCE)
            includeTag = tagPool[UnityEngine.Random.Range(0, tagPool.Count)];

        string excludeTag = null;
        if (UnityEngine.Random.value < EXCLUDE_TAG_REQUEST_CHANCE)
        {
            var toppingTags = PickupDrinkData.CollectTags(new[] { toppingTid });
            var excludePool = tagPool.Where(t => t != includeTag && !toppingTags.Contains(t)).ToList();
            if (excludePool.Count > 0)
                excludeTag = excludePool[UnityEngine.Random.Range(0, excludePool.Count)];
        }

        return DeliveryOrderData.Create(NextOrderNo++, drinkTid, toppingTid, includeTag, excludeTag);
    }

    private static CTable.DrinkRequestRow PickWeighted(List<CTable.DrinkRequestRow> rows)
    {
        int total = rows.Sum(r => r.Weight);
        int roll = UnityEngine.Random.Range(0, total);
        foreach (var row in rows)
        {
            roll -= row.Weight;
            if (roll < 0)
                return row;
        }
        return rows[rows.Count - 1];
    }
}
