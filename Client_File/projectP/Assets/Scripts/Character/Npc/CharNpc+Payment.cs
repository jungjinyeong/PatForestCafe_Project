using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

// 손님 결제: 고른 디저트(카트) 정산, 음료 주문(제작 가능한 것만 · 재료 소모), 손에 드는 음료 표시.
public partial class CharNpc
{
    // 무작위로 뽑은 음료가 재료 부족으로 제작 불가하면 다시 뽑는 횟수. 모두 실패하면 기본 음료(아메리카노 — 무한 재료만 씀).
    private const int DRINK_PICK_TRIES = 3;

    private int mCartDessertTid;
    private Intaraction_DessertStand mCartStand;
    private readonly List<eDessertQuality> mCartQualities = new List<eDessertQuality>();

    private bool HasCart => mCartDessertTid != 0 && mCartQualities.Count > 0;

    // stand: 디저트를 집은 진열대(결제를 포기하면 여기로 돌려놓는다).
    private void SetCart(Intaraction_DessertStand stand, List<eDessertQuality> qualities)
    {
        int dessertTid = stand.TableId;
        mCartStand = stand;
        mCartDessertTid = dessertTid;
        mCartQualities.Clear();
        mCartQualities.AddRange(qualities);

        var menuItemRow = GameInstance.Model.Dessert.Get(dessertTid)?.MenuItemRow;
        LobbyUI?.SetDessertCart(LoadSprite(menuItemRow?.Atlas, menuItemRow?.Icon), mCartQualities.Count);
    }

    private void ResetCart()
    {
        ClearCartData();
        LobbyUI?.ClearDessertCart();
    }

    // 장바구니 데이터만 비운다(머리 위 디저트 표시는 그대로). 결제한 디저트는 음료처럼 퇴장할 때까지 들고 다닌다.
    private void ClearCartData()
    {
        mCartDessertTid = 0;
        mCartStand = null;
        mCartQualities.Clear();
    }

    private void CompleteCounterPayment(bool includeDrink)
    {
        ReceiveCartGold();
        if (includeDrink)
            OrderDrink();
        TryReserveTerraceSeat();

        PlayPaymentEffectThenAdvance();
    }

    private void CompleteKioskPayment()
    {
        ReceiveCartGold();
        TryReserveTerraceSeat();
        PlayPaymentEffectThenAdvance();
    }

    // 결제를 포기했을 때: 고른 디저트를 집었던 진열대에 다시 놓는다. 진열대가 철거됐거나 종류가 바뀌었으면 창고(생산 재고)로 돌린다.
    private void ReturnCartToStand()
    {
        if (!HasCart)
            return;

        if (mCartStand != null && mCartStand.TableId == mCartDessertTid)
            mCartStand.ReturnDesserts(mCartQualities);
        else
        {
            foreach (var quality in mCartQualities)
                GameInstance.Model.Dessert.AddProduced(mCartDessertTid, quality);
        }

        ResetCart();
    }

    // 결제가 끝나면 바로 다음 단계로 걸어가고, 동전 → 만족 아이콘 연출은 걷는 동안 머리 옆에 띄운다
    // (기획: 카운터 앞 2초 대기 = 직원 게이지 후 음료를 든 채 이동).
    private void PlayPaymentEffectThenAdvance()
    {
        LobbyUI?.PlayPaymentEffect(null);
        AdvanceBehaviorQueue();
    }

    // 판매가 = 메뉴 가격 × 디저트 품질 배율 × 결제 수익 업그레이드 배율 × 계절 판매 배율(Season.csv). 한 개마다 정산한다.
    private void ReceiveCartGold()
    {
        if (!HasCart)
            return;

        var dessertData = GameInstance.Model.Dessert.Get(mCartDessertTid);
        if (dessertData?.MenuItemRow != null)
        {
            var business = GameInstance.Model.Business;
            foreach (var quality in mCartQualities)
            {
                int price = Mathf.RoundToInt(dessertData.MenuItemRow.Price * DessertQuality.GetSaleMultiplier(quality));
                int gold = business.ApplySaleRate(GameInstance.Model.Upgrade.ApplyGoldIncomeMultiplier(price));
                GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Add(gold);
                business.RecordDessertSale(gold);
                GameInstance.Model.Cafe.AddExp(CafeModel.EXP_STORE_PAYMENT);
            }
        }

        // 결제 완료 — 포기 시 진열대로 돌려놓을 대상이 아니므로 데이터만 비우고, 머리 위 디저트는 퇴장할 때까지 보여 준다.
        ClearCartData();
    }

    // 음료 1잔: 제작 가능한 음료를 골라 재료를 쓰고 값을 받은 뒤 손에 들려 준다.
    private void OrderDrink()
    {
        var drink = PickOrderableDrink();
        if (drink?.MenuItemRow == null)
            return;

        var drinkModel = GameInstance.Model.Drink;
        if (!drinkModel.CanMake(drink.TId))
            Logger.Warning($"[CharNpc] 기본 음료 재료가 부족합니다(무한 재료 설정 확인). Tid={drink.TId}");
        drinkModel.ConsumeIngredients(drink.TId);

        var business = GameInstance.Model.Business;
        int gold = business.ApplySaleRate(GameInstance.Model.Upgrade.ApplyGoldIncomeMultiplier((int)drink.MenuItemRow.Price));
        GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Add(gold);
        business.RecordDrinkSale(gold);
        GameInstance.Model.Cafe.AddExp(CafeModel.EXP_STORE_PAYMENT);

        var row = drink.MenuItemRow;
        string handIcon = string.IsNullOrEmpty(row.HandIcon) ? row.Icon : row.HandIcon;
        LobbyUI?.SetDrinkSprite(LoadSprite(row.Atlas, handIcon));
    }

    // 레시피북으로 해금된 레시피(+기본 음료) 중 DrinkRow.Weight × 계절 온도 인기 배율(Season.csv) 가중치로 하나를 뽑는다.
    // 재료가 모자라 만들 수 없으면 최대 DRINK_PICK_TRIES번 다시 뽑고, 모두 실패하면 기본 음료(아메리카노).
    private DrinkData PickOrderableDrink()
    {
        var drinkModel = GameInstance.Model.Drink;
        var unlockedTids = new List<int>(GameInstance.Model.RecipeBook.GetDiscoveredTids());
        if (drinkModel.DefaultDrink != null && !unlockedTids.Contains(drinkModel.DefaultDrink.TId))
            unlockedTids.Add(drinkModel.DefaultDrink.TId);

        for (int i = 0; i < DRINK_PICK_TRIES; i++)
        {
            var drink = PickWeightedDrink(unlockedTids);
            if (drink != null && drinkModel.CanMake(drink.TId))
                return drink;
        }

        return drinkModel.DefaultDrink;
    }

    private static DrinkData PickWeightedDrink(List<int> tids)
    {
        var drinkModel = GameInstance.Model.Drink;
        var business = GameInstance.Model.Business;
        DrinkData picked = null;
        float totalWeight = 0f;

        foreach (var tid in tids)
        {
            var drinkData = drinkModel.Get(tid);
            int baseWeight = drinkData?.Row?.Weight ?? 0;
            if (baseWeight <= 0) continue;

            float weight = baseWeight * business.GetDrinkPopularityRate(drinkData.Row.DrinkTempType);
            totalWeight += weight;
            if (Random.Range(0f, totalWeight) < weight)
                picked = drinkData;
        }

        return picked;
    }

    // 아틀라스 주소(Addressables) + 아틀라스 안 스프라이트 이름으로 스프라이트를 찾는다.
    private static Sprite LoadSprite(string atlasAddress, string spriteName)
    {
        if (string.IsNullOrEmpty(atlasAddress) || string.IsNullOrEmpty(spriteName))
            return null;

        var atlas = GameInstance.Resource.LoadSync<SpriteAtlas>(atlasAddress);
        return atlas != null ? atlas.GetSprite(spriteName) : null;
    }
}
