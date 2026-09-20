using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 상점가 독자 가구 목록 모달 — 로비의 UIFurnitureList(가구배치 목록)와 구매 로직(PurchaseAndBeginPlacement)만
// 공유하고, 표시는 별도 버튼 목록으로 만든다(사용자 확정: 상점가에도 독자적인 목록을 둔다).
// 항목이 적어 ScrollRect 없이 단순 버튼 목록으로 뼈대만 구성 — 카탈로그가 커지면 UIScrollEx로 교체 필요.
public class UIPopupShopFurniture : UIWndBase, IUIParam<UIPopupShopFurniture.Param>
{
    public struct Param
    {
    }

    [SerializeField] private Transform mListContainer;
    [SerializeField] private GameObject mRowButtonTemplate;
    [SerializeField] private UIFurnitureList mFurnitureList;

    private readonly List<GameObject> mSpawnedRows = new List<GameObject>();

    public override eUIType GetUIType() => eUIType.UIPopupShopFurniture;

    public override void Open()
    {
        base.Open();

        RefreshList();
    }

    public void Set(Param param)
    {
    }

    private void RefreshList()
    {
        foreach (var go in mSpawnedRows)
            Destroy(go);
        mSpawnedRows.Clear();

        if (mFurnitureList == null || mListContainer == null || mRowButtonTemplate == null)
            return;

        foreach (var data in mFurnitureList.BuildFurnitureShopData())
        {
            var row = Instantiate(mRowButtonTemplate, mListContainer);
            row.SetActive(true);

            var text = row.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"{data.Name}  {data.Price:N0}G";

            var btn = row.GetComponent<UIButtonEx>();
            var captured = data;
            btn.OnSubscribeOnClick(() => OnClickBuy(captured)).AddTo(btn);

            mSpawnedRows.Add(row);
        }
    }

    private void OnClickBuy(UIScrollFurnitureData data)
    {
        if (mFurnitureList == null)
            return;

        // 구매 성공 시 곧바로 드래그 배치 모드로 전환되므로, 모달을 닫아 로비 화면이 보이게 한다.
        if (mFurnitureList.PurchaseAndBeginPlacement(data))
            SelfClose();
    }
}
