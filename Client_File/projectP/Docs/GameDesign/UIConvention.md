# UI 제작 규약 (초안)

> 이 문서는 `UI_Popup_RecipeBook` 프리팹(+`UIPopupRecipeBook.cs`, `UIWndBase.cs`)을 분석해서 뽑아낸 **초안**입니다.
> 아직 검토/확정 전이며, 실제 프로젝트에 맞게 수정이 필요합니다.

---

## 1. 네이밍 / 파일 위치

| 대상 | 규칙 | 예시 |
|---|---|---|
| 팝업 프리팹 | `UI_Popup_{기능명}.prefab` | `UI_Popup_RecipeBook.prefab` |
| 팝업 프리팹 위치 | `Assets/Datas/UI/{도메인}/Popup/` | `Assets/Datas/UI/Lobby/Popup/` |
| 팝업 스크립트 | `UIPopup{기능명}.cs` | `UIPopupRecipeBook.cs` |
| 팝업 스크립트 위치 | `Assets/Scripts/UI/{도메인}/` | `Assets/Scripts/UI/Drink/` |
| 루트 GameObject 이름 | 프리팹 파일명과 동일 | `UI_Popup_RecipeBook` |
| 스크롤 아이템 컨트롤러 | `UIScroll{기능명}.cs` | `UIScrollRecipeBook.cs` |
| 스크롤 아이템 데이터 클래스 | `UIScroll{기능명}Data` | `UIScrollRecipeBookData` |

`eUIType` enum에 `UIPopup{기능명}` 항목을 등록하고, `UIManager.prefab`의 캐시 딕셔너리에 프리팹을 드래그 등록해야 실제로 열립니다(이 부분은 코드만으로 불가능 — 에디터 작업 필요).

---

## 2. 계층 구조 (표준 3단 구성)

```
UI_Popup_{Name}                              ← 루트, RectTransform 풀스트레치 (Anchor 0,0-1,1)
├── background                                ← 반투명 딤 오버레이 (배경 클릭 시 닫힘)
└── panel                                     ← 실제 팝업 박스 (중앙 고정 앵커 + 절대 크기)
    ├── (콘텐츠: Scroll View / Text / Image 등)
    ├── UI_Btn_Close                          ← 공통 닫기 버튼 (우상단 코너 앵커)
    └── UI_Btn_{Action}                       ← 액션 버튼 (공통 버튼 프리팹 재사용)
```

### 2.1 루트
- `m_AnchorMin: {0,0}`, `m_AnchorMax: {1,1}`, `m_SizeDelta: {0,0}` — 캔버스 전체를 채움
- 컴포넌트: `RectTransform` + 팝업 스크립트(`UIPopup{Name}`)만 붙임. Canvas/CanvasScaler는 루트가 아니라 `UIManager.prefab` 쪽에서 관리하므로 팝업 프리팹 자체에는 넣지 않는다.

### 2.2 background (딤 오버레이)
- 루트와 동일하게 풀스트레치
- `Image` 컴포넌트, `m_Color: {0, 0, 0, ~0.47}` (반투명 검정)
- `UIButtonEx` 컴포넌트를 붙여서 `mBtnBgClose`로 연결 → 배경 클릭 시 팝업이 닫힌다.
  - `UIWndBase.Init()`이 `mBtnClose`/`mBtnBgClose`를 **자동으로 `SelfClose()`에 바인딩**하므로, 하위 스크립트에서 별도로 닫기 로직을 짤 필요 없음 (`base.Init()` 호출만 하면 됨).

### 2.3 panel (팝업 본체)
- `m_AnchorMin/Max: {0.5, 0.5}` (중앙 고정), `m_SizeDelta`는 절대 픽셀 크기로 지정 (예: `225 x 225`)
- `Image`(배경, Type: Sliced) + `Outline` 컴포넌트(테두리) 조합이 표준 팝업 박스 스타일
  - Outline 예시: `m_EffectColor: {0.45, 0.32, 0.21, 0.5}`, `m_EffectDistance: {3, 3}`
- panel의 자식으로 실제 콘텐츠(스크롤뷰, 텍스트, 버튼)가 들어간다.

---

## 3. 앵커 규칙

| 요소 유형 | 앵커 방식 | 이유 |
|---|---|---|
| 루트 / background | 풀스트레치 (0,0)-(1,1) | 화면(캔버스) 크기에 항상 맞아야 함 |
| panel (팝업 박스) | 중앙 고정 (0.5,0.5) + 절대 SizeDelta | 팝업은 고정 크기, 화면 중앙에 뜸 |
| panel 내부의 "영역"(Scroll View 등) | **비율 앵커** (예: `AnchorMin {0.05, 0.18}`, `AnchorMax {0.95, 0.92}`) | panel 크기가 바뀌어도 내부 여백 비율이 유지됨 — 절대 좌표보다 이 방식을 우선 사용 |
| 리스트 아이템 내부 텍스트/이미지 | 부모 기준 스트레치(top/bottom 폭 고정) 또는 중앙 고정 | 스크롤 아이템 폭은 부모(Content)를 따라가야 하므로 좌우 스트레치 사용 |
| 코너에 고정되는 버튼(닫기 등) | 코너 앵커(`{1,1}` 등) + Pivot 동일 + 작은 여백(AnchoredPosition) | 팝업 크기가 바뀌어도 코너에서의 거리가 일정하게 유지됨 |

**원칙: 절대 좌표(`AnchoredPosition`/`SizeDelta`)는 최소화하고, 가능하면 앵커 비율이나 스트레치로 표현한다.** 이번에 해상도 변경(CanvasScaler `Scale With Screen Size` → `Constant Pixel Size`)으로 여러 팝업이 깨졌던 이유가, 대부분의 요소가 절대 좌표로 박혀 있었기 때문. `Scroll View`처럼 비율 앵커를 쓴 요소는 이번 변경에 영향을 받지 않았다.

---

## 4. 공통 버튼 재사용 패턴

닫기 버튼, 확인 버튼 등은 새로 만들지 않고 `Assets/Datas/UI/Common/Btns/`의 공통 버튼 프리팹(`UI_Btn_Common`, `UI_Btn_Close` 등)을 **Nested Prefab Instance**로 배치하고, 다음 속성만 오버라이드한다:

- `m_Pivot`, `m_AnchorMin`, `m_AnchorMax` — 배치 위치에 맞는 앵커/피벗 (보통 코너 또는 중앙)
- `m_SizeDelta`, `m_AnchoredPosition` — 실제 크기/위치
- `m_Name` — 인스턴스 이름(`UI_Btn_Close`, `UI_Btn_DevelopRecipe` 등 용도가 드러나게)
- (텍스트 버튼인 경우) `m_text`, `m_fontSize` — 버튼 라벨과 폰트 크기

버튼 클릭 로직은 프리팹이 아니라 **스크립트에서 `[SerializeField] private UIButtonEx mBtnXxx`로 참조를 받아 `OnSubscribeOnClick`으로 구독**한다 (아래 5번 참고). `mBtnClose`는 `UIWndBase`가 이미 처리하므로 예외.

---

## 5. 스크립트 연결 규칙

```csharp
public class UIPopup{Name} : UIWndBase, IUIParam<UIPopup{Name}.Param>
{
    public struct Param { /* 팝업 오픈 시 필요한 데이터 */ }

    [Header("Scroll")]
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject m{Name}RowPrefab;

    [Header("Buttons")]
    [SerializeField] private UIButtonEx mBtnXxx;

    public override eUIType GetUIType() => eUIType.UIPopup{Name};

    public override void Init()
    {
        base.Init();                 // mBtnClose / mBtnBgClose 자동 바인딩
        mScrollEx.Init(m{Name}RowPrefab);
        mBtnXxx.OnSubscribeOnClick(OnClickXxx).AddTo(this);
    }

    public override void Open()
    {
        base.Open();
        RefreshXxx();                // GameInstance.Model/Table 조회는 여기서 — Init이 아니라 Open에서
    }

    public void Set(Param param) { }
}
```

- `[Header("...")]`로 인스펙터에서 그룹핑 (`Scroll`, `Buttons` 등)
- `GameInstance.Model`/`GameInstance.Table` 접근은 `Init()`이 아니라 **`Open()` 이후** 에서 (CLAUDE.md `GameInstance 의존 초기화 규칙` 준수 — 팝업은 `GameInstance.Init()` 완료 후에 열리므로 `Open()` 시점은 안전)
- `Set(Param)`으로 외부에서 데이터 주입, `UIMgr.Open<T, T.Param>(eUIType.Xxx, param)`으로 오픈

---

## 6. 스크롤 리스트 패턴

- `UIScrollEx` + `UIScrollRow<TData>`(제네릭 베이스) 조합
- Hierarchy: `Scroll View` → `Viewport`(Mask) → `Content`(Layout 방향에 따라 Anchor 0,1-1,1 등) → 아이템 프리팹
- 아이템 프리팹은 팝업 프리팹 안에 직접 배치해두고 `mXxxRowPrefab` 필드로 참조 (스크롤 아이템도 팝업과 함께 한 프리팹에 포함되는 구조 — 별도 파일로 안 뺀다)
- 아이템 데이터 바인딩: `UIScroll{Name}Data`(순수 데이터 클래스, `Tid`/`Name`/`Discovered` 등 필드만) → `UIScroll{Name}.OnSetData()`에서 텍스트/이미지 갱신

---

## 7. 텍스트(TMP) 규칙

- 폰트 에셋: 프로젝트 공통 1종 사용 (`fileID: 11400000, guid: de3ffd8ec94fd1f44bed34f7005abfa9`)
- 기본은 `m_enableAutoSizing: 0`(고정 크기) + `fontStyle: Bold`가 리스트/라벨 텍스트에서 흔함
- 자동 크기가 필요한 곳(팝업 안 긴 설명 텍스트 등)만 `m_enableAutoSizing: 1` + `fontSizeMin/Max` 지정
- 폰트 크기는 절대값이 아니라 **박스(SizeDelta) 대비 비율**로 감을 잡는다 (박스 높이의 40~60% 정도가 리스트 텍스트에서 자연스러웠음)

---

## 8. 크기 기준값 (현재 CanvasScaler 기준: Constant Pixel Size, Scale Factor 4)

> 참고용 감(感) 수치. 팝업마다 다를 수 있음.

| 요소 | 대략적 크기 |
|---|---|
| 팝업 패널(작은 것) | 200~230 정도 (정사각) |
| 팝업 패널(큰 것, 스크롤 목록 포함) | 440~460 (너비) |
| 코너 버튼(닫기) | 20×20, 코너에서 5px 여백 |
| 액션 버튼(텍스트 있는 것) | 80×22.5 ~ 110×35 |
| 리스트 아이템 | 35×35 슬롯 기준 |
| 본문 텍스트 | fontSize 4.5~15 |
| 버튼 라벨 | fontSize 6~15 |

---

## 9. 아직 정하지 못한 것 (검토 필요)

- [ ] 팝업 열기/닫기 애니메이션(트랜지션) 유무 및 방식
- [ ] 스크롤 아이템 프리팹을 팝업 안에 직접 둘지, 별도 프리팹으로 분리할지 (현재는 직접 두는 쪽)
- [ ] 폰트 크기 자동/고정 기준을 더 명확히 (본문 vs 라벨 vs 숫자)
- [ ] 팝업 최대/최소 크기 가이드라인 (해상도 대응 범위)
- [ ] `background` 클릭 닫기를 항상 켤지, 팝업 종류별로 끌 수 있게 할지
