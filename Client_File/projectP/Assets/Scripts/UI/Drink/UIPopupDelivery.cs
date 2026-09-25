using System;
using UnityEngine;
using TMPro;
using UniRx;
using DG.Tweening;
using Extension;

// 둘기딜리버리 팝업(프리팹 UI_Popup_Delivery). 주문서 레일 + 완성 음료 픽업대.
// 주문서와 픽업대 음료를 하나씩 골라 음료(온도 포함)가 맞으면 배달 벨로 배달한다(DeliveryModel.TryDeliver).
// 음료 제작은 커피머신 팝업(UIPopupDrinkRecipeProduction)에서 하고, 완성 음료가 픽업대로 들어온다.
public class UIPopupDelivery : UIWndBase, IUIParam<UIPopupDelivery.Param>
{
    public struct Param
    {
    }

    private const float NOTICE_SECONDS = 2f;

    [Header("Orders")]
    [SerializeField] private UIDeliveryOrderSlot[] mOrderSlots;

    [Header("Pickup")]
    [SerializeField] private UIPickupSlot[] mPickupSlots;
    [SerializeField] private UIButtonEx mBtnDeliver;
    [SerializeField] private UIButtonEx mBtnDiscardPickup;

    [Header("Notice")]
    [SerializeField] private TextMeshProUGUI mTextNotice;
    [SerializeField] private GameObject mEmptyOrderGuide;

    private int mSelectedOrderNo;
    private int mSelectedPickupIndex = -1;
    private bool mIsModelSubscribed;
    private Tween mDeliverTween;
    private readonly SerialDisposable mNoticeDisposable = new SerialDisposable();

    public override eUIType GetUIType() => eUIType.PopupDelivery;

    public override void Init()
    {
        base.Init();

        for (int i = 0; i < mOrderSlots.Length; i++)
        {
            int index = i;
            mOrderSlots[i].Init(() => OnClickOrder(index));
        }

        for (int i = 0; i < mPickupSlots.Length; i++)
        {
            int index = i;
            mPickupSlots[i].Init(() => OnClickPickup(index));
        }

        mBtnDeliver.OnSubscribeOnClick(OnClickDeliver).AddTo(this);
        mBtnDiscardPickup.OnSubscribeOnClick(OnClickDiscardPickup).AddTo(this);

        mNoticeDisposable.AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        SubscribeModelOnce();

        mSelectedOrderNo = 0;
        mSelectedPickupIndex = -1;
        mTextNotice.SetTextEx(string.Empty);

        RefreshOrders();
        RefreshPickups();
    }

    public void Set(Param param)
    {
    }

    private void OnDisable()
    {
        KillDeliverTween();
    }

    // Init()이 GameInstance 모델 준비 이전에 불릴 수 있어, 모델 구독은 첫 Open에서 1회만 건다.
    private void SubscribeModelOnce()
    {
        if (mIsModelSubscribed) return;
        mIsModelSubscribed = true;

        var delivery = GameInstance.Model.Delivery;

        delivery.Orders.ObserveCountChanged()
            .Subscribe(_ => RefreshOrders())
            .AddTo(this);

        foreach (var slot in delivery.PickupSlots)
        {
            slot.Skip(1)
                .Subscribe(_ => RefreshPickups())
                .AddTo(this);
        }
    }

    private void OnClickOrder(int index)
    {
        var order = mOrderSlots[index].Order;
        if (order == null) return;

        mSelectedOrderNo = mSelectedOrderNo == order.OrderNo ? 0 : order.OrderNo;
        RefreshSelection();
    }

    private void OnClickPickup(int index)
    {
        if (mPickupSlots[index].Drink == null) return;

        mSelectedPickupIndex = mSelectedPickupIndex == index ? -1 : index;
        RefreshSelection();
    }

    private void OnClickDeliver()
    {
        int orderIndex = FindSelectedOrderIndex();
        if (!GameInstance.Model.Delivery.TryDeliver(orderIndex, mSelectedPickupIndex, out long reward))
            return;

        mSelectedOrderNo = 0;
        mSelectedPickupIndex = -1;
        RefreshSelection();
        ShowNotice($"배달 완료! +{reward:N0} 골드");
    }

    private void OnClickDiscardPickup()
    {
        if (mSelectedPickupIndex < 0) return;

        GameInstance.Model.Delivery.DiscardPickup(mSelectedPickupIndex);
        mSelectedPickupIndex = -1;
        RefreshSelection();
    }

    private void RefreshOrders()
    {
        var orders = GameInstance.Model.Delivery.Orders;
        for (int i = 0; i < mOrderSlots.Length; i++)
            mOrderSlots[i].Set(i < orders.Count ? orders[i] : null);

        if (mEmptyOrderGuide != null)
            mEmptyOrderGuide.SetActive(orders.Count == 0);

        if (FindSelectedOrderIndex() < 0)
            mSelectedOrderNo = 0;

        RefreshSelection();
    }

    private void RefreshPickups()
    {
        var slots = GameInstance.Model.Delivery.PickupSlots;
        for (int i = 0; i < mPickupSlots.Length; i++)
            mPickupSlots[i].Set(i < slots.Length ? slots[i].Value : null);

        if (mSelectedPickupIndex >= 0 && mPickupSlots[mSelectedPickupIndex].Drink == null)
            mSelectedPickupIndex = -1;

        RefreshSelection();
    }

    private void RefreshSelection()
    {
        foreach (var slot in mOrderSlots)
            slot.SetSelected(slot.Order != null && slot.Order.OrderNo == mSelectedOrderNo);

        for (int i = 0; i < mPickupSlots.Length; i++)
            mPickupSlots[i].SetSelected(i == mSelectedPickupIndex);

        int orderIndex = FindSelectedOrderIndex();
        var order = orderIndex >= 0 ? GameInstance.Model.Delivery.Orders[orderIndex] : null;
        var drink = mSelectedPickupIndex >= 0 ? mPickupSlots[mSelectedPickupIndex].Drink : null;

        bool deliverable = DeliveryModel.IsDeliverable(order, drink);
        mBtnDeliver.interactable = deliverable;
        mBtnDiscardPickup.interactable = drink != null;

        if (deliverable) PlayDeliverTween();
        else KillDeliverTween();
    }

    private int FindSelectedOrderIndex()
    {
        if (mSelectedOrderNo == 0) return -1;

        var orders = GameInstance.Model.Delivery.Orders;
        for (int i = 0; i < orders.Count; i++)
        {
            if (orders[i].OrderNo == mSelectedOrderNo)
                return i;
        }
        return -1;
    }

    // 배달 가능할 때 배달 벨을 흔들어 알려준다(시안의 bell-ready 연출).
    private void PlayDeliverTween()
    {
        if (mDeliverTween != null && mDeliverTween.IsActive()) return;

        mDeliverTween = mBtnDeliver.transform
            .DOPunchRotation(new Vector3(0f, 0f, 8f), 0.6f, 6)
            .SetDelay(0.8f)
            .SetLoops(-1)
            .SetLink(mBtnDeliver.gameObject);
    }

    private void KillDeliverTween()
    {
        mDeliverTween?.Kill();
        mDeliverTween = null;
        if (mBtnDeliver != null)
            mBtnDeliver.transform.localRotation = Quaternion.identity;
    }

    private void ShowNotice(string message)
    {
        if (mTextNotice == null) return;

        mTextNotice.SetTextEx(message);
        mNoticeDisposable.Disposable = Observable.Timer(TimeSpan.FromSeconds(NOTICE_SECONDS))
            .Subscribe(_ => mTextNotice.SetTextEx(string.Empty));
    }
}
