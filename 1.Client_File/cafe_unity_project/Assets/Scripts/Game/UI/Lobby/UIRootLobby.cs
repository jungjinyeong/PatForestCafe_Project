using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Framework.UI;
using Framework.Extension;

public class UIRootLobby : UIBase
{
    public override eUIType GetUIType()
    {
        return eUIType.UIRootLobby;
    }
}