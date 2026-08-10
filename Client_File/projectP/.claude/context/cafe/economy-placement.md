# 카페 시뮬레이션 — 재화/배치 기능

`/setup-cafe-base`의 컨셉 지시가 재화(골드 등), 언락, 가구/오브젝트 배치와 관련되면 이 파일을 먼저 읽는다.

## 매핑표

| 컨셉 개념 | 실제 구현 |
|---|---|
| 재화 | `ItemModel` + `WealthData` (`GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)`), CTable: `ItemRow`(`ItemType == Money`), `ItemMoneyRow` |
| 일반 아이템 | `ItemModel`/`ItemData` — `Get`/`Add`/`Consume`/`Set`/`HasEnough` |
| 배치(가구 등) 시스템 | `PlacementModel`/`PlacementModel+Ctrl` (`Assets/Scripts/ViewModel/Placement/`), `PlaceableObject`, `PlacementGridArea` (`Assets/Scripts/Placement/`), UI: `UIPlacementConfirm` |
| 배치 모드 진입/이탈 | `PlacementModel.IsEditMode`(ReactiveProperty) + `ToggleEditMode()`/`SetEditMode(bool)` — 2026-08 신규. `UIRootLobby`의 "가구 배치" 버튼(`mBtnTogglePlacementMode`)이 토글. `PlaceableObject.OnPointerDown()`은 이제 `IsEditMode`가 켜져 있을 때만 드래그를 시작한다(이전엔 언제든 드래그 가능했음). 모드를 끌 때 드래그 중이었다면 `Cancel()`로 원위치 복구 후 정리 |
| 배치 확인/취소 UI | `UIPlacementConfirm`(`Assets/Scripts/UI/Placement/`) — 2026-08 프리팹 신규 작성, `UI_Root_Lobby.prefab`의 상시 활성 자식으로 배치(`mRoot`가 가리키는 `Panel`만 `IsPlacing` 값에 따라 활성/비활성). `UIWndBase`를 상속하지 않는 평범한 MonoBehaviour라 `eUIType`/`GameInstance.UI.Open()`으로 열리지 않고, `UIRootLobby.Init()`이 직접 `mPlacementConfirm.Init()`을 호출해 구독을 건다 |
| 가구 구매/카탈로그 | `UIFurnitureList`/`UIScrollFurniture`(`Assets/Scripts/UI/Placement/`) — 2026-08 신규. CTable `FurnitureRow`(`Tid,Name,Atlas,Icon,PrefabPath,Price`)를 순회해 목록을 뿌리고, 골드 소비 후 `GameInstance.Resource.LoadSync<GameObject>(PrefabPath)`로 프리팹을 불러와 씬에 배치(`PlaceableObject.BeginPlacementFromSpawn`)한다. `PrefabPath`는 **Resources 상대 경로가 아니라 어드레서블 주소**(`Worlds/Table`처럼 `{그룹명}/{파일명}`) — Resources 폴더에 없는 프리팹도 어드레서블로만 등록해두면 동작함 |
| 재료 상점(골드 구매) | `UIPopupMaterialShop`/`UIScrollMaterialShop`(`Assets/Scripts/UI/MaterialIsland/`) — 2026-08 신규. `DrinkMaterialRow`/`BreadMaterialRow`에 추가한 `Price` 컬럼 기준으로 골드 소비 후 `MaterialModel.Gather()` 지급. 가공섬 클릭 채집(무료)과는 별개의 유료 획득 경로 |
| 재배형 공방 + 고용 | `WorkshopModel`/`WorkshopSlotData`(`Assets/Scripts/ViewModel/Workshop/`), UI: `UIPopupWorkshop`/`UIWorkshopSlotView` — 2026-08 신규. 골드로 일꾼을 고용(`TryHireWorker`)하고, 고용한 일꾼을 슬롯에 배치하면서 생산할 재료를 플레이어가 지정(`AssignWorker`)하면 `Observable.Interval`로 주기적으로 `MaterialModel.Gather()` 자동 생산(방치형). 일꾼은 종류 구분 없이 전부 동일 |

## 참고

- `ItemModel.Init()`은 CTable `ItemRow`를 순회하며 `Money` 타입은 `mDicWealths`, 그 외는 `mDicItems`에 분리 적재한다. 새 재화 종류를 추가하려면 CTable에 `eMoneyType` 값이 필요 — CTable 수정이므로 사용자 확인 후 진행.
- 저장/복원은 `ItemModel.SetByTid`가 `mDicItems`와 `mDicWealths`를 모두 훑어 tid로 매칭한다(`SaveManager`와 연결되는 지점으로 추정, 상세는 `SaveManager.cs`/`SaveData.cs` 확인).
- 언락 조건 같은 진행도 데이터는 아직 전용 Model이 없다 — 필요해지면 여기가 실제 갭.

## 갭 채우기 사례 — 가구 구매 + 재료 상점 + 재배형 공방/고용 (2026-08)

과거엔 "가구 신규 구매/카탈로그 없음", "가공섬 상점 미착수", "재배형 공방/고용탭 미착수"가 전부 갭이었으나 순서대로 채웠다.

1. **가구 구매**: 새 CTable `FurnitureRow`/`FurnitureTable`(`Tid,Name,Atlas,Icon,PrefabPath,Price`) 추가(사용자 승인됨). `PrefabPath`는 어드레서블 주소 규칙을 따른다 — Resources 폴더 기준 상대 경로로 착각하고 `Resources.Load`로 불러오려 하면 실패하니 주의(과거에 실제로 이 버그가 있었음: CSV엔 `World/Table`인데 실제 어드레서블 주소는 `Worlds/Table`이었고, 로딩 코드도 `Resources.Load`를 쓰고 있어서 두 가지가 겹쳐 실패했었다). `GameInstance.Resource.LoadSync<T>()`(어드레서블 우선, 실패 시 Resources 폴백)를 쓰면 두 경우 다 커버된다.
2. **재료 상점**: 새 Model 없이 기존 `MaterialModel.Gather()` 재사용. `DrinkMaterialRow`/`BreadMaterialRow`에 `Price`(long) 컬럼만 추가(사용자 승인됨, 가안 가격).
3. **재배형 공방 + 고용**: 새 CTable 없이 순수 코드로 처리 — 일꾼이 전부 동일 개체라 "일꾼 마스터 데이터" 자체가 불필요했다. 슬롯 수/고용 비용 곡선/생산 주기는 `UpgradeModel`과 동일하게 코드 내 고정 상수로 관리(기획 확정 전까지 임시, 사전 협의됨).

세 기능 모두 러프 프리팹까지 손으로 작성 완료. 공통으로 남은 후속 작업은 `.claude/context/cafe/order-menu.md`의 "`mCachedUIDic`가 진짜" 섹션과 동일한 패턴 — `UIManager.prefab` 드래그 등록 + 진입 버튼 배치.
