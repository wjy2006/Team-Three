using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Game.Systems.Items;
using Game.UI.Menu;
using Game.UI.Shop;

public class GemSocketInteractTrigger : MonoBehaviour
{
    private static readonly HashSet<GemSocketInteractTrigger> ActiveTriggers = new HashSet<GemSocketInteractTrigger>();

    [Header("Refs")]
    public GemSocket2D socket;

    [Header("Dialogue (Optional)")]
    [FormerlySerializedAs("cantInsert")] public DialogueAsset emptySlotDialogue;
    [FormerlySerializedAs("insertedDialogue")] public DialogueAsset insertingDialogue;
    public DialogueAsset insertedSlotDialogue;

    private PlayerInputReader input;
    private bool inRange;
    private FixedMenuController menu;

    private void Awake()
    {
        if (socket == null)
            socket = GetComponentInParent<GemSocket2D>();
    }

    private void OnEnable()
    {
        ActiveTriggers.Add(this);
    }

    private void OnDisable()
    {
        inRange = false;
        ActiveTriggers.Remove(this);
    }

    private void Update()
    {
        if (!IsPlayerInRange()) return;

        if (input == null)
        {
            input = GameRoot.I != null ? GameRoot.I.playerInput : null;
            if (input == null) return;
        }

        if (GameRoot.I != null && (GameRoot.I.InputLocked || (GameRoot.I.Dialogue != null && GameRoot.I.Dialogue.IsOpen)))
            return;

        menu = FixedMenuController.Instance;
        if (menu != null && menu.menuPanel != null && menu.menuPanel.activeInHierarchy)
            return;

        if (ShopController.Instance != null && ShopController.Instance.isActiveAndEnabled)
            return;

        if (!input.ConsumeInteractDown())
            return;

        TryHandleInteract();
    }

    public static bool TryInsertAnyInRangeForHeldGem(ItemDefinition heldGem)
    {
        if (heldGem == null) return false;

        foreach (var trigger in ActiveTriggers)
        {
            if (trigger == null || !trigger.IsPlayerInRange() || trigger.socket == null) continue;
            if (!trigger.socket.MatchesGem(heldGem)) continue;

            if (trigger.socket.TryInsert())
            {
                trigger.ShowDialogue(trigger.insertingDialogue);
                return true;
            }
        }

        return false;
    }

    private void TryHandleInteract()
    {
        if (socket == null) return;

        if (socket.IsInserted)
        {
            ShowDialogue(insertedSlotDialogue);
            return;
        }

        bool inserted = socket.TryInsert();
        if (inserted)
        {
            ShowDialogue(insertingDialogue);
            return;
        }

        ShowDialogue(emptySlotDialogue);
    }

    private void ShowDialogue(DialogueAsset dialogue)
    {
        if (dialogue == null || GameRoot.I == null || GameRoot.I.Dialogue == null) return;
        if (GameRoot.I.Dialogue.IsOpen) return;
        GameRoot.I.Dialogue.Open("_gem_socket", dialogue);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;
        inRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;
        inRange = false;
    }

    private bool IsPlayerInRange()
    {
        return inRange;
    }

    private static bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;

        var root = other.transform != null ? other.transform.root : null;
        return root != null && root.CompareTag("Player");
    }
}
