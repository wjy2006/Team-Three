using System.Collections;
using UnityEngine;

namespace Game.UI.Shop
{
    public class ShopPortraitController : MonoBehaviour
    {
        public enum Pose
        {
            Idle,
            Talk,
            Confirm,
            BuySuccess,
            BuyFail
        }

        [Header("Portrait GameObjects")]
        public GameObject idleGO;
        public GameObject blinkGO;
        public GameObject talkGO;
        public GameObject confirmGO;
        public GameObject successGO;
        public GameObject failGO;

        [Header("Blink Settings")]
        public float blinkMinInterval = 2.5f;
        public float blinkMaxInterval = 5f;
        public float blinkDuration = 0.15f;

        private Pose basePose = Pose.Idle;
        private Coroutine overrideCo;
        private Coroutine blinkCo;
        private bool isOverriding;

        private void Start()
        {
            ApplyBasePose();
            TryStartBlink();
        }

        // =========================
        // Base Pose
        // =========================
        public void SetBasePose(Pose pose)
        {
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
            if (isOverriding) return;
            StopBlink();
            SetBasePose(Pose.Talk);
        }

        public void OnTypingEnd()
        {
            if (isOverriding) return;
            SetBasePose(Pose.Idle);
        }

        // =========================
        // Blinking (Idle only)
        // =========================
        private void TryStartBlink()
        {
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
        }

        private void SetActive(GameObject go)
        {
            if (go != null)
                go.SetActive(true);
        }
    }
}
