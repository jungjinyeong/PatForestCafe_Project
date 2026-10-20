namespace CTable
{
    // FurnitureRow.FixedType(int)를 코드에서 읽을 때 쓰는 타입. CsvToCsTool이 재생성하는 CTable 파일과
    // 달리 이 파일은 Assets/CTable 밖에 있어 재생성 시 덮어써지지 않는다 — FurnitureRow.FixedType은
    // CSV의 Enum(...) 컬럼이 아니라 평범한 int라 (데이터가 한 종류뿐이면 값 일부가 누락되는 문제 없이)
    // 항상 이 3가지 값을 전부 갖는다.
    public enum eFurnitureFixedType
    {
        Fixed,      // 고정 — 한번 배치되면 이동 불가
        WallFixed,  // 벽고정 — 벽에 붙어서만 배치 가능
        Free,       // 자유 — 어디든 자유 배치
    }

    // FurnitureRow.FurnitureType(int)를 코드에서 읽을 때 쓰는 타입. eFurnitureFixedType과 같은 이유로
    // Assets/CTable 밖에 손으로 관리한다.
    public enum eFurnitureType
    {
        BreadTable, // 빵테이블
        Counter,    // 계산대
        Door,       // 문
        Stairwell,  // 통합 계단실
        Kiosk,      // 키오스크
        Wall,       // 벽
        Tile,       // 타일
        Carpet,     // 카펫
    }

    // 배치된 가구가 바라보는 방향. 값 = Down(정면이 아래) 기준 반시계 90° 회전 횟수(칸/손님 위치 계산에 그대로 쓴다).
    // 프리팹은 Down 기준으로 만들고, R키는 시계 방향(Down→Left→Up→Right)으로 돈다.
    // 그림(DirectionalSprite)은 Down=Front, Up=Back, Right=Side, Left=Side 좌우반전. 바닥타일(RotateTile)은 그림 회전 단계로 쓴다.
    public enum eFurnitureDirection
    {
        Down,
        Right,
        Up,
        Left,
    }

    // 가구의 회전(R) 방식. 아트 데이터(FurnitureVisualData)에서 가구마다 지정한다.
    public enum eFurnitureOrientMode
    {
        None,               // 회전 없음
        DirectionalSprite,  // 일반 가구 — 방향별 스프라이트(앞/뒤/옆) + 한쪽 옆면은 FlipX, footprint도 같이 돈다
        FlipXOnly,          // 벽가구 — 좌우반전만
        RotateTile,         // 바닥타일 — 그림 자체를 90°씩 회전(+선택적으로 FlipX)
    }

    // FurnitureRow.Category / SubFurnitureRow.Category(int)를 코드에서 읽을 때 쓰는 타입. 가구 구입 목록의 탭 분류.
    // All은 표에 쓰지 않는 "전체" 탭 전용 값이다.
    public enum eFurnitureCategory
    {
        All,        // 전체
        Display,    // 진열대
        Table,      // 테이블
        Chair,      // 의자
        Stand,      // 스탠드
        Deco,       // 장식
        Partition,  // 파티션
    }
}
