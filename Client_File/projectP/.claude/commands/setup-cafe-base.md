---
description: "방치형 카페 경영 시뮬레이션 게임의 베이스 프로젝트 구조를 생성한다 (메뉴 직접 선택 방식, 프로젝트 컨벤션 준수)"
argument-hint: "[게임 컨셉 추가 설명 (선택)]"
---

# 명령: 방치형 카페 시뮬레이션 베이스 생성

너는 지금부터 "동물/캐릭터 손님이 찾아오는 방치형 카페 경영 시뮬레이션" 장르의 Unity 프로젝트 베이스를 구축한다. 코어는 방치형(idle)이지만, **음료 제공만큼은 플레이어가 메뉴에서 직접 골라 서빙하는 능동적 상호작용**이다. 추가 컨셉 지시: $ARGUMENTS

이 작업은 프로젝트의 `CLAUDE.md`에 정의된 코딩 컨벤션(MVC/ViewModel 구조, 네이밍, UniRx 사용법, GameInstance.Init() 규칙, 절대 수정 금지 파일 등)을 **그대로 참조하여** 따른다. 이 커맨드 파일에는 컨벤션을 다시 적지 않고, 이 카페 게임에 한정된 구현 방향만 적는다. 컨벤션과 이 파일 내용이 충돌하면 `CLAUDE.md`가 우선한다.

## 0단계 — 시작 전 확인
- `docs/GDD.md`, `CONVENTIONS.md`, `.claude/context/` 등 게임 기획/컨벤션 문서가 있는지 먼저 확인하고 반드시 참고할 것.
- 기존 `Assets/Scripts/ViewModel/`, `Assets/CTable/`, `Assets/CSV/`에 이미 있는 Model/테이블을 먼저 훑어보고, 재사용 가능한 것이 있으면 새로 만들지 말고 재사용할 것.
- **절대 수정 금지**: `Assets/CTable/`, `Assets/CSV/`, `Assets/Plugins/`, `Assets/StreamingAssets/config.json`, `ProjectSettings/`, `Packages/manifest.json`. 이 파일들의 수정이 필요해 보이면 절대 직접 수정하지 말고, 먼저 사용자에게 물어볼 것.

## 기본 기획 전제 (문서가 없을 경우 이 정의를 기준으로 진행)
- **손님은 두 부류로 나뉜다**:
  1. **상호작용 손님 (동시에 최대 3마리)**: 원하는 음료를 직접 고를 수 있는 슬롯을 차지한 손님. 주문 아이콘이 표시되고, 플레이어가 메뉴판에서 직접 골라 제작·서빙해야 함.
  2. **자동 결제 손님 (그 외 전체)**: 상호작용 슬롯이 가득 찼을 때 들어오는 손님. 별도 조작 없이 기본 음료를 소비하고 자동으로 결제·퇴장 처리됨.
- **코어 루프**: 상호작용 슬롯(3자리) 중 하나가 비면 새 손님 배정 → 주문 표시 → 플레이어가 메뉴에서 음료 선택/제작 → 서빙 → 만족도 판정 → 퇴장·정산. 슬롯이 가득 찼을 때 도착한 손님은 자동 결제 큐로 배정되어 기본 음료 소비 후 자동 퇴장.
- **방치형 특성**: 자동 결제 매출은 오프라인에도 정산됨. 상호작용 슬롯은 오프라인 중 비활성 처리.
- **비폭력/저자극**: 실패로 게임오버되지 않음, 오답도 "약간의 손해"로만 처리.
- **메뉴/손님 마스터 데이터**: 음료 종류, 손님 타입, 언락 조건 등 정적 데이터는 이 프로젝트의 CTable/CSV 시스템으로 관리하는 것이 원칙. 다만 CSV/CTable 폴더는 수정 금지 대상이므로, 필요한 테이블 스키마(예: `MenuItemTable`, `CustomerTypeTable`)는 **직접 만들지 말고 어떤 컬럼이 필요한지 정리해서 사용자에게 먼저 제안**할 것.

## 1단계 — 폴더 구조 (기존 프로젝트 컨벤션 그대로 사용, 새 폴더 만들지 않음)
```
Assets/
├── Scripts/
│   ├── ViewModel/     # Model + Model+Ctrl (이번 작업에서 여기에 파일 추가)
│   └── View/          # MonoBehaviour View 클래스 (없으면 이번에 생성)
├── CSV/                # 수정 금지 — 스키마 제안만
├── CTable/              # 수정 금지 — 스키마 제안만
├── Scenes/
├── Datas/Textures/
├── Datas/Characters/
├── Datas/Characters/Animations/
└── Datas/UI/
```

## 2단계 — Model 정의 (`Assets/Scripts/ViewModel/`)
> Model/Controller 작성 규칙(파일 분리, 네이밍, ReactiveProperty 사용법 등)은 `CLAUDE.md`를 따를 것. 여기서는 이 게임에 필요한 Model 목록과 각 Model이 들고 있어야 할 상태만 정의한다.

- `CustomerModel` — `Id`, `TypeId`(CTable 참조), `State`(`eCustomerState`), `DesiredMenuId`, `PatienceRemaining`
  - Ctrl 책임: 주문 생성, 만족도 계산, 상태 전이
- `InteractionSlotModel` — `SlotCount`(기본 3), `Slots`(손님 참조 배열)
  - Ctrl 책임: 슬롯 배정/해제, 대기열 관리
- `AutoCheckoutModel` — 자동 결제 대기열, 기본 음료 소비 타이머 상태
  - Ctrl 책임: 타이머 진행, 만료 시 정산 트리거
- `MenuOrderModel` — 손님별 주문 큐, 정답/오답 판정 결과
  - Ctrl 책임: 메뉴 선택 입력 → 매칭 → 판정
- `EconomyModel` — 재화, 언락 진행 상태
  - Ctrl 책임: 재화 증감, 언락 처리
- `GameConfigModel` — 슬롯 개수, 오답 페널티, 오프라인 보상 상한 등
  - 가능하면 CTable에서 로드, 임시로는 상수로 시작하고 TODO 표시

> `eCustomerState { eIdle, eSeated, eOrdering, eWaitingServe, eSatisfied, eLeaving }` — 열거형 네이밍은 CLAUDE.md 규칙 그대로 적용.

## 3단계 — View 정의 (`Assets/Scripts/View/`, MonoBehaviour만)
> View 작성 규칙(로직 금지, `GameInstance.Init()` 의존 시 `Init()` 분리 등)은 `CLAUDE.md`를 따를 것. 여기서는 필요한 View 목록만 정의한다.

- `CustomerView` — 손님 스프라이트/애니메이션, `CustomerModel.State` 구독 반영
- `OrderBubbleView` — 상호작용 슬롯 손님 머리 위 주문 아이콘 표시
- `MenuPanelView` — 메뉴 버튼 목록
- `InteractionSlotView` — 슬롯 3자리 표시, 슬롯별 손님 View 배치
- `FacilityView` — 좌석/카운터 등 배치 오브젝트

## 4단계 — 초기화 체인
`GameInstance.Init()` 의존성 처리 방식은 CLAUDE.md 규칙(Awake/Start 직접 호출 금지, `Init()` 분리 후 상위 체인에서 호출)을 그대로 따른다. 이 게임에서는 `InteractionSlotModel`, `AutoCheckoutModel`과 관련 View들이 이 규칙의 적용 대상이다. 실제 프로젝트에 `GameModeLobby+FSM` 또는 유사한 상태 머신이 있다면 그 안의 적절한 단계에서 호출하도록 연결하고, 없다면 어디서 호출해야 하는지 TODO로 명시.

## 5단계 — UniRx 적용 지점 (사용법 자체는 CLAUDE.md 참조)
- 자동 결제 타이머 → `Observable.Timer`
- 대기 시간 감소 틱 → `Observable.Interval`
- 슬롯/손님 상태 변화 → `ReactiveProperty` 구독
- 메뉴 버튼 클릭 → `UIButtonEx.OnSubscribeOnClick`
- MonoBehaviour가 아닌 Ctrl에서 구독이 필요한 경우 처리 방식은 CLAUDE.md 규칙대로 `CompositeDisposable` 사용

## 6단계 — 네이밍/스타일
네이밍, 접근 제한자, 이벤트 메서드 순서 등은 전부 `CLAUDE.md` 규칙을 그대로 적용한다. 이번 작업 특이사항만 명시: 상호작용 슬롯 개수 상수는 `MAX_INTERACTION_SLOT`, 손님 상태 열거형은 `eCustomerState`로 통일.

## 7단계 — 완료 후 보고
작업 완료 후 다음을 요약해서 보여줄 것:
1. 생성된 Model / View 파일 목록 (ViewModel, View 폴더별로)
2. CTable/CSV에 추가가 필요해서 **사용자 확인이 필요한 스키마 제안 목록** (예: MenuItemTable 컬럼안)
3. `Init()` 체인 연결이 필요한데 실제 프로젝트의 상태 머신 구조를 몰라 TODO로 남긴 부분
4. 다음 단계 제안 (예: 슬롯 손님 선택 방식 A/B 확정, 메뉴 초기 세트 정의)