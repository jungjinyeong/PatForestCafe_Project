
using UnityEngine;

public interface IGameMode
{
    void Init();

    void Release(); 
}

public class GameModeBase : MonoBehaviour, IGameMode
{
    public virtual void Init()
    {

    }

    public virtual void Release()
    {

    }
}