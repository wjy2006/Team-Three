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

        // ✅ 表情系统用：开始说话/结束说话
        public event Action OnTypingStart;
        public event Action OnTypingEnd;

        private Coroutine typingCo;

        public bool IsTyping => typingCo != null;

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

            for (int i = 0; i < text.Length; i++)
            {
                contentText.text += text[i];

                float extra = 0f;
                if (punctuationPause > 0f && IsPunctuation(text[i]))
                    extra = punctuationPause;

                float wait = secPerChar + extra;
                float t = 0f;
                while (t < wait)
                {
                    t += Time.unscaledDeltaTime;
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
