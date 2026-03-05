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
        public AudioSource voiceAudioSource;
        public AudioClip voiceClip;
        public float voiceInterval = 0.06f;
        public float voicePitch = 1.0f;

        // ✅ 表情系统用
        public event Action OnTypingStart;
        public event Action OnTypingEnd;

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
            // 关键：对话可能发生在暂停期间，不能让声音挂掉
            voiceAudioSource.ignoreListenerPause = true;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Time.frameCount == openFrame) return;
            if (input == null) return;

            if (input.ConsumeContinueDown())
            {
                input.ConsumeInteractDown();
                if (isTyping) return;
                Next();
                return;
            }

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
            if (contentText == null) return;

            contentText.text = "";
            isTyping = true;
            skipRequested = false;

            // ✅ 开始说话
            OnTypingStart?.Invoke();

            if (charsPerSecond <= 0f)
            {
                contentText.text = content;
                isTyping = false;
                // ✅ 立刻结束说话
                OnTypingEnd?.Invoke();
                return;
            }

            typingCo = StartCoroutine(TypeLine(content));
        }

        private IEnumerator TypeLine(string text)
        {
            float secPerChar = 1f / Mathf.Max(1f, charsPerSecond);
            float lastVoiceTime = -voiceInterval; // 确保第一个字能响

            // ✅ 必须关闭 loop，我们要手动控制播放频率
            if (voiceAudioSource != null)
            {
                voiceAudioSource.loop = false;
                voiceAudioSource.pitch = voicePitch;
                voiceAudioSource.clip = voiceClip;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (skipRequested) break;

                contentText.text += text[i];

                // ✅ 核心改动：基于时间的频率控制
                // 只有当时间到了，且当前字符不是空格时，才播放音效
                if (voiceAudioSource != null && voiceClip != null)
                {
                    if (Time.unscaledTime - lastVoiceTime >= voiceInterval && !char.IsWhiteSpace(text[i]))
                    {
                        // 使用 PlayOneShot 防止声音被下一帧强行切断，听起来更完整
                        voiceAudioSource.PlayOneShot(voiceClip);
                        lastVoiceTime = Time.unscaledTime;
                    }
                }

                // 计算等待时间
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

            // ✅ 打完收工
            isTyping = false;
            typingCo = null;
            skipRequested = false;

            OnTypingEnd?.Invoke();
        }

        private void RequestSkip()
        {
            skipRequested = true;
        }

        private void StopTyping()
        {
            if (typingCo != null)
            {
                //StopCoroutine(typingCo);
                typingCo = null;
            }

            // 如果正在打字被打断，也算结束说话
            if (isTyping) OnTypingEnd?.Invoke();

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
