# 카페 시뮬레이션 — 주문/메뉴 기능

`/setup-cafe-base`의 컨셉 지시가 주문 UI, 음료 제작, 메뉴/재료 마스터 데이터, 손님이 무엇을 요청하는지와 관련되면 이 파일을 먼저 읽는다.

## 매핑표

| 컨셉 개념 | 실제 구현 |
|---|---|
| 음료 레시피 제작(연구소, 재료 조합) | `UIPopupDrinkRecipeProduction`(2026-08 스페셜 주문 삭제 후 재사용, 이후 `UIPopupSpecialDrinkProduction`에서 개명 — 프리팹 `UI_Popup_SpecialDrinkProduction.prefab`의 `m_Script`는 guid 기반이라 클래스/파일명 변경과 무관하게 유지됨, 프리팹 파일명 자체는 안 바꿈) — 재료 스크롤 선택 → 전체 `DrinkRow` 중 **미발견**(`RecipeBookModel.IsDiscovered` false) 레시피와 매칭(목표를 미리 지정하지 않고 자동 탐색, `UIPopupBreadProduction`과 동일 패턴) → 성공 시 재료 소모 + "레시피 개발북" 아이템(Tid=1002) 1개 소모 + `RecipeBookModel.Discover()`. 골드 보상 없음(손님 거래가 아니므로). `UIPopupRecipeBook`의 "개발하기" 버튼(`mBtnDevelopRecipe`)에서 진입 |
| 손님 주문 표시/요청 | **삭제됨(2026-08)** — 스페셜 주문 기획 전체 삭제. `LobbyCharUI.IsSpecialOrderActive`/`DesiredDrinkTid`, `UIPopupOrderDetail`(도달 불가능한 죽은 코드로만 남음, 프리팹 GUID 보존 목적), `ISpecialOrderWaiter`, `Waypoint.eWaypointType.Wait_SpecialOrder`, `WaypointGroup.IsSpecialOrderZone`, `SpawnManager.DecideSpecialOrder`/`CanAssignSpecialOrder` 모두 제거. 카운터(`Trigger_Order`)는 이제 항상 무조건 기본 음료 금액만 자동 청구 |
| 음료/재료/빵 마스터 데이터 | `DrinkModel`/`DrinkData`, `BreadModel`/`BreadData` (`Assets/Scripts/ViewModel/`), CTable: `DrinkRow`(재료 슬롯 `DrinkMaterial1~5`), `DrinkMaterialRow`, `MenuItemRow`, `BreadRow`(재료 슬롯 `BreadMaterial1~5`), `BreadMaterialRow`, `ItemRow`(신규 `eItemType.Normal`, Tid=1002 레시피 개발북) |
| 빵 상호작용(진열대에서 빵 집기) | `Intaraction_BreadStand`/`Intaraction_Bread` — `CEvent.BreadPickup` 이벤트로 `CharNpc`(→`IBreadPickup`)와 연결, `LobbyCharUI.AttachBread` |
| 빵 진열대 재고 선택 UI | `UIPopupBreadSelect`(+`UIScrollBread`) — `Intaraction_BreadStand` 월드 탭 시 오픈, 빵 목록 중 이 진열대의 `TableId`와 일치하는 항목을 선택하면 `Intaraction_BreadStand.AddBread()` 호출(기존 `mBtnAddBread` 버튼과 로직 공유) |
| 재료 인벤토리(음료+빵 공용) | `MaterialModel`/`MaterialData` (`Assets/Scripts/ViewModel/Material/`) — `DrinkMaterialRow`와 `BreadMaterialRow`를 같은 딕셔너리에 로드해 보유 수량(`ReactiveProperty<int>`) 추적. `Gather`/`HasEnough`/`Consume`/`SetByTid` 제공, `SaveManager`가 그대로 저장/복원 |
| 가공섬 재료 클릭 채집(음료 재료 전용) | `UIRootMaterialIsland` — `DrinkMaterialRow` 목록만 표시(빵 재료는 여기 안 뜸), 버튼 클릭 시 `MaterialModel.Gather()` |
| 빵 재료 획득(미니게임) | `UIPopupBreadMinigame` — 자리표시자: 버튼 1회 클릭 → 즉시 완료 → `BreadMaterialRow` 중 랜덤 하나 `MaterialModel.Gather()` 지급. 실제 미니게임 규칙 미정 |
| 빵 제작(재료 조합) | `UIPopupBreadProduction` — `UIPopupDrinkRecipeProduction`(음료 레시피 제작)과 같은 선택→매칭 패턴(기존 `UIScrollDrinkMaterial` 행 재사용), 전체 `BreadRow` 레시피 대상으로 매칭(둘 다 동일하게 "목표 미지정, 전체 테이블 자동 매칭" 방식). 성공 시 재료 소모 + `BreadModel.Register()`+`Add()`로 생산 수량 증가 |

## UI 팝업 프리팹 등록 방식 — `UIPathInfo`는 사실상 미사용, `UIManager.prefab.mCachedUIDic`가 진짜

처음엔 "프리팹이 하나도 없다"고 잘못 기록했었다(과거 스냅샷 당시엔 사실이었을 수 있음). 실제로는 `Assets/Datas/UI/**` 하위에 `UI_Popup_OrderDetail`, `UI_Popup_SpecialDrinkProduction`, `UI_Popup_BreadSelect`, `UI_Popup_OfflineIncome`, `UI_Root_MaterialLand` 등 프리팹이 이미 다수 존재한다. `Assets/Resources/UIPathInfo.asset`(`m_pathInfoDic`)는 값이 사실상 비어 있어 미사용으로 보이고, 실제 등록 지점은 `Assets/Resources/UIManager.prefab`의 `mCachedUIDic`(Odin Serializer로 이진 직렬화된 `Dictionary<eUIType, UIWndBase>`)다. `UIManager.Open()`은 이 딕셔너리에 없으면 `Resources.Load(GetUIPath(uiType))` 폴백을 시도하지만 그 경로도 비어 있어 결국 안 열린다.

**`mCachedUIDic`는 Odin 이진 직렬화라 텍스트로 손댈 수 없다.** 새 팝업을 추가할 때 프리팹 자체(배경 딤/패널/텍스트/버튼)는 기존 프리팹(`UI_Popup_OfflineIncome.prefab` 등)의 YAML 구조를 그대로 본떠 손으로 작성 가능하고 실제로 여러 번 성공했다(아래 참고). 하지만 `mCachedUIDic` 등록과 Popup 캔버스 하위 배치만큼은 반드시 Unity 에디터에서 드래그로 해야 한다 — 이 영역을 다루는 컨셉 지시가 오면 이 사실을 먼저 알릴 것.

## (사용 중단) DrinkRequest 요청 테이블

과거 "손님이 무슨 음료를 랜덤으로 요청할지"를 위해 `Assets/CSV/DrinkRequest.csv`+`DrinkRequestRow`/`DrinkRequestTable`을 추가했었다. **2026-08 스페셜 주문 기획 삭제로 이 테이블을 읽는 코드(`DrinkModel.GetRandomSpecialOrderTid()` 등)를 전부 제거했다** — CTable/CSV는 절대 수정 금지 대상이라 파일 자체는 삭제하지 않고 그대로 남아 있지만, 현재 아무 코드도 참조하지 않는 고아 데이터다. 새로 이 개념이 필요해지면(예: 손님별 음료 취향이 부활) 이 테이블을 재사용할 것.

## 갭 채우기 사례 — RecipeBook 아이템(레시피 개발북)

"레시피 개발을 위한 소비 아이템"이 기존 `ItemRow`(`eItemType.Money`만 존재)로 표현이 안 되던 문제를, 다음으로 해결했다(사용자 승인됨):

1. `Assets/CTable/TableEnum.cs`의 `eItemType`에 `Normal` 추가(기존 `Money` 뒤에 추가해 기존 값 순서 보존).
2. `Assets/CSV/Item.csv`에 `Tid=1002, ItemType=Normal, ItemName=레시피 개발북` 행 추가.
3. 새 Model 없이 **기존 `ItemModel`을 그대로 재사용** — `ItemModel.Init()`이 `Money`가 아닌 모든 `ItemRow`를 이미 `mDicItems`에 일반 아이템으로 적재하므로 코드 변경 전혀 불필요. `Get`/`Add`/`Consume`/`HasEnough`로 바로 사용 가능.

CTable/CSV는 실제 파일 생성 전 컬럼안을 제안하고 사용자 승인을 받았다. 획득 경로(드랍/구매 등)는 이번 범위에서 제외 — 소비/개발 UI만 우선 구현.

## 갭 채우기 사례 — BreadMaterial(빵 재료) 테이블 + 재료 인벤토리 통합

컨셉 지시("가공섬 빵 공장에서 재료를 조합해 빵을 만들고, 빵 재료는 미니게임에서 획득")를 처리하며 확인한 것: `BreadRow`엔 원래 `Tid`만 있고 `DrinkRow.DrinkMaterial1~5` 같은 재료 슬롯이 없었다. `DrinkRequest` 사례와 같은 방식으로 채웠다:

1. `Assets/CSV/BreadMaterial.csv` + `Assets/CTable/BreadMaterialRow.cs`/`BreadMaterialTable.cs` 신규(컬럼 `Tid, Name`, `DrinkMaterialRow`/`DrinkMaterialTable`과 완전히 동일한 패턴으로 수기 작성).
2. `Assets/CTable/BreadRow.cs`에 `BreadMaterial1~5` 추가, `BreadTable.cs` 파싱 갱신, `Assets/CSV/Bread.csv`에 해당 컬럼 + 기존 소금빵(20001) 레시피(밀가루+버터+소금) 데이터 추가.
3. 새 `BreadMaterialModel`을 만들지 않고 **기존 `MaterialModel`을 확장**해 `DrinkMaterialRow`와 `BreadMaterialRow`를 같은 딕셔너리에 로드(`MaterialData.Create(int tid, string name)`로 팩토리를 테이블 비의존적으로 일반화). 재고 추적 로직이 완전히 동일해서 별도 Model을 만들 이유가 없었음.
4. 음료 재료와 빵 재료가 같은 인벤토리에 있지만 **획득 경로는 분리**해야 했다 — 클릭 채집 UI(`UIRootMaterialIsland`)가 `MaterialModel.GetAll()`(전체)이 아니라 `DrinkMaterialRow` 테이블만 순회하도록 명시적으로 스코프를 좁혀서, 빵 재료가 클릭 채집 목록에 새지 않게 했다. 인벤토리를 공유 저장소로 합치되 "무엇을 보여줄지"는 View 쪽에서 테이블 기준으로 필터링하는 패턴 — 비슷하게 재료 풀이 겹치되 획득 수단이 다른 갭을 만나면 이 방식을 재사용할 것.

CTable/CSV는 이번에도 실제 파일 생성 전 컬럼안을 제안하고 사용자 승인을 받았다.

## 갭 채우기 사례 — DrinkRow.Weight(계산대 랜덤 구매 확률) (2026-08)

컨셉 지시("계산대에서 손님이 해금된 레시피 중 랜덤으로 음료 구매")를 위해 `CharNpc.ReceiveRandomUnlockedDrinkGold()`(옛 `ReceiveDefaultDrinkGold`)를 추가했을 때, 처음엔 `RecipeBookModel.GetDiscoveredTids() ∪ {DrinkModel.DefaultDrink.TId}` 풀에서 균등 확률로 뽑았다. 사용자가 "확률은 테이블에 지정해줘"라고 요청해 다음으로 확장(사용자 승인됨, 기본 음료 가중치를 더 높게):

1. `Assets/CSV/Drink.csv`/`Assets/CTable/DrinkRow.cs`/`DrinkTable.cs`에 `Weight`(int) 컬럼 추가(맨 뒤). 기본 음료(Tid=10001, 아이스 아메리카노) `Weight=5`, 나머지 11개 `Weight=1` — 초기값, 밸런스는 CSV에서 숫자만 조정하면 됨.
2. 새 Model 없이 **기존 `DrinkModel`/`RecipeBookModel` 조합만으로 해결** — `CharNpc.ReceiveRandomUnlockedDrinkGold()`가 스트리밍 가중치 추첨(reservoir sampling, 각 항목을 `weight/누적합` 확률로 교체)으로 확장됨. `Weight <= 0`인 행은 추첨에서 제외(향후 "일시 품절" 같은 개념에 재사용 가능).

CTable/CSV는 이번에도 실제 파일 생성 전 컬럼안을 제안하고 사용자 승인을 받았다.

## 갭 채우기 사례 — DrinkRow.Atlas/Icon(구매한 음료를 손에 들기) (2026-08)

컨셉 지시("음료를 구매한 npc 손에 음료를 배치할거야. 음료 이미지는 Drink테이블을 참조해야해")를 위해 `DrinkRow`에 `Atlas`/`Icon`(둘 다 string) 컬럼을 추가했다(사용자 승인됨, `MenuItemRow`/`ItemRow`/`FurnitureRow`와 동일한 두 컬럼 구성 — 단 `Atlas`는 그 테이블들에선 죽은 컬럼이었지만 이번엔 실제로 `SpriteAtlas.GetSprite(Icon)` 조회에 쓰인다). `Assets/CSV/Drink.csv`의 값(`CoffeeAtlas`/`IceAmericanoIcon` 등)은 `MenuItem.csv`의 기존 10001/10002 값과 이름 패턴만 맞춘 가안이며, 실제 아틀라스/스프라이트 에셋 존재 여부는 확인되지 않았다.

1. `CharNpc.ReceiveRandomUnlockedDrinkGold()`가 가중치 추첨으로 `purchasedDrink`(`DrinkData`)를 정하면, `LoadDrinkSprite(purchasedDrink.Row)`(신규 private static 헬퍼, `GameInstance.Resource.LoadSync<SpriteAtlas>(Atlas)` → `.GetSprite(Icon)`)로 스프라이트를 구해 `LobbyCharUI.SetDrinkSprite()`를 호출한다.
2. **전용 프리팹 없이 절차적으로 생성**(`CharStaff`의 픽업 게이지바와 동일한 이유/패턴) — `LobbyCharUI.BuildDrinkIconObj()`가 런타임에 `GameObject` + `SpriteRenderer`를 만들어 `mDrinkOffset`/`mDrinkSize`(SerializeField, 기본값 있음, 인스펙터 연결 불필요) 위치에 배치한다. 빵(`Intaraction_Bread`, 진열대별 전용 프리팹 풀링)과 달리 음료는 매번 다른 `Tid`가 뽑히므로 프리팹 풀링 대신 스프라이트만 교체하는 방식을 택함.
3. `CharNpc.DespawnToPool()`에서 `LobbyCharUI.ClearDrink()` 호출 — 오브젝트 풀 재사용 시 이전 손님의 음료 아이콘이 다음 손님에게 남지 않도록.
4. 음료 아이콘은 결제 시점부터 손님이 퇴장(디스폰)할 때까지 계속 손에 들려 있다 — 빵처럼 "다 마시면 사라짐" 같은 소비 연출은 없음(요청 범위 밖).

## 최적화 사례 — 레시피 매칭 O(N) 전체 스캔 → O(1) 서명 조회 (2026-08)

컨셉 지시("레시피 제작대=연구소, 제작 버튼 클릭 시 DrinkTable에서 일치하는 음료를 찾는 방식을 알고리즘 최적화")를 위해 `UIPopupDrinkRecipeProduction.OnClickConfirmRecipe()`(당시 클래스명은 `UIPopupSpecialDrinkProduction`, 이후 개명)가 클릭마다 미발견 `DrinkRow` 전체를 순회(O(N))하며 각 row의 재료 슬롯을 딕셔너리로 변환해 비교하던 방식을 없앴다. **"재료 조합 하나당 음료 하나"를 전제**(사용자 확정, 현재 `Drink.csv`엔 실제로 중복 조합 없음)로 CTable/CSV 스키마 변경 없이 순수 로직만 교체:

1. `DrinkModel`에 `mRecipeSignatureToTid`(`Dictionary<string,int>`) 캐시 추가. 재료 조합을 Tid 오름차순 정규화 문자열("Tid:Count,Tid:Count...")로 만들어 서명화 — 선택 순서와 무관하게 같은 조합이면 같은 키가 나온다. `DrinkModel.Init()`이 어차피 `DrinkRow` 전체를 한 번 순회하며 `mDicDrinks`를 채우고 있어 그 루프에 얹었으므로 추가 스캔 비용이 없다(테이블은 런타임에 바뀌지 않아 캐시 무효화도 불필요). 중복 조합이 실제로 발견되면 `Logger.Warning`만 남기고 첫 번째 값을 유지(조용히 덮어쓰지 않도록 로그로 신호).
2. `DrinkModel.TryGetRecipeMatch(IReadOnlyDictionary<int,int> selectedMaterialCounts, out int drinkTid)` 추가 — 선택 재료(≤5종)로 같은 방식 서명을 만들어 O(1) 조회. 재료 조합→음료 판별은 데이터 도출 로직이라 View가 아니라 기존 `DrinkModel`(Model 책임)에 넣었다.
3. `UIPopupDrinkRecipeProduction.OnClickConfirmRecipe()`는 이제 `GameInstance.Model.Drink.TryGetRecipeMatch(...)` 한 번 호출 + `RecipeBook.IsDiscovered` 체크로 끝난다. 옛 `IsRecipeMatch(DrinkRow)`(전체 스캔 루프 안에서만 쓰이던 헬퍼)는 삭제.
4. 새 Model/View 없이 기존 `DrinkModel`/`UIPopupDrinkRecipeProduction` 확장만으로 해결 — 비슷하게 "선택된 재료 조합을 무언가와 매칭"하는 갭(예: `UIPopupBreadProduction`의 별도 `IsRecipeMatch`)이 커지면 같은 서명 캐시 패턴을 재사용할 것.
5. **2026-08 추가 개명**: 클래스/파일명을 `UIPopupSpecialDrinkProduction` → `UIPopupDrinkRecipeProduction`으로 변경(사용자 요청, 스페셜 주문 개념이 사라진 지금 이름이 실제 기능과 맞지 않았기 때문). `.cs`/`.cs.meta`를 함께 리네임해 guid를 보존했고, `eUIType.UIPopupSpecialDrinkProduction`도 `eUIType.UIPopupDrinkRecipeProduction`으로 이름만 변경(선언 순서 그대로라 `UIManager.prefab`의 Odin 바이너리 `mCachedUIDic`가 참조하는 정수값은 안 바뀜). 프리팹 파일명(`UI_Popup_SpecialDrinkProduction.prefab`) 자체는 바꾸지 않았다 — Unity는 MonoBehaviour 참조를 guid로 하므로 파일명이 클래스명과 달라도 동작에 지장 없음.

## 버그 수정 사례 — UIPopupBreadSelect 텍스트 미표시 + UIScrollEx 잔여 템플릿 행 (2026-08)

`UI_Popup_BreadSelect.prefab`에서 빵 이름 텍스트가 안 보이는 문제 발견. 원인: `mBreadRowPrefab`이 `UIScrollBread`가 아니라 재료 선택용 `UIScrollItemDrinkMaterial.prefab`(컴포넌트: `UIScrollDrinkMaterial`)의 중첩 인스턴스를 잘못 참조 — `UIScrollRow<T>.SetData(object)`의 `(T)data` 캐스팅(`UIScrollBreadData → UIScrollDrinkMaterialData`)이 `InvalidCastException`을 던져 루프가 첫 행에서 멈추고 전부 플레이스홀더 텍스트로 남음. **사용자가 프리팹 쪽(컴포넌트 스왑)은 에디터에서 직접 수정함.**

조사 중 발견한 부수 문제(공용 `UIScrollEx`, `Assets/Scripts/UI/Scroll/UIScrollEx.cs`) — 사용자 요청으로 같이 수정:
- 행 템플릿(`mRowPrefab`)이 `Content` 트랜스폼의 실제 자식으로 상시 배치되는 이 프로젝트의 손-저작 패턴상, `Init()`이 재호출되거나 이전 상태가 남아있으면 `Content` 밑에 여분의 행이 계속 쌓이거나 템플릿 자체가 데이터 행과 별개로 화면에 노출될 수 있었다.
- `UIScrollEx.Init()`을 "재호출 시 `Content`의 기존 자식을 전부 정리(`Destroy`, 새로 등록하는 `mRowPrefab` 자신은 제외) + `mActiveRows`/`mRowPool` 초기화 + `mRowPrefab.SetActive(false)`"로 변경. `UIScrollEx`를 쓰는 모든 팝업(재료 선택, 빵 선택, 레시피북 등)에 공통 적용되는 수정.

## 참고

- `DrinkRow.DrinkMaterial1~5`는 0을 "빈 슬롯"으로 취급한다(`DrinkModel.BuildRecipeSignature`, 2026-08부터 — 옛 `UIPopupSpecialDrinkProduction.IsRecipeMatch`는 삭제됨). `BreadRow.BreadMaterial1~5`도 `UIPopupBreadProduction.IsRecipeMatch`에서 동일하게 취급한다.
- 기본 음료(`DefaultDrink`)는 `eConfigType.DefaultDrinkTid`로 결정된다. 음료 레시피 제작(`UIPopupDrinkRecipeProduction`)은 기본 음료도 포함해 전체 `DrinkRow`를 순회하지만, 기본 음료는 보통 처음부터 발견 상태로 취급하거나(별도 초기화 필요) 애초에 재료 슬롯이 없는 더미 레시피로 등록하면 자동으로 매칭 후보에서 제외된다 — 코드가 자동으로 걸러주지는 않음.
- **해결됨 — 진열대/생산량 분리**: `BreadData`/`BreadModel`에 `ProducedCount`(생산 재고)를 `Count`(진열 수량)와 별개로 추가했다. `UIPopupBreadProduction`은 성공 시 `AddProduced()`만 호출(진열되지 않음). `Intaraction_BreadStand.AddBread()`는 `TryConsumeProduced()`로 생산 재고를 먼저 소비해야 `SpawnBread()`+`Add()`(진열)를 진행하도록 바뀌었다 — 재고가 없으면 아무 일도 안 일어남. 이전엔 `AddBread()`가 재료 소비 없이 무제한으로 진열 가능했는데, 이제 빵 공장에서 만든 만큼만 진열 가능하다(동작 변경, `mBtnAddBread`/`UIPopupBreadSelect` 양쪽 다 영향받음). (`Assets/Scripts/ViewModel/Bread/BreadData.cs`, `BreadModel.cs`, `Assets/Scripts/Interaction/Intaraction_BreadStand.cs`, `Assets/Scripts/UI/MaterialIsland/UIPopupBreadProduction.cs`)
- `UIPopupBreadMinigame`/`UIPopupBreadProduction`/`UIPopupRecipeBook`/`UIPopupUpgrade` 모두 러프 프리팹까지는 만들어져 있다(`UI_Popup_OfflineIncome.prefab`/`UI_Popup_SpecialDrinkProduction.prefab` 구조를 본떠 손으로 작성). 남은 건 위 "`mCachedUIDic`가 진짜" 섹션에서 설명한 `UIManager.prefab` 드래그 등록뿐이다.
- 손으로 프리팹을 새로 만들 때 참고할 것: 새 스크립트를 프리팹이 참조하려면 GUID가 고정돼 있어야 하므로, 그 스크립트의 `.cs.meta`를 미리 만들어 GUID를 정해둬야 한다(평소엔 스크립트에 `.meta`를 만들지 않는 게 기본이지만, 프리팹이 참조할 예정이면 이 경우가 그 예외에 해당).
