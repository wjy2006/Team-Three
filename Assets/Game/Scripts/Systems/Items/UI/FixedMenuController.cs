using TMPro;
using UnityEngine;
using Game.Gameplay.Player;
using Assets.Game.Scripts.Systems.Items;

namespace Game.UI.Menu
{
    public class FixedMenuController : MonoBehaviour
    {
        private const int SLOT_COUNT = 7;

        [Header("Root")]
        public GameObject menuPanel;

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
        [Header("Empty Slot Dialogues (optional)")]


        private PlayerInputReader input;

        private bool isOpen;
        private int selectedIndex;


        // 子菜单：Info / Hold / Drop
        private enum MenuState { Inventory, ItemAction }
        private MenuState state = MenuState.Inventory;

        // 0 Info, 1 Hold, 2 Drop
        private int actionIndex = 0;

        void Awake()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
        }

        void Start()
        {
            if (GameRoot.I != null)
                input = GameRoot.I.playerInput;
        }

        void Update()
        {
            if (input == null) return;
            if (GameRoot.I != null && GameRoot.I.Dialogue != null && GameRoot.I.Dialogue.IsOpen)
            {
                // 把 Menu 输入吞掉，避免对话结束后一松手立刻开菜单
                input.ConsumeMenuDown();
                return;
            }

            // C 打开/关闭菜单
            if (input.ConsumeMenuDown())
            {
                if (isOpen) Close();
                else Open();
                return;
            }

            if (!isOpen) return;

            // Cancel：子菜单返回；否则关闭菜单
            if (input.ConsumeCancelDown())
            {
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
                if (input.ConsumeUpDown())
                {
                    selectedIndex = Mathf.Clamp(selectedIndex - 1, 0, SLOT_COUNT - 1);
                    RefreshAll();
                }

                if (input.ConsumeDownDown())
                {
                    selectedIndex = Mathf.Clamp(selectedIndex + 1, 0, SLOT_COUNT - 1);
                    RefreshAll();
                }

                // ✅ Interact：无论空不空，都进入三选一（你要“空也能选”）
                if (input.ConsumeInteractDown())
                {
                    state = MenuState.ItemAction;
                    actionIndex = 0; // 默认 Info
                    RefreshAll();
                }
            }
            else // ItemAction
            {
                if (input.ConsumeLeftDown())
                {
                    actionIndex = Mathf.Max(actionIndex - 1, 0);
                    RefreshAll();
                }

                if (input.ConsumeRightDown())
                {
                    actionIndex = Mathf.Min(actionIndex + 1, 2);
                    RefreshAll();
                }

                // ✅ Interact：确认执行
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

            // 菜单锁移动
            if (input != null) input.SetMoveEnabled(false);

            RefreshAll();
        }

        public void Close()
        {
            isOpen = false;
            state = MenuState.Inventory;

            if (menuPanel != null) menuPanel.SetActive(false);

            // 恢复移动
            if (input != null) input.SetMoveEnabled(true);
        }

        void ExecuteAction()
        {
            var item = inventory != null ? inventory.GetAt(selectedIndex) : null;

            switch (actionIndex)
            {
                case 0: // Info
                    {
                        Close();

                        if (item != null)
                        {
                            if (item.infoDialogue != null)
                                OpenDialogueAsset(item.infoDialogue);
                            else
                                OpenOneLine("npc.all.unknown.name","dlg.all.default_checked");
                        }
                        else
                            OpenOneLine("npc.all.ancients.name","dlg.all.empty_checked");

                        return;
                    }

                case 1: // Hold
                    {
                        HoldOrSwapSelected();
                        state = MenuState.Inventory;
                        Close();
                        return;
                    }

                case 2: // Drop
                    {
                        Close();

                        if (item != null)
                        {
                            // ✅ 先从背包移除
                            inventory.RemoveAt(selectedIndex);

                            // ✅ 丢弃提示：优先 dropDialogue，否则默认句
                            if (item.dropDialogue != null)
                                OpenDialogueAsset(item.dropDialogue);
                            else
                                OpenOneLine("npc.all.unknown.name","dlg.all.default_dropped");
                        }
                        else
                            OpenOneLine("npc.all.ancients.name","dlg.all.empty_dropped");

                        return;
                    }
            }
        }


        void HoldOrSwapSelected()
        {
            if (heldItem == null || inventory == null) return;

            var slotItem = inventory.GetAt(selectedIndex); // 可能为 null
            var handItem = heldItem.held;                  // 可能为 null

            // 槽空 & 手空：不做事
            if (slotItem == null && handItem == null) return;

            // 槽有 & 手空：拿起
            if (slotItem != null && handItem == null)
            {
                heldItem.held = slotItem;
                inventory.RemoveAt(selectedIndex);
                return;
            }

            // 槽空 & 手有：放下
            if (slotItem == null && handItem != null)
            {
                inventory.SetAt(selectedIndex, handItem);
                heldItem.held = null;
                return;
            }

            // 槽有 & 手有：互换
            if (slotItem != null && handItem != null)
            {
                heldItem.held = slotItem;
                inventory.SetAt(selectedIndex, handItem);
                return;
            }
        }




        void RefreshAll()
        {
            if (stats != null)
            {
                if (hpText != null) hpText.text = $"HP {stats.hp}/{stats.maxHp}";
                if (moneyText != null) moneyText.text = $"G  {stats.money,6}";
            }

            // heldText 只显示手持物
            if (heldText != null)
            {
                var held = heldItem != null ? heldItem.held : null;
                heldText.text = held != null ? held.DisplayName : "  ——";
            }

            // slots
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                if (slotTexts == null || i >= slotTexts.Length || slotTexts[i] == null) continue;

                var item = inventory != null ? inventory.GetAt(i) : null;
                slotTexts[i].text = item != null ? item.DisplayName : "  ——";

                slotTexts[i].color =
                    (i == selectedIndex) ? Color.yellow : Color.white;
            }

            // action texts：只有 ItemAction 状态下才高亮选项（否则全部白色）
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

            // npcId 随便给个系统用的
            GameRoot.I.Dialogue.Open("_menu", asset);
        }

        void OpenOneLine(string name,string content)
        {
            if (GameRoot.I == null || GameRoot.I.Dialogue == null || GameRoot.I.Dialogue.ui == null) return;

            GameRoot.I.Dialogue.ui.Open(new[]
            {
                new DialogueLine { speakerKey = name, textKey = content }
            });
        }

    }
}
