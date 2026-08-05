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
| 가공섬 | 기본 수급(클릭 채집) + 빵 재료 미니게임(자리표시자) + 빵 공장(재료 조합) 코드+러프 프리팹 완료, 상점/재배형 공방 미구현 | `MaterialModel`, `UIRootMaterialIsland`, `UIPopupBreadMinigame`, `UIPopupBreadProduction` |
| 카운터 구역 분리(특별/일반), 결제 연출(동전/만족 아이콘) | 코드 구현됨, 프리팹/씬 연결 필요 | `CharNpc`, `LobbyCharUI`, `WaypointGroup.IsTerraceZone` |
| 오프라인 수익 정산 | 코드+러프 프리팹 완료, `UIManager` 등록만 필요 | `SaveManager`, `SaveData`, `UIPopupOfflineIncome` |
| 레시피 도감(수집) | 코드+러프 프리팹 완료, `UIManager` 등록/진입 버튼 필요 | `RecipeBookModel`, `UIPopupRecipeBook` |
| 업그레이드 시스템(생산성/수익) | 골드 수익 배율 코드+러프 프리팹 완료(조리/계산 속도는 미구현), `UIManager` 등록/진입 버튼 필요 | `UpgradeModel`, `UIPopupUpgrade` |
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
- [x] **발견 기록 모델** — `RecipeBookModel` 신규 추가: 발견한 `DrinkRow` Tid를 `HashSet<int>`로 관리 (`Discover`/`IsDiscovered`/`GetDiscoveredTids`/`SetDiscovered`). `CommonModelManager.RecipeBook`으로 등록하고, `UIPopupSpecialDrinkProduction.OnRecipeSuccess()`에서 성공한 `desiredDrinkTid`를 `Discover()` 하도록 연결함. `SaveData.DiscoveredRecipeTids`를 추가해 `SaveManager.Save()/Load()`에서 발견 목록을 영속화함. (`Assets/Scripts/ViewModel/Recipe/RecipeBookModel.cs`, `Assets/Scripts/Manager/CommonModelManager.cs`, `Assets/Scripts/UI/Drink/UIPopupSpecialDrinkProduction.cs`, `Assets/Scripts/Manager/SaveData.cs`, `Assets/Scripts/Manager/SaveManager.cs`)
  - **에디터 작업**: 없음. 순수 코드 변경이라 Unity 에디터에서 별도로 연결할 것이 없고, 현재 상태로도 특별 음료 제작 성공 시 발견 기록/저장까지 정상 동작함.
- [x] **도감 열람 UI (코드만)** — `UIPopupRecipeBook` 신규 추가: `UIScrollEx`로 전체 `DrinkRow` 목록을 뿌리고, 각 행(`UIScrollRecipeBook`)은 `RecipeBookModel.IsDiscovered()` 결과에 따라 발견한 음료는 이름을, 미발견 음료는 `"???"`를 표시함. `eUIType.UIPopupRecipeBook` 등록함. (`Assets/Scripts/UI/Drink/UIPopupRecipeBook.cs`, `Assets/Scripts/UI/Drink/UIScrollRecipeBook.cs`, `Assets/Scripts/Manager/UIManager.cs`)
  - [x] **팝업 프리팹(러프)** — `Assets/Datas/UI/Lobby/Popup/UI_Popup_RecipeBook.prefab` + 행 프리팹 `Assets/Datas/UI/Lobby/UIScrollRecipeBook.prefab` 신규 추가. `UI_Popup_OfflineIncome.prefab`/`UI_Popup_SpecialDrinkProduction.prefab` 구조를 참고해 손으로 작성(스크롤뷰 + 닫기 버튼, 목록 전용이라 확인/초기화 버튼은 없음). `UIScrollRecipeBook.cs`/`UIPopupRecipeBook.cs`에 GUID 고정용 `.meta` 추가.
  - **후속 작업(에디터, 필수)**: `UIManager.prefab`의 `mCachedUIDic`는 Odin Serializer 이진 직렬화라 손으로 편집 불가 — Unity 에디터에서 `eUIType.UIPopupRecipeBook → UI_Popup_RecipeBook`으로 드래그 등록하고, 프리팹을 Popup 캔버스 하위에 배치해야 실제로 열림. (`Assets/Resources/UIManager.prefab`)
  - **후속 작업(에디터)**: 도감을 여는 진입점(로비 UI의 버튼 등)이 아직 없음 — 버튼 배치 후 `GameInstance.UI.Open<UIPopupRecipeBook, UIPopupRecipeBook.Param>(eUIType.UIPopupRecipeBook, new UIPopupRecipeBook.Param())` 호출 연결 필요.
  - **후속 작업(비주얼)**: 러프 프리팹이므로 배치/사이즈/색상은 가안임. 발견/미발견 음료 아이콘, 잠금 표시 등 실제 아트 리소스로 교체 필요.

### 4단계 — 업그레이드 시스템 (진행 중)
조리 속도/계산 속도 같은 카운터 능력치 시스템이 아직 없어(2단계 참고), 사용자 확인 후
**우선 골드 결제 수익 배율 업그레이드만** 구현. 조리/계산 속도 업그레이드는 해당 능력치 시스템이 생긴 뒤 별도 진행.
- [x] **골드 수익 배율 업그레이드 모델** — `UpgradeModel` 신규 추가: `Level`, `GoldIncomeMultiplier`(`1 + Level * 0.1`), `GetNextUpgradeCost()`(`100 * 1.5^Level`), `ApplyGoldIncomeMultiplier(int)`, `TryUpgrade()`(골드 소모 후 레벨업). 레벨/비용/배율 수치는 CTable/CSV 대신 코드 내 상수로 임시 관리(기획 확정 후 정식 테이블로 교체 예정, 사전 협의됨). `CommonModelManager.Upgrade`로 등록. (`Assets/Scripts/ViewModel/Upgrade/UpgradeModel.cs`, `Assets/Scripts/Manager/CommonModelManager.cs`)
- [x] **결제 골드에 배율 적용** — 빵 결제(`CharNpc.TryReceiveBreadGold`), 기본 음료 결제(`CharNpc.ReceiveDefaultDrinkGold`), 특별 음료 결제(`UIPopupSpecialDrinkProduction.OnRecipeSuccess`) 세 지점 모두 `GameInstance.Model.Upgrade.ApplyGoldIncomeMultiplier()`로 감싸 지급하도록 수정함. 오프라인 수익(`SaveManager.ApplyOfflineIncome`)은 이번 범위에서 제외(별도 `mOfflineCoinPerSecond` 값으로 관리 중). (`Assets/Scripts/Character/Npc/CharNpc.cs`, `Assets/Scripts/UI/Drink/UIPopupSpecialDrinkProduction.cs`)
- [x] **업그레이드 레벨 세이브/로드** — `SaveData.GoldIncomeUpgradeLevel` 추가, `SaveManager.Save()/Load()`에서 저장·복원. (`Assets/Scripts/Manager/SaveData.cs`, `Assets/Scripts/Manager/SaveManager.cs`)
  - **에디터 작업**: 없음. 순수 코드 변경이라 지금 상태로도 배율 적용/저장까지 정상 동작함.
- [x] **업그레이드 UI (코드만)** — `UIPopupUpgrade` 신규 추가: 현재 레벨/골드 수익 배율/다음 업그레이드 비용을 텍스트로 표시하고, 버튼 클릭 시 `UpgradeModel.TryUpgrade()` 호출 후 텍스트를 갱신함. 골드 부족 시 로그만 남기고 무시. `eUIType.UIPopupUpgrade` 등록함. (`Assets/Scripts/UI/Common/UIPopupUpgrade.cs`, `Assets/Scripts/Manager/UIManager.cs`)
  - [x] **팝업 프리팹(러프)** — `Assets/Datas/UI/Lobby/Popup/UI_Popup_Upgrade.prefab` 신규 추가. `UI_Popup_OfflineIncome.prefab` 구조를 참고해 손으로 작성(레벨/배율/다음 비용 텍스트 3개 + 업그레이드 버튼 + 닫기 버튼). `UIPopupUpgrade.cs`에 GUID 고정용 `.meta` 추가.
  - **후속 작업(에디터, 필수)**: `UIManager.prefab`의 `mCachedUIDic`는 손으로 편집 불가 — Unity 에디터에서 `eUIType.UIPopupUpgrade → UI_Popup_Upgrade`로 드래그 등록하고 Popup 캔버스 하위에 배치해야 실제로 열림(= 등록 전까지는 실제로 레벨을 올릴 방법이 없어 항상 Level 0·배율 1배로 동작). (`Assets/Resources/UIManager.prefab`)
  - **후속 작업(에디터)**: 업그레이드 팝업을 여는 진입점(로비 UI의 버튼 등)이 아직 없음 — 버튼 배치 후 `GameInstance.UI.Open<UIPopupUpgrade, UIPopupUpgrade.Param>(eUIType.UIPopupUpgrade, new UIPopupUpgrade.Param())` 호출 연결 필요.
  - **후속 작업(비주얼)**: 러프 프리팹이므로 배치/사이즈/색상은 가안임.
- [ ] **조리 속도 / 계산 속도 업그레이드** — 카운터 능력치 시스템 자체가 없어 보류. 능력치 시스템 설계 후 재논의.

### 5단계 — 가공섬 콘텐츠 확장 (진행 중)
`UIRootMaterialIsland`는 빈 껍데기였지만 진입 버튼(`UIHudController`의 로비 ↔ 가공섬 토글)은 이미 연결되어 있었음.
기획서 순서(기본 수급 → 미니게임 → 상점 → 재배형 공방)를 그대로 따른다.

1. **기본 수급 (자동/클릭 채집)** — [x] 클릭 채집 + 제작 소모 연동까지 완료
   - **재료 인벤토리** — `MaterialModel`/`MaterialData` 신규 추가: 기존 `CTable.DrinkMaterialRow`(재료 이름 테이블, CSV 변경 없음)의 Tid를 그대로 사용해 보유 수량을 `ReactiveProperty<int>`로 추적. `Get`/`GetAll`/`Gather`/`HasEnough`/`Consume`/`SetByTid` 제공. `CommonModelManager.Material`로 등록. (`Assets/Scripts/ViewModel/Material/MaterialData.cs`, `MaterialModel.cs`, `Assets/Scripts/Manager/CommonModelManager.cs`)
   - **가공섬 클릭 채집 UI (코드만)** — `UIRootMaterialIsland`에 `UIScrollEx` 목록을 채워 재료별 이름/보유수량/채집 버튼(`UIScrollMaterialGather`)을 표시. 버튼 클릭 시 `MaterialModel.Gather()` 호출 후 목록 갱신. (`Assets/Scripts/UI/MaterialIsland/UIRootMaterialIsland.cs`, `Assets/Scripts/UI/MaterialIsland/UIScrollMaterialGather.cs`)
   - **특별 음료 제작 소모 연동** — `UIPopupSpecialDrinkProduction`의 재료 선택 목록에 보유 수량을 함께 표시(`{이름} ({수량})`)하고, 보유량을 초과해 선택할 수 없도록 막음. 레시피 성공 시 선택한 재료를 `MaterialModel.Consume()`으로 실제 차감. 이전까지는 재료가 무제한으로 선택 가능했던 동작이 바뀜. (`Assets/Scripts/UI/Drink/UIPopupSpecialDrinkProduction.cs`, `Assets/Scripts/UI/Drink/UIScrollDrinkMaterial.cs`)
   - **재료 인벤토리 세이브/로드** — `SaveData.Materials` 추가, `SaveManager.Save()/Load()`에서 저장·복원. (`Assets/Scripts/Manager/SaveData.cs`, `Assets/Scripts/Manager/SaveManager.cs`)
   - [x] **채집 행 프리팹(러프)** — `Assets/Datas/UI/MaterialLand/UIScrollMaterialGather.prefab` 신규 추가(재료명+보유수량+채집버튼, `UIScrollItemDrinkMaterial.prefab` 패턴 참고). `UI_Root_MaterialLand.prefab`(기존에 있던 빈 껍데기)에 이 행을 쓰는 스크롤뷰를 직접 추가하고 `mScrollEx`/`mMaterialGatherRowPrefab` 필드까지 연결 완료. `UIScrollMaterialGather.cs`에 GUID 고정용 `.meta` 추가.
   - **에디터 작업**: `UI_Root_MaterialLand.prefab`은 `UIManager.prefab`의 `mCachedUIDic`에 등록돼 있어야 열림(등록 여부 확인 필요, 안 돼 있으면 Unity 에디터에서 드래그 등록). 재료 초기 보유량이 전부 0이라, 가공섬에서 채집하기 전까지는 특별 음료 제작이 항상 "재료 부족"으로 막힘 — 밸런스(자동 채집 속도, 시작 보유량 등)는 기획 확정 필요.
2. **미니게임 (빵 재료 획득) + 빵 공장 (재료 조합 제작)** — [x] 자리표시자 수준으로 완료 (컨셉 지시: "가공섬 빵 공장에서 재료를 조합해 빵을 만들고, 빵 재료는 미니게임에서 획득")
   - **CTable/CSV 스키마 추가 (사용자 승인 완료)** — `Assets/CSV/BreadMaterial.csv`+`Assets/CTable/BreadMaterialRow.cs`/`BreadMaterialTable.cs` 신규(밀가루/버터/소금, `DrinkMaterialRow`와 동일 패턴). `Assets/CTable/BreadRow.cs`에 `BreadMaterial1~5` 슬롯 추가(`DrinkRow` 패턴), `BreadTable.cs` 파싱 갱신, `Assets/CSV/Bread.csv`에 컬럼 + 소금빵 레시피(밀가루+버터+소금) 데이터 추가.
   - **재료 인벤토리 통합** — 새 Model을 만들지 않고 `MaterialModel`을 확장: `MaterialData.Create(int tid, string name)`로 팩토리를 일반화하고, `MaterialModel.Init()`에서 `DrinkMaterialRow`와 `BreadMaterialRow`를 모두 같은 딕셔너리에 로드. 음료 재료와 빵 재료가 같은 인벤토리에 저장되지만 획득 경로는 분리됨(음료 재료=가공섬 클릭 채집 목록, 빵 재료=미니게임 전용). 이를 위해 `UIRootMaterialIsland.RefreshMaterialList()`가 `MaterialModel.GetAll()` 대신 `DrinkMaterialRow` 테이블만 순회하도록 수정(빵 재료가 클릭 채집 목록에 잘못 노출되지 않도록). (`Assets/Scripts/ViewModel/Material/MaterialData.cs`, `MaterialModel.cs`, `Assets/Scripts/UI/MaterialIsland/UIRootMaterialIsland.cs`)
   - **미니게임 자리표시자** — `UIPopupBreadMinigame` 신규: 실제 규칙(타이밍/퍼즐 등)은 미정이라 버튼 1회 클릭 시 즉시 완료 처리, `BreadMaterialRow` 중 랜덤 하나를 `MaterialModel.Gather()`로 지급. 규칙이 정해지면 `OnClickPlay()` 내부만 교체하면 됨. (`Assets/Scripts/UI/MaterialIsland/UIPopupBreadMinigame.cs`)
   - **빵 공장(재료 조합)** — `UIPopupBreadProduction` 신규: `UIPopupSpecialDrinkProduction`과 동일한 재료 선택→매칭 패턴이지만 특정 NPC 주문에 묶이지 않고 전체 `BreadRow` 레시피를 대상으로 매칭(기존 `UIScrollDrinkMaterial` 행 재사용, 새 행 타입 안 만듦). 성공 시 재료 소모 + `BreadModel.Register()`(안전을 위해 항상 호출, 이미 등록돼 있으면 무동작) + `BreadModel.Add()`로 생산된 빵 수량 증가. 이 수량은 기존 `Intaraction_BreadStand`가 진열대 재고로 읽던 것과 같은 모델이지만, 현재 `Intaraction_BreadStand.AddBread()`는 이 생산 수량을 소비하지 않고 독자적으로 빵을 즉시 스폰하는 별도 동작이라 아직 서로 연결되어 있지 않음(진짜 갭, 아래 참고). (`Assets/Scripts/UI/MaterialIsland/UIPopupBreadProduction.cs`)
   - `UIRootMaterialIsland`에 두 팝업을 여는 버튼(`mBtnOpenBreadMinigame`, `mBtnOpenBreadProduction`) 추가.
   - **eUIType 등록**: `UIPopupBreadMinigame`, `UIPopupBreadProduction`. (`Assets/Scripts/Manager/UIManager.cs`)
   - [x] **팝업 프리팹(러프)** — `Assets/Datas/UI/MaterialLand/UI_Popup_BreadMinigame.prefab`(텍스트+플레이 버튼, `UI_Popup_OfflineIncome.prefab` 패턴), `Assets/Datas/UI/MaterialLand/UI_Popup_BreadProduction.prefab`(재료 스크롤+선택 텍스트+만들기/초기화 버튼, `UI_Popup_SpecialDrinkProduction.prefab` 패턴 — 행 프리팹은 기존 `UIScrollItemDrinkMaterial.prefab` 재사용) 신규 추가. `UI_Root_MaterialLand.prefab`에 두 팝업을 여는 버튼도 배치하고 `mBtnOpenBreadMinigame`/`mBtnOpenBreadProduction` 필드까지 연결 완료. 새 스크립트 4개(`UIPopupBreadMinigame`, `UIPopupBreadProduction`, `UIScrollMaterialGather`, `UIScrollRecipeBook`)에 GUID 고정용 `.meta` 추가.
   - **에디터 작업(필수)**: `UIManager.prefab`의 `mCachedUIDic`는 손으로 편집 불가 — Unity 에디터에서 `eUIType.UIPopupBreadMinigame`/`UIPopupBreadProduction` 둘 다 드래그 등록하고 Popup 캔버스 하위에 배치해야 실제로 열림.
   - **에디터 작업(비주얼)**: 러프 프리팹이므로 배치/사이즈/색상은 가안임.
   - [x] **진열대-생산량 연결(해결됨)** — `BreadData`/`BreadModel`에 `ProducedCount`(생산 재고)를 `Count`(진열 수량)와 분리 추가. `UIPopupBreadProduction` 성공 시 `AddProduced()`만 호출(자동으로 진열되지 않음). `Intaraction_BreadStand.AddBread()`는 `TryConsumeProduced()`로 생산 재고를 소비해야만 `SpawnBread()`+진열(`Add()`)을 진행 — 재고 없으면 무동작. 이전엔 `AddBread()`(`mBtnAddBread` 버튼/`UIPopupBreadSelect` 확정 양쪽)가 재료 소비 없이 무제한 진열 가능했는데, 이제 빵 공장에서 만든 만큼만 진열 가능하도록 동작이 바뀜. (`Assets/Scripts/ViewModel/Bread/BreadData.cs`, `BreadModel.cs`, `Assets/Scripts/Interaction/Intaraction_BreadStand.cs`, `Assets/Scripts/UI/MaterialIsland/UIPopupBreadProduction.cs`)
3. 상점 (재료 구매) — 미착수, 재화로 재료 구매하는 구조라 신규 테이블 필요 가능성 있음 (사전 논의 대상)
4. 재배형 공방 + 고용탭 (일꾼 고용, 가장 복잡하므로 마지막) — 미착수, 신규 테이블 필요 가능성 높음 (사전 논의 대상)

## 진행 시 유의사항
- **새 팝업 5개(`UI_Popup_OfflineIncome`/`RecipeBook`/`Upgrade`/`BreadMinigame`/`BreadProduction`) 전부 프리팹까지는 만들어져 있지만, `UIManager.prefab`의 `mCachedUIDic` 등록만 공통으로 남아 있음.** 이 딕셔너리는 Odin Serializer 이진 직렬화라 텍스트로 편집 불가 — Unity 에디터에서 각 `eUIType`에 해당 프리팹을 드래그 등록하고 Popup 캔버스 하위에 배치해야 실제로 열림. 이 등록 전까지 각 기능의 백엔드 로직(저장/계산/소모 등)은 정상 동작하지만 화면에 UI가 뜨지 않음.
- 2, 4, 5단계 모두 신규 데이터 테이블이 필요할 가능성이 높음 — `Assets/CTable`, `Assets/CSV`는 직접 수정 금지 대상이므로 착수 전 반드시 먼저 확인받을 것
- 신규 Model/Controller는 `Assets/Scripts/ViewModel/` 하위에 `{Name}Model.cs` / `{Name}Model+Ctrl.cs` 구조로 추가
- `GameInstance.Init()` 이후에만 유효한 매니저/모델 접근은 CLAUDE.md의 "GameInstance 의존 초기화 규칙"을 따를 것
