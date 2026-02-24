using UnityEngine;

namespace Game.Systems.Items
{
    [CreateAssetMenu(menuName = "Game/Items/Item Definition", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;

        [Header("Dialogue")]
        public DialogueAsset infoDialogue;
        public DialogueAsset dropDialogue;

        [Header("Presentation")]
        [SerializeField] private ItemVisualConfig visual;

        [Header("Audio")]
        [Tooltip("使用该物品时播放的音效")]
        [SerializeField] private AudioClip useSfx; 

        [Header("Category")]
        [SerializeField] private ItemType type;

        [Header("Economy")]
        [Min(0)][SerializeField] private int buyPrice;
        [Min(0)][SerializeField] private int sellPrice;
        [SerializeField] private ItemEffect effect;

        // ======= 对外只读访问 =======
        public string ItemId => itemId;
        public string DisplayName => displayName;
        public ItemType Type => type;
        public int BuyPrice => buyPrice;
        public int SellPrice => sellPrice;
        public ItemEffect Effect => effect;
        public ItemVisualConfig Visual => visual;
        
        // ✅ 新增音效访问器
        public AudioClip UseSfx => useSfx;
    }
}