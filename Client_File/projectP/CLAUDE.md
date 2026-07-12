# 프로젝트 개요

Unity 프로젝트입니다.

---

## 폴더 구조

```
Assets/
├── Scripts/                          # C# 스크립트
│   └── ViewModel/                    # MVC Model + Controller 파일
├── CSV/                              # 데이터 테이블 csv
├── CTable/                           # 데이터 테이블 스크립트
├── Scenes/                           # 씬 파일
├── Datas/Textures/                   # 텍스처 이미지
├── Datas/Characters/                 # 캐릭터 프리팹
├── Datas/Characters/Animations/      # 애니메이션 클립 및 컨트롤러
└── Datas/UI/                         # UI 관련 에셋
```

---

## 코딩 컨벤션 (C#)

### 기본 원칙
- Unity 기본 스타일을 따름
- 한 클래스는 하나의 책임만 가짐
- MonoBehaviour를 상속하는 클래스는 파일명과 클래스명을 일치시킴

### 네이밍 규칙

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스 | PascalCase | `PlayerController` |
| 메서드 | PascalCase | `TakeDamage()` |
| public 변수 | PascalCase | `MaxHealth` |
| private 변수 | camelCase | `mCurrentHealth` |
| 상수 | ALL_CAPS | `MAX_LEVEL` |
| 인터페이스 | I + PascalCase | `IDamageable` |
| 열거형(enum) | PascalCase | `eGameState` |
| 열거형 멤버 | PascalCase | `eGameState.Playing` |

### 직렬화 필드
```csharp
// SerializeField로 Inspector 노출, private 유지
[SerializeField] private float moveSpeed = 5f;
[SerializeField] private GameObject bulletPrefab;
```

### 접근 제한자
- 항상 명시적으로 작성 (`private`, `public`, `protected`)
- 기본값에 의존하지 않음

### Unity 이벤트 메서드 순서
```csharp
// 1. 필드 선언
// 2. 프로퍼티
// 3. Awake / OnEnable / Start
// 4. Update / FixedUpdate / LateUpdate
// 5. 이벤트 / 콜백 메서드
// 6. public 메서드
// 7. private 메서드
```

---

## MVC 패턴

### 구조 원칙
- **Model** — 데이터 및 상태만 보유, 비즈니스 로직 없음
- **Controller** — Model을 조작하고 View에 반응 전달, MonoBehaviour 없음
- **View** — MonoBehaviour 상속, UI/렌더링만 담당

### 파일 위치 및 네이밍
모든 Model과 Controller는 `Assets/Scripts/ViewModel/` 에 위치합니다.

```
Assets/Scripts/ViewModel/
├── ItemModel.cs          # 데이터 클래스
├── ItemModel+Ctrl.cs     # ItemModel 전용 컨트롤러
├── PlayerModel.cs
├── PlayerModel+Ctrl.cs
└── ...
```

- Model 파일명: `{Name}Model.cs`
- Controller 파일명: `{Name}Model+Ctrl.cs`

### 예시
```csharp
// ItemModel.cs
public partial class ItemModel
{
    public ReactiveProperty<int> Count { get; } = new ReactiveProperty<int>(0);
    public ReactiveProperty<string> Name { get; } = new ReactiveProperty<string>();
}

// ItemModel+Ctrl.cs
public partial class ItemModel
{
    public void AddItem(int amount) => Count.Value += amount;
}
```

---

## UniRx

코루틴 대신 UniRx를 사용합니다.

### 기본 원칙
- 시간 지연, 반복, 비동기 흐름은 모두 UniRx Observable로 처리
- `AddTo(this)` 로 반드시 라이프사이클에 바인딩하여 메모리 누수 방지
- 코루틴(`IEnumerator`, `StartCoroutine`)은 UniRx로 대체 불가한 경우에만 사용

### 자주 쓰는 패턴

```csharp
// 시간 지연 (코루틴 대체)
Observable.Timer(TimeSpan.FromSeconds(2f))
    .Subscribe(_ => DoSomething())
    .AddTo(this);

// 매 프레임 실행 (Update 대체)
Observable.EveryUpdate()
    .Subscribe(_ => Tick())
    .AddTo(this);

// 값 변화 감지 (ReactiveProperty)
mModel.Count
    .Subscribe(count => UpdateUI(count))
    .AddTo(this);

// 조건 충족 시 1회 실행
mModel.IsReady
    .Where(v => v)
    .First()
    .Subscribe(_ => OnReady())
    .AddTo(this);

// 일정 간격 반복
Observable.Interval(TimeSpan.FromSeconds(1f))
    .Subscribe(_ => OnTick())
    .AddTo(this);

// 버튼 및 UI onValueChange (UIButtonEx를 보통 사용, Extension)
mButton.OnSubscribeOnClick(OnClickButton).AddTo(this);

```

### 구독 해제
- MonoBehaviour에서는 `.AddTo(this)` 필수
- 수동 해제가 필요한 경우 `CompositeDisposable` 사용

```csharp
private CompositeDisposable mDisposables = new CompositeDisposable();

void OnDestroy() => mDisposables.Dispose();
```

---

## 주의사항

- `Find()`, `GetComponent()`는 Awake/Start에서만 호출, Update에서 사용 금지
- 코루틴은 UniRx로 대체 불가한 경우에만 사용
- 씬 이름과 파일명은 PascalCase (예: `MainMenu`, `GamePlay`)
- Controller는 MonoBehaviour를 상속하지 않음

---

## 절대 수정 금지 파일
다음 파일/폴더는 어떤 경우에도 수정하지 마세요:
- Assets/CTable/ (테이블 데이터들)
- Assets/CSV/ (테이블 csv 파일)
- Assets/Plugins/ (서드파티 에셋, 건드리면 깨짐)
- Assets/StreamingAssets/config.json (런타임 설정)
- ProjectSettings/ (프로젝트 세팅)
- Packages/manifest.json (패키지 의존성)

위 파일 수정이 필요해 보이는 상황이면, 수정 대신 반드시 먼저 나에게 물어보세요.