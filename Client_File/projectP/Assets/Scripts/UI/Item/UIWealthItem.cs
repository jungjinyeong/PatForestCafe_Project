using TMPro;
using UnityEngine;
using Extension;

public class UIWealthItem : UIBase
{
    [SerializeField] private CTable.eMoneyType mMoneyType;
    [SerializeField] private TextMeshProUGUI mAmountText;

    public CTable.eMoneyType MoneyType => mMoneyType;

    public void UpdateWealthInfos(int amount)
    {
        mAmountText.SetTextEx(amount.ToString());
    }
}
