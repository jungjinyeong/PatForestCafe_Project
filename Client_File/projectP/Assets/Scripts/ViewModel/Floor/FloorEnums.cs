// FloorRow.FloorType(int)를 코드에서 읽을 때 쓰는 타입. FurnitureEnums와 같은 이유로 Assets/CTable 밖에서 직접 관리한다.
public enum eFloorType
{
    Store,      // 매장층(B3~1F) — 손님이 빵을 사고 결제하는 층
    Terrace,    // 테라스층(T1~T3) — 손님이 마지막에 들러 머무는 층
}
