using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.UI.Shop
{
    public class ShopTalkDialogueUI : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text nameText;
        public TMP_Text contentText;

        [Header("Typewriter")]
        public float charsPerSecond = 40f;
        public float punctuationPause = 0.03f;

        public bool IsOpen { get; private set; }

        public event Action OnFinished;

        private PlayerInputReader input;

        private DialogueLine[] lines;
        private int index;

        private Coroutine typingCo;
        private bool isTyping;
        private bool skipRequested;
        private int openFrame;

        private void Start()
        {
            input = GameRoot.I != null ? GameRoot.I.playerInput : null;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Time.frameCount == openFrame) return; // 打开当帧保护
            if (input == null) return;

            // Continue：不在打字时推进；打字时不推进（Undertale风格）
            if (input.ConsumeContinueDown())
            {
                input.ConsumeInteractDown(); // 同帧吞掉交互

                if (isTyping) return;
                Next();
                return;
            }

            // Cancel：打字时跳过
            if (input.ConsumeCancelDown())
            {
                input.ConsumeInteractDown();
                if (isTyping) RequestSkip();
                return;
            }
        }

        public void PlayDialogueAsset(DialogueAsset asset, string npcId = "_shop")
        {
            if (asset == null)
            {
                Close();
                return;
            }

            // 复用全局 DialogueState（支持 count/repeat 之类）
            var state = GameRoot.I != null && GameRoot.I.Dialogue != null
                ? GameRoot.I.Dialogue.DialogueState
                : new DialogueState();

            var session = asset.BuildSession(npcId, state);
            if (session == null || session.lines == null || session.lines.Length == 0)
            {
                Close();
                return;
            }

            Open(session.lines);
        }

        public void Open(DialogueLine[] newLines)
        {
            lines = newLines;
            index = 0;

            IsOpen = true;
            openFrame = Time.frameCount;

            Show();
        }

        private void Next()
        {
            index++;
            if (lines == null || index >= lines.Length)
            {
                Close();
                return;
            }
            Show();
        }

        private void Show()
        {
            StopTyping();

            var loc = GameRoot.I != null ? GameRoot.I.Localization : null;

            string speakerKey = lines[index].speakerKey;
            string contentKey = lines[index].textKey;

            string speaker = string.IsNullOrEmpty(speakerKey) ? "" : (loc != null ? loc.Get(speakerKey) : speakerKey);
            string content = string.IsNullOrEmpty(contentKey) ? "" : (loc != null ? loc.Get(contentKey) : contentKey);

            if (nameText != null) nameText.text = speaker;

            if (contentText == null)
                return;

            contentText.text = "";
            isTyping = true;
            skipRequested = false;

            if (charsPerSecond <= 0f)
            {
                contentText.text = content;
                isTyping = false;
                return;
            }

            typingCo = StartCoroutine(TypeLine(content));
        }

        private IEnumerator TypeLine(string text)
        {
            float secPerChar = 1f / Mathf.Max(1f, charsPerSecond);

            for (int i = 0; i < text.Length; i++)
            {
                if (skipRequested) break;

                contentText.text += text[i];

                float extra = 0f;
                if (punctuationPause > 0f && IsPunctuation(text[i]))
                    extra = punctuationPause;

                float wait = secPerChar + extra;
                float t = 0f;
                while (t < wait)
                {
                    if (skipRequested) break;
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            contentText.text = text;
            isTyping = false;
            typingCo = null;
            skipRequested = false;
        }

        private void RequestSkip()
        {
            skipRequested = true;
        }

        private void StopTyping()
        {
            if (typingCo != null)
            {
                StopCoroutine(typingCo);
                typingCo = null;
            }
            isTyping = false;
            skipRequested = false;
        }

        public void Close()
        {
            StopTyping();
            IsOpen = false;
            lines = null;

            OnFinished?.Invoke();
        }

        private bool IsPunctuation(char c)
        {
            return c == '。' || c == '！' || c == '？' || c == '，' ||
                   c == '、' || c == '：' || c == ';' || c == '；' ||
                   c == '.' || c == '!' || c == '?' || c == ',' || c == ':';
        }
    }
}
