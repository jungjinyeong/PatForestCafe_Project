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

## 참고

- `ItemModel.Init()`은 CTable `ItemRow`를 순회하며 `Money` 타입은 `mDicWealths`, 그 외는 `mDicItems`에 분리 적재한다. 새 재화 종류를 추가하려면 CTable에 `eMoneyType` 값이 필요 — CTable 수정이므로 사용자 확인 후 진행.
- 저장/복원은 `ItemModel.SetByTid`가 `mDicItems`와 `mDicWealths`를 모두 훑어 tid로 매칭한다(`SaveManager`와 연결되는 지점으로 추정, 상세는 `SaveManager.cs`/`SaveData.cs` 확인).
- 언락 조건 같은 진행도 데이터는 아직 전용 Model이 없다 — 필요해지면 여기가 실제 갭.
- **가구 신규 구매/카탈로그 없음**: 배치 시스템은 씬에 이미 존재하는 `PlaceableObject`를 드래그로 재배치하는 것뿐이다. 새 가구를 "구매해서 추가"하는 상점/카탈로그 개념은 코드에 전혀 없다 — 필요해지면 신규 기능.
