using System.Collections.Generic;
using UnityEngine;

// 손님이 출입구에서 나타날 때 고르는 행동 세트(손님 로직 기획서 — 손님 매장 이용 루트).
// CTable 없이 코드 상수로 관리한다. 세트 하나를 골라 큐(FIFO)로 넘긴다.
// - 디저트 + 음료: 디저트 고르기(없으면 바로 카운터) → 카운터 결제 → 테라스 or 퇴장
// - 음료만: 카운터 결제 → 테라스 or 퇴장
// - 디저트만: 디저트 고르기(없으면 땀방울 후 퇴장) → 키오스크 결제 → 테라스 or 퇴장
public static class NpcBehaviorRuleSet
{
    private static readonly eNpcBehaviorStepType[][] mRuleSets =
    {
        new[] { eNpcBehaviorStepType.BuyDessert, eNpcBehaviorStepType.PayCounter, eNpcBehaviorStepType.GoTerraceOrExit },
        new[] { eNpcBehaviorStepType.PayCounter, eNpcBehaviorStepType.GoTerraceOrExit },
        new[] { eNpcBehaviorStepType.BuyDessert, eNpcBehaviorStepType.PayKiosk, eNpcBehaviorStepType.GoTerraceOrExit },
    };

    public static Queue<eNpcBehaviorStepType> GetRandomQueue()
    {
        var ruleSet = mRuleSets[Random.Range(0, mRuleSets.Length)];
        return new Queue<eNpcBehaviorStepType>(ruleSet);
    }
}
