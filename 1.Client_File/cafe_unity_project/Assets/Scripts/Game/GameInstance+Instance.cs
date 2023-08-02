using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class GameInstance
{
    public UIManager UIMgr => UIManager.Instance;

    public static SoundManager Sound
    {
        get
        {
            if(null==mSoundMgr)
            {
                mSoundMgr = new SoundManager();
            }
            return mSoundMgr;
        }
    }
    private static SoundManager mSoundMgr;
}