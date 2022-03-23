using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UniRx;

public class UserInfo
{
    public int mId = 0;
}

public class GameManager : Singleton<GameManager>
{
    private ReactiveProperty<List<UserInfo>> mUserInfos = new ReactiveProperty<List<UserInfo>>();

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }
}