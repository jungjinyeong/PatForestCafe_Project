
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : IManager
{

    public void Init()
    {
        // TODO : GameFramework 오브젝트가 존재하지 않으면 생성하도록 수정 필요
        // SceneManager, SpawnManager, WayPointManager 등도 GameFramework 오브젝트에 붙여서 관리하도록 수정 필요
        var gameFramework = GameObject.Find("GameFramework");
        if(gameFramework == null)
        {
            Logger.Error("GameFramework 오브젝트가 존재하지 않습니다. GameFramework 오브젝트를 생성하고 GameModeBase를 붙여야합니다.");
            return;
        }

        var gameModeBase = gameFramework.GetComponent<GameModeBase>();
        if (gameModeBase is not null)
        {
            gameModeBase.Init();
        }
        else
        {
            Logger.Error("GameFramework 오브젝트에 GameModeBase가 붙어있지 않습니다. GameModeBase를 붙여야합니다.");
            return;
        }
    }

    public void Subscribe()
    {
    }

    public void Destory()
    {
    }

    public void Clear()
    {
    }
}