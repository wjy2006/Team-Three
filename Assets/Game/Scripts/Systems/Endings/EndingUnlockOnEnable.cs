using UnityEngine;
using TMPro;

namespace Game.Systems.Endings
{
    public class EndingUnlockOnEnable : MonoBehaviour
    {
        [SerializeField] private string endingId = "ending.tbc";
        [SerializeField] private string endingTitle = "To Be Continued";
        [SerializeField] private bool unlockOnEnable = true;
        [SerializeField] private bool logWhenUnlocked;

        [Header("End Scene Text")]
        [SerializeField] private TMP_Text endingMessageText;
        [SerializeField] private string endingMessageTextKey = "ui.end.scene.unlocked";
        [SerializeField] private string endingMessageFallback = "你达成了一个结局";

        private void OnEnable()
        {
            ApplyEndingMessageLocalization();
            if (unlockOnEnable) UnlockNow();
        }

        [ContextMenu("Unlock Ending Now")]
        public void UnlockNow()
        {
            if (string.IsNullOrWhiteSpace(endingId)) return;

            bool firstUnlock = EndingCollectionService.Unlock(endingId, endingTitle);
            if (logWhenUnlocked)
            {
                Debug.Log($"[EndingUnlockOnEnable] ending='{endingId}', firstUnlock={firstUnlock}", this);
            }
        }

        private void ApplyEndingMessageLocalization()
        {
            TMP_Text target = ResolveEndingMessageText();
            if (target == null || string.IsNullOrWhiteSpace(endingMessageTextKey))
                return;

            string localized = null;
            var root = global::GameRoot.I;
            if (root != null && root.Localization != null)
            {
                localized = root.Localization.Get(endingMessageTextKey);
            }
            else
            {
                var loc = FindFirstObjectByType<LocalizationService>();
                if (loc != null)
                    localized = loc.Get(endingMessageTextKey);
            }

            if (string.IsNullOrWhiteSpace(localized) || localized == $"[{endingMessageTextKey}]")
                localized = endingMessageFallback;

            if (!string.IsNullOrWhiteSpace(localized))
                target.text = localized;
        }

        private TMP_Text ResolveEndingMessageText()
        {
            if (endingMessageText != null) return endingMessageText;

            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "TOBECONTINUED")
                {
                    endingMessageText = texts[i];
                    return endingMessageText;
                }
            }

            if (texts.Length > 0)
                endingMessageText = texts[0];

            return endingMessageText;
        }
    }
}
