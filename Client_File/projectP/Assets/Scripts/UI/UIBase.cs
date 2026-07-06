using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class UIBase : MonoBehaviour
{
    public void Active()
    {
        gameObject.SetActive(true);
    }

    public void Deative()
    {
        gameObject.SetActive(false);
    }
}