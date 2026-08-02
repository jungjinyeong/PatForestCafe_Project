# 카페 시뮬레이션 — 손님 이동/웨이포인트 기능

`/setup-cafe-base`의 컨셉 지시가 손님 이동, 경로, 존(zone), 스폰, 슬롯 캡, 자동 결제, 입력과 관련되면 이 파일을 먼저 읽는다.

## 매핑표

| 컨셉 개념 | 실제 구현 |
|---|---|
| 손님 이동/상태 | `CharNpc` (`Assets/Scripts/Character/Npc/CharNpc.cs`) — 웨이포인트 경로 이동, 장애물 회피(`WaypointPathfinder`), 일시정지/재개 |
| 이동 경로/존 | `Waypoint`, `WaypointGroup` (`Assets/Scripts/WayPoint/`) — 카테고리(`SpwanPoint`, `Trigger`, `Wait`, `Exit`)로 정지 지점 정의, `WaypointGroup` 옵션 플래그로 존 단위 동작 분기(`IsSpecialOrderZone`, `IsBreadFreeRoamZone`) |
| 상호작용 슬롯(동시 처리 인원 제한) | `eConfigType.MaxSpecialOrderNpc` (Config) + `SpawnManager.CanAssignSpecialOrder` — 정해진 인원수만 특수 주문(상호작용) 상태가 되도록 캡 |
| 자동 결제 손님 | 슬롯 캡을 못 받은 NPC는 `Waypoint.eWaypointType.Trigger_Order`에서 `CharNpc.TriggerPause` → `ReceiveDefaultDrinkGold()` / `TryReceiveBreadGold()`로 자동 정산·자동 재개 |
| 손님 스폰/풀링 | `SpawnManager` — 그룹별 스폰 포인트, 오브젝트 풀, 자동 스폰 인터벌 |
| 입력(손님 탭) | `InputManager` — `CharNpc`에 `IsWaitingSpecialOrder`일 때만 `UIPopupOrderDetail` 오픈 |
| 입력(월드 오브젝트 탭 — NPC 외) | `InputManager`가 `Physics2D.OverlapPoint(mInteractableLayerMask)` 결과에서 `CharNpc`가 아니면 `Intaraction_BreadStand`인지 확인해 `UIPopupBreadSelect` 오픈. `mInteractableLayerMask = LayerMask.GetMask("Character", "UI")` — `CharNpc`는 Character 레이어(6), `Intaraction_BreadStand`류(UI/RectTransform 기반 오브젝트)는 UI 레이어(5)라 둘 다 포함해야 함. 새 상호작용 오브젝트를 추가할 때 레이어가 이 두 개도 아니면 마스크에 추가하거나(선호) 전용 레이어가 꼭 필요하면 `ProjectSettings` 수정 승인부터 받을 것 |

## WaypointGroup 존 옵션 확장 패턴

`WaypointGroup`은 그룹 단위 bool 옵션 + `GetPathWaypoints()` 분기로 존별 특수 동작을 표현한다. 새 존 동작이 필요하면 대부분 이 패턴으로 해결 가능하다 — `CharNpc`/`SpawnManager`는 `GetPathWaypoints()`가 반환하는 배열만 소비하므로 그쪽은 건드릴 필요가 없는 경우가 많다.

- `mIsSpecialOrderZone` — 이 존에서 스폰되는 NPC가 특수 주문(재료 조합 제작) 대상이 될 수 있는지. `SpawnManager.DecideSpecialOrder` / `CharNpc.TryEnterSpecialOrderWait`에서 참조.
- `mIsBreadFreeRoamZone` — 켜져 있으면 `GetPathWaypoints()`가 그룹 내 전체 경로 대신, `Trigger_Bread` 웨이포인트 중 하나를 랜덤으로 골라 `[선택된 빵 테이블, Exit]` 2개짜리 경로만 반환한다(`WaypointGroup.GetBreadFreeRoamPath()`). 빵 테이블 1곳 랜덤 방문 후 바로 퇴장하는 동작을 구현한 사례.
  - 도착 후 실제로 멈춰서 상호작용할지는 `CharNpc.mBreadStopChance`(기본 0.5) 판정이 여전히 적용됨 — 존 옵션은 "어디를 들를지"만 결정하고, "들렀을 때 반드시 상호작용하는지"는 결정하지 않는다. 필요하면 이 확률을 존별로 끄는 옵션을 별도로 논의할 것.

`eWaypointType.SpawnPoint_Bread`/`Exit_Bread`는 enum에 정의만 되어 있고 아직 어떤 로직도 이 세부 타입을 구분해서 쓰지 않는다(카테고리 `SpwanPoint`/`Exit`로만 취급됨). 세부 타입 분기가 필요해지면 이 부분이 갭이다.

## 참고

- `Waypoint.eWaypointCategoryType`/`eWaypointType`는 상위 16비트 시프트로 카테고리를 인코딩한다(`GetCategoryType()`). 새 트리거 타입 추가 시 이 인코딩 규칙을 따를 것.
- 경로 탐색은 `WaypointPathfinder.FindPath` (A*, `Waypoint.Neighbors` 기반) — 장애물 사이 목적지까지 우회 경로가 필요하면 여기를 확장.
