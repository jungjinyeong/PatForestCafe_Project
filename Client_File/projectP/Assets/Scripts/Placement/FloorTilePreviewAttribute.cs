using System;

// FurnitureVisualData '칠할 타일' 아래에 샘플 모양으로 칠한 결과를 Inspector에서 보여준다.
// 그리는 쪽: Editor/FloorTilePreviewDrawer(Odin). 런타임 동작에는 영향이 없다.
[AttributeUsage(AttributeTargets.Field)]
public class FloorTilePreviewAttribute : Attribute
{
}
