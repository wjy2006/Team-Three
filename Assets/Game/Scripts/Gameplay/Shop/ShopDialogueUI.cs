using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.UI.Shop
{
    public class ShopDialogueUI : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text nameText;
        public TMP_Text contentText;

        [Header("Typewriter")]
        [Tooltip("每秒显示多少个字符。0或负数=瞬间显示")]
        public float charsPerSecond = 40f;

        [Tooltip("标点额外停顿（秒），0=不额外停顿")]
        public float punctuationPause = 0.03f;
        public AudioSource voiceAudioSource;
        public AudioClip voiceClip;
        public float voiceInterval = 0.06f;
        public float voicePitch = 1.0f;

        // ✅ 表情系统用：开始说话/结束说话
        public event Action OnTypingStart;
        public event Action OnTypingEnd;

        private Coroutine typingCo;

        public bool IsTyping => typingCo != null;
        private void Start()
        {
            // 关键：对话可能发生在暂停期间，不能让声音挂掉
            voiceAudioSource.ignoreListenerPause = true;
        }
        public void ShowKeys(string speakerKey, string contentKey)
        {
            var loc = GameRoot.I != null ? GameRoot.I.Localization : null;

            string speaker = string.IsNullOrEmpty(speakerKey) ? "" : (loc != null ? loc.Get(speakerKey) : speakerKey);
            string content = string.IsNullOrEmpty(contentKey) ? "" : (loc != null ? loc.Get(contentKey) : contentKey);

            ShowRaw(speaker, content);
        }

        public void ShowRaw(string speaker, string content)
        {
            if (nameText != null) nameText.text = speaker ?? "";
            StartTypewriter(content ?? "");
        }

        public void Clear()
        {
            StopTypewriterIfNeeded();
            if (nameText != null) nameText.text = "";
            if (contentText != null) contentText.text = "";
        }

        private void StartTypewriter(string full)
        {
            StopTypewriterIfNeeded();
            if (contentText == null) return;

            contentText.text = "";

            // ✅ 开始说话
            OnTypingStart?.Invoke();

            if (charsPerSecond <= 0f)
            {
                contentText.text = full;
                // ✅ 立刻结束说话
                OnTypingEnd?.Invoke();
                return;
            }

            typingCo = StartCoroutine(TypeLine(full));
        }

        private IEnumerator TypeLine(string text)
        {
            float secPerChar = 1f / Mathf.Max(1f, charsPerSecond);
            float voiceTimer = 0f; // 音频播放冷却计时器

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                contentText.text += c;

                // ✅ 发声逻辑：非空格 + 冷却完毕 + 资源存在
                if (!char.IsWhiteSpace(c) && voiceTimer <= 0f && voiceAudioSource != null && voiceClip != null)
                {
                    voiceAudioSource.pitch = voicePitch;
                    voiceAudioSource.PlayOneShot(voiceClip);
                    voiceTimer = voiceInterval; // 重置冷却时间（通常设为 0.06s 左右）
                }

                float extra = 0f;
                if (punctuationPause > 0f && IsPunctuation(c))
                    extra = punctuationPause;

                float wait = secPerChar + extra;
                float t = 0f;
                while (t < wait)
                {
                    float dt = Time.unscaledDeltaTime;
                    t += dt;
                    voiceTimer -= dt; // 在等待字符显示的每一帧里减少冷却时间
                    yield return null;
                }
            }

            typingCo = null;

            // ✅ 结束说话
            OnTypingEnd?.Invoke();
        }

        private void StopTypewriterIfNeeded()
        {
            if (typingCo != null)
            {
                StopCoroutine(typingCo);
                typingCo = null;

                // 被打断也算结束（避免表情卡在张嘴）
                OnTypingEnd?.Invoke();
            }
        }

        private bool IsPunctuation(char c)
        {
            return c == '。' || c == '！' || c == '？' || c == '，' ||
                   c == '、' || c == '：' || c == ';' || c == '；' ||
                   c == '.' || c == '!' || c == '?' || c == ',' || c == ':';
        }
    }
}
