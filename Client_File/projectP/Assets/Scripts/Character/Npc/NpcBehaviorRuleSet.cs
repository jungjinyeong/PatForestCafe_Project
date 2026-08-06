using System.Collections.Generic;
using UnityEngine;

// NPC 스폰 시 실행할 행동 규칙(빵 구입 -> 음료 주문 -> 테라스 퇴장 등)을 고정 세트로 정의한다.
// CTable 없이 코드 상수로 관리(사용자 승인된 방식) — 스폰 시 세트 하나를 골라 큐(FIFO)로 반환한다.
public static class NpcBehaviorRuleSet
{
    private static readonly eNpcBehaviorStepType[][] mRuleSets =
    {
        new[] { eNpcBehaviorStepType.BuyBread, eNpcBehaviorStepType.OrderDrink, eNpcBehaviorStepType.ExitToTerrace },
        new[] { eNpcBehaviorStepType.OrderDrink, eNpcBehaviorStepType.ExitToTerrace },
        new[] { eNpcBehaviorStepType.BuyBread, eNpcBehaviorStepType.ExitToTerrace },
    };

    public static Queue<eNpcBehaviorStepType> GetRandomQueue()
    {
        var ruleSet = mRuleSets[Random.Range(0, mRuleSets.Length)];
        return new Queue<eNpcBehaviorStepType>(ruleSet);
    }
}
