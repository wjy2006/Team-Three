using UnityEngine;

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

        public override bool Apply(ItemUseContext ctx)
        {
            if (GameRoot.I == null || GameRoot.I.Global == null) return false;

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
                if (delta > 0) ShowDialogue(alreadyMaxDialogue);
                else if (delta < 0) ShowDialogue(alreadyMinDialogue);
                return false;
            }

            if (next > current) ShowDialogue(levelUpDialogue);
            else if (next < current) ShowDialogue(levelDownDialogue);

            // Non-consumable design: keep held item after use.
            return false;
        }

        private void ShowDialogue(DialogueAsset asset)
        {
            if (asset == null) return;
            if (GameRoot.I == null || GameRoot.I.Dialogue == null) return;
            GameRoot.I.Dialogue.Open("_chaos", asset);
        }
    }
}
