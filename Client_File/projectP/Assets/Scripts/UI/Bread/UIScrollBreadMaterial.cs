using UnityEngine;
using TMPro;
using UniRx;
using Extension;

public class UIScrollBreadMaterialData
{
    public int Tid;
    // 보유 수량(다른 트레이 예약량은 굽기 시작 시 한 번에 검사하므로 여기선 보유 수량 그대로).
    public int OwnedCount;
}

// 오븐 재료 선택 모달의 빵 재료 1칸.
public class UIScrollBreadMaterial : UIScrollRow<UIScrollBreadMaterialData>
{
    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextGrade;
    [SerializeField] private TextMeshProUGUI mTextCount;
    [SerializeField] private TextMeshProUGUI mTextTags;
    [SerializeField] private TextMeshProUGUI mTextDesc;

    public UIScrollBreadMaterialData CurrentData { get; private set; }

    private void Awake()
    {
        mButton.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollBreadMaterialData data)
    {
        CurrentData = data;
        if (data == null) return;

        var row = GameInstance.Table.Get<CTable.BreadMaterialRow>(data.Tid);
        var quality = BreadQuality.FromGrade(row != null ? row.Grade : 1);

        mTextName.SetTextEx(row?.Name ?? data.Tid.ToString());
        mTextGrade.SetTextEx($"<color={BreadQuality.GetColorHex(quality)}>{BreadQuality.GetName(quality)}</color>");
        mTextCount.SetTextEx($"보유 {data.OwnedCount}");
        mTextTags.SetTextEx(row != null ? string.Join(", ", row.GetTags()) : string.Empty);
        mTextDesc.SetTextEx(row?.Desc ?? string.Empty);
    }
}
