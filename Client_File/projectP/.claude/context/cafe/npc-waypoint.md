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
| 자동 결제 손님 | 이제 모든 손님이 자동 결제 대상이다(예전엔 슬롯 캡을 못 받은 NPC만). `Waypoint.eWaypointType.Trigger_Order`에서 `CharNpc.TriggerPause` → `ProcessOrderPayment()`(`ReceiveRandomUnlockedDrinkGold()` 무조건 호출) / `TryReceiveBreadGold()`로 자동 정산·자동 재개. `Trigger` 도착은 항상 큐가 의도한 행동이라 예전의 `mBreadStopChance` 랜덤 스킵은 삭제됨(도착하면 무조건 상호작용). **2026-08 변경**: 예전엔 항상 `DefaultDrink` 가격만 청구했으나, 이제 `RecipeBookModel.GetDiscoveredTids()`(레시피북으로 해금된 레시피) + `DrinkModel.DefaultDrink`를 합친 풀에서 `DrinkRow.Weight` 가중치에 따라 하나를 골라 그 `MenuItemRow.Price`를 청구한다(`CharNpc.ReceiveRandomUnlockedDrinkGold`) — 새 Model 없이 기존 `RecipeBookModel`/`DrinkModel` 그대로 재사용, `Weight` 컬럼 추가는 `order-menu.md` 참고 |
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
- **스페셜 주문 기획 전체 삭제(2026-08)**: `WaypointGroup.IsSpecialOrderZone`, `Waypoint.eWaypointType.Wait_SpecialOrder`/카테고리 `Wait` 자체, `CharNpc`의 `ISpecialOrderWaiter` 구현·`ApplySpecialOrderParam`·`TryEnterSpecialOrderWait`·`IsSpecialOrderCounterZone`, `LobbyCharUI`의 `IsSpecialOrderActive`/`DesiredDrinkTid`/`SetSpecialOrderActive`, `SpawnManager`의 슬롯 캡 로직 모두 제거됨. 손님이 특정 음료를 요구하며 대기하는 개념 자체가 없다 — 이제 손님은 카운터(`Trigger_Order`) 도착 시 무조건 기본 음료 금액만 자동 결제한다. 재료 조합으로 음료를 만드는 UI(`UIPopupDrinkRecipeProduction`, 2026-08 `UIPopupSpecialDrinkProduction`에서 개명)는 삭제되지 않고 "음료 레시피 제작"(연구소) 기능으로 재사용됨 — 자세한 내용은 `order-menu.md` 참고.
- `IsTerraceZone`은 여전히 코드에서 읽는 곳이 없는 씬 저작용 플래그다(테라스 = 그냥 다음 체인 존의 Exit). NPC가 테라스에서 실제로 머무르는 연출은 미구현 — 필요해지면 이 플래그를 실제로 읽는 로직을 추가해야 하는 진짜 갭.

`eWaypointType.SpawnPoint_Order`/`SpawnPoint_Bread`/`Exit_Order`/`Exit_Bread`(세부 타입, 아무 로직도 구분해서 쓰지 않던 죽은 값)는 삭제됨(2026-08) — `SpwanPoint`/`Exit`는 카테고리 대표값(`SpwanPoint_Min`/`Exit_Min`)만 남아 있고 앞으로도 카테고리 단위로만 취급된다. 세부 타입 분기가 필요해지면 다시 추가할 것.

## 갭 채우기 사례 — CharStaff 작업속도 + 픽업 게이지바 (2026-08)

컨셉 지시("카운터 CharStaff에 게이지바, 작업속도 테이블 추가, 음료 픽업 속도가 작업속도에 따라 달라짐")를 다음으로 채웠다(CTable 신규 생성 사용자 승인됨, WorkSpeed 배율 방식·기준시간 2초 코드 상수 승인됨):

1. 새 CTable `StaffRow`/`StaffTable`(`Tid, Name, WorkSpeed(float)`) + `Assets/CSV/Staff.csv`(현재 Tid=1 "일반 바리스타" WorkSpeed=1 한 줄만). `DrinkMaterialRow`와 동일한 단순 패턴.
2. `CharStaff`(`Assets/Scripts/Character/Staff/CharStaff.cs`)에 `mTid`(SerializeField) 추가, `Init()`에서 `GameInstance.Table.Get<CTable.StaffRow>(mTid)`로 `StaffRow` 조회. `WorkSpeed` 프로퍼티(행이 없거나 0 이하면 1.0 폴백).
3. **픽업 게이지바는 전용 프리팹/스프라이트 에셋 없이 런타임에 절차적으로 생성**(`SpriteRenderer` 2장 — 배경/채움, 1x1 흰색 `Texture2D`를 코드에서 `Sprite.Create`). `CharStaff`가 씬에 직접 배치되는 캐릭터라 전용 프리팹 자체가 없어(=만들 프리팹이 없어) 이 방식을 택함 — 새 UI 프리팹이 필요한 다른 갭과는 다른 케이스. 채움 스프라이트는 중앙 기준 스케일로 늘어나는 러프 버전(왼쪽 정렬 아님).
4. `CharStaff.BeginPickup(Action onComplete)` — `BaseDrinkPickupSeconds(2f, 코드 상수) / WorkSpeed`만큼 `Observable.EveryUpdate()`로 게이지를 채우고 완료 시 콜백. `CharNpc.TriggerPause()`의 `Trigger_Order` 분기가 예전엔 곧장 `ProcessOrderPayment()`를 호출했지만, 이제 `triggerWaypoint.GetComponentInParent<CharStaff>()`로 소유 스태프를 찾아 `BeginPickup(ProcessOrderPayment)`를 거치도록 변경(스태프를 못 찾으면 예전처럼 즉시 처리 — 폴백).
5. `CharStaff.Init()` 호출 지점은 `Intaraction_BreadStand`와 동일하게 `GameModeLobby+FSM.OnEnterInit()`에 `InitCharStaffs()`(`FindObjectsByType<CharStaff>` 순회) 추가.
6. **동시 도착 큐잉(2026-08 추가)**: 카운터(스태프) 하나당 게이지가 하나뿐이라, 같은 층 같은 카운터에 손님이 동시에 도착하면 원래 뒤에 온 손님이 게이지를 리셋시키는 문제가 있었다. `CharStaff`에 `mPickupQueue`(FIFO)를 추가해 `BeginPickup()` 호출 시 이미 진행 중이면 대기열에 넣고, 진행 중인 픽업이 끝날 때(`StartPickup()` 내부 완료 분기) 큐에서 다음 콜백을 꺼내 이어서 처리하도록 변경. 층별로 `CharStaff` 인스턴스가 분리돼 있어 이 큐도 층마다(정확히는 카운터마다) 독립적이다.

**미해결**: 씬(`Assets/Scenes/`)에 아직 `CharStaff` 오브젝트 자체가 배치돼 있지 않다(그레핑 결과 0건) — 이 로직이 실제로 동작하려면 카운터 위치에 `CharStaff` 컴포넌트 + 자식 `Trigger_Order` 웨이포인트를 에디터에서 배치해야 한다. 코드/절차적 게이지는 이번에 완료했지만 씬 배치는 에디터 작업이라 범위 밖([[feedback-editor-scope-boundary]]).

## 갭 채우기 사례 — 빵 재고 없음 조기 퇴장 + 땀방울 UI (2026-08)

컨셉 지시("진열대에 빵이 없으면 좀 더 둘러보다가 땀방울을 흘리고 퇴장, LobbyCharUI에 땀방울 on/off만 — 애니메이션은 보류")를 다음으로 채웠다. 새 CTable/Model 없이 기존 `BreadModel`/`AdvanceBehaviorQueue`만으로 해결됨:

1. `CharNpc.TriggerPause()`의 `Trigger_Bread` 분기에서 `GameInstance.Model.Bread.GetCount(triggerWaypoint.TableId)?.Value`로 재고를 먼저 동기 확인. 재고 있으면 기존과 동일(`CEvent.BreadPickup` 발행 → `Intaraction_BreadStand`가 비동기로 처리). 재고 0이면 `BeginBreadUnavailableFlow()`로 분기.
2. **"둘러보기"는 실제 이동이 아니라 Idle 대기로 구현**했다 — `eWaypointCategoryType`이 `SpwanPoint`/`Trigger`/`Exit` 세 개뿐이라(옛 `Wait` 카테고리는 삭제됨), 다른 웨이포인트로 실제로 이동시키면 그 지점이 `Trigger`일 경우 도착 즉시 `ProcessArrivalCategoryLogic`이 다시 `TriggerPause`를 걸어 의도치 않은 재상호작용이 발생한다. 순수 대기(`Observable.Timer`, 기존 `LingerInTerrace`와 동일 패턴)로 이 문제를 피함.
3. `BeginBreadUnavailableFlow()`(둘러보기 대기) → `ShowSweatThenExit()`(`LobbyCharUI.SetSweatIconActive(true)`, 1.2초 대기) → `ExitEarlyDueToNoBread()`(아이콘 끄기 + `mBehaviorQueue.Clear()` + `ResumeFromPause()`). 큐를 비운 채 기존 `AdvanceBehaviorQueue()`를 그대로 호출하는 것만으로 "남은 큐(예: 음료 주문) 포기하고 곧장 Exit 웨이포인트로 향함 → `TryMoveToNextGroup()`으로 테라스 이동/디스폰"까지 전부 기존 로직 재사용.
4. `LobbyCharUI`에 `mSweatIconObj`(GameObject on/off) + `SetSweatIconActive(bool)` 추가 — `mCoinIconObj`/`mSatisfactionIconObj`와 완전히 동일한 패턴. 애니메이션 자체는 사용자가 보류 의사를 밝혀 추가하지 않음(필요해지면 `mAnimator2D.PlayAnimation(...)` 호출 지점을 `BeginBreadUnavailableFlow`/`ShowSweatThenExit`에 추가하면 됨).
5. **둘러보기 대기 시간을 Config로 이동(2026-08 추가)**: 처음엔 `mBreadLookAroundMinSeconds`/`mBreadLookAroundMaxSeconds`(1.5~3초 랜덤, `CharNpc` SerializeField)였는데, 사용자가 "땀방울 흘리기 전 0.8초 정도로 고정하고 config 데이터에 넣어달라"고 요청. `eConfigType`에 `BreadUnavailablePauseMs`(int, 밀리초 단위 — `ConfigData`가 `SerializableDictionary<eConfigType, int>`라 float 저장이 안 돼 ms 정수로 저장 후 코드에서 `/1000f`) 추가, `Assets/Resources/ConfigData.asset`에 값 800(0.8초) 등록. **`ConfigData.asset`은 CLAUDE.md 금지 목록에 없지만(`Assets/StreamingAssets/config.json`과는 다른 파일) `SerializableDictionary`가 `m_keys`/`m_values`를 blittable 배열의 hex-packed 텍스트로 직렬화해 사람이 읽기 어렵다 — 기존 3개 항목(`000000000100000002000000`/`030000001127000003000000`, 4바이트 LE 정수 나열, 길이 프리픽스 없음)을 역산해서 검증한 뒤 4번째 키/값(`03000000`/`20030000` = enum 3, 800)을 직접 이어붙였다. 새 `eConfigType` 값은 반드시 기존 값 뒤에 추가해야 한다(중간 삽입 시 기존 저장된 enum 정수 순서가 깨짐).

**참고**: 땀 아이콘 오브젝트(`mSweatIconObj`)는 `LobbyCharUI` 프리팹에 아직 연결 안 돼 있다(코드만 완료) — 씬/프리팹에서 코인/만족 아이콘과 나란히 새 아이콘 오브젝트를 만들고 인스펙터에 드래그해야 실제로 보인다(에디터 작업, 이번 세션 범위 밖).

## 참고

- `Waypoint.eWaypointCategoryType`/`eWaypointType`는 상위 16비트 시프트로 카테고리를 인코딩한다(`GetCategoryType()`). 새 트리거 타입 추가 시 이 인코딩 규칙을 따를 것.
- 경로 탐색은 `WaypointPathfinder.FindPath` (A*, `Waypoint.Neighbors` 기반) — 장애물 사이 목적지까지 우회 경로가 필요하면 여기를 확장.
- **가구/NPC 이동 시 주의**: `Waypoint.Neighbors`는 씬 오브젝트 참조 배열이라, 가구나 `CharStaff`(및 그 자식 웨이포인트)를 옮기거나 복제해도 자동으로 갱신되지 않는다. 재배치하면 해당 웨이포인트의 `Neighbors`를 에디터에서 수동으로 다시 연결해야 A* 경로가 끊기지 않는다 — 자동 그래프 복구는 범위 밖.
