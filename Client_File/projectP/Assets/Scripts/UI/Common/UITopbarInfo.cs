using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class UITopbarInfo : UIBase
{
    [SerializeField] private SerializableDictionary<CTable.eMoneyType, UIWealthItem> mWealthItems = new();

    private void Start()
    {
        UpdateWealthInfos();
    }

    private void UpdateWealthInfos()
    {
        foreach (var item in mWealthItems)
        {
            int amount = GameInstance.Model.Item.GetWealth(item.Key)?.Count?.Value ?? 0;
            item.Value.UpdateWealthInfos(amount);
        }
    }
}
