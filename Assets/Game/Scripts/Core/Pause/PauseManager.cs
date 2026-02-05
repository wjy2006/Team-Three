using UnityEngine;

namespace Game.Core
{
    public class PauseManager : MonoBehaviour
    {
        public bool pauseAudio = true;

        // 允许多个系统同时请求暂停（菜单、对话、背包…）
        private int _pauseCount = 0;

        public bool IsPaused => _pauseCount > 0;

        public void PushPause(string reason = null)
        {
            _pauseCount++;
            Apply();
        }

        public void PopPause(string reason = null)
        {
            _pauseCount = Mathf.Max(0, _pauseCount - 1);
            Apply();
        }

        public void ForceResume()
        {
            _pauseCount = 0;
            Apply();
        }

        private void Apply()
        {
            Time.timeScale = IsPaused ? 0f : 1f;
            if (pauseAudio) AudioListener.pause = IsPaused;
        }
    }
}
