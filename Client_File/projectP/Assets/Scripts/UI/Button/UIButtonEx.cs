using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIButtonEx : Button
{

    protected override void OnDestroy()
    {
        base.OnDestroy();

        onClick.RemoveAllListeners();
    }
}