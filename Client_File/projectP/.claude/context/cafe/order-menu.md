# 카페 시뮬레이션 — 주문/메뉴 기능

`/setup-cafe-base`의 컨셉 지시가 주문 UI, 음료 제작, 메뉴/재료 마스터 데이터, 손님이 무엇을 요청하는지와 관련되면 이 파일을 먼저 읽는다.

## 매핑표

| 컨셉 개념 | 실제 구현 |
|---|---|
| 주문 표시 UI | `LobbyCharUI` (`Assets/Scripts/UI/Lobby/LobbyCharUI.cs`) — 주문 아이콘, `IsSpecialOrderActive`, `DesiredDrinkTid` |
| 주문 상세 팝업 | `UIPopupOrderDetail` — NPC 탭 시 오픈, "제작하기"/"확인" |
| 음료 제작(재료 조합) | `UIPopupSpecialDrinkProduction` — 재료 스크롤 선택 → `DrinkRow.DrinkMaterial1~5`와 매칭 → 성공 시 골드 지급 + `ISpecialOrderWaiter.ResumeFromSpecialOrderWait()` |
| 음료/재료/빵 마스터 데이터 | `DrinkModel`/`DrinkData`, `BreadModel`/`BreadData` (`Assets/Scripts/ViewModel/`), CTable: `DrinkRow`, `DrinkMaterialRow`, `MenuItemRow`, `BreadRow` |
| 손님이 무슨 음료를 요청할지 결정 | `DrinkModel.GetRandomSpecialOrderTid()` — `Assets/CSV/DrinkRequest.csv` → `DrinkRequestRow`/`DrinkRequestTable` 가중치 기반 랜덤(테이블 비어있으면 기본 음료 제외 균등 랜덤 폴백) |
| 빵 상호작용(진열대에서 빵 집기) | `Intaraction_BreadStand`/`Intaraction_Bread` — `CEvent.BreadPickup` 이벤트로 `CharNpc`(→`IBreadPickup`)와 연결, `LobbyCharUI.AttachBread` |
| 빵 진열대 재고 선택 UI | `UIPopupBreadSelect`(+`UIScrollBread`) — `Intaraction_BreadStand` 월드 탭 시 오픈, 빵 목록 중 이 진열대의 `TableId`와 일치하는 항목을 선택하면 `Intaraction_BreadStand.AddBread()` 호출(기존 `mBtnAddBread` 버튼과 로직 공유) |

## 중요 발견 — UI 팝업 프리팹이 실제로는 비어 있음

`UIPopupOrderDetail`/`UIPopupSpecialDrinkProduction`을 포함해 이 프로젝트엔 아직 **Resources에 연결된 실제 UI 프리팹이 하나도 없다**(`Assets/Resources/UIPathInfo.asset`의 `m_pathInfoDic`에 유효한 매핑이 없고 `Resources/UIPrefabs` 폴더 자체가 없음). 즉 View 스크립트는 있어도 `UIManager.Open`이 지금은 프리팹을 못 찾아 실제로는 안 열린다. 새 팝업(`UIPopupBreadSelect`)을 추가할 때도 스크립트만 작성하고 프리팹 제작(Canvas/ScrollView/버튼 배치)과 `UIPathInfo` 등록은 에디터 작업으로 남겨뒀다 — 참고할 기존 프리팹이 없어 텍스트로 대신 만드는 건 신뢰할 수 없다고 판단했기 때문. 이 영역을 다루는 컨셉 지시가 오면 이 사실을 먼저 알릴 것.

## 갭 채우기 사례 — DrinkRequest 요청 테이블

"손님이 무슨 음료를 랜덤으로 요청할지"에 실제 요청 테이블이 없던 문제를, 새 Model을 만들지 않고 다음으로 해결했다:

1. `Assets/CSV/DrinkRequest.csv` 추가 — 컬럼 `Tid, DrinkTid, Weight`. 기존 `CsvToCsTool`(`Assets/Scripts/Tools/CSV/Editor/CsvToCsTool.cs`)이 생성했을 형식 그대로 수기 작성.
2. `Assets/CTable/DrinkRequestRow.cs`, `DrinkRequestTable.cs`도 같은 이유로 수기 작성(`TableManager`가 CSV 파일명 기준으로 `CTable.{이름}Table` 타입을 리플렉션으로 자동 로드하므로 별도 등록 코드 불필요).
3. `DrinkModel.Init()`에서 `DrinkRequestRow`를 로드해 가중치 리스트 구성, `GetRandomSpecialOrderTid()`를 가중치 랜덤으로 교체(테이블이 비어 있으면 기존 균등 랜덤 로직으로 폴백).

CTable/CSV는 절대 수정 금지 대상이므로 실제 파일 생성 전 사용자에게 컬럼안을 제안하고 승인받았다. 새로운 마스터 데이터가 필요한 갭을 만나면 이 방식(CSV+CTable 최소 추가, 기존 Model 확장)을 기본값으로 삼는다.

## 참고

- `DrinkRow.DrinkMaterial1~5`는 0을 "빈 슬롯"으로 취급한다(`UIPopupSpecialDrinkProduction.IsRecipeMatch`).
- 기본 음료(`DefaultDrink`)는 `eConfigType.DefaultDrinkTid`로 결정되며, 특수 주문 랜덤 후보에서 자동 제외되는 로직은 폴백 경로(`GetRandomNonDefaultDrinkTid`)에만 있다. `DrinkRequest` 테이블을 쓸 경우 기본 음료를 후보에서 빼고 싶다면 CSV에 아예 안 넣으면 된다(코드가 자동으로 걸러주지 않음).
