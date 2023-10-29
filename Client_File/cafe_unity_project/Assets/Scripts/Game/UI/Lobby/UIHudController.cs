using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Framework.UI;
using Framework.Extension;
using Game.Event;

public enum eUIHudType
{
    Top,
    Bottom,
}

public class UIHudController : UIWndBase
{
    [SerializeField]
    SerializableDictionary<eUIHudType, UIHudBase> m_uiHudDic = new SerializableDictionary<eUIHudType, UIHudBase>();

    public override eUIType GetUIType() => eUIType.UIHudController;

    public override void Init()
    {
        base.Init();

        MessageBroker.Default.Receive<IUIEvent>().Subscribe(evt =>
        {
            switch(evt)
            {
                case UIBottomEvent bottomEvent:
                    {
                        var hud = GetHud(eUIHudType.Bottom);
                        if(hud)
                        {
                            hud.OnEvent(bottomEvent);
                        }
                    }
                    break;
                case UITopEvent topEvent:
                    {
                        var hud = GetHud(eUIHudType.Top);
                        if(hud)
                        {
                            hud.OnEvent(topEvent);
                        }
                    }break;
            }
        }).AddTo(this);
    }

    UIHudBase GetHud(eUIHudType type)
    {
        if (m_uiHudDic != null && m_uiHudDic.TryGetValue(type, out var hudBase))
            return hudBase;

        return null;
    }
}
