using System.Linq;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

public class UIScrollBreadRecipeData
{
    public int BreadTid;
}

// 오븐 반죽 설정의 "해금된 디저트 레시피" 1행(발견한 빵 레시피만 표시).
public class UIScrollBreadRecipe : UIScrollRow<UIScrollBreadRecipeData>
{
    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextMaterials;
    [SerializeField] private GameObject mSelectedMark;

    public UIScrollBreadRecipeData CurrentData { get; private set; }

    private void Awake()
    {
        mButton.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollBreadRecipeData data)
    {
        CurrentData = data;
        SetSelected(false);
        if (data == null) return;

        mTextName.SetTextEx(GameInstance.Table.Get<CTable.MenuItemRow>(data.BreadTid)?.Name ?? data.BreadTid.ToString());

        var materialTable = GameInstance.Table.GetTable<CTable.BreadMaterialRow>();
        var names = GameInstance.Table.Get<CTable.BreadRow>(data.BreadTid).GetMaterialTids()
            .Select(tid => materialTable?.Get(tid)?.Name ?? tid.ToString());
        mTextMaterials.SetTextEx(string.Join(" + ", names));
    }

    public void SetSelected(bool selected)
    {
        if (mSelectedMark != null)
            mSelectedMark.SetActive(selected);
    }
}
