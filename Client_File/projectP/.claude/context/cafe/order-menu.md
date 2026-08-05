# 카페 시뮬레이션 — 주문/메뉴 기능

`/setup-cafe-base`의 컨셉 지시가 주문 UI, 음료 제작, 메뉴/재료 마스터 데이터, 손님이 무엇을 요청하는지와 관련되면 이 파일을 먼저 읽는다.

## 매핑표

| 컨셉 개념 | 실제 구현 |
|---|---|
| 주문 표시 UI | `LobbyCharUI` (`Assets/Scripts/UI/Lobby/LobbyCharUI.cs`) — 주문 아이콘, `IsSpecialOrderActive`, `DesiredDrinkTid` |
| 주문 상세 팝업 | `UIPopupOrderDetail` — NPC 탭 시 오픈, "제작하기"/"확인" |
| 음료 제작(재료 조합) | `UIPopupSpecialDrinkProduction` — 재료 스크롤 선택 → `DrinkRow.DrinkMaterial1~5`와 매칭 → 성공 시 골드 지급 + `ISpecialOrderWaiter.ResumeFromSpecialOrderWait()` |
| 음료/재료/빵 마스터 데이터 | `DrinkModel`/`DrinkData`, `BreadModel`/`BreadData` (`Assets/Scripts/ViewModel/`), CTable: `DrinkRow`(재료 슬롯 `DrinkMaterial1~5`), `DrinkMaterialRow`, `MenuItemRow`, `BreadRow`(재료 슬롯 `BreadMaterial1~5`), `BreadMaterialRow` |
| 손님이 무슨 음료를 요청할지 결정 | `DrinkModel.GetRandomSpecialOrderTid()` — `Assets/CSV/DrinkRequest.csv` → `DrinkRequestRow`/`DrinkRequestTable` 가중치 기반 랜덤(테이블 비어있으면 기본 음료 제외 균등 랜덤 폴백) |
| 빵 상호작용(진열대에서 빵 집기) | `Intaraction_BreadStand`/`Intaraction_Bread` — `CEvent.BreadPickup` 이벤트로 `CharNpc`(→`IBreadPickup`)와 연결, `LobbyCharUI.AttachBread` |
| 빵 진열대 재고 선택 UI | `UIPopupBreadSelect`(+`UIScrollBread`) — `Intaraction_BreadStand` 월드 탭 시 오픈, 빵 목록 중 이 진열대의 `TableId`와 일치하는 항목을 선택하면 `Intaraction_BreadStand.AddBread()` 호출(기존 `mBtnAddBread` 버튼과 로직 공유) |
| 재료 인벤토리(음료+빵 공용) | `MaterialModel`/`MaterialData` (`Assets/Scripts/ViewModel/Material/`) — `DrinkMaterialRow`와 `BreadMaterialRow`를 같은 딕셔너리에 로드해 보유 수량(`ReactiveProperty<int>`) 추적. `Gather`/`HasEnough`/`Consume`/`SetByTid` 제공, `SaveManager`가 그대로 저장/복원 |
| 가공섬 재료 클릭 채집(음료 재료 전용) | `UIRootMaterialIsland` — `DrinkMaterialRow` 목록만 표시(빵 재료는 여기 안 뜸), 버튼 클릭 시 `MaterialModel.Gather()` |
| 빵 재료 획득(미니게임) | `UIPopupBreadMinigame` — 자리표시자: 버튼 1회 클릭 → 즉시 완료 → `BreadMaterialRow` 중 랜덤 하나 `MaterialModel.Gather()` 지급. 실제 미니게임 규칙 미정 |
| 빵 제작(재료 조합) | `UIPopupBreadProduction` — `UIPopupSpecialDrinkProduction`과 같은 선택→매칭 패턴(기존 `UIScrollDrinkMaterial` 행 재사용)이지만 특정 NPC에 안 묶이고 전체 `BreadRow` 레시피 대상으로 매칭. 성공 시 재료 소모 + `BreadModel.Register()`+`Add()`로 생산 수량 증가 |

## UI 팝업 프리팹 등록 방식 — `UIPathInfo`는 사실상 미사용, `UIManager.prefab.mCachedUIDic`가 진짜

처음엔 "프리팹이 하나도 없다"고 잘못 기록했었다(과거 스냅샷 당시엔 사실이었을 수 있음). 실제로는 `Assets/Datas/UI/**` 하위에 `UI_Popup_OrderDetail`, `UI_Popup_SpecialDrinkProduction`, `UI_Popup_BreadSelect`, `UI_Popup_OfflineIncome`, `UI_Root_MaterialLand` 등 프리팹이 이미 다수 존재한다. `Assets/Resources/UIPathInfo.asset`(`m_pathInfoDic`)는 값이 사실상 비어 있어 미사용으로 보이고, 실제 등록 지점은 `Assets/Resources/UIManager.prefab`의 `mCachedUIDic`(Odin Serializer로 이진 직렬화된 `Dictionary<eUIType, UIWndBase>`)다. `UIManager.Open()`은 이 딕셔너리에 없으면 `Resources.Load(GetUIPath(uiType))` 폴백을 시도하지만 그 경로도 비어 있어 결국 안 열린다.

**`mCachedUIDic`는 Odin 이진 직렬화라 텍스트로 손댈 수 없다.** 새 팝업을 추가할 때 프리팹 자체(배경 딤/패널/텍스트/버튼)는 기존 프리팹(`UI_Popup_OfflineIncome.prefab` 등)의 YAML 구조를 그대로 본떠 손으로 작성 가능하고 실제로 여러 번 성공했다(아래 참고). 하지만 `mCachedUIDic` 등록과 Popup 캔버스 하위 배치만큼은 반드시 Unity 에디터에서 드래그로 해야 한다 — 이 영역을 다루는 컨셉 지시가 오면 이 사실을 먼저 알릴 것.

## 갭 채우기 사례 — DrinkRequest 요청 테이블

"손님이 무슨 음료를 랜덤으로 요청할지"에 실제 요청 테이블이 없던 문제를, 새 Model을 만들지 않고 다음으로 해결했다:

1. `Assets/CSV/DrinkRequest.csv` 추가 — 컬럼 `Tid, DrinkTid, Weight`. 기존 `CsvToCsTool`(`Assets/Scripts/Tools/CSV/Editor/CsvToCsTool.cs`)이 생성했을 형식 그대로 수기 작성.
2. `Assets/CTable/DrinkRequestRow.cs`, `DrinkRequestTable.cs`도 같은 이유로 수기 작성(`TableManager`가 CSV 파일명 기준으로 `CTable.{이름}Table` 타입을 리플렉션으로 자동 로드하므로 별도 등록 코드 불필요).
3. `DrinkModel.Init()`에서 `DrinkRequestRow`를 로드해 가중치 리스트 구성, `GetRandomSpecialOrderTid()`를 가중치 랜덤으로 교체(테이블이 비어 있으면 기존 균등 랜덤 로직으로 폴백).

CTable/CSV는 절대 수정 금지 대상이므로 실제 파일 생성 전 사용자에게 컬럼안을 제안하고 승인받았다. 새로운 마스터 데이터가 필요한 갭을 만나면 이 방식(CSV+CTable 최소 추가, 기존 Model 확장)을 기본값으로 삼는다.

## 갭 채우기 사례 — BreadMaterial(빵 재료) 테이블 + 재료 인벤토리 통합

컨셉 지시("가공섬 빵 공장에서 재료를 조합해 빵을 만들고, 빵 재료는 미니게임에서 획득")를 처리하며 확인한 것: `BreadRow`엔 원래 `Tid`만 있고 `DrinkRow.DrinkMaterial1~5` 같은 재료 슬롯이 없었다. `DrinkRequest` 사례와 같은 방식으로 채웠다:

1. `Assets/CSV/BreadMaterial.csv` + `Assets/CTable/BreadMaterialRow.cs`/`BreadMaterialTable.cs` 신규(컬럼 `Tid, Name`, `DrinkMaterialRow`/`DrinkMaterialTable`과 완전히 동일한 패턴으로 수기 작성).
2. `Assets/CTable/BreadRow.cs`에 `BreadMaterial1~5` 추가, `BreadTable.cs` 파싱 갱신, `Assets/CSV/Bread.csv`에 해당 컬럼 + 기존 소금빵(20001) 레시피(밀가루+버터+소금) 데이터 추가.
3. 새 `BreadMaterialModel`을 만들지 않고 **기존 `MaterialModel`을 확장**해 `DrinkMaterialRow`와 `BreadMaterialRow`를 같은 딕셔너리에 로드(`MaterialData.Create(int tid, string name)`로 팩토리를 테이블 비의존적으로 일반화). 재고 추적 로직이 완전히 동일해서 별도 Model을 만들 이유가 없었음.
4. 음료 재료와 빵 재료가 같은 인벤토리에 있지만 **획득 경로는 분리**해야 했다 — 클릭 채집 UI(`UIRootMaterialIsland`)가 `MaterialModel.GetAll()`(전체)이 아니라 `DrinkMaterialRow` 테이블만 순회하도록 명시적으로 스코프를 좁혀서, 빵 재료가 클릭 채집 목록에 새지 않게 했다. 인벤토리를 공유 저장소로 합치되 "무엇을 보여줄지"는 View 쪽에서 테이블 기준으로 필터링하는 패턴 — 비슷하게 재료 풀이 겹치되 획득 수단이 다른 갭을 만나면 이 방식을 재사용할 것.

CTable/CSV는 이번에도 실제 파일 생성 전 컬럼안을 제안하고 사용자 승인을 받았다.

## 참고

- `DrinkRow.DrinkMaterial1~5`는 0을 "빈 슬롯"으로 취급한다(`UIPopupSpecialDrinkProduction.IsRecipeMatch`). `BreadRow.BreadMaterial1~5`도 `UIPopupBreadProduction.IsRecipeMatch`에서 동일하게 취급한다.
- 기본 음료(`DefaultDrink`)는 `eConfigType.DefaultDrinkTid`로 결정되며, 특수 주문 랜덤 후보에서 자동 제외되는 로직은 폴백 경로(`GetRandomNonDefaultDrinkTid`)에만 있다. `DrinkRequest` 테이블을 쓸 경우 기본 음료를 후보에서 빼고 싶다면 CSV에 아예 안 넣으면 된다(코드가 자동으로 걸러주지 않음).
- **해결됨 — 진열대/생산량 분리**: `BreadData`/`BreadModel`에 `ProducedCount`(생산 재고)를 `Count`(진열 수량)와 별개로 추가했다. `UIPopupBreadProduction`은 성공 시 `AddProduced()`만 호출(진열되지 않음). `Intaraction_BreadStand.AddBread()`는 `TryConsumeProduced()`로 생산 재고를 먼저 소비해야 `SpawnBread()`+`Add()`(진열)를 진행하도록 바뀌었다 — 재고가 없으면 아무 일도 안 일어남. 이전엔 `AddBread()`가 재료 소비 없이 무제한으로 진열 가능했는데, 이제 빵 공장에서 만든 만큼만 진열 가능하다(동작 변경, `mBtnAddBread`/`UIPopupBreadSelect` 양쪽 다 영향받음). (`Assets/Scripts/ViewModel/Bread/BreadData.cs`, `BreadModel.cs`, `Assets/Scripts/Interaction/Intaraction_BreadStand.cs`, `Assets/Scripts/UI/MaterialIsland/UIPopupBreadProduction.cs`)
- `UIPopupBreadMinigame`/`UIPopupBreadProduction`/`UIPopupRecipeBook`/`UIPopupUpgrade` 모두 러프 프리팹까지는 만들어져 있다(`UI_Popup_OfflineIncome.prefab`/`UI_Popup_SpecialDrinkProduction.prefab` 구조를 본떠 손으로 작성). 남은 건 위 "`mCachedUIDic`가 진짜" 섹션에서 설명한 `UIManager.prefab` 드래그 등록뿐이다.
- 손으로 프리팹을 새로 만들 때 참고할 것: 새 스크립트를 프리팹이 참조하려면 GUID가 고정돼 있어야 하므로, 그 스크립트의 `.cs.meta`를 미리 만들어 GUID를 정해둬야 한다(평소엔 스크립트에 `.meta`를 만들지 않는 게 기본이지만, 프리팹이 참조할 예정이면 이 경우가 그 예외에 해당).
