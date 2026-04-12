using UnityEngine;

namespace Game.Systems.Items
{
    [CreateAssetMenu(menuName = "Game/Items/Item Definition", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayNameKey;

        [Header("Dialogue")]
        public DialogueAsset infoDialogue;
        public DialogueAsset dropDialogue;

        [Header("Presentation")]
        [SerializeField] private ItemVisualConfig visual;

        [Header("Audio")]
        [SerializeField] private AudioClip useSfx;

        [Header("Category")]
        [SerializeField] private ItemType type;

        [Header("Economy")]
        [Min(0)][SerializeField] private int buyPrice;
        [Min(0)][SerializeField] private int sellPrice;
        [SerializeField] private ItemEffect effect;

        public string ItemId => itemId;
        public string DisplayNameKey => displayNameKey;

        // Forced localization by key. No fallback to legacy displayName.
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(displayNameKey))
                    return "[item.name.missing_key]";

                var loc = GameRoot.I != null ? GameRoot.I.Localization : null;
                if (loc == null)
                    return $"[{displayNameKey}]";

                return loc.Get(displayNameKey);
            }
        }

        public ItemType Type => type;
        public int BuyPrice => buyPrice;
        public int SellPrice => sellPrice;
        public ItemEffect Effect => effect;
        public ItemVisualConfig Visual => visual;
        public AudioClip UseSfx => useSfx;
    }
}
