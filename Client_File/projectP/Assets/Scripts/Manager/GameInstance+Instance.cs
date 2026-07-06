using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class GameInstance
{
    static GameInstance mInstance;
    public static GameInstance Instance { get { return mInstance; } }

    public static ResourceMgr Resource
    {
        get { return mResourceMgr; }
    }
    private static ResourceMgr mResourceMgr;

    public static TableManager Table
    {
        get
        {
            return mTableMgr;
        }
    }
    private static TableManager mTableMgr;

    public static UIManager UI => UIManager.Instance;

    public static SoundManager Sound
    {
        get
        {
            return mSoundMgr;
        }
    }
    private static SoundManager mSoundMgr;

    public static InputManager Input
    {
        get
        {
            return mInputMgr;
        }
    }
    private static InputManager mInputMgr;

    public static SceneManager Scene
    {
        get { return mSceneMgr; }
    }
    private static SceneManager mSceneMgr;

    public static ObjectPoolManager Pool
    {
        get { return mPoolMgr; }
    }
    private static ObjectPoolManager mPoolMgr;

    public static SpawnManager Spawn
    {
        get { return mSpawnMgr; }
    }
    private static SpawnManager mSpawnMgr;

    public static WayPointManager WayPoint
    {
        get { return mWayPointMgr; }
    }
    private static WayPointManager mWayPointMgr;

    public static CommonModelManager Model
    {
        get { return mCommonModelMgr; }
    }
    private static CommonModelManager mCommonModelMgr;

    public static DayNightManager DayNight
    {
        get { return mDayNightMgr; }
    }
    private static DayNightManager mDayNightMgr;

    public static TimeManager Time
    {
        get { return mTimeMgr; }
    }
    private static TimeManager mTimeMgr;
}