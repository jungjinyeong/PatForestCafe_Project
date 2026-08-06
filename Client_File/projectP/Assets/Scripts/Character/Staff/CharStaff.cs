// 주문받는 NPC(바리스타/점원) — 카운터에 고정 배치되는 정적 캐릭터.
// CharNpc와 달리 이동/AI 로직이 없다. 손님이 걸어와 상호작용하는 지점은
// 이 오브젝트의 자식으로 붙는 Trigger_Order Waypoint가 맡고, 실제 상호작용 처리는
// 기존 CharNpc/Waypoint 로직이 그대로 담당한다(웨이포인트 소유자가 가구에서 이 NPC로
// 바뀌어도 CharNpc는 웨이포인트 타입만 보고 동작하므로 별도 코드 변경이 필요 없다).
// 애니메이션은 이 오브젝트가 원래 갖고 있는 Unity Animator 컨트롤러가 자체 기본 상태로
// 재생하므로 별도 재생 로직이 필요 없다(Animator2D 래퍼 미사용).
public class CharStaff : CharBase
{
}
