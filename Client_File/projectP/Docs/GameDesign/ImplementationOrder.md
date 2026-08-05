# 기획 구현 순서 (현재 프로젝트 분석 기준)

이 문서는 `Docs/GameDesign/` 하위 기획 문서(장르/개발목표/게임환경/게임로비/카운터/특별음료제작/가공섬)를
현재 코드베이스 상태에 맞춰 어떤 순서로 구현하면 좋을지 정리한 문서입니다.
새로운 데이터 테이블(Assets/CTable, Assets/CSV)이 필요한 항목은 해당 파일이 수정 금지 대상이므로
착수 전 반드시 먼저 논의가 필요합니다.

## 현재 구현 상태 요약

| 영역 | 상태 | 관련 코드 |
|---|---|---|
| NPC 이동/웨이포인트 | 구현됨 | `CharNpc`, `WaypointGroup`, `WaypointPathfinder`, `SpawnManager` |
| 빵 진열/선택/픽업 | 구현됨 | `Intaraction_BreadStand`, `Intaraction_Bread`, `UIPopupBreadSelect`, `BreadModel` |
| 기본 음료 자동 결제 | 구현됨 (단순 형태) | `CharNpc.ProcessArrivalCategoryLogic` (`Trigger_Order`) |
| 특별 음료 주문 대기 + 제작 UI | 구현됨 | `ISpecialOrderWaiter`, `Wait_SpecialOrder`, `UIPopupSpecialDrinkProduction` |
| 그리드 배치 시스템 | 구현됨 | `PlacementGridArea`, `PlaceableObject`, `PlacementModel(+Ctrl)` |
| Day/Night 사이클 | 구현됨 | `DayNightManager`, `UIDayNightBg`, `TimeManager` |
| 재화/저장 시스템 | 구현됨 | `ItemModel`, `SaveManager`, `SaveData` |
| 가공섬 | 진입점만 존재, 콘텐츠 없음 | `UIRootMaterialIsland` (빈 껍데기) |
| 카운터 구역 분리(특별/일반), 결제 연출(동전/만족 아이콘) | 코드 구현됨, 프리팹/씬 연결 필요 | `CharNpc`, `LobbyCharUI`, `WaypointGroup.IsTerraceZone` |
| 오프라인 수익 정산 | 코드 구현됨 (고정 config 값 기반), 프리팹/씬 연결 필요 | `SaveManager`, `SaveData`, `UIPopupOfflineIncome` |
| 레시피 도감(수집) | 미구현 | - |
| 업그레이드 시스템(생산성/수익) | 미구현 | - |
| 가공섬 콘텐츠(미니게임/상점/재배형 공방/고용) | 미구현 | - |

## 구현 순서

### 1단계 — 카운터 시스템 정식화 (진행 중)
현재는 `Trigger_Order` 웨이포인트 도착 시 즉시 골드가 지급되는 단순 로직뿐이며,
기획서의 "특별 주문 구역/일반 구역 분리", "결제 연출(동전/만족 아이콘)", "퇴장 후 테라스 이동"은 없다.
로비 NPC 흐름의 마지막 단계이자 이후 오프라인 수익 정산의 기반이 되므로 가장 먼저 정리한다.

- [x] **카운터 구역 분리** — `CharNpc.IsSpecialOrderCounterZone()` 추가: `Trigger_Order` 웨이포인트이면서 `mCurrentGroup.IsSpecialOrderZone == true`인 경우를 특별 주문 손님 구역으로 판정. 해당 구역에서는 특별 음료 제작 시 이미 결제가 끝난 것으로 보고 카운터에서 기본 음료 금액을 재청구하지 않도록 `ProcessOrderPayment()`에서 분기 처리함. (`Assets/Scripts/Character/Npc/CharNpc.cs`)
- [x] **결제 연출** — `LobbyCharUI.PlayPaymentEffect(Action onComplete)` 추가: 동전 아이콘 표시 → (딜레이) → 만족 아이콘 표시 → (딜레이) → 완료 콜백 순서로 UniRx 타이머 기반 연출. `CharNpc.ProcessOrderPayment()`에서 결제 후 이 연출을 재생하고, 연출이 끝나면 `ResumeFromPause()`로 이동을 재개하도록 연결함. (`Assets/Scripts/UI/Lobby/LobbyCharUI.cs`)
  - **후속 작업(에디터)**: `mCoinIconObj` / `mSatisfactionIconObj` 필드에 실제 동전·만족 아이콘 오브젝트를 인스펙터에서 연결해야 화면에 표시됨. 현재는 필드가 비어 있어도 null 체크로 안전하게 스킵됨.
- [x] **퇴장 후 테라스 이동 (기반 작업)** — `WaypointGroup.IsTerraceZone` 플래그 추가 (`IsSpecialOrderZone`/`IsBreadFreeRoamZone`과 동일한 패턴). NPC 이동은 이미 `Exit` 웨이포인트 도착 시 `WayPointManager.GetNextGroup(Order)`로 다음 그룹을 자동으로 찾아가는 범용 체인 구조(`CharNpc.TryMoveToNextGroup`)로 되어 있어, 별도 이동 로직 없이도 카운터보다 큰 Order 값을 가진 테라스 그룹을 씬에 배치하면 자동으로 연결됨.
  - **후속 작업(에디터)**: 씬에 테라스용 `WaypointGroup`을 새로 만들고 `IsTerraceZone`을 체크, 카운터 그룹보다 큰 `Order` 값을 부여 + 마지막에 `Exit` 타입 웨이포인트 배치 필요. (코드 작업 아님, Unity 에디터에서 진행)

### 2단계 — 오프라인 수익 정산 (진행 중)
카운터 캐릭터 능력치(조리 속도/계산 속도) 시스템이 아직 존재하지 않아, 사용자 확인 후
**능력치 기반 계산 대신 고정 config 값으로 초당 수익을 계산하는 임시 방식**으로 진행함.
추후 능력치 시스템이 생기면 `SaveManager.mOfflineCoinPerSecond`를 능력치 기반 계산으로 교체하면 된다.

- [x] **마지막 접속 시각 저장** — `SaveData.LastSaveUnixSeconds` 추가, `SaveManager.Save()`에서 `DateTimeOffset.UtcNow.ToUnixTimeSeconds()`로 기록. (`Assets/Scripts/Manager/SaveData.cs`, `SaveManager.cs`)
- [x] **경과 시간 기반 코인 계산** — `SaveManager.Load()` → `ApplyOfflineIncome()`: 마지막 저장 시각과 현재 시각의 차를 `mOfflineMaxSeconds`(기본 8시간)로 clamp한 뒤, `mOfflineCoinPerSecond`(기본 1)를 곱해 골드 지급. 계산 결과는 `TryConsumePendingOfflineIncome()`으로 1회 소비 가능하게 캐싱함. (`Assets/Scripts/Manager/SaveManager.cs`)
- [x] **오프라인 수익 정산 알림 UI** — `UIPopupOfflineIncome` 신규 추가, `eUIType.UIPopupOfflineIncome` 등록. `GameModeLobby+FSM.OnEnterOpenLobbyUI()`에서 로비 UI를 연 뒤 `ShowPendingOfflineIncomeIfAny()`로 대기 중인 정산 결과가 있으면 팝업 오픈. (`Assets/Scripts/UI/Common/UIPopupOfflineIncome.cs`, `Assets/Scripts/GameMode/GameModeLobby+FSM.cs`)
- [x] **팝업 프리팹(러프)** — `Assets/Datas/UI/Lobby/Popup/UI_Popup_OfflineIncome.prefab` 신규 추가. 기존 `UI_Popup_OrderDetail.prefab` 구조를 참고해 동일한 버튼/폰트 에셋을 재사용한 최소 구성(배경 딤 처리 + 패널 + 안내 텍스트 + 확인/닫기 버튼)으로 손으로 작성함. 스크립트 메타파일(`UIPopupOfflineIncome.cs.meta`)도 함께 생성해 GUID를 고정함.
  - **후속 작업(에디터, 필수)**: 이 프로젝트의 다른 팝업들(`UI_Popup_OrderDetail` 등)은 `UIPathInfo`/`Resources.Load`가 아니라 `UIManager` 프리팹(`Assets/Resources/UIManager.prefab`)의 `mCachedUIDic`에 직접 드래그해 등록하는 방식으로 동작함. `UIManager.prefab`은 손으로 편집하기에 너무 크고 위험해 건드리지 않았으므로, Unity 에디터에서 이 프리팹을 Popup 캔버스 하위에 배치하고 `mCachedUIDic`에 `eUIType.UIPopupOfflineIncome → UI_Popup_OfflineIncome`으로 등록해야 실제로 팝업이 뜸(등록 전까지 골드 지급 자체는 정상 동작).
  - **후속 작업(비주얼)**: 러프 프리팹이므로 배치/사이즈/색상은 가안임. 실제 아트 리소스로 교체 필요.
  - **후속 작업(밸런스)**: `SaveManager` 인스펙터의 `mOfflineCoinPerSecond`(기본 1), `mOfflineMaxSeconds`(기본 8시간) 값은 임시 기본값이므로 기획 확정 후 조정 필요.

### 3단계 — 레시피 도감 (수집 요소)
특별 음료 제작(`UIPopupSpecialDrinkProduction`)에서 이미 레시피 매칭 로직이 동작하고 있으므로,
성공 시점에 "발견 여부"를 기록하기만 하면 되어 비교적 적은 비용으로 추가 가능하다.
- 레시피 성공 시 발견 목록에 기록하는 모델 추가 (`ViewModel/Drink` 또는 신규 `RecipeBookModel`)
- 도감 열람 UI 추가

### 4단계 — 업그레이드 시스템
카운터 능력치(2단계)와 재화 시스템이 준비된 이후에 붙여야 효과를 수치로 연결할 수 있다.
- 업그레이드 대상(조리 속도/계산 속도/생산성 등) 및 비용 테이블 설계 (테이블 추가 필요 → 사전 논의 대상)
- 업그레이드 UI 및 적용 로직 추가

### 5단계 — 가공섬 콘텐츠 확장
`UIRootMaterialIsland`가 빈 껍데기 상태이며, 로비/카운터 루프와 독립적으로 개발 가능해 가장 마지막에 배치한다.
기획서 순서(기본 수급 → 미니게임 → 상점 → 재배형 공방)를 그대로 따른다.
1. 기본 수급 (자동/클릭 채집)
2. 미니게임
3. 상점 (재료 구매)
4. 재배형 공방 + 고용탭 (일꾼 고용, 가장 복잡하므로 마지막)

## 진행 시 유의사항
- 2, 4, 5단계 모두 신규 데이터 테이블이 필요할 가능성이 높음 — `Assets/CTable`, `Assets/CSV`는 직접 수정 금지 대상이므로 착수 전 반드시 먼저 확인받을 것
- 신규 Model/Controller는 `Assets/Scripts/ViewModel/` 하위에 `{Name}Model.cs` / `{Name}Model+Ctrl.cs` 구조로 추가
- `GameInstance.Init()` 이후에만 유효한 매니저/모델 접근은 CLAUDE.md의 "GameInstance 의존 초기화 규칙"을 따를 것
