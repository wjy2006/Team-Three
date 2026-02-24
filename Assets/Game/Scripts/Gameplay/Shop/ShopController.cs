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
            BuyConfirm,
            Sell,
            TalkSelect,
            TalkDialogue
        }

        [SerializeField] private State state = State.Root;

        // ===== Audio SFX =====
        [Header("Audio SFX")]
        public AudioSource uiAudioSource;
        public AudioClip moveSfx;      // 切换选项
        public AudioClip confirmSfx;   // 确认进入
        public AudioClip cancelSfx;    // 取消/返回
        public AudioClip executeSfx;   // 交易成功（买/卖）

        [Header("Leave Target")]
        public string leaveSceneName = "World_Town";
        public string leaveSpawnId = "FromShop";

        // ===== Panels =====
        [Header("Panels")]
        public GameObject leftPanel;
        public GameObject rootPanel;
        public GameObject buyPanel;
        public GameObject infoPanel;

        public GameObject hintPanel;
        public GameObject confirmPanel;
        public GameObject sellPanel;
        public GameObject talkPanel;
        public GameObject dialoguePanel;

        // ===== Display-only Dialogue Areas =====
        [Header("Display-only Dialogue Areas")]
        public ShopDialogueUI leftDialogue;
        public ShopDialogueUI infoDialogue;
        public ShopDialogueUI hintDialogue;

        // ===== Confirm UI =====
        [Header("Buy Confirm UI")]
        public TMP_Text confirmPromptText;
        public TMP_Text[] confirmOptionTexts = new TMP_Text[2];
        public string confirmPromptFormat = "{0}买下{1}？";
        public string confirmYesText = "确定";
        public string confirmNoText = "取消";

        // ===== Talk UI =====
        [Header("Talk UI")]
        public TMP_Text[] talkOptions = new TMP_Text[4];
        public string[] talkOptionTextKeys = new string[4];
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
        public string buySelectHintKey;
        public string hintSoldOutKey;

        // ===== Root UI =====
        [Header("Root UI")]
        public TMP_Text[] rootOptions = new TMP_Text[4];
        public TMP_Text moneyText;
        public TMP_Text slotsText;

        // ===== Buy UI =====
        [Header("Buy UI")]
        public TMP_Text[] buyNameTexts = new TMP_Text[4];
        public TMP_Text[] buyPriceTexts = new TMP_Text[4];
        public BuySlot[] buySlots = new BuySlot[4];

        [Header("Buy: Per-item Info Keys (4 items)")]
        public string[] itemInfoSpeakerKeys = new string[4];
        public string[] itemInfoContentKeys = new string[4];

        // ===== Sell UI =====
        [Header("Sell UI (8 slots: 0-6 inv, 7 held)")]
        public TMP_Text[] sellNameTexts = new TMP_Text[8];
        public TMP_Text[] sellPriceTexts = new TMP_Text[8];

        [Header("Portrait")]
        public ShopPortraitController portrait;

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
        private int confirmIndex = 0;
        private bool boughtLastFrame = false;

        private void Awake()
        {
            if (uiAudioSource != null)
            {
                uiAudioSource.spatialBlend = 0f; // 2D音效
                uiAudioSource.ignoreListenerPause = true; // 暂停时也能听到
            }
        }

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

            HookTyping(leftDialogue);
            HookTyping(infoDialogue);
            HookTyping(hintDialogue);
            if (talkDialogueUI != null) HookTyping(talkDialogueUI);

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

            UnhookTyping(leftDialogue);
            UnhookTyping(infoDialogue);
            UnhookTyping(hintDialogue);
            if (talkDialogueUI != null) UnhookTyping(talkDialogueUI);

            if (GameRoot.I != null && GameRoot.I.Pause != null)
                GameRoot.I.Pause.PopPause("Shop");
        }

        private void PlaySFX(AudioClip clip)
        {
            if (uiAudioSource != null && clip != null)
                uiAudioSource.PlayOneShot(clip);
        }

        private void Update()
        {
            if (input == null) return;

            input.ConsumeMenuDown();

            if (state != State.TalkDialogue)
            {
                if (input.ConsumeCancelDown())
                {
                    if (state == State.BuyConfirm)
                    {
                        PlaySFX(cancelSfx);
                        CloseBuyConfirm(goBackToBuy: true);
                        return;
                    }

                    if (state != State.Root)
                    {
                        PlaySFX(cancelSfx);
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
            }
        }

        // =========================
        // Root Logic
        // =========================
        private void UpdateRoot()
        {
            if (input.ConsumeUpDown())
            {
                rootIndex = Mathf.Clamp(rootIndex - 1, 0, 3);
                PlaySFX(moveSfx);
                RefreshRootOptions();
            }
            if (input.ConsumeDownDown())
            {
                rootIndex = Mathf.Clamp(rootIndex + 1, 0, 3);
                PlaySFX(moveSfx);
                RefreshRootOptions();
            }

            if (input.ConsumeInteractDown())
            {
                PlaySFX(confirmSfx);
                switch (rootIndex)
                {
                    case 0: buyIndex = 0; SetState(State.Buy); RefreshAll(); break;
                    case 1: sellIndex = 0; SetState(State.Sell); RefreshAll(); break;
                    case 2: talkIndex = 0; SetState(State.TalkSelect); RefreshAll(); break;
                    case 3: LeaveShop(); break;
                }
            }
        }

        // =========================
        // Buy Logic
        // =========================
        private void UpdateBuy()
        {
            bool moved = false;
            if (input.ConsumeUpDown()) { buyIndex = Mathf.Clamp(buyIndex - 1, 0, 3); moved = true; }
            if (input.ConsumeDownDown()) { buyIndex = Mathf.Clamp(buyIndex + 1, 0, 3); moved = true; }

            if (moved)
            {
                PlaySFX(moveSfx);
                RefreshBuyList();
                RefreshBuyItemInfo();
            }

            if (input.ConsumeInteractDown())
            {
                TryOpenBuyConfirm(buyIndex);
            }
        }

        private void TryOpenBuyConfirm(int idx)
        {
            if (stats == null || inventory == null) { ShowHintFail(hintNoItemKey); return; }
            BuySlot slot = GetBuySlot(idx);
            if (slot == null || slot.item == null) { ShowHintFail(hintNoItemKey); return; }

            if (IsSoldOut(slot))
            {
                string k = !string.IsNullOrEmpty(slot.soldOutHintKey) ? slot.soldOutHintKey : hintSoldOutKey;
                ShowHintFail(k);
                return;
            }

            int price = slot.item.BuyPrice;
            if (stats.Money < price) { ShowHintFail(hintNotEnoughMoneyKey); return; }
            if (!CanPlacePurchasedItem()) { ShowHintFail(hintBagFullKey); return; }

            PlaySFX(confirmSfx); // 准备购买，进入确认界面
            OpenBuyConfirm(slot.item, price);
        }

        private void OpenBuyConfirm(ItemDefinition item, int price)
        {
            confirmIndex = 0;
            SetState(State.BuyConfirm);
            string priceStr = $"{price}G";
            string itemName = item != null ? item.DisplayName : "";
            if (confirmPromptText != null)
                confirmPromptText.text = string.Format(confirmPromptFormat, priceStr, itemName);
            RefreshConfirmOptions();
        }

        private void UpdateBuyConfirm()
        {
            bool moved = false;
            if (input.ConsumeUpDown() || input.ConsumeLeftDown()) { confirmIndex = 0; moved = true; }
            if (input.ConsumeDownDown() || input.ConsumeRightDown()) { confirmIndex = 1; moved = true; }

            if (moved)
            {
                PlaySFX(moveSfx);
                RefreshConfirmOptions();
            }

            if (input.ConsumeInteractDown())
            {
                if (confirmIndex == 0)
                {
                    ExecutePendingBuy(buyIndex);
                }
                else
                {
                    PlaySFX(cancelSfx);
                    CloseBuyConfirm(goBackToBuy: true);
                }
            }
        }

        private void ExecutePendingBuy(int idx)
        {
            BuySlot slot = GetBuySlot(idx);
            int price = slot.item.BuyPrice;

            bool placed = TryPlacePurchasedItem(slot.item);
            if (!placed) { ShowHintFail(hintBagFullKey); return; }

            bool spent = stats.TrySpendMoney(price);
            if (!spent) { ShowHintFail(hintNotEnoughMoneyKey); return; }

            AddBoughtCount(slot, 1);
            boughtLastFrame = true;

            PlaySFX(executeSfx); // 钱扣了，货到了，响起来！
            ShowHintSuccess(hintThanksKey);

            RefreshRootStats();
            RefreshBuyList();
            CloseBuyConfirm(goBackToBuy: true);
        }

        private void CloseBuyConfirm(bool goBackToBuy)
        {
            if (!goBackToBuy) return;
            SetState(State.Buy);
            RefreshBuyList();
            RefreshBuyItemInfo();
        }

        // =========================
        // Sell Logic
        // =========================
        private void UpdateSell()
        {
            bool moved = false;
            if (input.ConsumeRightDown()) { if (sellIndex <= 3) { sellIndex += 4; moved = true; } }
            if (input.ConsumeLeftDown()) { if (sellIndex >= 4) { sellIndex -= 4; moved = true; } }
            if (input.ConsumeUpDown()) { if (sellIndex % 4 != 0) { sellIndex -= 1; moved = true; } }
            if (input.ConsumeDownDown()) { if (sellIndex % 4 != 3) { sellIndex += 1; moved = true; } }

            if (moved)
            {
                PlaySFX(moveSfx);
                RefreshSellList();
            }

            if (input.ConsumeInteractDown())
            {
                TrySell(sellIndex);
            }
        }

        private void TrySell(int idx)
        {
            if (stats == null || inventory == null) return;
            ItemDefinition item = GetSellItem(idx);
            if (item == null || !IsSellable(item)) return;

            int price = Mathf.Max(0, item.SellPrice);
            bool removed = RemoveSellItem(idx);
            if (!removed) return;

            if (price > 0) stats.AddMoney(price);

            PlaySFX(executeSfx); // 卖破烂成功
            RefreshRootStats();
            RefreshSellList();
        }

        // =========================
        // Talk Logic
        // =========================
        private void UpdateTalkSelect()
        {
            if (input.ConsumeUpDown())
            {
                talkIndex = Mathf.Clamp(talkIndex - 1, 0, 3);
                PlaySFX(moveSfx);
                RefreshTalkOptions();
            }
            if (input.ConsumeDownDown())
            {
                talkIndex = Mathf.Clamp(talkIndex + 1, 0, 3);
                PlaySFX(moveSfx);
                RefreshTalkOptions();
            }

            if (input.ConsumeInteractDown())
            {
                PlaySFX(confirmSfx);
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
        // UI Helpers
        // =========================
        private void SetState(State s)
        {
            bool enteringBuyFromOutside = (s == State.Buy) && (state != State.Buy) && (state != State.BuyConfirm);
            state = s;

            if (portrait != null)
                portrait.SetBasePose(state == State.BuyConfirm ? ShopPortraitController.Pose.Confirm : ShopPortraitController.Pose.Idle);

            ApplyPanelsForState();

            if (state == State.Buy)
            {
                if (boughtLastFrame) { ShowHint(hintThanksKey); boughtLastFrame = false; }
                else if (enteringBuyFromOutside && !string.IsNullOrEmpty(buySelectHintKey)) { ShowHint(buySelectHintKey); }
            }
        }

        private void ApplyPanelsForState()
        {
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
            if (hintPanel != null) hintPanel.SetActive(isBuy);
            if (confirmPanel != null) confirmPanel.SetActive(isBuyConfirm);
            if (sellPanel != null) sellPanel.SetActive(isSell);
            if (talkPanel != null) talkPanel.SetActive(isTalkSelect);
            if (dialoguePanel != null) dialoguePanel.SetActive(isTalkDialogue);

            if (isSell)
            {
                // 卖出界面隐藏所有无关UI
                if (leftPanel) leftPanel.SetActive(false);
                if (rootPanel) rootPanel.SetActive(false);
                if (buyPanel) buyPanel.SetActive(false);
                if (infoPanel) infoPanel.SetActive(false);
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
            string[] labels = { "购买", "出售", "对话", "离开" };
            for (int i = 0; i < 4; i++)
            {
                rootOptions[i].text = labels[i];
                rootOptions[i].color = (state == State.Root && i == rootIndex) ? Color.yellow : Color.white;
            }
        }

        private void RefreshRootStats()
        {
            if (moneyText != null && stats != null) moneyText.text = $"{stats.Money}G";
            if (slotsText != null && inventory != null)
            {
                int used = CountUsedSlots(inventory) + (heldItem != null && heldItem.held != null ? 1 : 0);
                slotsText.text = $"{used} / {inventory.Capacity + 1}";
            }
        }

        private int CountUsedSlots(Inventory inv)
        {
            int used = 0;
            for (int i = 0; i < inv.Capacity; i++) if (inv.GetAt(i) != null) used++;
            return used;
        }

        private void RefreshBuyList()
        {
            var loc = GameRoot.I != null ? GameRoot.I.Localization : null;
            for (int i = 0; i < 4; i++)
            {
                var nameTmp = buyNameTexts[i];
                var priceTmp = buyPriceTexts[i];
                BuySlot slot = buySlots[i];
                bool soldOut = IsSoldOut(slot);

                if (nameTmp != null)
                {
                    if (slot == null || slot.item == null || (soldOut && string.IsNullOrEmpty(slot.soldOutNameKey))) nameTmp.text = "  ——";
                    else if (soldOut) nameTmp.text = loc != null ? loc.Get(slot.soldOutNameKey) : slot.soldOutNameKey;
                    else nameTmp.text = slot.item.DisplayName;
                    nameTmp.color = (i == buyIndex) ? Color.yellow : Color.white;
                }
                if (priceTmp != null)
                {
                    priceTmp.text = (slot == null || slot.item == null || soldOut) ? "" : $"{slot.item.BuyPrice}G";
                    priceTmp.color = (i == buyIndex) ? Color.yellow : Color.white;
                }
            }
        }

        private void RefreshBuyItemInfo()
        {
            if (infoDialogue == null) return;
            string sk = GetKey(itemInfoSpeakerKeys, buyIndex);
            string ck = GetKey(itemInfoContentKeys, buyIndex);
            if (string.IsNullOrEmpty(sk) && string.IsNullOrEmpty(ck)) infoDialogue.Clear();
            else infoDialogue.ShowKeys(sk, ck);
        }

        private void RefreshSellList()
        {
            if (state != State.Sell) return;
            for (int i = 0; i < 8; i++)
            {
                var item = GetSellItemForDisplay(i);
                bool selected = (i == sellIndex);
                bool sellable = IsSellable(item);

                if (sellNameTexts[i])
                {
                    sellNameTexts[i].text = item != null ? item.DisplayName : "  ——";
                    sellNameTexts[i].color = selected ? Color.yellow : (sellable ? Color.white : Color.gray);
                }
                if (sellPriceTexts[i])
                {
                    sellPriceTexts[i].text = item == null ? "" : (sellable ? $"{item.SellPrice}G" : "NO!!");
                    sellPriceTexts[i].color = selected ? Color.yellow : (sellable ? Color.white : Color.gray);
                }
            }
        }

        private ItemDefinition GetSellItemForDisplay(int idx)
        {
            if (idx <= 6) return (inventory != null) ? inventory.GetAt(idx)?.Definition : null;
            return (heldItem != null) ? heldItem.held : null;
        }

        private void RefreshTalkOptions()
        {
            var loc = GameRoot.I != null ? GameRoot.I.Localization : null;
            for (int i = 0; i < 4; i++)
            {
                if (loc != null && talkOptions[i]) talkOptions[i].text = loc.Get(GetKey(talkOptionTextKeys, i));
                if (talkOptions[i]) talkOptions[i].color = (state == State.TalkSelect && i == talkIndex) ? Color.yellow : Color.white;
            }
        }

        private void RefreshConfirmOptions()
        {
            if (confirmOptionTexts == null) return;
            confirmOptionTexts[0].text = confirmYesText;
            confirmOptionTexts[1].text = confirmNoText;
            for (int i = 0; i < 2; i++)
                confirmOptionTexts[i].color = (state == State.BuyConfirm && i == confirmIndex) ? Color.yellow : Color.white;
        }

        // =========================
        // System Helpers
        // =========================
        private bool CanPlacePurchasedItem() => (inventory != null && !inventory.IsFull()) || (heldItem != null && heldItem.held == null);

        private bool TryPlacePurchasedItem(ItemDefinition item)
        {
            if (inventory != null && inventory.TryAdd(item)) return true;
            if (heldItem != null && heldItem.held == null) { heldItem.held = item; var vis = GameRoot.I.vis; if (vis) vis.RefreshNow(); return true; }
            return false;
        }

        private ItemDefinition GetSellItem(int idx) => GetSellItemForDisplay(idx);

        private bool RemoveSellItem(int idx)
        {
            if (idx <= 6) return inventory != null && inventory.RemoveAt(idx);
            if (heldItem != null && heldItem.held != null) { heldItem.held = null; var vis = GameRoot.I.vis; if (vis) vis.RefreshNow(); return true; }
            return false;
        }

        private static bool IsSellable(ItemDefinition item) => item != null && item.Type != ItemType.Key && item.Type != ItemType.Quest && item.SellPrice > 0;

        private BuySlot GetBuySlot(int idx) => (buySlots != null && idx >= 0 && idx < buySlots.Length) ? buySlots[idx] : null;

        private bool IsSoldOut(BuySlot slot)
        {
            if (slot == null || slot.item == null) return true;
            if (!slot.limited) return false;
            int count = (GameRoot.I != null && GameRoot.I.Global != null && !string.IsNullOrEmpty(slot.boughtCountGlobalKey)) ? GameRoot.I.Global.GetInt(slot.boughtCountGlobalKey) : 0;
            return count >= slot.maxCount;
        }

        private void AddBoughtCount(BuySlot slot, int delta)
        {
            if (slot != null && slot.limited && GameRoot.I?.Global != null && !string.IsNullOrEmpty(slot.boughtCountGlobalKey))
                GameRoot.I.Global.AddInt(slot.boughtCountGlobalKey, delta);
        }

        private void ShowLeftWelcome() => leftDialogue?.ShowKeys(welcomeSpeakerKey, welcomeContentKey);

        private void ShowHint(string key) { if (hintDialogue) { if (hintPanel) hintPanel.SetActive(true); hintDialogue.ShowKeys(hintSpeakerKey, key); } }

        private void ShowHintSuccess(string key) { if (portrait) portrait.OverridePose(ShopPortraitController.Pose.BuySuccess, 2f); ShowHint(key); }

        private void ShowHintFail(string key) { PlaySFX(cancelSfx); if (portrait) portrait.OverridePose(ShopPortraitController.Pose.BuyFail, 2f); ShowHint(key); }

        private string GetKey(string[] arr, int idx) => (arr != null && idx >= 0 && idx < arr.Length) ? arr[idx] : null;

        private void LeaveShop() { PlaySFX(cancelSfx); GameRoot.I?.TransitionTo(leaveSceneName, leaveSpawnId); }

        private void HookTyping(ShopDialogueUI ui) { if (ui && portrait) { ui.OnTypingStart += portrait.OnTypingStart; ui.OnTypingEnd += portrait.OnTypingEnd; } }
        private void HookTyping(ShopTalkDialogueUI ui) { if (ui && portrait) { ui.OnTypingStart += portrait.OnTypingStart; ui.OnTypingEnd += portrait.OnTypingEnd; } }
        private void UnhookTyping(ShopDialogueUI ui) { if (ui && portrait) { ui.OnTypingStart -= portrait.OnTypingStart; ui.OnTypingEnd -= portrait.OnTypingEnd; } }
        private void UnhookTyping(ShopTalkDialogueUI ui) { if (ui && portrait) { ui.OnTypingStart -= portrait.OnTypingStart; ui.OnTypingEnd -= portrait.OnTypingEnd; } }
    }
}