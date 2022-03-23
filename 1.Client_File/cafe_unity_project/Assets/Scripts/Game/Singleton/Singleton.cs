
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                var singletonObj = new GameObject();
                _instance = singletonObj.AddComponent<T>();
                singletonObj.name = typeof(T).ToString();
                DontDestroyOnLoad(singletonObj);
            }

            return _instance;
        }
    }

    public virtual void Initialize() { }

    public virtual void Destroy() { }

    public void Awake()
    {
        Initialize();
    }

    public void OnDestroy()
    {
        Destroy();
        _instance = null;
    }
}