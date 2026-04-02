using TMPro;
using UnityEngine;
using Game.Systems.Items;
using Game.Gameplay.Player;

namespace Game.UI.Shop
{
    public class ShopController : MonoBehaviour
    {
        public static ShopController Instance { get; private set; }

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
        public AudioClip moveSfx;      // move selection
        public AudioClip confirmSfx;   // confirm action
        public AudioClip cancelSfx;    // cancel / back
        public AudioClip executeSfx;   // transaction success (buy/sell)

        [Header("Mouse Hover")]
        public bool useMouseHoverSelection = true;
        public bool useRenderedTextHitArea = true;
        public Vector2 renderedTextHitPadding = new Vector2(6f, 4f);

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
        public string confirmPromptTextKey = "ui.shop.confirm.buy_template";
        public string confirmYesTextKey = "ui.shop.confirm.yes";
        public string confirmNoTextKey = "ui.shop.confirm.no";

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
        public string[] rootOptionTextKeys = new string[4]
        {
            "ui.shop.root.buy",
            "ui.shop.root.sell",
            "ui.shop.root.talk",
            "ui.shop.root.leave"
        };
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
        public string sellUnsellableTextKey = "ui.shop.sell.unsellable";

        [Header("Portrait")]
        public ShopPortraitController portrait;

        // ===== Runtime refs =====
        private PlayerInputReader input;
        private PlayerStats stats;
        private Inventory inventory;
        private HeldItem heldItem;
        private PlayerStats subscribedStats;
        private Inventory subscribedInventory;

        // ===== Selections =====
        private int rootIndex = 0;
        private int buyIndex = 0;
        private int sellIndex = 0;
        private int talkIndex = 0;
        private int confirmIndex = 0;
        private bool boughtLastFrame = false;
        private int hoveredRootIndex = -1;
        private int hoveredBuyIndex = -1;
        private int hoveredConfirmIndex = -1;
        private int hoveredSellIndex = -1;
        private int hoveredTalkIndex = -1;

        private void Awake()
        {
            Instance = this;

            if (uiAudioSource != null)
            {
                uiAudioSource.spatialBlend = 0f; // force 2D SFX
                uiAudioSource.ignoreListenerPause = true; // still audible while paused
            }
        }

        private void Start()
        {
            ResolveRuntimeRefs();

            if (GameRoot.I != null && GameRoot.I.Pause != null)
                GameRoot.I.Pause.PushPause("Shop");

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

        private void ResolveRuntimeRefs()
        {
            var root = GameRoot.I;

            if (root != null)
            {
                root.RefreshRuntimeRefs();

                if (input == null) input = root.playerInput;
                if (inventory == null) inventory = root.Inventory;
                if (heldItem == null) heldItem = root.playerHeldItem;

                if (stats == null && root.playerHeldItem != null)
                    stats = root.playerHeldItem.GetComponent<PlayerStats>();
            }

            if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
            if (heldItem == null) heldItem = FindFirstObjectByType<HeldItem>();

            if (stats == null && heldItem != null)
                stats = heldItem.GetComponent<PlayerStats>();
            if (stats == null)
                stats = FindFirstObjectByType<PlayerStats>();

            SyncRuntimeSubscriptions();
        }

        private void SyncRuntimeSubscriptions()
        {
            if (subscribedStats != stats)
            {
                if (subscribedStats != null) subscribedStats.OnStatsChanged -= RefreshRootStats;
                if (stats != null) stats.OnStatsChanged += RefreshRootStats;
                subscribedStats = stats;
            }

            if (subscribedInventory != inventory)
            {
                if (subscribedInventory != null) subscribedInventory.OnChanged -= RefreshRootStats;
                if (inventory != null) inventory.OnChanged += RefreshRootStats;
                subscribedInventory = inventory;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (subscribedStats != null) subscribedStats.OnStatsChanged -= RefreshRootStats;
            if (subscribedInventory != null) subscribedInventory.OnChanged -= RefreshRootStats;

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
            if (clip == null) return;

            if (uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(clip);
                return;
            }

            var globalSfx = GameRoot.I != null ? GameRoot.I.globalSfxSource : null;
            if (globalSfx != null)
                globalSfx.PlayOneShot(clip);
        }

        private void Update()
        {
            if (input == null)
            {
                ResolveRuntimeRefs();
                if (input == null) return;
            }

            input.ConsumeMenuDown();

            if (state != State.TalkDialogue)
            {
                if (input.ConsumeCancelDown() || input.ConsumeInteractDown())
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

            UpdateMouseHoverSelection();
            if (TryHandleRootPanelClick()) return;

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
        }

        private void ExecuteRootSelection()
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

            if (ConsumeBuyClick(out int clicked))
            {
                if (buyIndex != clicked)
                {
                    buyIndex = clicked;
                    RefreshBuyList();
                    RefreshBuyItemInfo();
                }
                TryOpenBuyConfirm(buyIndex);
            }
        }

        private void TryOpenBuyConfirm(int idx)
        {
            ResolveRuntimeRefs();
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

            PlaySFX(confirmSfx); // open buy confirmation
            OpenBuyConfirm(slot.item, price);
        }

        private void OpenBuyConfirm(ItemDefinition item, int price)
        {
            confirmIndex = 0;
            SetState(State.BuyConfirm);
            string priceStr = $"{price}G";
            string itemName = item != null ? item.DisplayName : "";
            if (confirmPromptText != null)
            {
                string template = Loc(confirmPromptTextKey, "{0} buy {1}?");
                confirmPromptText.text = string.Format(template, priceStr, itemName);
            }
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

            if (ConsumeBuyConfirmClick(out int clicked))
            {
                if (confirmIndex != clicked)
                {
                    confirmIndex = clicked;
                    RefreshConfirmOptions();
                }

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
            ResolveRuntimeRefs();
            if (stats == null || inventory == null) { ShowHintFail(hintNoItemKey); return; }

            BuySlot slot = GetBuySlot(idx);
            if (slot == null || slot.item == null) { ShowHintFail(hintNoItemKey); return; }
            int price = slot.item.BuyPrice;

            bool placed = TryPlacePurchasedItem(slot.item);
            if (!placed) { ShowHintFail(hintBagFullKey); return; }

            bool spent = stats.TrySpendMoney(price);
            if (!spent) { ShowHintFail(hintNotEnoughMoneyKey); return; }

            AddBoughtCount(slot, 1);
            boughtLastFrame = true;

            PlaySFX(executeSfx); // buy succeeded
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

            if (ConsumeSellClick(out int clicked))
            {
                if (sellIndex != clicked)
                {
                    sellIndex = clicked;
                    RefreshSellList();
                }
                TrySell(sellIndex);
            }
        }

        private void TrySell(int idx)
        {
            ResolveRuntimeRefs();
            if (stats == null || inventory == null) { ShowHintFail(hintNoItemKey); return; }
            ItemDefinition item = GetSellItem(idx);
            if (item == null || !IsSellable(item)) return;

            int price = Mathf.Max(0, item.SellPrice);
            bool removed = RemoveSellItem(idx);
            if (!removed) return;

            if (price > 0) stats.AddMoney(price);

            PlaySFX(executeSfx); // sell succeeded
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

            if (ConsumeTalkClick(out int clicked))
            {
                if (talkIndex != clicked)
                {
                    talkIndex = clicked;
                    RefreshTalkOptions();
                }
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
            hoveredRootIndex = -1;
            hoveredBuyIndex = -1;
            hoveredConfirmIndex = -1;
            hoveredSellIndex = -1;
            hoveredTalkIndex = -1;

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
                // Hide unrelated panels in sell mode.
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
            bool rootPanelVisibleByState = IsRootPanelVisibleByState();
            for (int i = 0; i < 4; i++)
            {
                string key = GetKey(rootOptionTextKeys, i);
                rootOptions[i].text = Loc(key, rootOptions[i].text);
                rootOptions[i].color = (rootPanelVisibleByState && i == rootIndex) ? Color.yellow : Color.white;
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
            if (inv == null) return 0;

            int used = 0;
            for (int i = 0; i < inv.Capacity; i++)
            {
                var inst = inv.GetAt(i);
                if (inst != null && inst.Definition != null) used++;
            }
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
                    if (slot == null || slot.item == null || (soldOut && string.IsNullOrEmpty(slot.soldOutNameKey))) nameTmp.text = "  \u2014\u2014";
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
                    sellNameTexts[i].text = item != null ? item.DisplayName : "  \u2014\u2014";
                    sellNameTexts[i].color = selected ? Color.yellow : (sellable ? Color.white : Color.gray);
                }
                if (sellPriceTexts[i])
                {
                    sellPriceTexts[i].text = item == null ? "" : (sellable ? $"{item.SellPrice}G" : Loc(sellUnsellableTextKey, "NO!!"));
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
            confirmOptionTexts[0].text = Loc(confirmYesTextKey, "Yes");
            confirmOptionTexts[1].text = Loc(confirmNoTextKey, "Cancel");
            for (int i = 0; i < 2; i++)
                confirmOptionTexts[i].color = (state == State.BuyConfirm && i == confirmIndex) ? Color.yellow : Color.white;
        }

        private void UpdateMouseHoverSelection()
        {
            if (!useMouseHoverSelection || input == null) return;

            Vector2 pointer = input.PointerPos;

            if (IsRootPanelVisibleByState())
            {
                int hovered = GetHoveredTextIndex(rootOptions, 4, pointer);
                if (hovered != hoveredRootIndex)
                {
                    if (hovered >= 0) PlaySFX(moveSfx);
                    hoveredRootIndex = hovered;
                }

                if (hovered >= 0 && rootIndex != hovered)
                {
                    rootIndex = hovered;
                    RefreshRootOptions();
                }
            }

            if (state == State.TalkDialogue) return;

            switch (state)
            {
                case State.Buy:
                {
                    int hovered = GetHoveredBuyIndex(pointer);
                    if (hovered != hoveredBuyIndex)
                    {
                        if (hovered >= 0) PlaySFX(moveSfx);
                        hoveredBuyIndex = hovered;
                    }

                    if (hovered >= 0 && buyIndex != hovered)
                    {
                        buyIndex = hovered;
                        RefreshBuyList();
                        RefreshBuyItemInfo();
                    }
                    break;
                }
                case State.BuyConfirm:
                {
                    int hovered = GetHoveredTextIndex(confirmOptionTexts, 2, pointer);
                    if (hovered != hoveredConfirmIndex)
                    {
                        if (hovered >= 0) PlaySFX(moveSfx);
                        hoveredConfirmIndex = hovered;
                    }

                    if (hovered >= 0 && confirmIndex != hovered)
                    {
                        confirmIndex = hovered;
                        RefreshConfirmOptions();
                    }
                    break;
                }
                case State.Sell:
                {
                    int hovered = GetHoveredSellIndex(pointer);
                    if (hovered != hoveredSellIndex)
                    {
                        if (hovered >= 0) PlaySFX(moveSfx);
                        hoveredSellIndex = hovered;
                    }

                    if (hovered >= 0 && sellIndex != hovered)
                    {
                        sellIndex = hovered;
                        RefreshSellList();
                    }
                    break;
                }
                case State.TalkSelect:
                {
                    int hovered = GetHoveredTextIndex(talkOptions, 4, pointer);
                    if (hovered != hoveredTalkIndex)
                    {
                        if (hovered >= 0) PlaySFX(moveSfx);
                        hoveredTalkIndex = hovered;
                    }

                    if (hovered >= 0 && talkIndex != hovered)
                    {
                        talkIndex = hovered;
                        RefreshTalkOptions();
                    }
                    break;
                }
            }
        }

        private int GetHoveredBuyIndex(Vector2 screenPos)
        {
            for (int i = 0; i < 4; i++)
            {
                if (IsTextHit(GetTextAt(buyNameTexts, i), screenPos) || IsTextHit(GetTextAt(buyPriceTexts, i), screenPos))
                    return i;
            }
            return -1;
        }

        private int GetHoveredSellIndex(Vector2 screenPos)
        {
            for (int i = 0; i < 8; i++)
            {
                if (IsTextHit(GetTextAt(sellNameTexts, i), screenPos) || IsTextHit(GetTextAt(sellPriceTexts, i), screenPos))
                    return i;
            }
            return -1;
        }

        private int GetHoveredTextIndex(TMP_Text[] texts, int maxCount, Vector2 screenPos)
        {
            if (texts == null) return -1;
            int count = Mathf.Min(maxCount, texts.Length);
            for (int i = 0; i < count; i++)
            {
                if (IsTextHit(texts[i], screenPos)) return i;
            }
            return -1;
        }

        private bool IsRootPanelVisibleByState()
        {
            return state == State.Root || state == State.TalkSelect || state == State.TalkDialogue;
        }

        private bool TryHandleRootPanelClick()
        {
            if (!IsRootPanelVisibleByState() || input == null || !input.ClickDown) return false;

            int clicked = GetHoveredTextIndex(rootOptions, 4, input.PointerPos);
            if (clicked < 0) return false;

            input.ConsumeClickDown(out _);

            if (rootIndex != clicked)
            {
                rootIndex = clicked;
                RefreshRootOptions();
            }

            if (state == State.TalkDialogue && talkDialogueUI != null && talkDialogueUI.IsOpen)
                talkDialogueUI.Close();

            ExecuteRootSelection();
            return true;
        }

        private bool ConsumeBuyClick(out int clicked)
        {
            clicked = -1;
            if (input == null || !input.ConsumeClickDown(out Vector2 clickPos)) return false;
            clicked = GetHoveredBuyIndex(clickPos);
            return clicked >= 0;
        }

        private bool ConsumeBuyConfirmClick(out int clicked)
        {
            clicked = -1;
            if (input == null || !input.ConsumeClickDown(out Vector2 clickPos)) return false;
            clicked = GetHoveredTextIndex(confirmOptionTexts, 2, clickPos);
            return clicked >= 0;
        }

        private bool ConsumeSellClick(out int clicked)
        {
            clicked = -1;
            if (input == null || !input.ConsumeClickDown(out Vector2 clickPos)) return false;
            clicked = GetHoveredSellIndex(clickPos);
            return clicked >= 0;
        }

        private bool ConsumeTalkClick(out int clicked)
        {
            clicked = -1;
            if (input == null || !input.ConsumeClickDown(out Vector2 clickPos)) return false;
            clicked = GetHoveredTextIndex(talkOptions, 4, clickPos);
            return clicked >= 0;
        }

        private TMP_Text GetTextAt(TMP_Text[] arr, int i)
        {
            if (arr == null || i < 0 || i >= arr.Length) return null;
            return arr[i];
        }

        private bool IsTextHit(TMP_Text text, Vector2 screenPos)
        {
            if (text == null || !text.gameObject.activeInHierarchy) return false;

            RectTransform rect = text.rectTransform;
            Canvas canvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            if (!useRenderedTextHitArea)
                return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam);

            if (rect == null) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, cam, out Vector2 localPoint))
                return false;

            text.ForceMeshUpdate();
            Bounds textBounds = text.textBounds;
            if (textBounds.size.sqrMagnitude <= 0.0001f)
                return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam);

            Vector2 min = new Vector2(textBounds.min.x, textBounds.min.y) - renderedTextHitPadding;
            Vector2 max = new Vector2(textBounds.max.x, textBounds.max.y) + renderedTextHitPadding;
            return localPoint.x >= min.x && localPoint.x <= max.x &&
                   localPoint.y >= min.y && localPoint.y <= max.y;
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

        private void ShowHintSuccess(string key) { if (portrait) portrait.OverridePose(ShopPortraitController.Pose.BuySuccess, 2f); /*ShowHint(key);*/ }

        private void ShowHintFail(string key) { PlaySFX(cancelSfx); if (portrait) portrait.OverridePose(ShopPortraitController.Pose.BuyFail, 2f); ShowHint(key); }

        private string GetKey(string[] arr, int idx) => (arr != null && idx >= 0 && idx < arr.Length) ? arr[idx] : null;
        private string Loc(string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key)) return fallback;
            var loc = GameRoot.I != null ? GameRoot.I.Localization : null;
            if (loc == null) return fallback;
            return loc.Get(key);
        }

        private void LeaveShop() { PlaySFX(cancelSfx); GameRoot.I?.TransitionTo(leaveSceneName, leaveSpawnId); }

        private void HookTyping(ShopDialogueUI ui) { if (ui && portrait) { ui.OnTypingStart += portrait.OnTypingStart; ui.OnTypingEnd += portrait.OnTypingEnd; } }
        private void HookTyping(ShopTalkDialogueUI ui) { if (ui && portrait) { ui.OnTypingStart += portrait.OnTypingStart; ui.OnTypingEnd += portrait.OnTypingEnd; } }
        private void UnhookTyping(ShopDialogueUI ui) { if (ui && portrait) { ui.OnTypingStart -= portrait.OnTypingStart; ui.OnTypingEnd -= portrait.OnTypingEnd; } }
        private void UnhookTyping(ShopTalkDialogueUI ui) { if (ui && portrait) { ui.OnTypingStart -= portrait.OnTypingStart; ui.OnTypingEnd -= portrait.OnTypingEnd; } }
    }
}
