using UnityEngine;
using TMPro;
using UniRx;
using Extension;

public class UIScrollBreadStockData
{
    public int BreadTid;
}

// 오븐 왼쪽 "빵 재고" 1칸 — 창고(생산 재고) 수량 합계와 품질별 수량.
public class UIScrollBreadStock : UIScrollRow<UIScrollBreadStockData>
{
    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextCount;
    [SerializeField] private TextMeshProUGUI mTextQuality;

    public UIScrollBreadStockData CurrentData { get; private set; }

    private void Awake()
    {
        mButton.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollBreadStockData data)
    {
        CurrentData = data;
        if (data == null) return;

        var bread = GameInstance.Model.Bread.Get(data.BreadTid);
        mTextName.SetTextEx(bread?.MenuItemRow?.Name ?? data.BreadTid.ToString());
        mTextCount.SetTextEx($"{bread?.ProducedCount.Value ?? 0}");
        mTextQuality.SetTextEx(BuildQualityText(bread));
    }

    // 예: "<고>2 <중>1 <하>5" — 수량이 있는 품질만.
    public static string BuildQualityText(BreadData bread)
    {
        if (bread == null) return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var quality in BreadQuality.HIGH_TO_LOW)
        {
            int count = bread.GetProduced(quality);
            if (count <= 0) continue;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append($"<color={BreadQuality.GetColorHex(quality)}>{BreadQuality.GetName(quality)}</color> {count}");
        }
        return sb.ToString();
    }
}
