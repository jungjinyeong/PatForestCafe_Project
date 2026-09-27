using System.Collections.Generic;

public enum eOvenTrayState
{
    Empty,      // 반죽 미설정
    Ready,      // 반죽 설정 완료(굽기 대기)
    Baking,     // 굽는 중(BakeEndUnixSeconds에 완료)
}

// 오븐 트레이 1칸의 반죽 설정. 재료는 레시피 1개 기준이며, 굽기 시작 시 × Quantity 만큼 소모된다.
public class OvenTrayData
{
    public eOvenTrayState State = eOvenTrayState.Empty;
    public int RecipeTid;
    public int Quantity;
    public List<int> BaseMaterialTids = new List<int>();
    public List<int> ExtraMaterialTids = new List<int>();
    public eBreadQuality Quality = eBreadQuality.Low;
    public long BakeEndUnixSeconds;
    // 굽기 시작 시점의 굽기 시간(초). 굽는 도중 오븐 업그레이드로 굽기 시간이 바뀌어도 진행 게이지가 맞도록 따로 둔다.
    public int BakeDurationSeconds;

    public bool IsEmpty => State == eOvenTrayState.Empty;

    public void Clear()
    {
        State = eOvenTrayState.Empty;
        RecipeTid = 0;
        Quantity = 0;
        BaseMaterialTids.Clear();
        ExtraMaterialTids.Clear();
        Quality = eBreadQuality.Low;
        BakeEndUnixSeconds = 0;
        BakeDurationSeconds = 0;
    }
}
