using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class UITopbarInfo : UIHUDBase
{
    [SerializeField] private SerializableDictionary<CTable.eMoneyType, UIWealthItem> mWealthItems = new();

    public override void Init()
    {
        base.Init();

        SubscribeWealthInfos();
    }

    private void SubscribeWealthInfos()
    {
        foreach (var item in mWealthItems)
        {
            var wealth = GameInstance.Model.Item.GetWealth(item.Key);
            var wealthItem = item.Value;

            if (wealth == null)
            {
                wealthItem.UpdateWealthInfos(0);
                continue;
            }

            wealth.Count
                .Subscribe(amount => wealthItem.UpdateWealthInfos(amount))
                .AddTo(this);
        }
    }
}
