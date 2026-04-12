using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Systems.Items.Effects
{
    [CreateAssetMenu(menuName = "Game/Items/Effects/Chaos Shift", fileName = "ChaosShiftEffect")]
    public class ChaosShiftEffect : ItemEffect
    {
        [SerializeField] private int delta = 1;
        [SerializeField] private int minLevel = 0;
        [SerializeField] private int maxLevel = 2;
        [SerializeField] private int defaultRoomLevel = 0;

        [Header("Dialogue")]
        [SerializeField] private DialogueAsset levelUpDialogue;
        [SerializeField] private DialogueAsset levelDownDialogue;
        [SerializeField] private DialogueAsset alreadyMaxDialogue;
        [SerializeField] private DialogueAsset alreadyMinDialogue;
        [SerializeField] private DialogueAsset increaseBlockedDialogue;

        [Header("Lab Chaos Gate")]
        [SerializeField] private bool gateIncreaseByScenePrefix = true;
        [SerializeField] private string gatedScenePrefix = "Room_Lab";
        [SerializeField] private string increaseUnlockBoolKey = GameRoot.STATE_ADMIN_DISABLED;

        public override bool Apply(ItemUseContext ctx)
        {
            if (GameRoot.I == null || GameRoot.I.Global == null) return false;

            var roomContext = ResolveCurrentRoomContext();
            string activeSceneName = SceneManager.GetActiveScene().name;

            if (ShouldBlockIncrease(roomContext, activeSceneName))
            {
                ShowDialogue(ResolveDialogue(roomContext, DialogueKind.IncreaseBlocked));
                RaiseHeldItemUsedEvent(ctx);
                return false;
            }

            int current;
            int next;
            bool changed = RoomChaosService.TryShiftCurrentRoomLevel(
                GameRoot.I.Global,
                delta,
                minLevel,
                maxLevel,
                defaultRoomLevel,
                out current,
                out next);

            if (!changed)
            {
                if (delta > 0) ShowDialogue(ResolveDialogue(roomContext, DialogueKind.AlreadyMax));
                else if (delta < 0) ShowDialogue(ResolveDialogue(roomContext, DialogueKind.AlreadyMin));
                RaiseHeldItemUsedEvent(ctx);
                return false;
            }

            if (next > current) ShowDialogue(ResolveDialogue(roomContext, DialogueKind.LevelUp));
            else if (next < current) ShowDialogue(ResolveDialogue(roomContext, DialogueKind.LevelDown));

            RaiseHeldItemUsedEvent(ctx);
            // Non-consumable design: keep held item after use.
            return false;
        }

        private bool ShouldBlockIncrease(RoomChaosContext roomContext, string sceneName)
        {
            if (delta <= 0) return false;

            bool gateEnabled = gateIncreaseByScenePrefix;
            string scenePrefix = gatedScenePrefix;
            string unlockKey = increaseUnlockBoolKey;

            if (roomContext != null)
            {
                gateEnabled = roomContext.gateIncreaseByScenePrefix;
                scenePrefix = roomContext.gatedScenePrefix;
                unlockKey = roomContext.chaosIncreaseUnlockBoolKey;
            }

            if (!gateEnabled) return false;
            if (string.IsNullOrWhiteSpace(scenePrefix)) return false;
            if (!sceneName.StartsWith(scenePrefix.Trim(), StringComparison.Ordinal)) return false;

            if (string.IsNullOrWhiteSpace(unlockKey))
                return true;

            return !GameRoot.I.Global.GetBool(unlockKey.Trim());
        }

        private DialogueAsset ResolveDialogue(RoomChaosContext roomContext, DialogueKind kind)
        {
            if (roomContext == null)
                return GetDefaultDialogue(kind);

            DialogueAsset overrideAsset = kind switch
            {
                DialogueKind.LevelUp => roomContext.chaosLevelUpDialogueOverride,
                DialogueKind.LevelDown => roomContext.chaosLevelDownDialogueOverride,
                DialogueKind.AlreadyMax => roomContext.chaosAlreadyMaxDialogueOverride,
                DialogueKind.AlreadyMin => roomContext.chaosAlreadyMinDialogueOverride,
                DialogueKind.IncreaseBlocked => roomContext.chaosIncreaseBlockedDialogueOverride,
                _ => null
            };

            return overrideAsset != null ? overrideAsset : GetDefaultDialogue(kind);
        }

        private DialogueAsset GetDefaultDialogue(DialogueKind kind)
        {
            return kind switch
            {
                DialogueKind.LevelUp => levelUpDialogue,
                DialogueKind.LevelDown => levelDownDialogue,
                DialogueKind.AlreadyMax => alreadyMaxDialogue,
                DialogueKind.AlreadyMin => alreadyMinDialogue,
                DialogueKind.IncreaseBlocked => increaseBlockedDialogue,
                _ => null
            };
        }

        private static RoomChaosContext ResolveCurrentRoomContext()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            var contexts = UnityEngine.Object.FindObjectsByType<RoomChaosContext>(FindObjectsSortMode.None);
            for (int i = 0; i < contexts.Length; i++)
            {
                var context = contexts[i];
                if (context != null && context.gameObject.scene == activeScene)
                    return context;
            }
            return null;
        }

        private void ShowDialogue(DialogueAsset asset)
        {
            if (asset == null) return;
            if (GameRoot.I == null || GameRoot.I.Dialogue == null) return;
            GameRoot.I.Dialogue.Open("_chaos", asset);
        }

        private static void RaiseHeldItemUsedEvent(ItemUseContext ctx)
        {
            if (ctx.item == null) return;
            if (GameRoot.I == null) return;
            GameRoot.I.Triggers?.Raise(new HeldItemUsedEvent(ctx.item));
        }

        private enum DialogueKind
        {
            LevelUp,
            LevelDown,
            AlreadyMax,
            AlreadyMin,
            IncreaseBlocked
        }
    }
}
