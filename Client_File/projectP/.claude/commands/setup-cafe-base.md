---
description: "방치형 카페 경영 시뮬레이션 게임 — 기존 베이스 위에 컨셉 지시에 맞는 갭만 식별해서 채운다 (메뉴 직접 선택 방식, 프로젝트 컨벤션 준수)"
argument-hint: "[게임 컨셉 추가 설명 (선택)]"
---

# 명령: 방치형 카페 시뮬레이션 — 갭 채우기

"동물/캐릭터 손님이 찾아오는 방치형 카페 경영 시뮬레이션" 장르 게임이다. 코어는 방치형(idle)이지만, **음료 제공만큼은 플레이어가 메뉴에서 직접 골라 서빙하는 능동적 상호작용**이다. 추가 컨셉 지시: $ARGUMENTS

**이 프로젝트는 이미 이 게임의 베이스가 상당 부분 구현되어 있다.** 이 커맨드는 "처음부터 새로 만드는" 용도가 아니라, 컨셉 지시에 맞춰 **기존 구조 대비 실제로 빠진 부분만 찾아서 최소한으로 채우는** 용도다. 아래 1단계에서 관련 참조 파일을 먼저 읽지 않고 새 Model/View를 만들면 안 된다.

이 작업은 프로젝트의 `CLAUDE.md`에 정의된 코딩 컨벤션(MVC/ViewModel 구조, 네이밍, UniRx 사용법, GameInstance.Init() 규칙, 절대 수정 금지 파일 등)을 **그대로 참조하여** 따른다. 이 커맨드 파일에는 컨벤션을 다시 적지 않는다. 컨벤션과 이 파일 내용이 충돌하면 `CLAUDE.md`가 우선한다.

## 0단계 — 시작 전 확인
- `docs/GDD.md`, `CONVENTIONS.md` 등 추가 기획/컨벤션 문서가 있는지 먼저 확인. (2026-07 기준: 없음. 이 파일 + `.claude/context/cafe/*.md` + `CLAUDE.md`가 기준.)
- **절대 수정 금지**: `Assets/CTable/`, `Assets/CSV/`, `Assets/Plugins/`, `Assets/StreamingAssets/config.json`, `ProjectSettings/`, `Packages/manifest.json`. 이 파일들의 수정(새 테이블 추가 포함)이 필요해 보이면 절대 먼저 만들지 말고, 스키마안을 제시하고 사용자 확인을 받은 뒤 진행한다.

## 1단계 — 기존 구현 매핑 (재사용 우선, 새로 만들지 않음)

기능별 매핑표는 아래 참조 파일로 분리되어 있다. **컨셉 지시가 어느 영역에 해당하는지 먼저 판단하고, 해당 파일(들)을 읽은 뒤에 작업을 시작한다.** 여러 영역에 걸치면 관련된 파일을 전부 읽는다.

| 컨셉 지시가 이런 내용이면... | 이 파일을 읽는다 |
|---|---|
| 손님 이동/경로, 웨이포인트/존 옵션, 상호작용 슬롯 캡, 자동 결제, 스폰/풀링, 입력(탭) | `.claude/context/cafe/npc-waypoint.md` |
| 주문 UI, 음료 제작(재료 조합), 메뉴/재료/빵 마스터 데이터, 손님이 뭘 요청할지 | `.claude/context/cafe/order-menu.md` |
| 재화/골드, 아이템, 언락, 가구/오브젝트 배치 | `.claude/context/cafe/economy-placement.md` |

각 파일에는 매핑표뿐 아니라 실제로 갭을 채웠던 사례(예: `DrinkRequest` 요청 테이블, `WaypointGroup` 빵 자유 배회 존)가 있다 — 새 갭을 채울 때 규모/방식의 기준으로 삼는다: 새 Model·View 여러 개를 한 번에 만드는 대규모 설계가 아니라, 기존 클래스에 메서드/필드를 더하거나 CTable 한두 개를 추가하는 수준.

## 2단계 — 갭 식별 절차
1. 컨셉 지시에서 요구하는 동작을 해당 참조 파일의 기존 구현으로 설명할 수 있는지 먼저 검토한다.
2. 설명이 안 되는 부분만 "진짜 갭"으로 분리한다. (예: 마스터 데이터에 없는 조건, 기존 상태 머신이 표현 못 하는 분기 등)
3. 갭이 CTable/CSV 스키마를 필요로 하면, `order-menu.md`의 `DrinkRequest` 사례처럼 컬럼안을 만들어 **사용자에게 먼저 제안**한다. 승인 전에는 CSV/CTable 파일을 만들지 않는다.
4. 갭이 순수 로직이면 새 Model을 만들기보다 관련 기존 Model/컴포넌트(참조 파일에 나열된 것들) 확장으로 먼저 해결 가능한지 검토한다. 기존 클래스 책임과 명백히 다른 새 개념일 때만 `Assets/Scripts/ViewModel/`에 새 Model(+Ctrl)을 추가한다.
5. 갭의 종료 조건/반복 규칙 등이 코드에서 유추 불가능할 만큼 애매하거나, 여러 기존 시스템의 동작을 바꿔야 할 만큼 크면 코드를 쓰기 전에 사용자에게 방향을 확인한다.
6. View도 마찬가지로 기존 UI(각 참조 파일의 UI 목록)로 커버되는지 먼저 확인 후, 안 되면 최소 범위로 새 View를 추가한다.

## 3단계 — 초기화 체인
새 Model/컴포넌트가 `GameInstance.Model`, `GameInstance.Table`, `GameInstance.Config` 등에 의존하면 `CLAUDE.md`의 GameInstance 의존 초기화 규칙을 그대로 따른다. 실제 체인: `GameInstance.Init()` → `TableManager.LoadAllTables()` → `CommonModelManager.Init()`(Model들) → 이후 `GameModeLobby+FSM` 로비 진입 단계에서 UI/스폰 관련 `Init()` 호출. 새 로직을 어디에 걸어야 할지 애매하면 `GameModeLobby+FSM.cs`의 기존 단계들을 먼저 확인한다.

## 4단계 — UniRx 적용 지점 (사용법 자체는 CLAUDE.md 참조)
- 시간 지연/타이머(예: `CharNpc`의 일시정지 재개) → `Observable.Timer`
- 반복 틱(예: `SpawnManager`의 자동 스폰) → `Observable.Interval`
- 상태 변화 구독 → `ReactiveProperty`(예: `BreadData.Count`, `ItemData`/`WealthData`)
- 버튼 클릭 → `UIButtonEx.OnSubscribeOnClick`
- MonoBehaviour가 아닌 곳(Model 등)에서 구독이 필요하면 `CompositeDisposable` 사용

## 5단계 — 완료 후 보고
작업 완료 후 다음을 요약해서 보여줄 것:
1. 참조 파일의 매핑표 기준으로 "이미 있어서 재사용한 것" vs "실제로 추가/수정한 것" 구분
2. CTable/CSV에 추가가 필요해서 **사용자 확인을 받은/받아야 하는 스키마**와 그 승인 여부
3. 수정한 파일 목록과, 왜 새 Model/View를 만들지 않고 기존 클래스를 확장했는지(또는 왜 새로 만들 수밖에 없었는지)
4. `Init()` 체인 연결이 필요한데 실제 상태 머신 진입 지점이 불명확해 TODO로 남긴 부분
5. 새로 채운 갭이 향후에도 재사용될 패턴이면, 해당 `.claude/context/cafe/*.md` 참조 파일에 매핑/사례를 추가할지 사용자에게 물어본다
6. 다음 단계 제안
