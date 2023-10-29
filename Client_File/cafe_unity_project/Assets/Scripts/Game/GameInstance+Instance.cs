using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class GameInstance
{
    static GameInstance _instance;
    public static GameInstance Instance { get { return _instance; } }

    public UIManager UIMgr => UIManager.Instance;

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
}