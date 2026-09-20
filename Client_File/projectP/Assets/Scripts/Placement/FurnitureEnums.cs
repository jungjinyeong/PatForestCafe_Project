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
}
