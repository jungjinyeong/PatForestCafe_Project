using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 자리표시자 미니게임: 실제 규칙(타이밍/퍼즐 등)은 미정이라 버튼 1회 클릭으로 즉시 완료 처리하고
// 등급 확률(하/중/고급)로 등급을 먼저 뽑은 뒤 그 등급의 빵 재료 중 하나를 랜덤 지급한다.
// 규칙이 정해지면 OnClickPlay() 내부만 교체하면 된다.
public class UIPopupBreadMinigame : UIWndBase, IUIParam<UIPopupBreadMinigame.Param>
{
    public struct Param
    {
    }

    // TODO(기획): 임시 보상 등급 확률(%). 인덱스 0=하급 1=중급 2=고급.
    private static readonly int[] GRADE_WEIGHTS = { 70, 25, 5 };

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI mTextResult;

    [Header("Buttons")]
    [SerializeField] private UIButtonEx mBtnPlay;

    public override eUIType GetUIType() => eUIType.PopupBreadMinigame;

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

    private static int RollGrade()
    {
        int total = 0;
        foreach (int weight in GRADE_WEIGHTS) total += weight;

        int roll = Random.Range(0, total);
        for (int i = 0; i < GRADE_WEIGHTS.Length; i++)
        {
            roll -= GRADE_WEIGHTS[i];
            if (roll < 0)
                return i + 1;
        }
        return 1;
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

        int grade = RollGrade();
        var rows = new List<CTable.BreadMaterialRow>();
        foreach (var row in group.All.Values)
        {
            if (row.Grade == grade)
                rows.Add(row);
        }

        if (rows.Count == 0)
            rows.AddRange(group.All.Values);

        var reward = rows[Random.Range(0, rows.Count)];
        var quality = BreadQuality.FromGrade(reward.Grade);

        GameInstance.Model.Material.Gather(reward.Tid);

        mTextResult?.SetTextEx($"<color={BreadQuality.GetColorHex(quality)}>[{BreadQuality.GetName(quality)}]</color> {reward.Name} 획득!");
    }
}
