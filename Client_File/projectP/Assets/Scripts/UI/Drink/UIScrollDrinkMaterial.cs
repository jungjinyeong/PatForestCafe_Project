using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Extension;
using UniRx;

public class UIScrollDrinkMaterialData
{
    public int Tid;
    public string Name;
    // 보유 수량 - 이미 슬롯에 넣어 예약된 수량.
    public int RemainCount;
}

// 커피머신 재료 선택 모달(UIDrinkMaterialPicker)의 재료 1칸.
public class UIScrollDrinkMaterial : UIScrollRow<UIScrollDrinkMaterialData>
{
    [SerializeField] private TextMeshProUGUI mName;
    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private Image mImgIcon;
    [SerializeField] private TextMeshProUGUI mTextCount;
    [SerializeField] private TextMeshProUGUI mTextDesc;
    [SerializeField] private TextMeshProUGUI mTextTags;

    public UIScrollDrinkMaterialData CurrentData { get; private set; }

    private void Awake()
    {
        mButton.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollDrinkMaterialData data)
    {
        CurrentData = data;
        if (data == null) return;

        var row = GameInstance.Table.Get<CTable.DrinkMaterialRow>(data.Tid);

        mButton.interactable = data.RemainCount > 0;
        mImgIcon.SetSpriteEx(row?.Atlas, row?.Icon);

        // mTextCount가 연결되지 않은 기존 행 프리팹은 이름 옆에 수량을 붙여 보여준다.
        if (mTextCount != null)
        {
            mName.SetTextEx(data.Name);
            mTextCount.SetTextEx($"{data.RemainCount}개");
        }
        else
        {
            mName.SetTextEx($"{data.Name} ({data.RemainCount})");
        }

        mTextDesc.SetTextEx(row?.Desc ?? string.Empty);
        mTextTags.SetTextEx(row != null ? string.Join(", ", row.GetTags()) : string.Empty);
    }
}
