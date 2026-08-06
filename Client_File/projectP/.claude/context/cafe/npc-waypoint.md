# 카페 시뮬레이션 — 손님 이동/웨이포인트 기능

`/setup-cafe-base`의 컨셉 지시가 손님 이동, 경로, 존(zone), 스폰, 슬롯 캡, 자동 결제, 입력과 관련되면 이 파일을 먼저 읽는다.

## 매핑표

| 컨셉 개념 | 실제 구현 |
|---|---|
| 손님 이동/상태 | `CharNpc` (`Assets/Scripts/Character/Npc/CharNpc.cs`) — 존 내부에서 행동 큐 기반 자유 배회, 장애물 회피(`WaypointPathfinder`), 일시정지/재개 |
| 이동 경로/존 | `Waypoint`, `WaypointGroup` (`Assets/Scripts/WayPoint/`) — 카테고리(`SpwanPoint`, `Trigger`, `Exit`)로 지점 정의(2026-08 스페셜 주문 삭제로 `Wait` 카테고리 자체가 제거됨). `SpwanPoint`/`Exit`는 씬 고정(`mStaticWaypoints`), `Trigger`(가구 또는 주문받는 NPC의 자식으로 배치)는 `WaypointGroup.mZoneAreas`(`PlacementGridArea[]`) 범위 내에서 런타임 동적 스캔(`RescanDynamicWaypoints`)으로 수집 |
| 주문받는 NPC(바리스타/점원) | `CharStaff` (`Assets/Scripts/Character/Staff/CharStaff.cs`, 2026-08 신규) — `CharBase` 상속, 이동/AI 없이 카운터에 고정 배치되는 정적 캐릭터. `CharNpc`처럼 스폰/풀링 대상이 아니라 씬에 직접 배치. `Trigger_Order` 웨이포인트를 자식으로 가지며(가구가 아니라 이 NPC 자식으로 배치), `RescanDynamicWaypoints()`는 부모가 가구인지 NPC인지 구분하지 않고 위치/카테고리만으로 스캔하므로 별도 코드 없이 그대로 인식됨 |
| 손님 행동 규칙(스폰 시 큐) | `eNpcBehaviorStepType`(`BuyBread`/`OrderDrink`/`ExitToTerrace`), `NpcBehaviorRuleSet.GetRandomQueue()`(`Assets/Scripts/Character/Npc/`) — 스폰 시 고정 규칙 세트 중 하나를 무작위로 골라 FIFO 큐로 부여. `CharNpc.AdvanceBehaviorQueue()`가 큐를 하나씩 꺼내 `TryGetTargetForStep()`으로 목표 웨이포인트를 찾아 이동(못 찾으면 다음 단계로 스킵, 큐 소진 시 Exit로 퇴장). CTable 없이 C# 코드 상수로만 관리(사용자 승인된 방식) |
| 자유 배회 목표 탐색 | `WaypointGroup.TryGetRandomWaypoint(category, specificType, out result)` — 옛 `GetPathWaypoints()`/`GetBreadFreeRoamPath()`/`mIsBreadFreeRoamZone`을 대체. 정해진 순서 없이, 매 단계마다 조건에 맞는 웨이포인트 중 무작위로 하나를 골라 기존 A*(`WaypointPathfinder`)로 이동 — 이게 "정해진 구역을 자유롭게 돌아다니는" 동작의 실체 |
| 가구↔웨이포인트 연동 | `PlacementGridArea.Bounds`/`Contains(Vector3)` — 존의 물리적 영역. `WaypointGroup.Init()`이 `GameInstance.Model.Placement.IsPlacing`가 true→false로 바뀔 때(배치 확정/취소 직후)마다 재스캔 구독. `UIRootLobby.RegisterWaypointGroups()`가 각 존의 `Init()`을 호출해 최초 스캔을 가동시킴 |
| 상호작용 슬롯(동시 처리 인원 제한) | **삭제됨(2026-08)** — 스페셜 주문 기획 삭제로 `SpawnManager.DecideSpecialOrder`/`CanAssignSpecialOrder`/`HasSpecialOrderZone`, `eConfigType.MaxSpecialOrderNpc`(선언은 config.json 위험 회피를 위해 남겨두었으나 미사용) 모두 제거됨 — 더 이상 동시 처리 인원을 캡할 대상 자체가 없음 |
| 자동 결제 손님 | 이제 모든 손님이 자동 결제 대상이다(예전엔 슬롯 캡을 못 받은 NPC만). `Waypoint.eWaypointType.Trigger_Order`에서 `CharNpc.TriggerPause` → `ProcessOrderPayment()`(항상 `ReceiveDefaultDrinkGold()` 무조건 호출) / `TryReceiveBreadGold()`로 자동 정산·자동 재개. `Trigger` 도착은 항상 큐가 의도한 행동이라 예전의 `mBreadStopChance` 랜덤 스킵은 삭제됨(도착하면 무조건 상호작용) |
| 손님 스폰/풀링 | `SpawnManager` — `WaypointGroup.GetSpawnPoints()`로 스폰 포인트를 얻고 `CharNpc.Init(group, spawnPoint)` 호출(스폰 시 행동 큐도 함께 부여), 오브젝트 풀, 자동 스폰 인터벌 |
| 입력(손님 탭) | **삭제됨(2026-08)** — 스페셜 주문 삭제로 손님이 플레이어 상호작용을 "대기"하는 상태 자체가 없어짐. `InputManager`의 NPC 탭 분기는 `if (npc != null) return;`만 남은 자리표시자(향후 다른 NPC 상호작용용, TODO 주석 존재). `UIPopupOrderDetail`은 도달 불가능한 죽은 코드로만 남음(프리팹 GUID 보존 목적, 삭제하지 않음) |
| 입력(월드 오브젝트 탭 — NPC 외) | `InputManager`가 `Physics2D.OverlapPoint(mInteractableLayerMask)` 결과에서 `CharNpc`가 아니면 `Intaraction_BreadStand`인지 확인해 `UIPopupBreadSelect` 오픈. `mInteractableLayerMask = LayerMask.GetMask("Character", "UI")` — `CharNpc`는 Character 레이어(6), `Intaraction_BreadStand`류(UI/RectTransform 기반 오브젝트)는 UI 레이어(5)라 둘 다 포함해야 함. 새 상호작용 오브젝트를 추가할 때 레이어가 이 두 개도 아니면 마스크에 추가하거나(선호) 전용 레이어가 꼭 필요하면 `ProjectSettings` 수정 승인부터 받을 것 |

## 행동 큐 + 자유 배회 모델 (2026-08 리팩터링 이후)

옛 구조는 `WaypointGroup`이 그룹 단위 bool 옵션(`mIsBreadFreeRoamZone` 등) + `GetPathWaypoints()` 분기로 "정해진 순서의 경로"를 반환하고, `CharNpc`가 배열 인덱스를 앞뒤로 순회하는 방식이었다. 컨셉 지시("waypoint는 가구 프리팹에 배치, NPC는 정해진 구역을 자유롭게 배회, 스폰 시 행동 규칙 큐 실행")에 맞춰 다음으로 교체됨:

- **가구/주문받는 NPC→웨이포인트**: `Trigger` 카테고리 웨이포인트(`Trigger_Bread`/`Trigger_Order`)는 더 이상 `WaypointGroup`의 씬 고정 자식이 아니라 가구 프리팹(빵 진열대) 또는 주문받는 NPC(`CharStaff`, 카운터)의 자식으로 배치. `WaypointGroup.mZoneAreas`(`PlacementGridArea[]`)가 이 존의 물리적 범위를 정의하고, `RescanDynamicWaypoints()`가 그 범위 안의 `Trigger` 웨이포인트를 런타임에 스캔해 풀을 구성 — 부모가 가구인지 NPC인지는 구분하지 않고 카테고리+위치만 본다. 가구 배치가 확정/취소될 때(`PlacementModel.IsPlacing` true→false)마다 자동 재스캔.
- **주문받는 NPC 추가(2026-08)**: 손님이 카운터에서 상호작용하는 대상이 "카운터 가구"가 아니라 "주문받는 NPC(`CharStaff`)"가 되도록 컨셉이 명확해짐에 따라 `CharStaff` 클래스 신규 추가. `CharNpc`의 `TriggerPause`/`ProcessOrderPayment` 등 상호작용 로직은 `Waypoint.WaypointType`만 보고 동작하므로 웨이포인트 소유자가 가구에서 NPC로 바뀌어도 코드 변경이 전혀 필요 없었다 — 순수하게 "어느 오브젝트의 자식으로 웨이포인트를 두는지"의 문제였음. `CharStaff`는 `SpawnManager` 풀링 대상이 아니며(손님과 달리 상시 고정 배치), 씬에 직접 배치한다.
- **자유 배회**: `WaypointGroup.TryGetRandomWaypoint(category, specificType, out result)`가 조건에 맞는 웨이포인트 중 하나를 무작위로 반환하고, `CharNpc`는 기존 `WaypointPathfinder`(A*)로 그곳까지 이동만 한다 — 정해진 순서 없이 매번 다른 목적지를 고르는 것 자체가 "자유롭게 돌아다니는" 동작이다. 씬 고정 지점(`SpwanPoint`/`Exit`)은 `mStaticWaypoints`에서, 가구 지점(`Trigger`)은 동적 풀에서 검색.
- **행동 규칙 큐**: 스폰 시 `NpcBehaviorRuleSet.GetRandomQueue()`가 고정 규칙 세트(C# 코드 상수, CTable 아님) 중 하나를 무작위로 골라 `eNpcBehaviorStepType` FIFO 큐로 부여(`CharNpc.mBehaviorQueue`). `AdvanceBehaviorQueue()`가 하나씩 꺼내 `TryGetTargetForStep()`으로 목표 카테고리/타입을 매핑(`BuyBread→Trigger_Bread`, `OrderDrink→Trigger_Order`, `ExitToTerrace→Exit`)하고 이동, 도착하면 항상 상호작용(예전 `mBreadStopChance` 랜덤 스킵은 삭제 — 큐에서 나온 목표는 항상 의도된 행동이므로). 큐가 소진되면 Exit로 향해 퇴장.
- **존 간 이동은 그대로**: `WaypointGroup.Order`/`WayPointManager.GetNextGroup` 체인은 변경 없이 재사용 — `ExitToTerrace` 단계 실행은 결국 현재 존의 Exit 도착이고, 이는 기존 `TryMoveToNextGroup`/`DespawnToPool` 로직으로 다음 존(테라스 등)에 진입하거나 디스폰된다. `mBehaviorQueue`는 존 이동과 무관하게 유지되므로, 새 존에 들어가도 남은 큐를 계속 이어서 실행한다.
- **스페셜 주문 기획 전체 삭제(2026-08)**: `WaypointGroup.IsSpecialOrderZone`, `Waypoint.eWaypointType.Wait_SpecialOrder`/카테고리 `Wait` 자체, `CharNpc`의 `ISpecialOrderWaiter` 구현·`ApplySpecialOrderParam`·`TryEnterSpecialOrderWait`·`IsSpecialOrderCounterZone`, `LobbyCharUI`의 `IsSpecialOrderActive`/`DesiredDrinkTid`/`SetSpecialOrderActive`, `SpawnManager`의 슬롯 캡 로직 모두 제거됨. 손님이 특정 음료를 요구하며 대기하는 개념 자체가 없다 — 이제 손님은 카운터(`Trigger_Order`) 도착 시 무조건 기본 음료 금액만 자동 결제한다. 재료 조합으로 음료를 만드는 UI(`UIPopupSpecialDrinkProduction`)는 삭제되지 않고 "레시피 개발" 기능으로 재사용됨 — 자세한 내용은 `order-menu.md` 참고.
- `IsTerraceZone`은 여전히 코드에서 읽는 곳이 없는 씬 저작용 플래그다(테라스 = 그냥 다음 체인 존의 Exit). NPC가 테라스에서 실제로 머무르는 연출은 미구현 — 필요해지면 이 플래그를 실제로 읽는 로직을 추가해야 하는 진짜 갭.

`eWaypointType.SpawnPoint_Order`/`SpawnPoint_Bread`/`Exit_Order`/`Exit_Bread`(세부 타입, 아무 로직도 구분해서 쓰지 않던 죽은 값)는 삭제됨(2026-08) — `SpwanPoint`/`Exit`는 카테고리 대표값(`SpwanPoint_Min`/`Exit_Min`)만 남아 있고 앞으로도 카테고리 단위로만 취급된다. 세부 타입 분기가 필요해지면 다시 추가할 것.

## 참고

- `Waypoint.eWaypointCategoryType`/`eWaypointType`는 상위 16비트 시프트로 카테고리를 인코딩한다(`GetCategoryType()`). 새 트리거 타입 추가 시 이 인코딩 규칙을 따를 것.
- 경로 탐색은 `WaypointPathfinder.FindPath` (A*, `Waypoint.Neighbors` 기반) — 장애물 사이 목적지까지 우회 경로가 필요하면 여기를 확장.
- **가구/NPC 이동 시 주의**: `Waypoint.Neighbors`는 씬 오브젝트 참조 배열이라, 가구나 `CharStaff`(및 그 자식 웨이포인트)를 옮기거나 복제해도 자동으로 갱신되지 않는다. 재배치하면 해당 웨이포인트의 `Neighbors`를 에디터에서 수동으로 다시 연결해야 A* 경로가 끊기지 않는다 — 자동 그래프 복구는 범위 밖.
