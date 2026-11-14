using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 상점가 독자 가구 목록 모달 — 로비 패널의 가구 배치 화면(UISidePlacementView)과 구매 로직
// (PlacementModel.TryPurchaseAndBeginPlacement)만 공유하고, 표시는 별도 버튼 목록으로 만든다(사용자 확정: 상점가에도 독자적인 목록을 둔다).
// 항목이 적어 ScrollRect 없이 단순 버튼 목록으로 뼈대만 구성 — 카탈로그가 커지면 UIScrollEx로 교체 필요.
public class UIPopupShopFurniture : UIWndBase, IUIParam<UIPopupShopFurniture.Param>
{
    public struct Param
    {
    }

    [SerializeField] private Transform mListContainer;
    [SerializeField] private GameObject mRowButtonTemplate;
    [SerializeField] private UIFurnitureCategoryTabs mCategoryTabs;

    private readonly List<GameObject> mSpawnedRows = new List<GameObject>();

    public override eUIType GetUIType() => eUIType.PopupShopFurniture;

    public override void Init()
    {
        base.Init();

        if (mCategoryTabs != null)
            mCategoryTabs.Init(_ => RefreshList());
    }

    public override void Open()
    {
        base.Open();

        if (mCategoryTabs != null)
            mCategoryTabs.Select(CTable.eFurnitureCategory.All);
        else
            RefreshList();
    }

    public void Set(Param param)
    {
    }

    private void RefreshList()
    {
        // Destroy는 프레임 끝에 처리되므로, 탭을 바꾼 같은 프레임에 이전 행이 새 행과 겹쳐 보이지 않게 먼저 숨긴다.
        foreach (var go in mSpawnedRows)
        {
            go.SetActive(false);
            Destroy(go);
        }
        mSpawnedRows.Clear();

        if (mListContainer == null || mRowButtonTemplate == null)
            return;

        var category = mCategoryTabs != null ? mCategoryTabs.Selected : CTable.eFurnitureCategory.All;
        foreach (var data in GameInstance.Model.Placement.BuildFurnitureShopData(category))
        {
            var row = Instantiate(mRowButtonTemplate, mListContainer);
            row.SetActive(true);

            var text = row.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = data.IsUnlimitedUnlocked
                    ? $"{data.Name} <size=80%>{data.SizeText}</size>  보유"
                    : data.StoredCount > 0
                    ? $"{data.Name} <size=80%>{data.SizeText}</size>  {data.Price:N0}G <size=80%>(보관 {data.StoredCount})</size>"
                    : $"{data.Name} <size=80%>{data.SizeText}</size>  {data.Price:N0}G";

            var btn = row.GetComponent<UIButtonEx>();
            var captured = data;
            btn.OnSubscribeOnClick(() => OnClickBuy(captured)).AddTo(btn);

            mSpawnedRows.Add(row);
        }
    }

    private void OnClickBuy(UIScrollFurnitureData data)
    {
        // 영업 중에는 배치할 수 없으니 사서 보관함에 넣기만 한다(영업 정산 후 로비 가구배치 목록에서 꺼내 놓는다).
        if (!GameInstance.Model.Business.CanEditLayout)
        {
            var placement = GameInstance.Model.Placement;
            string message = placement.IsUnlimitedUnlocked(data.Tid, data.IsSub)
                ? $"{data.Name}은(는) 이미 가지고 있어요 · 영업 정산 후에 배치할 수 있어요"
                : placement.TryPurchaseToStorage(data)
                    ? $"{data.Name}을(를) 보관함에 넣었어요 · 영업 정산 후에 배치할 수 있어요"
                    : "골드가 부족해요";
            GameInstance.Model.Notice.Push(eNoticeType.Business, message);
            return;
        }

        // 구매 성공 시 곧바로 드래그 배치 모드로 전환되므로, 모달을 닫아 로비 화면이 보이게 한다.
        // 상점가 팝업이 남아 있으면 화면을 덮어 배치 입력(PlacementInputController의 UI 레이캐스트 체크)이 막히므로 같이 닫는다.
        if (GameInstance.Model.Placement.TryPurchaseAndBeginPlacement(data))
        {
            SelfClose();
            GameInstance.UI.Close(eUIType.PopupShopStreet);
        }
    }
}
