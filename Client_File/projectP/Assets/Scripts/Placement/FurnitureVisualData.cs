using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

// 가구/소품 1개의 아트 설정. 아트 작업자가 Inspector에서 직접 고치는 값만 둔다
// (가격/칸 수/그룹 같은 기획 값과 아이템 아이콘(Atlas/Icon)은 Furniture.csv / SubFurniture.csv).
// 어떤 가구가 이 에셋을 쓰는지는 CSV의 VisualPath(이 에셋의 Addressables 주소)로만 정한다 — 에셋에는 Tid를 두지 않는다.
// 에셋 위치: Datas/Graphic/Furniture/FurnitureVisual/{Tiles|Display|Dining}/, 주소: FurnitureVisual/{에셋명}
// (Tools/Furniture/Register Visual Data Addressables로 일괄 등록).
[CreateAssetMenu(fileName = "FV_", menuName = "Furniture/Visual Data")]
public class FurnitureVisualData : ScriptableObject
{
    // Addressables 주소 = ADDRESS_PREFIX + 에셋명(FurnitureVisualDataTool이 자동 등록). CSV VisualPath에 이 주소를 적는다.
    public const string ADDRESS_PREFIX = "FurnitureVisual/";

    [LabelText("가구 프리팹")]
    [PropertyTooltip("배치할 가구 프리팹(Datas/Graphic/Furniture/FurniturePrefabs, Addressables 등록 필요).")]
    [SerializeField] private AssetReferenceGameObject mPrefab;

    [LabelText("회전 방식")]
    [PropertyTooltip("R키 회전 방식.\n- 회전 없음\n- 일반 가구: 방향별 그림(앞/뒤/옆)으로 바뀌고 차지하는 칸도 같이 돈다\n- 벽가구: 좌우반전만\n- 바닥타일: 그림 자체를 90°씩 회전")]
    [SerializeField] private CTable.eFurnitureOrientMode mOrientMode = CTable.eFurnitureOrientMode.None;

    [Title("방향별 그림")]
    [LabelText("정면")]
    [PropertyTooltip("정면(아래를 보는 방향) 그림. 비어 있으면 프리팹에 지정된 그림을 쓴다.")]
    [SerializeField] private Sprite mFront;
    [LabelText("뒷면")]
    [PropertyTooltip("뒷면(위를 보는 방향) 그림. 비어 있으면 정면 그림을 쓴다. 회전 방식이 '일반 가구'일 때만 쓴다.")]
    [SerializeField] private Sprite mBack;
    [LabelText("옆면")]
    [PropertyTooltip("옆면 그림(오른쪽을 보는 방향 기준, 왼쪽은 FlipX). 비어 있으면 정면 그림을 쓴다. 회전 방식이 '일반 가구'일 때만 쓴다.")]
    [SerializeField] private Sprite mSide;

    [Title("바닥타일")]
    [LabelText("좌우반전 포함")]
    [PropertyTooltip("회전 방식이 '바닥타일'일 때만 쓴다. 켜면 R 회전 순환에 좌우반전 상태를 포함한다(0°,90°,180°,270° 다음 반전된 4방향).")]
    [SerializeField] private bool mTileIncludeFlipX;

    public AssetReferenceGameObject Prefab => mPrefab;
    public CTable.eFurnitureOrientMode OrientMode => mOrientMode;
    public Sprite Front => mFront;
    public Sprite Back => mBack;
    public Sprite Side => mSide;
    public bool TileIncludeFlipX => mTileIncludeFlipX;

#if UNITY_EDITOR
    // Play 중 Inspector에서 값을 바꾸면 발행한다. 가구 테스트 씬(GameModeFurnitureTest)이 받아 배치된 가구 그림을 바로 갱신한다.
    public static event System.Action<FurnitureVisualData> EditorValidated;

    private void OnValidate()
    {
        if (Application.isPlaying)
            EditorValidated?.Invoke(this);
    }
#endif
}
