using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UniRx;

public partial class GameInstance : MonoBehaviour
{
    static GameInstance _instance;
    public static GameInstance Instance { get { return _instance; } }

    void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this);

        Init();
    }

    public void Init()
    {

    }
}