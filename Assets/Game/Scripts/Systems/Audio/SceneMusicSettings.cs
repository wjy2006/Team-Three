using UnityEngine;

public class SceneMusicSettings : MonoBehaviour
{
    public AudioClip sceneBGM;
    private readonly float crossfadeTime = 1f;
    void Start()
    {
        if (GameRoot.I != null && GameRoot.I.Audio != null)
        {
            // 如果 sceneBGM 和当前播的一样，AudioSystem 会直接返回，不会有任何声音波动
            GameRoot.I.Audio.PlayMusic(sceneBGM, crossfadeTime);
        }
    }
}