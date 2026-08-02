# 카페 시뮬레이션 — 재화/배치 기능

`/setup-cafe-base`의 컨셉 지시가 재화(골드 등), 언락, 가구/오브젝트 배치와 관련되면 이 파일을 먼저 읽는다.

## 매핑표

| 컨셉 개념 | 실제 구현 |
|---|---|
| 재화 | `ItemModel` + `WealthData` (`GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)`), CTable: `ItemRow`(`ItemType == Money`), `ItemMoneyRow` |
| 일반 아이템 | `ItemModel`/`ItemData` — `Get`/`Add`/`Consume`/`Set`/`HasEnough` |
| 배치(가구 등) 시스템 | `PlacementModel`/`PlacementModel+Ctrl` (`Assets/Scripts/ViewModel/Placement/`), `PlaceableObject`, `PlacementGridArea` (`Assets/Scripts/Placement/`), UI: `UIPlacementConfirm` |

## 참고

- `ItemModel.Init()`은 CTable `ItemRow`를 순회하며 `Money` 타입은 `mDicWealths`, 그 외는 `mDicItems`에 분리 적재한다. 새 재화 종류를 추가하려면 CTable에 `eMoneyType` 값이 필요 — CTable 수정이므로 사용자 확인 후 진행.
- 저장/복원은 `ItemModel.SetByTid`가 `mDicItems`와 `mDicWealths`를 모두 훑어 tid로 매칭한다(`SaveManager`와 연결되는 지점으로 추정, 상세는 `SaveManager.cs`/`SaveData.cs` 확인).
- 언락 조건 같은 진행도 데이터는 아직 전용 Model이 없다 — 필요해지면 여기가 실제 갭.
