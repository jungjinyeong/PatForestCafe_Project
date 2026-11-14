using System;

// FurnitureVisualData 방향별 그림 아래에 회전 방식에 따라 실제로 나올 방향(정면/옆/뒷면/반전)을 나란히 보여준다.
// 그리는 쪽: Editor/FurnitureDirectionPreviewDrawer(Odin). 런타임 동작에는 영향이 없다.
[AttributeUsage(AttributeTargets.Field)]
public class FurnitureDirectionPreviewAttribute : Attribute
{
}
