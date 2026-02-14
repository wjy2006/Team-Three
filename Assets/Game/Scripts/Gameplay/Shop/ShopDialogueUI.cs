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

        private Coroutine typingCo;

        /// <summary>
        /// 用 key 显示（走 Localization），逐字输出 content。
        /// </summary>
        public void ShowKeys(string speakerKey, string contentKey)
        {
            var loc = GameRoot.I != null ? GameRoot.I.Localization : null;

            string speaker = string.IsNullOrEmpty(speakerKey) ? "" : (loc != null ? loc.Get(speakerKey) : speakerKey);
            string content = string.IsNullOrEmpty(contentKey) ? "" : (loc != null ? loc.Get(contentKey) : contentKey);

            ShowRaw(speaker, content);
        }

        /// <summary>
        /// 直接显示文本（不走 localization），逐字输出 content。
        /// </summary>
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

            if (charsPerSecond <= 0f)
            {
                contentText.text = full;
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
        }

        private void StopTypewriterIfNeeded()
        {
            if (typingCo != null)
            {
                StopCoroutine(typingCo);
                typingCo = null;
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
