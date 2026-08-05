using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 자리표시자 미니게임: 실제 규칙(타이밍/퍼즐 등)은 미정이라 버튼 1회 클릭으로 즉시 완료 처리하고
// BreadMaterialRow 중 하나를 랜덤 지급한다. 규칙이 정해지면 OnClickPlay() 내부만 교체하면 된다.
public class UIPopupBreadMinigame : UIWndBase, IUIParam<UIPopupBreadMinigame.Param>
{
    public struct Param
    {
    }

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI mTextResult;

    [Header("Buttons")]
    [SerializeField] private UIButtonEx mBtnPlay;

    public override eUIType GetUIType() => eUIType.UIPopupBreadMinigame;

    public override void Init()
    {
        base.Init();

        mBtnPlay.OnSubscribeOnClick(OnClickPlay).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        mTextResult?.SetTextEx(string.Empty);
    }

    public void Set(Param param)
    {
    }

    private void OnClickPlay()
    {
        var group = GameInstance.Table.GetTable<CTable.BreadMaterialRow>();
        if (group == null || group.All.Count == 0)
        {
            Logger.Warning("[UIPopupBreadMinigame] BreadMaterialGroup을 찾을 수 없습니다.");
            return;
        }

        var rows = new List<CTable.BreadMaterialRow>(group.All.Values);
        var reward = rows[Random.Range(0, rows.Count)];

        GameInstance.Model.Material.Gather(reward.Tid);

        mTextResult?.SetTextEx($"{reward.Name} 획득!");
    }
}
