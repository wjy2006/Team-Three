using TMPro;
using UnityEngine;
using Game.Gameplay.Player;
using Game.Systems.Items;
using Game.Systems.Items.Runtime;

namespace Game.UI.Menu
{
    public class FixedMenuController : MonoBehaviour
    {
        private const int SLOT_COUNT = 7;

        [Header("Root")]
        public GameObject menuPanel;

        [Header("Audio SFX")]
        [Tooltip("负责播放菜单音效的组件")]
        public AudioSource uiAudioSource;
        [Tooltip("游标移动音效")]
        public AudioClip moveSfx;
        [Tooltip("确定/进入子菜单音效")]
        public AudioClip confirmSfx;
        [Tooltip("返回/关闭菜单音效")]
        public AudioClip cancelSfx;
        [Tooltip("执行动作（如拿起、放下）的音效")]
        public AudioClip executeSfx;

        [Header("Top Stats")]
        public TMP_Text hpText;
        public TMP_Text moneyText;

        [Header("Held (only shows held item)")]
        public TMP_Text heldText;

        [Header("Inventory Slots (size = 7)")]
        public TMP_Text[] slotTexts = new TMP_Text[SLOT_COUNT];

        [Header("Action Texts (Info / Hold / Drop)")]
        public TMP_Text infoText;
        public TMP_Text holdActionText;
        public TMP_Text dropText;

        [Header("Refs")]
        public PlayerStats stats;
        public HeldItem heldItem;
        public Inventory inventory;

        [Header("Empty Slot Dialogues")]
        public DialogueAsset EmptyDropped;
        public DialogueAsset EmptyChecked;

        private PlayerInputReader input;
        private bool isOpen;
        private int selectedIndex;

        // 子菜单：Inventory (选格子), ItemAction (选 检查/拿着/丢弃)
        private enum MenuState { Inventory, ItemAction }
        private MenuState state = MenuState.Inventory;

        // 0 Info, 1 Hold, 2 Drop
        private int actionIndex = 1;

        void Awake()
        {
            if (menuPanel != null) menuPanel.SetActive(false);

            // ✅ 自动初始化 AudioSource 属性，确保在 TimeScale = 0 时能响
            if (uiAudioSource != null)
            {
                uiAudioSource.spatialBlend = 0f; // 2D音效
                uiAudioSource.playOnAwake = false;
            }
            uiAudioSource.ignoreListenerPause = true;
        }

        void Start()
        {
            if (GameRoot.I != null)
                input = GameRoot.I.playerInput;
        }

        // ✅ 音效播放辅助方法
        private void PlaySFX(AudioClip clip)
        {
            if (uiAudioSource != null && clip != null)
            {
                uiAudioSource.PlayOneShot(clip);
            }
        }

        void Update()
        {
            if (input == null) return;

            // 对话框开启时，禁用菜单输入
            if (GameRoot.I != null && GameRoot.I.Dialogue != null && GameRoot.I.Dialogue.IsOpen)
            {
                input.ConsumeMenuDown();
                return;
            }

            // ✅ C 键：打开/关闭菜单
            if (input.ConsumeMenuDown())
            {
                if (isOpen)
                {
                    PlaySFX(cancelSfx);
                    Close();
                }
                else
                {
                    PlaySFX(confirmSfx);
                    Open();
                }
                return;
            }

            if (!isOpen) return;

            // ✅ Cancel 键：子菜单返回或关闭菜单
            if (input.ConsumeCancelDown())
            {
                PlaySFX(cancelSfx);
                if (state == MenuState.ItemAction)
                {
                    state = MenuState.Inventory;
                    RefreshAll();
                }
                else
                {
                    Close();
                }
                return;
            }

            if (state == MenuState.Inventory)
            {
                // ✅ 上下选择物品
                if (input.ConsumeUpDown())
                {
                    int prev = selectedIndex;
                    selectedIndex = Mathf.Clamp(selectedIndex - 1, 0, SLOT_COUNT - 1);
                    if (prev != selectedIndex) PlaySFX(moveSfx);
                    RefreshAll();
                }

                if (input.ConsumeDownDown())
                {
                    int prev = selectedIndex;
                    selectedIndex = Mathf.Clamp(selectedIndex + 1, 0, SLOT_COUNT - 1);
                    if (prev != selectedIndex) PlaySFX(moveSfx);
                    RefreshAll();
                }

                // ✅ 确定进入三选一子菜单
                if (input.ConsumeInteractDown())
                {
                    PlaySFX(confirmSfx);
                    state = MenuState.ItemAction;
                    actionIndex = 1; // 默认停在 Hold 上
                    RefreshAll();
                }
            }
            else // ItemAction 状态
            {
                // ✅ 左右切换动作选项
                if (input.ConsumeLeftDown())
                {
                    int prev = actionIndex;
                    actionIndex = Mathf.Max(actionIndex - 1, 0);
                    if (prev != actionIndex) PlaySFX(moveSfx);
                    RefreshAll();
                }

                if (input.ConsumeRightDown())
                {
                    int prev = actionIndex;
                    actionIndex = Mathf.Min(actionIndex + 1, 2);
                    if (prev != actionIndex) PlaySFX(moveSfx);
                    RefreshAll();
                }

                // ✅ 确定执行动作
                if (input.ConsumeInteractDown())
                {
                    ExecuteAction();
                }
            }
        }

        public void Open()
        {
            isOpen = true;
            state = MenuState.Inventory;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, SLOT_COUNT - 1);

            if (menuPanel != null) menuPanel.SetActive(true);

            // 暂停世界逻辑
            if (GameRoot.I != null && GameRoot.I.Pause != null)
                GameRoot.I.Pause.PushPause("Menu");

            // 菜单开启时锁住玩家移动
            if (input != null) input.SetMoveEnabled(false);

            RefreshAll();
        }

        public void Close()
        {
            isOpen = false;
            state = MenuState.Inventory;

            if (menuPanel != null) menuPanel.SetActive(false);

            if (GameRoot.I != null && GameRoot.I.Pause != null)
                GameRoot.I.Pause.PopPause("Menu");

            if (input != null) input.SetMoveEnabled(true);
        }

        void ExecuteAction()
        {
            ItemInstance inst = inventory != null ? inventory.GetAt(selectedIndex) : null;
            ItemDefinition item = inst != null ? inst.Definition : null;

            switch (actionIndex)
            {
                case 0: // Info (检查)
                    Close();
                    if (item != null)
                    {
                        if (item.infoDialogue != null) OpenDialogueAsset(item.infoDialogue);
                        else OpenOneLine("npc.all.unknown.name", "dlg.all.default_checked");
                    }
                    else
                    {
                        OpenDialogueAsset(EmptyChecked);
                    }
                    break;

                case 1: // Hold (拿起/放下)
                    HoldOrSwapSelected();
                    state = MenuState.Inventory;
                    Close();
                    break;

                case 2: // Drop (丢弃)
                    PlaySFX(confirmSfx);
                    Close();
                    if (item != null)
                    {
                        if (item.dropDialogue != null) OpenDialogueAsset(item.dropDialogue);
                        else OpenOneLine("npc.all.unknown.name", "dlg.all.default_dropped");

                        if (item.Type == ItemType.Key) return;
                        inventory.RemoveAt(selectedIndex);
                    }
                    else
                    {
                        OpenDialogueAsset(EmptyDropped);
                    }
                    break;
            }
        }

        void HoldOrSwapSelected()
        {
            if (heldItem == null || inventory == null) return;

            var beforeDef = heldItem.held;

            ItemInstance slotInst = inventory.GetAt(selectedIndex);
            ItemInstance handInst = heldItem.heldInstance;

            // ✅ 核心修复：防止 Unity 序列化生成的“空壳”实例绕过 null 检查。必须同时确认 Definition 存在。
            bool slotHasItem = slotInst != null && slotInst.Definition != null;
            bool handHasItem = handInst != null && handInst.Definition != null;

            if (!slotHasItem && !handHasItem) return; // 绝对的空，直接中止

            // 格子有，手空：拿起
            if (slotHasItem && !handHasItem)
            {
                PlaySFX(executeSfx);
                heldItem.SetHeld(slotInst);
                inventory.SetAt(selectedIndex, (ItemInstance)null);
            }
            // 格子空，手有：放回
            else if (!slotHasItem && handHasItem)
            {
                inventory.SetAt(selectedIndex, handInst);
                heldItem.SetHeld(null);
            }
            // 都有：交换
            else if (slotHasItem && handHasItem)
            {
                PlaySFX(executeSfx);
                inventory.SetAt(selectedIndex, handInst);
                heldItem.SetHeld(slotInst);
            }

            if (beforeDef != heldItem.held)
            {
                if (GameRoot.I != null && GameRoot.I.Triggers != null)
                {
                    GameRoot.I.Triggers.RaiseNextFrame(new HeldItemChangedEvent(), this);
                }
            }
        }

        void RefreshAll()
        {
            if (stats != null)
            {
                if (hpText != null) hpText.text = $"HP  {stats.Hp,2}/{stats.MaxHp,2}";
                if (moneyText != null) moneyText.text = $"G  {stats.Money,6}";
            }

            if (heldText != null)
            {
                var heldDef = heldItem != null ? heldItem.held : null;
                heldText.text = heldDef != null ? heldDef.DisplayName : "  ——";
            }

            for (int i = 0; i < SLOT_COUNT; i++)
            {
                if (slotTexts == null || i >= slotTexts.Length || slotTexts[i] == null) continue;

                ItemInstance inst = inventory != null ? inventory.GetAt(i) : null;
                ItemDefinition def = inst != null ? inst.Definition : null;

                slotTexts[i].text = def != null ? def.DisplayName : "  ——";
                slotTexts[i].color = (i == selectedIndex) ? Color.yellow : Color.white;
            }

            if (infoText != null)
                infoText.color = (state == MenuState.ItemAction && actionIndex == 0) ? Color.yellow : Color.white;

            if (holdActionText != null)
                holdActionText.color = (state == MenuState.ItemAction && actionIndex == 1) ? Color.yellow : Color.white;

            if (dropText != null)
                dropText.color = (state == MenuState.ItemAction && actionIndex == 2) ? Color.yellow : Color.white;
        }

        void OpenDialogueAsset(DialogueAsset asset)
        {
            if (asset == null) return;
            if (GameRoot.I == null || GameRoot.I.Dialogue == null) return;
            GameRoot.I.Dialogue.Open("_menu", asset);
        }

        void OpenOneLine(string name, string content)
        {
            if (GameRoot.I == null || GameRoot.I.Dialogue == null || GameRoot.I.Dialogue.ui == null) return;
            GameRoot.I.Dialogue.ui.Open(new[]
            {
                new DialogueLine { speakerKey = name, textKey = content }
            });
        }
    }
}