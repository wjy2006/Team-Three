using System.Collections;
using UnityEngine;

namespace Game.UI.Shop
{
    public class ShopPortraitController : MonoBehaviour
    {
        public enum Mode
        {
            ReactivePoses,
            LoopTwoBG
        }

        public enum Pose
        {
            Idle,
            Talk,
            Confirm,
            BuySuccess,
            BuyFail
        }

        [Header("Mode")]
        public Mode mode = Mode.ReactivePoses;

        [Header("Portrait GameObjects")]
        public GameObject idleGO;
        public GameObject blinkGO;
        public GameObject talkGO;
        public GameObject confirmGO;
        public GameObject successGO;
        public GameObject failGO;

        [Header("Loop Two BG (Mode = LoopTwoBG)")]
        public GameObject loopBgA;
        public GameObject loopBgB;
        public float loopBgADuration = 0.25f;
        public float loopBgBDuration = 0.25f;

        [Header("Blink Settings")]
        public float blinkMinInterval = 2.5f;
        public float blinkMaxInterval = 5f;
        public float blinkDuration = 0.15f;

        private Pose basePose = Pose.Idle;
        private Coroutine overrideCo;
        private Coroutine blinkCo;
        private Coroutine loopCo;
        private bool isOverriding;

        private void Start()
        {
            ApplyModeState();
        }

        private void OnDisable()
        {
            StopAllPortraitCoroutines();
        }

        private bool IsLoopMode => mode == Mode.LoopTwoBG;

        private void ApplyModeState()
        {
            StopAllPortraitCoroutines();
            DisableAll();

            if (IsLoopMode)
            {
                loopCo = StartCoroutine(LoopTwoBgRoutine());
                return;
            }

            ApplyBasePose();
            TryStartBlink();
        }

        private void StopAllPortraitCoroutines()
        {
            if (overrideCo != null)
            {
                StopCoroutine(overrideCo);
                overrideCo = null;
            }

            if (blinkCo != null)
            {
                StopCoroutine(blinkCo);
                blinkCo = null;
            }

            if (loopCo != null)
            {
                StopCoroutine(loopCo);
                loopCo = null;
            }

            isOverriding = false;
        }

        // =========================
        // Base Pose
        // =========================
        public void SetBasePose(Pose pose)
        {
            if (IsLoopMode) return;

            basePose = pose;

            if (!isOverriding)
                ApplyBasePose();

            TryStartBlink();
        }

        private void ApplyBasePose()
        {
            DisableAll();

            switch (basePose)
            {
                case Pose.Idle:
                    SetActive(idleGO);
                    break;
                case Pose.Talk:
                    SetActive(talkGO);
                    break;
                case Pose.Confirm:
                    SetActive(confirmGO);
                    break;
                case Pose.BuySuccess:
                    SetActive(successGO);
                    break;
                case Pose.BuyFail:
                    SetActive(failGO);
                    break;
            }
        }

        // =========================
        // Override (Success / Fail)
        // =========================
        public void OverridePose(Pose pose, float duration)
        {
            if (IsLoopMode) return;

            if (overrideCo != null)
                StopCoroutine(overrideCo);

            overrideCo = StartCoroutine(OverrideRoutine(pose, duration));
        }

        private IEnumerator OverrideRoutine(Pose pose, float duration)
        {
            isOverriding = true;
            StopBlink();

            DisableAll();

            switch (pose)
            {
                case Pose.BuySuccess: SetActive(successGO); break;
                case Pose.BuyFail: SetActive(failGO); break;
                case Pose.Confirm: SetActive(confirmGO); break;
                case Pose.Talk: SetActive(talkGO); break;
                default: SetActive(idleGO); break;
            }

            yield return new WaitForSecondsRealtime(duration);

            isOverriding = false;
            ApplyBasePose();
            TryStartBlink();
        }

        // =========================
        // Typing Hooks
        // =========================
        public void OnTypingStart()
        {
            if (IsLoopMode) return;
            if (isOverriding) return;
            StopBlink();
            SetBasePose(Pose.Talk);
        }

        public void OnTypingEnd()
        {
            if (IsLoopMode) return;
            if (isOverriding) return;
            SetBasePose(Pose.Idle);
        }

        // =========================
        // Blinking (Idle only)
        // =========================
        private void TryStartBlink()
        {
            if (IsLoopMode)
            {
                StopBlink();
                return;
            }

            if (basePose != Pose.Idle || isOverriding)
            {
                StopBlink();
                return;
            }

            if (blinkCo == null)
                blinkCo = StartCoroutine(BlinkRoutine());
        }

        private void StopBlink()
        {
            if (blinkCo != null)
            {
                StopCoroutine(blinkCo);
                blinkCo = null;
            }
        }

        private IEnumerator BlinkRoutine()
        {
            while (basePose == Pose.Idle && !isOverriding)
            {
                float wait = Random.Range(blinkMinInterval, blinkMaxInterval);
                yield return new WaitForSecondsRealtime(wait);

                if (basePose != Pose.Idle || isOverriding)
                    break;

                // 切换到 Blink
                DisableAll();
                SetActive(blinkGO);

                yield return new WaitForSecondsRealtime(blinkDuration);

                // 回到 Idle
                DisableAll();
                SetActive(idleGO);
            }

            blinkCo = null;
        }

        private IEnumerator LoopTwoBgRoutine()
        {
            GameObject a = loopBgA != null ? loopBgA : idleGO;
            GameObject b = loopBgB != null ? loopBgB : blinkGO;
            float aDuration = Mathf.Max(0.01f, loopBgADuration);
            float bDuration = Mathf.Max(0.01f, loopBgBDuration);

            if (a == null && b == null)
            {
                yield break;
            }

            if (a == null) a = b;
            if (b == null) b = a;

            if (a == b)
            {
                DisableAll();
                SetActive(a);
                yield break;
            }

            while (true)
            {
                DisableAll();
                SetActive(a);
                yield return new WaitForSecondsRealtime(aDuration);

                DisableAll();
                SetActive(b);
                yield return new WaitForSecondsRealtime(bDuration);
            }
        }

        // =========================
        // Helpers
        // =========================
        private void DisableAll()
        {
            if (idleGO) idleGO.SetActive(false);
            if (blinkGO) blinkGO.SetActive(false);
            if (talkGO) talkGO.SetActive(false);
            if (confirmGO) confirmGO.SetActive(false);
            if (successGO) successGO.SetActive(false);
            if (failGO) failGO.SetActive(false);
            if (loopBgA) loopBgA.SetActive(false);
            if (loopBgB) loopBgB.SetActive(false);
        }

        private void SetActive(GameObject go)
        {
            if (go != null)
                go.SetActive(true);
        }
    }
}
