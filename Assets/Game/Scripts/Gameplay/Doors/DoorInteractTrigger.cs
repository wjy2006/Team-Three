using System.Collections.Generic;
using UnityEngine;
using Game.Systems.Items;
using Game.UI.Menu;
using Game.UI.Shop;

public class DoorInteractTrigger : MonoBehaviour
{
    private static readonly HashSet<DoorInteractTrigger> ActiveTriggers = new HashSet<DoorInteractTrigger>();

    public KeyDoor2D door;
    public DialogueAsset cantOpen;

    [Header("Fallback Range")]
    [Tooltip("If trigger overlap is unreliable, use this collider for a proximity fallback check.")]
    [SerializeField] private Collider2D proximityCollider;
    [SerializeField, Min(0f)] private float proximityPadding = 0.2f;

    private PlayerInputReader input;
    private bool inRange;

    private FixedMenuController menu;

    private void Awake()
    {
        if (door == null)
            door = GetComponentInParent<KeyDoor2D>();
    }

    private void OnEnable()
    {
        ActiveTriggers.Add(this);
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

        if (input.ConsumeInteractDown())
        {
            if (door == null) return;

            bool ok = door.TryOpen();

            if (!ok) GameRoot.I.Dialogue.Open("Door", cantOpen);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = false;
    }

    private void OnDisable()
    {
        inRange = false;
        ActiveTriggers.Remove(this);
    }

    public static bool TryOpenAnyInRangeForHeldKey(ItemDefinition heldKey)
    {
        if (heldKey == null) return false;

        foreach (var trigger in ActiveTriggers)
        {
            if (trigger == null || !trigger.IsPlayerInRange() || trigger.door == null) continue;
            if (trigger.door.requiredKey != heldKey) continue;

            if (trigger.door.TryOpen())
                return true;
        }

        return false;
    }

    private bool IsPlayerInRange()
    {
        if (inRange) return true;

        var range = ResolveProximityCollider();
        if (range == null || !range.enabled) return false;

        GameObject player = ResolvePlayer();
        if (player == null) return false;

        var playerColliders = player.GetComponentsInChildren<Collider2D>(false);
        for (int i = 0; i < playerColliders.Length; i++)
        {
            var playerCollider = playerColliders[i];
            if (playerCollider == null || !playerCollider.enabled) continue;

            var distance = range.Distance(playerCollider);
            if (distance.isOverlapped || distance.distance <= proximityPadding)
                return true;
        }

        return false;
    }

    private Collider2D ResolveProximityCollider()
    {
        if (proximityCollider != null) return proximityCollider;

        if (door != null && door.blockingCollider != null)
            proximityCollider = door.blockingCollider;
        else
            proximityCollider = GetComponent<Collider2D>();

        return proximityCollider;
    }

    private static GameObject ResolvePlayer()
    {
        if (GameRoot.I != null && GameRoot.I.PlayerInteractor != null)
            return GameRoot.I.PlayerInteractor.gameObject;

        return GameObject.FindGameObjectWithTag("Player");
    }
}
