
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Extension;

public class UIScrollDrinkData
{
    public string name;
}

public class UIScrollDrinkRow : UIScrollRow<UIScrollDrinkData>
{
    [SerializeField]
    TextMeshProUGUI mName;
    
    protected override void OnSetData(UIScrollDrinkData data)
    {
        if (data == null)
            return;

        mName.SetTextEx(data.name);
    }
}