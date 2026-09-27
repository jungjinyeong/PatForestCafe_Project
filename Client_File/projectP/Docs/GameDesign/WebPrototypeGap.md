# 웹 프로토타입 대비 구현 갭 리스트

참고 사이트: https://forestcafe-ui-lab.pride9631.chatgpt.site/ (ForestCafe UI Lab — PC UI 프로토타입)
작성일: 2026-09-27. 사이트 6개 화면(매장 레이아웃 / 레시피 북 / 상점가 / 제작·커피머신 / 가공섬 / 제작·오븐)의
텍스트·구조를 추출해 현재 코드(`Assets/Scripts`)와 대조한 결과. 에디터/플레이 확인이 아니라 코드 기준 비교다.

진행 상황은 각 항목의 체크박스로 관리한다.

## A. 미구현 (신규 시스템)

- [x] **1. 카페 레벨 / 경험치** (2026-09-27, 퀘스트 경험치는 5번 구현 시 연결) — 탑바 `Lv.4 · 96/100`. 경험치 획득: 매장 결제(건당 소량) · 퀘스트 완료 · 둘기딜리버리 배달 완료. 경험치 안내 팝업.
- [x] **2. 날짜 / 계절 / 시계 표시** (2026-09-27) — `봄 3일 AM 10:24`. 계절별 판매 가격·음료 온도 인기 효과 포함.
- [x] **3. 영업 상태 / 조기 마감** (2026-09-27) — `영업 중 OPEN` → "오늘 영업을 마감할까요?"(새 손님 입장 중단, 남은 손님이 나가면 정산·저장) → `영업 종료`.
- [x] **4. 알림 패널** (2026-09-27, 이벤트 알림만 — 테라스/카운터 인원 상태 줄은 보류) — "오븐 식빵 ×20 조리 완료", "둘기 딜리버리 주문 접수", "테라스 이용 인원 3/5", "카운터 대기 인원 2/4".
- [ ] **5. 퀘스트** — 예: 빵 5개 굽기, 창고에 디저트 보관, 오늘의 주문 6건 완료, 신규 디저트 1종 굽기, 카페 소식 3개 확인. (경험치 획득원)
- [ ] **6. 가공섬 지도 / 섬 이동** — 지도, 가공 현황(진행 중 / 오늘 완료 / 창고 보관: 자동), 다른 가공섬 이동(가공섬 / 커피섬 / 과일섬 / 바다섬).
- [ ] **7. 목장 · 양봉장 시설** — 목장(우유·달걀), 양봉장(꿀), 완성 후 창고 자동 보관, "생산계획 화면". 현재 공방은 단일 일꾼+재료 선택 방식.
- [ ] **8. 가공섬 라이선스 / 공방 협약 / 가공섬 업그레이드** (찍찍이) — 커피농장·목장·요리연구소 라이선스, 목장·과수원 공급 협약, 시설별 업그레이드(고용 한도 3→6, 생산량, 품질, 씨앗 개선). 현재 "준비 중" 팝업.
- [ ] **9. 연구권 구입** (찍찍이) — 1/5/10장 묶음(500G / 2,500G / 5,000G). 레시피 개발권(1002) 획득 경로.

## B. 뼈대만 있음 (내용 채우기)

- [x] **10. 창고 화면** (2026-09-27, 보관 용량/확장은 기획 미정으로 보류) — 탭(가공섬 재료 / 디저트), 그리드, 선택 품목 상세(분류·보관 수량), 보관 공간 용량, "상점가에서 확장하기". 현재 `UIPopupWarehouse`는 빈 안내만.
- [x] **11. 직원 관리 화면** (2026-09-27, 층/업무 배치는 기획 미정으로 보류) — 채용 직원 목록, 빈 상태 시 "직업사무소로 이동". 현재 `UIPopupStaff` 빈 안내만(`StaffModel` 명단은 있음). 층/업무 배치 미구현.
- [ ] **12. 환경설정 확장** — 화면 / 소리 / 조작 / 게임 저장, ESC로 열기. 현재 BGM/SFX 슬라이더만.
- [ ] **13. 가구 구입 카테고리 탭** (똘이) — 전체/진열대/테이블/의자/스탠드/장식/파티션, 크기 표기(3×1 등), 음료 자판기·메뉴 게시판 등. 가구 추가는 `Furniture.csv` 수정 필요(사전 협의).
- [ ] **14. 시설 업그레이드 확장** (똘이) — 카운터 개선, 키오스크 계산 5초→4초(단계형). 카운터/계산 속도 능력치 시스템 선행 필요.
- [ ] **15. 셀프계산대(키오스크) 결제 시간** — 매장 맵 "셀프계산대 5초". `FurnitureEnums`에 Kiosk만 존재.

## C. 구현됨 — 디테일 차이만 확인

- **레시피 북/도감**: 책 형태, 음료/디저트 카테고리, 번호, 재료, 설명 노트, "발견 완료". `UIPopupCollection`의 디저트 카테고리·재료 표시 확인.
- **커피머신 + 둘기딜리버리**: 사이트는 제작·픽업대·배달을 한 화면에 통합. 현재는 `UIPopupDelivery` / `UIPopupDrinkRecipeProduction` 분리. 실패 음료("펑!") 연출 확인.
- **레시피 연구소**: 꾸리 NPC, 확인 팝업, 분석 연출, 도움말 페이지 — 거의 일치.
- **오븐**: 트레이 3칸/해금, 재료 등급 교체, 예상 품질, 창고 자동 보관 — 일치. 퀘스트(5번)와 빵 재고 창고 1/2 드래그 UI 확인.
- **하단 빠른 메뉴**: 창고/가공섬/상점/배치/직원/도감 — 버튼이 `UIRootLobby`/`UIHudController`에 분산. 하단 바 통합 레이아웃.

## D. 기획 충돌 — 결정 필요

- **층 구조**: 사이트 `T3/T2/T1/1F/B1/B2/B3`(지하·테라스 3층) vs 현재 6층 온천탑(1~5층 + 6층 공용 테라스).
- **2층 개방 공사**: 사이트는 15,000G, 카운터·키오스크 기본 설치. 현재 `UIFloorUnlock` 비용/기본 설비와 맞춰볼 것.

## 추천 순서

1. 테이블 수정 불필요: 1(카페 레벨) → 10(창고) → 11(직원) → 4(알림)
2. 코어 루프: 3(영업 마감·정산) → 2(날짜/계절)
3. CSV/CTable 추가 필요(수정 금지 대상 — 착수 전 협의): 5, 6~9, 13

## 진행 기록

### 1. 카페 레벨 / 경험치 (2026-09-27)

- **모델**: `CafeModel`(`Assets/Scripts/ViewModel/Cafe/CafeModel.cs`, `CafeModel+Ctrl.cs`) — `Level`/`Exp` ReactiveProperty, `OnLevelUp`, `AddExp()`(연속 레벨업 처리), `Restore()`. `CommonModelManager.Cafe`로 등록.
- **임시 수치(코드 상수, 기획 확정 후 교체)**: 최대 레벨 50, 필요 경험치 `50 × 1.25^(Lv-1)`, 매장 결제 +2, 배달 완료 +10, 퀘스트 +20(`EXP_QUEST`, 퀘스트 미구현이라 아직 호출처 없음).
- **획득 연결**: `CharNpc.TryReceiveBreadGold`(빵 결제 1개당), `CharNpc.ReceiveRandomUnlockedDrinkGold`(음료 결제), `DeliveryModel.TryDeliver`(배달 성공).
- **세이브**: `SaveData.CafeLevel/CafeExp`. 구 세이브(0)는 `Restore()`에서 Lv1로 보정.
- **UI**: `UITopbarInfo`에 `mTextCafeLevel`/`mTextCafeExp`/`mImageExpGauge`(Filled)/`mBtnCafeLevel`/`mExpGuideRoot`/`mBtnExpGuideClose` 추가. `UI_HudController.prefab`에 `UICafeLevel`(Lv 텍스트 + 게이지 + 경험치 텍스트) · `ExpGuide`(레벨 클릭 시 토글, 패널 클릭 시 닫힘) 추가·배선. `UIManager.prefab`의 정적 자리표시자 `Text_CafeLevel`은 실제 위젯으로 대체되어 제거.
- **검증**: 플레이 모드에서 `AddExp(130)` → Lv3 18/78, UI 반영, 세이브 저장값 확인 후 `save.json` 원복.
- **남은 것**: 레벨업 연출/보상 없음(`OnLevelUp` 구독처 없음), 레벨에 따른 해금 규칙 미정, 아트는 기존 재화 박스 스프라이트 재사용(가안).

### 10. 창고 화면 (2026-09-27)

- **스크립트**: `UIPopupWarehouse`(빈 안내 → 실제 화면), 그리드 칸 `UIWarehouseCell`(+`UIWarehouseCellData`) 신규. 둘 다 `Assets/Scripts/UI/Lobby/`.
- **표시 데이터(테이블 수정 없음)**:
  - 가공섬 재료 탭 = `MaterialModel`(음료 재료 + 빵 재료) 중 보유 수량 > 0.
  - 디저트 탭 = 오븐에서 구워 창고에 보관 중인(진열 전) 빵 재고 `BreadData.ProducedCount` > 0.
  - 상세: 이름 · 분류 도장 · 설명(`DrinkMaterial/BreadMaterial/Bread.csv` Desc) · 정보(분류 / 보관 수량 / 빵 재료 등급 또는 디저트 품질별 수량 / 속성).
  - 상단 "총 보관 N개"(두 탭 합계). 탭 라벨에 품목 종류 수.
- **실시간 갱신**: 열려 있는 동안 재료/빵 재고 수량이 바뀌면(공방 생산, 오븐 보관 등) 1프레임 스로틀 후 다시 그림. 닫히면 구독 해제.
- **프리팹**: `UI_Popup_Warehouse.prefab`을 도감(`UI_Popup_Collection`) 책 레이아웃을 복제해 재구성 — 배경 클릭/✕ 닫기, 탭 2개, `GridLayoutGroup` 4열 스크롤, 빈 상태 안내, 오른쪽 상세(`Detail`). 기존 로비 하단 [창고] 버튼 그대로 연결됨.
- **검증**: 플레이 모드에서 재료 20종/총 82개 표시, 빵 재료(고운 밀가루: 중급·속성) · 디저트(소금빵: 하급 2) 상세, 열린 상태에서 재료 +5 즉시 반영, ✕/배경 닫기 · 하단 버튼 재오픈 확인. `save.json` 원복.
- **남은 것**: 아이콘 리소스가 없어 이름 첫 글자로 대체(재료 아틀라스 없음). 음료 재료 "우유"와 빵 재료 "우유"가 같은 이름으로 두 칸 표시됨(상세 분류로만 구분). 보관 용량 · "상점가에서 확장하기"는 용량 기획이 정해지면 추가. 아이템(레시피 개발권 등) 탭은 시안에 없어 제외.

### 11. 직원 관리 화면 (2026-09-27)

- **스크립트**: `UIPopupStaff`(빈 안내 → 실제 화면), 명단 칸 `UIStaffCell`(+`UIStaffCellData`) 신규. `Assets/Scripts/UI/Lobby/`.
- **표시 데이터(테이블 수정 없음)**: `StaffModel.HiredStaff` 순서대로 명단 표시(`#1, #2…`). 상세 = 이름(`StaffRow.Name`) · 직원 번호 · 작업 속도(`StaffRow.WorkSpeed`) · 배치("미배치" 고정).
- **동작**: 비어 있으면 "아직 채용한 직원이 없어요…" 안내. [직업사무소로 이동] → `UIPopupJobOffice`를 위에 연다. 열려 있는 동안 `HiredCount`를 구독해 채용 즉시 명단 갱신 + 방금 채용한 직원 자동 선택.
- **프리팹**: `UI_Popup_Staff.prefab`을 창고 팝업 레이아웃을 복제해 재구성(탭 제거, 명단 그리드, 빈 상태, 직업사무소 버튼, 오른쪽 상세). 로비 하단 [직원] 버튼 그대로 연결.
- **검증**: 플레이 모드에서 빈 상태 → 직업사무소 오픈 → 2명 채용 → 닫으면 명단 2명·#2 선택, #1 선택/닫기/재오픈 확인. `save.json` 원복.
- **남은 것(기획 필요)**: 채용 직원은 아직 게임플레이 효과가 없음(`CharStaff`는 씬 미배치, 채용 명단과 연결 안 됨). 층/업무 배치, 해고, 직원별 초상화, 직업사무소 이력서 화면 리뉴얼(시안의 "업무·고용 조건")은 기획 확정 후.

### 4. 알림 패널 (2026-09-27)

- **모델**: `NoticeModel`(`Assets/Scripts/ViewModel/Notice/NoticeModel.cs`, `NoticeModel+Ctrl.cs`) — 최근 알림 `ReactiveCollection<NoticeData>`(최신이 0번, 최대 20개, 세이브 안 함), `Push()`, `Bind()`. `CommonModelManager.Notice`로 등록.
- **알림 소스(`Bind()`)**:
  - 오븐 `OvenModel.OnBakeCompleted` → "오븐 {빵} ×{수량} 굽기 완료"
  - 둘기딜리버리 `DeliveryModel.Orders` 추가 → "둘기딜리버리 주문 #{번호} 접수 · {음료}"
  - 카페 `CafeModel.OnLevelUp` → "카페 레벨 {N} 달성!"
  - 진열 수량 `BreadData.Count`가 >0 → 0 → "빵 진열대 재고 부족 · {빵}"
- **구독 시점**: 세이브 복원(배달 주문/진열 수량)이 알림으로 쌓이지 않도록 `GameModeLobby+FSM.OnEnterOpenLobbyUI()`에서 `Bind()` 호출(재호출 시 기존 구독 교체).
- **View**: `UILobbyNotice`(`UI_Root_Lobby/Sidebar/NoticeSlot/NoticePanel`에 부착, `Notice0~2` 배선) — 최신 3줄 표시, 없으면 "새 알림이 없어요", 긴 문장은 말줄임. `UIRootLobby.Init()`에서 `Init()`.
- **검증**: 플레이 모드 로드 직후 알림 0개(복원 노이즈 없음) → 레벨업/오븐 완료/배달 주문/재고 소진 4종 모두 기록·표시 확인. `save.json` 원복.
- **남은 것**: 시안의 상태 줄("테라스 이용 인원 3/5", "카운터 대기 인원 2/4")은 테라스 수용 인원·카운터 대기열 개념이 코드에 없어 보류. 알림 전체 목록 보기(패널 클릭 → 히스토리)·알림 종류별 아이콘 없음. 배달 주문이 한 번에 여러 장 채워지면 알림도 여러 줄 생김.

### 3. 영업 상태 / 조기 마감 · 정산 (2026-09-27)

- **확정 기획**: 22시 자동 마감 + [조기 마감] 버튼 / 정산 팝업 [다음 날 영업 시작]으로 일차 +1·AM 8시 / 종료 중엔 손님 입장만 중단(오븐·공방·배달·시계 계속) / 정산 = 매출·판매 건수·방문 손님·카페 경험치.
- **모델**: `BusinessModel`(`ViewModel/Business/`) — `State`(Open/Closing/Closed), `Day`, 오늘 통계 6종, `RequestClose/CompleteClose/StartNextDay/Restore`. 개점 8시·마감 22시는 코드 상수(임시).
- **통계 기록**: 빵/음료 결제(`CharNpc`), 배달(`DeliveryModel.TryDeliver`), 방문(`SpawnManager.SpawnNPC`), 경험치(`CafeModel.AddExp`).
- **흐름**: `GameModeLobby+Business.cs` — 상태 구독(Open → 자동 스폰 / Closing → 스폰 중단 + `SpawnManager.ActiveCount` 0 대기, 60초 초과 시 남은 손님 정리 / Closed → 저장 + 정산 팝업), 게임 시계 22시 통과 시 자동 마감. `OnEnterSpawn()`은 `StartAutoSpawn()` 대신 `BindBusiness()`.
- **세이브**: `SaveData.BusinessDay` + `Today*` 통계. 영업 상태는 저장 안 함(재실행 시 영업 중으로 시작).
- **UI**: 탑바 정적 자리표시자 `Text_DayClock`/`Text_ShopStatus`를 `UI_HudController.prefab` 본체로 옮겨 실데이터 연결(`N일 AM hh:mm`, `영업 중 OPEN`/`마감 중 · 손님 N명`/`영업 종료`, 상태 클릭 → 조기 마감 확인 또는 정산 다시 보기). 범용 `UIPopupConfirm`(`UI_Popup_Confirm`) · `UIPopupSettlement`(`UI_Popup_Settlement`) 신규, `eUIType.PopupConfirm/PopupSettlement` 추가 + `UIManager.prefab` 등록. 알림에 영업 시작/마감/종료 추가.
- **검증**: 플레이 모드 — 조기 마감(계속 영업 취소 포함) → 손님 2명 퇴장 후 정산(매출 15,000G/음료 3/방문 8/경험치 +6, 골드 증가분·save.json 일치) → ✕ 후 탑바로 재오픈 → 다음 날(2일 AM 08:00, 통계 0, 스폰 재개) → 22시 자동 마감 → 손님 이동 정지 상태에서 60초 안전장치로 종료. `save.json` 원복.
- **시간 테이블(2026-09-27 추가)**: 하루 길이·개점/마감 시각·마감 대기 제한을 새 테이블 `Assets/CSV/GameTime.csv`(Tid 1: `DayDurationSeconds, OpenHour, CloseHour, ClosingTimeoutSeconds`, 현재 120 / 8 / 22 / 60)로 분리. `CTable.GameTimeRow/GameTimeTable`은 `CsvToCsTool`로 이 CSV만 생성(전체 변환은 손으로 고친 기존 CTable을 덮어쓰므로 사용 안 함). `BusinessModel`의 정적 프로퍼티(`DayDurationSeconds/OpenHour/CloseHour/ClosingTimeoutSeconds`, 테이블 없으면 기본값)로 읽고, `TimeManager.Init()`이 하루 길이·시작 시각(=개점)을 테이블 값으로 덮어씀(인스펙터 `mDayDurationSeconds/mStartHour`는 더 이상 쓰이지 않음). 마감 24시는 자정 통과로 처리. 플레이 모드에서 300초/20시로 바꿔 반영 확인 후 원복.
- **코드 리뷰 수정(2026-09-27)**:
  - 마감 대기 초과 시 `SpawnManager.DespawnAll()`이 NPC 정리 없이 풀로 돌려 재사용 손님이 빵/타이머를 들고 나오던 문제 → `CharNpc.ForceLeave()`(들고 있던 빵 치우기 + 정상 퇴장 정리) 경유로 변경.
  - 정산 후 종료 전에 게임을 끄면 같은 날을 다시 영업해 통계가 중복되던 문제 → 영업 종료(Closed) 상태를 세이브(`SaveData.BusinessClosed`), 재실행 시 종료 상태로 복원해 정산 팝업부터 표시. 마감 중(Closing)은 여전히 저장 안 함(정산 전이라 중복 없음).
  - 영업 종료 중의 배달 매출·경험치가 이미 끝난 정산에 섞였다가 다음 날 초기화로 사라지던 문제 → `BusinessStats NextDayStats`에 따로 쌓고 [다음 날 영업 시작] 때 오늘 통계로 이월(세이브 포함). 세이브 필드는 `TodayStats`/`NextDayStats`(BusinessStats)로 정리.
  - 검증: 빵을 든 NPC 강제 퇴장 → 빵·타이머·목록 정리, 종료 상태 저장 → 재실행 시 Closed·정산 팝업·스폰 없음, 종료 중 배달 1,000G/경험치 10 → 다음 날 통계로 이월.
- **남은 것**: 시간 값 밸런스(현재 하루 120초 → 영업 약 70초), 정산 연출. 원본 테이블(엑셀 등)이 따로 있다면 GameTime 시트 추가 필요.

### 2. 날짜 / 계절 (2026-09-27)

- **확정 기획**: 계절 길이는 `GameTime.csv`에 `SeasonDays` 열 추가(28일), 계절별 수치는 새 `Season.csv`. 효과는 **판매 가격** + **음료 온도 인기**(손님 수 효과는 제외).
- **테이블**: `Assets/CSV/Season.csv` — `Tid(순서), Name, SaleRate, IceDrinkRate, HotDrinkRate` / 봄 1·1·1, 여름 1.1·1.5·0.6, 가을 1·1·1.2, 겨울 1.1·0.6·1.5 (가안). `GameTime.csv`에 `SeasonDays` 추가. 두 CSV만 `ProcessCsvFile`로 재생성(`GameTimeRow/Table`, `SeasonRow/Table`).
- **계산**: `BusinessModel` — 일차(`Day`)에서 계절 = `(Day-1)/SeasonDays % 계절 수`(Tid 순서로 순환), 계절 내 일 = `(Day-1)%SeasonDays+1`. `GetDateText(day)` → "봄 3일"(계절 테이블 없으면 "N일차"). `GetSaleRate/ApplySaleRate`, `GetDrinkPopularityRate(temp)`(0 이하 값은 1로 취급).
- **효과 적용(`CharNpc`)**: 빵·음료 매장 결제 골드 = 기존 계산 × 계절 `SaleRate`(배달 보상은 제외). 손님 음료 추첨 가중치 = `DrinkRow.Weight` × 온도별 인기 배율(정수 → 실수 가중치로 변경).
- **표시**: 탑바 "여름 1일 AM 08:00", 정산 팝업 제목 "봄 28일 영업 정산", 알림 "여름 1일 영업 시작"/"봄 28일 영업 종료" + 계절 첫날 "여름이 시작됐어요 · ICE 음료 인기".
- **검증**: 플레이 모드 — 날짜 경계(28→29 여름, 56→57 가을, 84→85 겨울, 112→113 봄) 확인, 봄 28일 마감 → 다음 날 여름 1일 전환·알림·판매 배율 1.1 확인. 전체 음료 테이블 기준 ICE 선택 비율 봄 0.80 / 여름 0.91 / 가을 0.77 / 겨울 0.62(2만 회 시뮬레이션). `save.json` 원복.
- **남은 것**: 계절별 배경·연출 없음, 연도 표기 없음(4계절 후 다시 봄), 수치 밸런스.

