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
        private const int HELD_INDEX = SLOT_COUNT;

        [Header("Root")]
        public GameObject menuPanel;

        [Header("Audio SFX")]
        [Tooltip("UI audio source for menu SFX")]
        public AudioSource uiAudioSource;
        [Tooltip("Cursor move SFX")]
        public AudioClip moveSfx;
        [Tooltip("Confirm/open submenu SFX")]
        public AudioClip confirmSfx;
        [Tooltip("Back/close menu SFX")]
        public AudioClip cancelSfx;
        [Tooltip("Execute action SFX (hold/drop)")]
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

        [Header("Localization Keys")]
        public string infoTextKey = "ui.menu.action.info";
        public string holdTextKey = "ui.menu.action.hold";
        public string dropTextKey = "ui.menu.action.drop";

        [Header("Click Targets (optional)")]
        [Tooltip("Optional larger hit boxes for slot clicks. Fallback to slotTexts rects.")]
        public RectTransform[] slotClickAreas = new RectTransform[SLOT_COUNT];
        [Tooltip("Optional click area for Info action. Fallback to infoText rect.")]
        public RectTransform infoClickArea;
        [Tooltip("Optional click area for Hold action. Fallback to holdActionText rect.")]
        public RectTransform holdClickArea;
        [Tooltip("Optional click area for Drop action. Fallback to dropText rect.")]
        public RectTransform dropClickArea;
        [Tooltip("Optional click area for held item row. Fallback to heldText rect/bounds.")]
        public RectTransform heldClickArea;
        [Tooltip("Use TMP rendered text bounds as automatic hit area when manual areas are not assigned.")]
        public bool useRenderedTextHitArea = true;
        [Tooltip("Extra padding for auto TMP text hit area.")]
        public Vector2 renderedTextHitPadding = new Vector2(6f, 4f);
        [Header("Debug")]
        [Tooltip("Enable click flow logs.")]
        public bool debugClickLogs = false;
        [Tooltip("Enable verbose hit test logs for each area/text bound.")]
        public bool debugHitDetails = false;
        [Header("Double Click")]
        [Tooltip("Double-click same slot to trigger default Hold action.")]
        public bool enableDoubleClickHold = true;
        [Min(0.05f)]
        [Tooltip("Max interval (seconds) between two clicks on the same slot.")]
        public float doubleClickInterval = 0.28f;

        [Header("Refs")]
        public PlayerStats stats;
        public HeldItem heldItem;
        public Inventory inventory;

        [Header("Empty Slot Dialogues")]
        public DialogueAsset EmptyDropped;
        public DialogueAsset EmptyChecked;
        [Header("Held Selection Dialogues")]
        [Tooltip("Shown when selecting Hold action while the selected item is already in hand.")]
        public DialogueAsset HeldHoldActionDialogue;

        private PlayerInputReader input;
        private bool isOpen;
        private int selectedIndex;
        private int hoveredSlotIndex = -1;
        private int hoveredActionIndex = -1;
        private int lastSlotClickIndex = -1;
        private float lastSlotClickTime = -999f;

        private enum MenuState { Inventory, ItemAction }
        private MenuState state = MenuState.Inventory;

        // 0 Info, 1 Hold, 2 Drop
        private int actionIndex = 1;
        public static FixedMenuController Instance;

        void Awake()
        {
            Instance = this;
            if (menuPanel != null) menuPanel.SetActive(false);

            if (uiAudioSource != null)
            {
                uiAudioSource.spatialBlend = 0f;
                uiAudioSource.playOnAwake = false;
                uiAudioSource.ignoreListenerPause = true;
            }
        }

        void Start()
        {
            if (GameRoot.I != null)
                input = GameRoot.I.playerInput;
        }

        private void PlaySFX(AudioClip clip)
        {
            if (uiAudioSource != null && clip != null)
            {
                uiAudioSource.PlayOneShot(clip);
            }
        }

        private void LogClick(string message)
        {
            if (!debugClickLogs) return;
            Debug.Log($"[FixedMenuController] {message}", this);
        }

        void Update()
        {
            if (input == null) return;

            // Disable menu input while dialogue is open.
            if (GameRoot.I != null && GameRoot.I.Dialogue != null && GameRoot.I.Dialogue.IsOpen)
            {
                input.ConsumeMenuDown();
                return;
            }

            // Keep existing Menu action for opening/closing.
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

            UpdateHoverPreview();

            // Cancel mapping stays in InputReader/InputSystem (you will map RMB to Cancel there).
            if (input.ConsumeCancelDown() || input.ConsumeInteractDown())
            {
                LogClick($"CancelDown state={state} selected={selectedIndex}");
                PlaySFX(cancelSfx);
                if (state == MenuState.ItemAction)
                {
                    state = MenuState.Inventory;
                    ResetDoubleClickTracking();
                    RefreshAll();
                }
                else
                {
                    Close();
                }
                return;
            }

            // Left click from InputReader.
            if (input.ConsumeClickDown(out var clickScreenPos))
            {
                LogClick($"ClickDown pos={clickScreenPos} state={state} selected={selectedIndex}");
                HandleLeftClick(clickScreenPos);
            }
        }

        private void UpdateHoverPreview()
        {
            int newHoveredSlot = -1;
            int newHoveredAction = -1;

            Vector2 pointerScreenPos = input.PointerPos;

            if (state == MenuState.Inventory)
            {
                newHoveredSlot = GetClickedSlotIndex(pointerScreenPos, enableHitLogs: false);
            }
            else
            {
                newHoveredAction = GetClickedActionIndex(pointerScreenPos, enableHitLogs: false);
            }

            if (newHoveredSlot != hoveredSlotIndex || newHoveredAction != hoveredActionIndex)
            {
                bool playHoverMoveSfx = false;
                if (state == MenuState.Inventory)
                {
                    playHoverMoveSfx = newHoveredSlot >= 0 && newHoveredSlot != hoveredSlotIndex;
                }
                else
                {
                    playHoverMoveSfx = newHoveredAction >= 0 && newHoveredAction != hoveredActionIndex;
                }

                if (playHoverMoveSfx) PlaySFX(moveSfx);

                hoveredSlotIndex = newHoveredSlot;
                hoveredActionIndex = newHoveredAction;
                RefreshAll();
            }
        }

        private void HandleLeftClick(Vector2 clickScreenPos)
        {
            if (state == MenuState.Inventory)
            {
                int itemIndex = GetClickedSlotIndex(clickScreenPos);
                if (itemIndex < 0)
                {
                    LogClick($"Inventory click missed all slots at {clickScreenPos}");
                    return;
                }

                bool isDoubleClick = RegisterAndCheckDoubleClick(itemIndex);
                bool changed = selectedIndex != itemIndex;
                selectedIndex = itemIndex;

                if (isDoubleClick)
                {
                    LogClick($"Inventory item double-click: index={itemIndex} -> default Hold");
                    ExecuteDefaultHoldFromDoubleClick();
                }
                else
                {
                    state = MenuState.ItemAction;
                    actionIndex = 1; // default: Hold

                    LogClick($"Inventory item hit: index={itemIndex}, changed={changed} -> enter ItemAction");
                    if (changed) PlaySFX(moveSfx);
                    PlaySFX(confirmSfx);
                    RefreshAll();
                }
                return;
            }

            int action = GetClickedActionIndex(clickScreenPos);
            if (action >= 0)
            {
                if (actionIndex != action) PlaySFX(moveSfx);
                actionIndex = action;
                LogClick($"Action hit: actionIndex={actionIndex}");
                ExecuteAction();
                return;
            }

            int clickedSlot = GetClickedSlotIndex(clickScreenPos);
            if (clickedSlot >= 0)
            {
                bool isDoubleClick = RegisterAndCheckDoubleClick(clickedSlot);
                bool changed = selectedIndex != clickedSlot;
                selectedIndex = clickedSlot;
                actionIndex = 1;

                if (isDoubleClick)
                {
                    LogClick($"ItemAction slot double-click: index={clickedSlot} -> default Hold");
                    ExecuteDefaultHoldFromDoubleClick();
                }
                else
                {
                    if (changed)
                    {
                        LogClick($"ItemAction slot hit: switch selected to index={clickedSlot}");
                        PlaySFX(moveSfx);
                        RefreshAll();
                    }
                    else
                    {
                        LogClick($"ItemAction slot single-click: index={clickedSlot} (waiting for double-click)");
                    }
                }
                return;
            }

            LogClick($"ItemAction click missed actions at {clickScreenPos}");
        }

        private bool RegisterAndCheckDoubleClick(int slotIndex)
        {
            if (!enableDoubleClickHold)
            {
                lastSlotClickIndex = slotIndex;
                lastSlotClickTime = Time.unscaledTime;
                return false;
            }

            float now = Time.unscaledTime;
            bool isDouble =
                slotIndex >= 0 &&
                slotIndex == lastSlotClickIndex &&
                now - lastSlotClickTime <= doubleClickInterval;

            lastSlotClickIndex = slotIndex;
            lastSlotClickTime = now;
            return isDouble;
        }

        private void ResetDoubleClickTracking()
        {
            lastSlotClickIndex = -1;
            lastSlotClickTime = -999f;
        }

        private void ExecuteDefaultHoldFromDoubleClick()
        {
            PlaySFX(confirmSfx);

            if (selectedIndex == HELD_INDEX)
            {
                Close();
                if (HeldHoldActionDialogue != null)
                    OpenDialogueAsset(HeldHoldActionDialogue);
                return;
            }

            HoldOrSwapSelected();
            state = MenuState.Inventory;
            Close();
        }

        private int GetClickedSlotIndex(Vector2 clickScreenPos, bool enableHitLogs = true)
        {
            int max = slotTexts != null ? Mathf.Min(SLOT_COUNT, slotTexts.Length) : SLOT_COUNT;
            for (int i = 0; i < max; i++)
            {
                RectTransform hitRect = null;
                if (slotClickAreas != null && i < slotClickAreas.Length)
                    hitRect = slotClickAreas[i];

                if (hitRect != null)
                {
                    if (IsRectHit(hitRect, clickScreenPos, $"slotClickAreas[{i}]", enableHitLogs)) return i;
                    continue;
                }

                if (slotTexts == null || i >= slotTexts.Length || slotTexts[i] == null) continue;

                if (useRenderedTextHitArea)
                {
                    if (IsTextRenderedHit(slotTexts[i], clickScreenPos, $"slotText[{i}]", enableHitLogs)) return i;
                }
                else
                {
                    if (IsRectHit(slotTexts[i].rectTransform, clickScreenPos, $"slotTextRect[{i}]", enableHitLogs)) return i;
                }
            }

            RectTransform heldRect = heldClickArea;
            if (heldRect != null)
            {
                if (IsRectHit(heldRect, clickScreenPos, "heldClickArea", enableHitLogs)) return HELD_INDEX;
            }
            else if (heldText != null)
            {
                if (useRenderedTextHitArea)
                {
                    if (IsTextRenderedHit(heldText, clickScreenPos, "heldText", enableHitLogs)) return HELD_INDEX;
                }
                else
                {
                    if (IsRectHit(heldText.rectTransform, clickScreenPos, "heldTextRect", enableHitLogs)) return HELD_INDEX;
                }
            }

            return -1;
        }

        private int GetClickedActionIndex(Vector2 clickScreenPos, bool enableHitLogs = true)
        {
            if (IsActionHit(infoClickArea, infoText, clickScreenPos, "Info", enableHitLogs)) return 0;
            if (IsActionHit(holdClickArea, holdActionText, clickScreenPos, "Hold", enableHitLogs)) return 1;
            if (IsActionHit(dropClickArea, dropText, clickScreenPos, "Drop", enableHitLogs)) return 2;
            return -1;
        }

        private bool IsActionHit(RectTransform overrideRect, TMP_Text fallbackText, Vector2 clickScreenPos, string label, bool enableHitLogs)
        {
            if (overrideRect != null) return IsRectHit(overrideRect, clickScreenPos, $"{label}.overrideRect", enableHitLogs);
            if (fallbackText == null) return false;

            if (useRenderedTextHitArea)
                return IsTextRenderedHit(fallbackText, clickScreenPos, $"{label}.textBounds", enableHitLogs);

            return IsRectHit(fallbackText.rectTransform, clickScreenPos, $"{label}.textRect", enableHitLogs);
        }

        private bool IsTextRenderedHit(TMP_Text text, Vector2 clickScreenPos, string label, bool enableHitLogs)
        {
            if (text == null) return false;

            var rect = text.rectTransform;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, clickScreenPos, cam, out var localPoint))
            {
                if (debugClickLogs && debugHitDetails && enableHitLogs)
                    LogClick($"TextHit[{label}] ScreenPointToLocalPoint failed pos={clickScreenPos}");
                return false;
            }

            text.ForceMeshUpdate();
            Bounds textBounds = text.textBounds;
            if (textBounds.size.sqrMagnitude <= 0.0001f)
            {
                if (debugClickLogs && debugHitDetails && enableHitLogs)
                    LogClick($"TextHit[{label}] empty textBounds for '{text.name}' text='{text.text}'");
                return false;
            }

            Vector2 min = new Vector2(textBounds.min.x, textBounds.min.y) - renderedTextHitPadding;
            Vector2 max = new Vector2(textBounds.max.x, textBounds.max.y) + renderedTextHitPadding;

            bool hit = localPoint.x >= min.x && localPoint.x <= max.x &&
                       localPoint.y >= min.y && localPoint.y <= max.y;

            if (debugClickLogs && debugHitDetails && enableHitLogs)
            {
                LogClick(
                    $"TextHit[{label}] text='{text.name}' local={localPoint} min={min} max={max} hit={hit} screen={clickScreenPos}");
            }

            return hit;
        }

        private bool IsRectHit(RectTransform rect, Vector2 clickScreenPos, string label, bool enableHitLogs)
        {
            if (rect == null) return false;

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            bool hit = RectTransformUtility.RectangleContainsScreenPoint(rect, clickScreenPos, cam);
            if (debugClickLogs && debugHitDetails && enableHitLogs)
                LogClick($"RectHit[{label}] rect='{rect.name}' hit={hit} screen={clickScreenPos}");
            return hit;
        }

        public void Open()
        {
            isOpen = true;
            state = MenuState.Inventory;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, SLOT_COUNT - 1);
            hoveredSlotIndex = -1;
            hoveredActionIndex = -1;
            ResetDoubleClickTracking();

            if (menuPanel != null) menuPanel.SetActive(true);

            if (GameRoot.I != null && GameRoot.I.Pause != null)
                GameRoot.I.Pause.PushPause("Menu");

            if (input != null) input.SetMoveEnabled(false);

            RefreshAll();
        }

        public void Close()
        {
            isOpen = false;
            state = MenuState.Inventory;
            hoveredSlotIndex = -1;
            hoveredActionIndex = -1;
            ResetDoubleClickTracking();

            if (menuPanel != null) menuPanel.SetActive(false);

            if (GameRoot.I != null && GameRoot.I.Pause != null)
                GameRoot.I.Pause.PopPause("Menu");

            if (input != null) input.SetMoveEnabled(true);
        }

        void ExecuteAction()
        {
            bool heldSelected = selectedIndex == HELD_INDEX;

            ItemInstance inst = heldSelected
                ? (heldItem != null ? heldItem.heldInstance : null)
                : (inventory != null ? inventory.GetAt(selectedIndex) : null);
            ItemDefinition item = inst != null ? inst.Definition : null;
            if (item == null && heldSelected && heldItem != null)
                item = heldItem.held;

            switch (actionIndex)
            {
                case 0: // Info
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

                case 1: // Hold (pick up / put back / swap)
                    if (heldSelected)
                    {
                        Close();
                        if (HeldHoldActionDialogue != null)
                            OpenDialogueAsset(HeldHoldActionDialogue);
                    }
                    else
                    {
                        HoldOrSwapSelected();
                        state = MenuState.Inventory;
                        Close();
                    }
                    break;

                case 2: // Drop
                    PlaySFX(confirmSfx);
                    Close();
                    if (item != null)
                    {
                        if (item.dropDialogue != null) OpenDialogueAsset(item.dropDialogue);
                        else OpenOneLine("npc.all.unknown.name", "dlg.all.default_dropped");

                        if (item.Type == ItemType.Key) return;
                        if (heldSelected)
                        {
                            if (heldItem != null) heldItem.SetHeld(null);
                        }
                        else
                        {
                            inventory.RemoveAt(selectedIndex);
                        }
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
            if (selectedIndex < 0 || selectedIndex >= SLOT_COUNT) return;

            var beforeDef = heldItem.held;

            ItemInstance slotInst = inventory.GetAt(selectedIndex);
            ItemInstance handInst = heldItem.heldInstance;

            bool slotHasItem = slotInst != null && slotInst.Definition != null;
            bool handHasItem = handInst != null && handInst.Definition != null;

            if (!slotHasItem && !handHasItem) return;

            if (slotHasItem && !handHasItem)
            {
                PlaySFX(executeSfx);
                heldItem.SetHeld(slotInst);
                inventory.SetAt(selectedIndex, (ItemInstance)null);
            }
            else if (!slotHasItem && handHasItem)
            {
                inventory.SetAt(selectedIndex, handInst);
                heldItem.SetHeld(null);
            }
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
                heldText.text = heldDef != null ? heldDef.DisplayName : "  \u2014\u2014";
            }

            for (int i = 0; i < SLOT_COUNT; i++)
            {
                if (slotTexts == null || i >= slotTexts.Length || slotTexts[i] == null) continue;

                ItemInstance inst = inventory != null ? inventory.GetAt(i) : null;
                ItemDefinition def = inst != null ? inst.Definition : null;

                slotTexts[i].text = def != null ? def.DisplayName : "  \u2014\u2014";
                bool highlightBySelection = state == MenuState.ItemAction && i == selectedIndex;
                bool highlightByHover = state == MenuState.Inventory && i == hoveredSlotIndex;
                slotTexts[i].color = (highlightBySelection || highlightByHover) ? Color.yellow : Color.white;
            }

            if (heldText != null)
            {
                bool highlightBySelection = state == MenuState.ItemAction && selectedIndex == HELD_INDEX;
                bool highlightByHover = state == MenuState.Inventory && hoveredSlotIndex == HELD_INDEX;
                heldText.color = (highlightBySelection || highlightByHover) ? Color.yellow : Color.white;
            }

            if (infoText != null)
            {
                infoText.text = Loc(infoTextKey, "Info");
                bool highlightByHover = state == MenuState.ItemAction && hoveredActionIndex == 0;
                infoText.color = highlightByHover ? Color.yellow : Color.white;
            }

            if (holdActionText != null)
            {
                holdActionText.text = Loc(holdTextKey, "Hold");
                bool highlightByHover = state == MenuState.ItemAction && hoveredActionIndex == 1;
                holdActionText.color = highlightByHover ? Color.yellow : Color.white;
            }

            if (dropText != null)
            {
                dropText.text = Loc(dropTextKey, "Drop");
                bool highlightByHover = state == MenuState.ItemAction && hoveredActionIndex == 2;
                dropText.color = highlightByHover ? Color.yellow : Color.white;
            }
        }

        private string Loc(string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key)) return fallback;
            var loc = GameRoot.I != null ? GameRoot.I.Localization : null;
            if (loc == null) return fallback;
            return loc.Get(key);
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
