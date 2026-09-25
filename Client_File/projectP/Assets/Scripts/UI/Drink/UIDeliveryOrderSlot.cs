using System;
using System.Text;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 주문 레일의 둘기딜리버리 주문서 1장. 빈 자리면 mEmptyRoot만 보인다.
public class UIDeliveryOrderSlot : UIBase
{
    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private GameObject mOrderRoot;
    [SerializeField] private GameObject mEmptyRoot;
    [SerializeField] private GameObject mSelectedMark;
    [SerializeField] private TextMeshProUGUI mTextOrderNo;
    [SerializeField] private TextMeshProUGUI mTextDrink;
    [SerializeField] private TextMeshProUGUI mTextRequests;

    public DeliveryOrderData Order { get; private set; }

    public void Init(Action onClick)
    {
        mButton.OnSubscribeOnClick(onClick).AddTo(this);
    }

    public void Set(DeliveryOrderData order)
    {
        Order = order;

        mButton.interactable = order != null;
        if (mOrderRoot != null) mOrderRoot.SetActive(order != null);
        if (mEmptyRoot != null) mEmptyRoot.SetActive(order == null);
        SetSelected(false);

        if (order == null)
            return;

        mTextOrderNo.SetTextEx($"둘기딜리버리 #{order.OrderNo:00}");
        mTextDrink.SetTextEx(GameInstance.Model.Drink.GetName(order.DrinkTid));
        mTextRequests.SetTextEx(BuildRequestText(order));
    }

    public void SetSelected(bool selected)
    {
        if (mSelectedMark != null)
            mSelectedMark.SetActive(selected && Order != null);
    }

    private static string BuildRequestText(DeliveryOrderData order)
    {
        var sb = new StringBuilder();
        if (order.HasTopping)
        {
            string toppingName = GameInstance.Table.Get<CTable.DrinkMaterialRow>(order.ToppingMaterialTid)?.Name;
            sb.AppendLine($"└ {toppingName} 추가");
        }
        if (order.HasIncludeTag)
            sb.AppendLine($"+ 포함 {order.IncludeTag}");
        if (order.HasExcludeTag)
            sb.AppendLine($"<color=#B14935>- 제외 {order.ExcludeTag}</color>");
        return sb.ToString().TrimEnd();
    }
}
