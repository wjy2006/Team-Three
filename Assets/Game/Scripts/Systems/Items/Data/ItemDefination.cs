using UnityEngine;

namespace Game.Systems.Items
{
    [CreateAssetMenu(menuName = "Game/Items/Item Definition", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId;      // 唯一ID（建议用手填：potion_small）
        [SerializeField] private string displayName; // 显示名（中文名）
        [Header("Dialogue")]
        [Tooltip("菜单里选择【信息】时播放的对话（可空）")]
        public DialogueAsset infoDialogue;

        [Tooltip("菜单里选择【丢弃】时播放的对话（可空）。为空则用默认：你丢弃了xxx。")]
        public DialogueAsset dropDialogue;

        [Header("Presentation")]
        [Header("World Presentation")]
        [SerializeField] private Sprite worldSprite;

        [SerializeField] private ItemVisualRotationMode rotationMode = ItemVisualRotationMode.RotateWithAim;

        [SerializeField] private float defaultAngleOffset = 0f; // 角度修正（单位：度）

        [SerializeField] private float holdDistance = 1f; // 距离玩家多远


        [Header("Category")]
        [SerializeField] private ItemType type;

        [Header("Economy")]
        [Min(0)][SerializeField] private int buyPrice;
        [Min(0)][SerializeField] private int sellPrice;
        [SerializeField] private ItemEffect effect;


        // ======= 对外只读访问 =======
        public string ItemId => itemId;
        public string DisplayName => displayName;
        public Sprite WorldSprite => worldSprite;
        public ItemType Type => type;
        public int BuyPrice => buyPrice;
        public int SellPrice => sellPrice;
        public ItemEffect Effect => effect;
        public ItemVisualRotationMode RotationMode => rotationMode;
        public float DefaultAngleOffset => defaultAngleOffset;
        public float HoldDistance => holdDistance;

    }
}
