using TMPro;
using UnityEngine;
using Game.Systems.Items;
using Game.Gameplay.Player;

namespace Game.UI.Shop
{
    public class ShopController : MonoBehaviour
    {
        private enum State
        {
            Root,
            Buy,
            BuyConfirm,   // ✅ 新增：购买确认
            Sell,
            TalkSelect,
            TalkDialogue
        }

        [SerializeField] private State state = State.Root;

        [Header("Leave Target")]
        public string leaveSceneName = "World_Town";
        public string leaveSpawnId = "FromShop";

        // ===== Panels =====
        [Header("Panels")]
        public GameObject leftPanel;     // Root 显示
        public GameObject rootPanel;     // Root / Talk 显示
        public GameObject buyPanel;      // Buy 显示
        public GameObject infoPanel;     // Buy 显示
        public GameObject hintPanel;     // Buy 显示
        public GameObject confirmPanel;  // ✅ BuyConfirm 显示（放在 hintPanel 的位置）
        public GameObject sellPanel;     // Sell 仅显示
        public GameObject talkPanel;     // TalkSelect 显示
        public GameObject dialoguePanel; // TalkDialogue 显示

        // ===== Display-only Dialogue Areas =====
        [Header("Display-only Dialogue Areas")]
        public ShopDialogueUI leftDialogue;  // Welcome / Root提示
        public ShopDialogueUI infoDialogue;  // Buy info
        public ShopDialogueUI hintDialogue;  // Buy hint

        // ===== Confirm UI =====
        [Header("Buy Confirm UI")]
        [Tooltip("确认弹窗的提示文本：例如 “10G买下药水？”")]
        public TMP_Text confirmPromptText;

        [Tooltip("两个选项 TMP：0=确定，1=取消")]
        public TMP_Text[] confirmOptionTexts = new TMP_Text[2];

        [Tooltip("提示文本格式：{0}=价格(含G), {1}=物品名")]
        public string confirmPromptFormat = "{0}买下{1}？"; // 例： "10G买下药水？"

        [Tooltip("选项显示文字（不想硬编码就自己改）")]
        public string confirmYesText = "确定";
        public string confirmNoText = "取消";

        // ===== Talk UI =====
        [Header("Talk UI")]
        public TMP_Text[] talkOptions = new TMP_Text[4];
        public string[] talkOptionTextKeys = new string[4];          // 从loc读标题
        public DialogueAsset[] talkDialogues = new DialogueAsset[4];
        public ShopTalkDialogueUI talkDialogueUI;

        // ===== Keys =====
        [Header("Keys (Inspector)")]
        public string welcomeSpeakerKey;
        public string welcomeContentKey;

        public string hintSpeakerKey;
        public string hintNotEnoughMoneyKey;
        public string hintThanksKey;
        public string hintBagFullKey;
        public string hintNoItemKey;

        [Tooltip("Buy界面：进入Buy时显示一次的提示 key")]
        public string buySelectHintKey;

        // ===== Root UI =====
        [Header("Root UI")]
        public TMP_Text[] rootOptions = new TMP_Text[4];
        public TMP_Text moneyText;   // "123G"
        public TMP_Text slotsText;   // "X/(cap+1)"

        // ===== Buy UI =====
        [Header("Buy UI")]
        public TMP_Text[] buyNameTexts = new TMP_Text[4];
        public TMP_Text[] buyPriceTexts = new TMP_Text[4];
        public ItemDefinition[] buyItems = new ItemDefinition[4];

        [Header("Buy: Per-item Info Keys (4 items)")]
        public string[] itemInfoSpeakerKeys = new string[4];
        public string[] itemInfoContentKeys = new string[4];

        // ===== Sell UI =====
        [Header("Sell UI (8 slots: 0-6 inv, 7 held)")]
        public TMP_Text[] sellNameTexts = new TMP_Text[8];
        public TMP_Text[] sellPriceTexts = new TMP_Text[8];

        // ===== Runtime refs =====
        private PlayerInputReader input;
        private PlayerStats stats;
        private Inventory inventory;
        private HeldItem heldItem;

        // ===== Selections =====
        private int rootIndex = 0;
        private int buyIndex = 0;
        private int sellIndex = 0;
        private int talkIndex = 0;

        // ===== Confirm runtime =====
        private int confirmIndex = 0;              // 0=确定，1=取消
        private ItemDefinition pendingBuyItem;     // 这次确认要买的物品
        private int pendingBuyPrice;
        private bool boughtLastFrame = false;


        private void Start()
        {
            input = GameRoot.I != null ? GameRoot.I.playerInput : null;
            inventory = GameRoot.I != null ? GameRoot.I.Inventory : null;

            stats = FindFirstObjectByType<PlayerStats>();
            heldItem = FindFirstObjectByType<HeldItem>();

            if (GameRoot.I != null && GameRoot.I.Pause != null)
                GameRoot.I.Pause.PushPause("Shop");

            if (stats != null) stats.OnStatsChanged += RefreshRootStats;
            if (inventory != null) inventory.OnChanged += RefreshRootStats;

            if (talkDialogueUI != null)
            {
                talkDialogueUI.OnFinished -= OnTalkFinished;
                talkDialogueUI.OnFinished += OnTalkFinished;
            }

            SetState(State.Root);
            ShowLeftWelcome();
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (stats != null) stats.OnStatsChanged -= RefreshRootStats;
            if (inventory != null) inventory.OnChanged -= RefreshRootStats;

            if (talkDialogueUI != null)
                talkDialogueUI.OnFinished -= OnTalkFinished;

            if (GameRoot.I != null && GameRoot.I.Pause != null)
                GameRoot.I.Pause.PopPause("Shop");
        }

        private void Update()
        {
            if (input == null) return;

            // ban 掉 C（Menu键）
            input.ConsumeMenuDown();

            // TalkDialogue 的输入由 talkDialogueUI 自己吃
            if (state != State.TalkDialogue)
            {
                // Cancel：BuyConfirm 优先当“取消购买”；其他非 Root 回 Root
                if (input.ConsumeCancelDown())
                {
                    if (state == State.BuyConfirm)
                    {
                        CloseBuyConfirm(goBackToBuy: true);
                        return;
                    }

                    if (state != State.Root)
                    {
                        SetState(State.Root);
                        ShowLeftWelcome();
                        RefreshAll();
                    }
                    return;
                }
            }

            switch (state)
            {
                case State.Root: UpdateRoot(); break;
                case State.Buy: UpdateBuy(); break;
                case State.BuyConfirm: UpdateBuyConfirm(); break;
                case State.Sell: UpdateSell(); break;
                case State.TalkSelect: UpdateTalkSelect(); break;
                case State.TalkDialogue:
                    break;
            }
        }

        // =========================
        // Root
        // =========================
        private void UpdateRoot()
        {
            if (input.ConsumeUpDown())
            {
                rootIndex = Mathf.Clamp(rootIndex - 1, 0, 3);
                RefreshRootOptions();
            }
            if (input.ConsumeDownDown())
            {
                rootIndex = Mathf.Clamp(rootIndex + 1, 0, 3);
                RefreshRootOptions();
            }

            if (input.ConsumeInteractDown())
            {
                switch (rootIndex)
                {
                    case 0: // Buy
                        buyIndex = 0;
                        SetState(State.Buy);
                        RefreshAll();
                        break;

                    case 1: // Sell
                        sellIndex = 0;
                        SetState(State.Sell);
                        RefreshAll();
                        break;

                    case 2: // Talk
                        talkIndex = 0;
                        SetState(State.TalkSelect);
                        RefreshAll();
                        break;

                    case 3: // Leave
                        LeaveShop();
                        break;
                }
            }
        }

        // =========================
        // Buy
        // =========================
        private void UpdateBuy()
        {
            bool moved = false;

            if (input.ConsumeUpDown())
            {
                buyIndex = Mathf.Clamp(buyIndex - 1, 0, 3);
                moved = true;
            }
            if (input.ConsumeDownDown())
            {
                buyIndex = Mathf.Clamp(buyIndex + 1, 0, 3);
                moved = true;
            }

            if (moved)
            {
                RefreshBuyList();
                RefreshBuyItemInfo();
                // 不再移动就ShowHint
            }

            if (input.ConsumeInteractDown())
            {
                // ✅ 不直接买，进入确认
                TryOpenBuyConfirm(buyIndex);
            }
        }

        private void TryOpenBuyConfirm(int idx)
        {
            if (stats == null || inventory == null)
            {
                ShowHint(hintNoItemKey);
                return;
            }

            var item = GetBuyItem(idx);
            if (item == null)
            {
                ShowHint(hintNoItemKey);
                return;
            }

            int price = item.BuyPrice;

            if (stats.Money < price)
            {
                ShowHint(hintNotEnoughMoneyKey);
                return;
            }

            // 能不能放下：背包有空 或 手持空
            if (!CanPlacePurchasedItem())
            {
                ShowHint(hintBagFullKey);
                return;
            }

            OpenBuyConfirm(item, price);
        }

        private bool CanPlacePurchasedItem()
        {
            bool invHasSpace = (inventory != null && !inventory.IsFull());
            bool heldEmpty = (heldItem != null && heldItem.held == null);
            return invHasSpace || heldEmpty;
        }

        private void OpenBuyConfirm(ItemDefinition item, int price)
        {
            pendingBuyItem = item;
            pendingBuyPrice = price;

            confirmIndex = 0; // 默认“确定”
            SetState(State.BuyConfirm);

            // 组装提示：XXG买下XXXX？
            string priceStr = $"{price}G";
            string itemName = item != null ? item.DisplayName : "";
            if (confirmPromptText != null)
                confirmPromptText.text = string.Format(confirmPromptFormat, priceStr, itemName);

            RefreshConfirmOptions();
        }

        private void UpdateBuyConfirm()
        {
            // 选项：上下/左右都支持（更顺手）
            bool moved = false;

            if (input.ConsumeUpDown() || input.ConsumeLeftDown())
            {
                confirmIndex = 0;
                moved = true;
            }
            if (input.ConsumeDownDown() || input.ConsumeRightDown())
            {
                confirmIndex = 1;
                moved = true;
            }

            if (moved) RefreshConfirmOptions();

            if (input.ConsumeInteractDown())
            {
                if (confirmIndex == 0)
                {
                    // 确定：真正购买
                    ExecutePendingBuy();
                    CloseBuyConfirm(goBackToBuy: true);
                }
                else
                {
                    // 取消：不买
                    CloseBuyConfirm(goBackToBuy: true);
                }
            }
        }

        private void ExecutePendingBuy()
        {
            if (pendingBuyItem == null || stats == null || inventory == null) return;

            // 再做一次保底校验（防止确认期间钱变化/物品变化）
            if (stats.Money < pendingBuyPrice)
            {
                ShowHint(hintNotEnoughMoneyKey);
                return;
            }

            if (!TryPlacePurchasedItem(pendingBuyItem))
            {
                ShowHint(hintBagFullKey);
                return;
            }

            bool spent = stats.TrySpendMoney(pendingBuyPrice);
            if (!spent)
            {
                ShowHint(hintNotEnoughMoneyKey);
                return;
            }
            boughtLastFrame = true;
            ShowHint(hintThanksKey);

            RefreshRootStats();
            RefreshBuyList();

        }

        private void CloseBuyConfirm(bool goBackToBuy)
        {
            pendingBuyItem = null;
            pendingBuyPrice = 0;

            // 关 confirmPanel 的显示由状态驱动
            if (goBackToBuy)
            {
                SetState(State.Buy);
                // 回到 Buy 时不要重复显示 buySelectHintKey（只在进入Buy显示一次）
                RefreshBuyList();
                RefreshBuyItemInfo();
            }
        }

        private bool TryPlacePurchasedItem(ItemDefinition item)
        {
            if (item == null) return false;

            // 1) 先背包
            if (inventory != null && inventory.TryAdd(item))
                return true;

            // 2) 背包满：放手持
            if (heldItem != null && heldItem.held == null)
            {
                heldItem.held = item;
                return true;
            }

            return false;
        }

        private ItemDefinition GetBuyItem(int idx)
        {
            if (buyItems == null || idx < 0 || idx >= buyItems.Length) return null;
            return buyItems[idx];
        }

        // =========================
        // Sell
        // =========================
        private void UpdateSell()
        {
            bool moved = false;

            // 左右：±4（0..3 <-> 4..7）
            if (input.ConsumeRightDown())
            {
                if (sellIndex <= 3) { sellIndex += 4; moved = true; }
            }
            if (input.ConsumeLeftDown())
            {
                if (sellIndex >= 4) { sellIndex -= 4; moved = true; }
            }

            // 上下：只在同一列移动
            if (input.ConsumeUpDown())
            {
                if (sellIndex % 4 != 0) { sellIndex -= 1; moved = true; }
            }
            if (input.ConsumeDownDown())
            {
                if (sellIndex % 4 != 3) { sellIndex += 1; moved = true; }
            }

            if (moved) RefreshSellList();

            if (input.ConsumeInteractDown())
            {
                TrySell(sellIndex);
            }
        }

        private void TrySell(int idx)
        {
            if (stats == null || inventory == null) return;

            ItemDefinition item = GetSellItem(idx);
            if (item == null) return;

            int price = Mathf.Max(0, item.SellPrice);

            bool removed = RemoveSellItem(idx);
            if (!removed) return;

            if (price > 0) stats.AddMoney(price);

            RefreshRootStats();
            RefreshSellList();
        }

        // idx: 0..6=inv；7=held
        private ItemDefinition GetSellItem(int idx)
        {
            if (idx < 0 || idx > 7) return null;
            if (idx <= 6) return inventory != null ? inventory.GetAt(idx) : null;
            return heldItem != null ? heldItem.held : null;
        }

        private bool RemoveSellItem(int idx)
        {
            if (idx < 0 || idx > 7) return false;

            if (idx <= 6)
                return inventory != null && inventory.RemoveAt(idx);

            if (heldItem == null || heldItem.held == null) return false;
            heldItem.held = null;
            return true;
        }

        // =========================
        // Talk
        // =========================
        private void UpdateTalkSelect()
        {
            if (input.ConsumeUpDown())
            {
                talkIndex = Mathf.Clamp(talkIndex - 1, 0, 3);
                RefreshTalkOptions();
            }
            if (input.ConsumeDownDown())
            {
                talkIndex = Mathf.Clamp(talkIndex + 1, 0, 3);
                RefreshTalkOptions();
            }

            if (input.ConsumeInteractDown())
            {
                StartTalkDialogue(talkIndex);
            }
        }

        private void StartTalkDialogue(int idx)
        {
            if (talkDialogueUI == null) return;

            var asset = (talkDialogues != null && idx >= 0 && idx < talkDialogues.Length) ? talkDialogues[idx] : null;
            if (asset == null) return;

            SetState(State.TalkDialogue);
            talkDialogueUI.PlayDialogueAsset(asset, "_shop");
        }

        private void OnTalkFinished()
        {
            if (state != State.TalkDialogue) return;

            SetState(State.TalkSelect);
            RefreshTalkOptions();
        }

        // =========================
        // Panels + Refresh
        // =========================
        private void SetState(State s)
        {
            // 记录是否“第一次进入Buy”（用于buySelectHint只显示一次）
            _ = state != State.Buy && s == State.Buy;

            state = s;
            ApplyPanelsForState();

            // 进入 Buy：显示一次选中提示
            if (state == State.Buy)
            {
                if (boughtLastFrame)
                {
                    // 买完回到Buy时优先显示Thank
                    ShowHint(hintThanksKey);
                    boughtLastFrame = false;
                }
                else if (!string.IsNullOrEmpty(buySelectHintKey))
                {
                    ShowHint(buySelectHintKey);
                }
            }


            // 离开 Buy：清理 buy 专属文本（避免残留）
            if (state != State.Buy && state != State.BuyConfirm)
            {
                if (infoDialogue != null) infoDialogue.Clear();
                if (hintDialogue != null) hintDialogue.Clear();
            }
        }

        private void ApplyPanelsForState()
        {
            // Root: LeftPanel + RootPanel
            // Buy:  BuyPanel + HintPanel + InfoPanel
            // BuyConfirm: BuyPanel + ConfirmPanel + InfoPanel（HintPanel隐藏）
            // Sell: only SellPanel
            // TalkSelect: TalkPanel + RootPanel
            // TalkDialogue: DialoguePanel + RootPanel

            bool isRoot = state == State.Root;
            bool isBuy = state == State.Buy;
            bool isBuyConfirm = state == State.BuyConfirm;
            bool isSell = state == State.Sell;
            bool isTalkSelect = state == State.TalkSelect;
            bool isTalkDialogue = state == State.TalkDialogue;

            if (leftPanel != null) leftPanel.SetActive(isRoot);
            if (rootPanel != null) rootPanel.SetActive(isRoot || isTalkSelect || isTalkDialogue);

            if (buyPanel != null) buyPanel.SetActive(isBuy || isBuyConfirm);
            if (infoPanel != null) infoPanel.SetActive(isBuy || isBuyConfirm);

            // ✅ Buy: hintPanel；BuyConfirm: confirmPanel
            if (hintPanel != null) hintPanel.SetActive(isBuy);
            if (confirmPanel != null) confirmPanel.SetActive(isBuyConfirm);

            if (sellPanel != null) sellPanel.SetActive(isSell);

            if (talkPanel != null) talkPanel.SetActive(isTalkSelect);
            if (dialoguePanel != null) dialoguePanel.SetActive(isTalkDialogue);

            // Sell：只显示 sellPanel
            if (isSell)
            {
                if (leftPanel != null) leftPanel.SetActive(false);
                if (rootPanel != null) rootPanel.SetActive(false);
                if (buyPanel != null) buyPanel.SetActive(false);
                if (hintPanel != null) hintPanel.SetActive(false);
                if (confirmPanel != null) confirmPanel.SetActive(false);
                if (infoPanel != null) infoPanel.SetActive(false);
                if (talkPanel != null) talkPanel.SetActive(false);
                if (dialoguePanel != null) dialoguePanel.SetActive(false);
            }
        }

        private void RefreshAll()
        {
            RefreshRootOptions();
            RefreshRootStats();

            RefreshBuyList();
            RefreshBuyItemInfo();

            RefreshSellList();
            RefreshTalkOptions();

            RefreshConfirmOptions();
        }

        private void RefreshRootOptions()
        {
            if (rootOptions == null || rootOptions.Length < 4) return;

            rootOptions[0].text = "购买";
            rootOptions[1].text = "出售";
            rootOptions[2].text = "对话";
            rootOptions[3].text = "离开";

            for (int i = 0; i < 4; i++)
                rootOptions[i].color = (state == State.Root && i == rootIndex) ? Color.yellow : Color.white;
        }

        private void RefreshRootStats()
        {
            if (moneyText != null && stats != null)
                moneyText.text = $"{stats.Money}G";

            // 槽位：X/(Capacity+1) —— 背包 + 手持
            if (slotsText != null && inventory != null)
            {
                int usedInv = CountUsedSlots(inventory);
                int usedHeld = (heldItem != null && heldItem.held != null) ? 1 : 0;

                int used = usedInv + usedHeld;
                int total = inventory.Capacity + 1;

                slotsText.text = $"{used}/{total}";
            }
        }

        private int CountUsedSlots(Inventory inv)
        {
            if (inv == null) return 0;
            int used = 0;
            for (int i = 0; i < inv.Capacity; i++)
                if (inv.GetAt(i) != null) used++;
            return used;
        }

        private void RefreshBuyList()
        {
            if (state != State.Buy && state != State.BuyConfirm) return;

            for (int i = 0; i < 4; i++)
            {
                var nameTmp = (buyNameTexts != null && i < buyNameTexts.Length) ? buyNameTexts[i] : null;
                var priceTmp = (buyPriceTexts != null && i < buyPriceTexts.Length) ? buyPriceTexts[i] : null;

                var item = GetBuyItem(i);

                if (nameTmp != null) nameTmp.text = item != null ? item.DisplayName : "  ——";
                if (priceTmp != null) priceTmp.text = item != null ? $"{item.BuyPrice}G" : "";

                bool selected = (i == buyIndex);
                if (nameTmp != null) nameTmp.color = selected ? Color.yellow : Color.white;
                if (priceTmp != null) priceTmp.color = selected ? Color.yellow : Color.white;
            }
        }

        private void RefreshBuyItemInfo()
        {
            if (infoDialogue == null) return;

            if (state != State.Buy && state != State.BuyConfirm)
            {
                infoDialogue.Clear();
                return;
            }

            string sk = GetKey(itemInfoSpeakerKeys, buyIndex);
            string ck = GetKey(itemInfoContentKeys, buyIndex);

            if (string.IsNullOrEmpty(sk) && string.IsNullOrEmpty(ck))
            {
                infoDialogue.Clear();
                return;
            }

            infoDialogue.ShowKeys(sk, ck);
        }

        private void RefreshSellList()
        {
            if (state != State.Sell) return;

            for (int i = 0; i < 8; i++)
            {
                var nameTmp = (sellNameTexts != null && i < sellNameTexts.Length) ? sellNameTexts[i] : null;
                var priceTmp = (sellPriceTexts != null && i < sellPriceTexts.Length) ? sellPriceTexts[i] : null;

                ItemDefinition item = GetSellItemForDisplay(i);

                if (nameTmp != null) nameTmp.text = item != null ? item.DisplayName : "  ——";
                if (priceTmp != null) priceTmp.text = item != null ? $"{item.SellPrice}G" : "";

                bool selected = (i == sellIndex);
                if (nameTmp != null) nameTmp.color = selected ? Color.yellow : Color.white;
                if (priceTmp != null) priceTmp.color = selected ? Color.yellow : Color.white;
            }
        }

        private ItemDefinition GetSellItemForDisplay(int idx)
        {
            if (idx < 0 || idx > 7) return null;
            if (idx <= 6) return inventory != null ? inventory.GetAt(idx) : null;
            return heldItem != null ? heldItem.held : null;
        }

        private void RefreshTalkOptions()
        {
            if (talkOptions == null || talkOptions.Length < 4) return;

            var loc = GameRoot.I != null ? GameRoot.I.Localization : null;

            for (int i = 0; i < 4; i++)
            {
                if (loc != null)
                {
                    string key = GetKey(talkOptionTextKeys, i);
                    if (!string.IsNullOrEmpty(key) && talkOptions[i] != null)
                        talkOptions[i].text = loc.Get(key);
                }

                if (talkOptions[i] != null)
                    talkOptions[i].color = (state == State.TalkSelect && i == talkIndex) ? Color.yellow : Color.white;
            }
        }

        private void RefreshConfirmOptions()
        {
            if (confirmOptionTexts == null || confirmOptionTexts.Length < 2) return;

            if (confirmOptionTexts[0] != null) confirmOptionTexts[0].text = confirmYesText;
            if (confirmOptionTexts[1] != null) confirmOptionTexts[1].text = confirmNoText;

            bool active = (state == State.BuyConfirm);
            if (!active)
            {
                // 不在确认状态就都白（不强制）
                if (confirmOptionTexts[0] != null) confirmOptionTexts[0].color = Color.white;
                if (confirmOptionTexts[1] != null) confirmOptionTexts[1].color = Color.white;
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                if (confirmOptionTexts[i] == null) continue;
                confirmOptionTexts[i].color = (i == confirmIndex) ? Color.yellow : Color.white;
            }
        }

        private void ShowLeftWelcome()
        {
            if (leftDialogue == null) return;
            leftDialogue.ShowKeys(welcomeSpeakerKey, welcomeContentKey);
        }

        private void ShowHint(string contentKey)
        {
            if (hintDialogue == null) return;
            hintDialogue.ShowKeys(hintSpeakerKey, contentKey);
        }

        private static string GetKey(string[] arr, int idx)
        {
            if (arr == null || idx < 0 || idx >= arr.Length) return null;
            return arr[idx];
        }

        private void LeaveShop()
        {
            if (GameRoot.I == null) return;
            GameRoot.I.TransitionTo(leaveSceneName, leaveSpawnId);
        }
    }
}
