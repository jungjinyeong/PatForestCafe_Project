using System;
using System.Collections.Generic;
using UnityEngine;

// 층 기본 배치. 첫 실행(처음부터 열린 층)과 층 해금 시 PlacementModel.ApplyFloorPreset이 이 배치를 그 층에 깐다.
// 가구 리셋 때도 고정이 아닌 항목을 보관 재고로 다시 깐다.
// 에셋은 Datas/FloorPreset/FP_{층Tid}이고 Addressables 주소 FloorPreset/FP_{층Tid}로 자동 등록된다(FloorPresetTool).
// 내용은 Play 모드에서 실제로 배치한 뒤 Tools/Furniture/층 기본 배치 창으로 저장한다. Fixed는 인스펙터에서 항목마다 켠다.
[CreateAssetMenu(fileName = "FP_0", menuName = "Furniture/Floor Preset")]
public class FloorPresetData : ScriptableObject
{
    public const string ADDRESS_PREFIX = "FloorPreset/";
    public const string ASSET_PREFIX = "FP_";

    [Serializable]
    public class FurnitureEntry
    {
        public int Tid;
        // true면 Tid가 SubFurnitureRow, false면 FurnitureRow.
        public bool IsSub;
        // footprint 기준 셀(좌하단).
        public Vector2Int Cell;
        public CTable.eFurnitureDirection Direction;
        public bool FlipX;
        // 디저트 진열대의 진열 종류. 0이면 미지정(첫 사용 때 정해짐).
        public int AssignedDessertTid;
        // true = 옮기기/철거 불가, 보유 수에 안 잡힘. false = 무료 지급(보유 +1), 일반 가구처럼 다룸.
        public bool Fixed;
    }

    [Serializable]
    public class FloorTileEntry
    {
        public Vector2Int Cell;
        public int Tid;
    }

    // Floor.csv Tid = WaypointGroup.Order. 에셋 이름(FP_{Floor})과 같아야 한다.
    public int Floor;
    public List<FurnitureEntry> Furniture = new List<FurnitureEntry>();
    public List<FloorTileEntry> FloorTiles = new List<FloorTileEntry>();

    public static string GetAddress(int floor) => ADDRESS_PREFIX + ASSET_PREFIX + floor;
}
