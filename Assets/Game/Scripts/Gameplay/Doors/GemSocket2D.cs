using System;
using UnityEngine;
using Game.Systems.Items;

[RequireComponent(typeof(Collider2D))]
public class GemSocket2D : MonoBehaviour
{
    [Header("Gem Binding")]
    [Tooltip("Assign the matching gem item (for example: Red/Green/Blue gem).")]
    public ItemDefinition requiredGem;
    [Tooltip("Consume one gem when inserted.")]
    public bool consumeGem = true;

    [Header("Visual")]
    [Tooltip("Optional visual object to show when gem is inserted.")]
    public GameObject insertedVisual;
    [Tooltip("Optional renderers to hide when inserted.")]
    public Renderer[] renderersToHideWhenInserted;

    [Header("Persist (Optional)")]
    [Tooltip("If set, insertion state is persisted in GlobalState bool.")]
    public string insertedGlobalKey;

    public event Action<GemSocket2D> OnInserted;

    private bool isInserted;
    public bool IsInserted => isInserted;

    private void Awake()
    {
        if (!string.IsNullOrEmpty(insertedGlobalKey) && GameRoot.I != null)
            isInserted = GameRoot.I.Global.GetBool(insertedGlobalKey);

        ApplyState();
    }

    public bool MatchesGem(ItemDefinition gem)
    {
        if (isInserted) return false;
        if (requiredGem == null) return false;
        return gem == requiredGem;
    }

    public bool CanInsert()
    {
        if (isInserted) return false;
        if (requiredGem == null) return false;
        return HasGem(requiredGem);
    }

    public bool TryInsert()
    {
        if (!CanInsert()) return false;

        if (consumeGem && requiredGem != null)
        {
            if (!TryConsumeGem(requiredGem)) return false;
        }

        Insert();
        return true;
    }

    public void Insert()
    {
        if (isInserted) return;
        isInserted = true;
        ApplyState();

        if (!string.IsNullOrEmpty(insertedGlobalKey))
            GameRoot.I?.Global?.SetBool(insertedGlobalKey, true);

        OnInserted?.Invoke(this);
    }

    private bool HasGem(ItemDefinition gem)
    {
        if (gem == null) return false;

        var inv = GameRoot.I != null ? GameRoot.I.Inventory : null;
        if (inv != null && inv.Contains(gem))
            return true;

        var held = GameRoot.I != null ? GameRoot.I.playerHeldItem : null;
        return held != null && held.held == gem;
    }

    private bool TryConsumeGem(ItemDefinition gem)
    {
        if (gem == null) return false;

        var held = GameRoot.I != null ? GameRoot.I.playerHeldItem : null;
        if (held != null && held.held == gem)
        {
            held.SetHeld(null);
            held.held = null; // keep compatibility with legacy direct access
            return true;
        }

        var inv = GameRoot.I != null ? GameRoot.I.Inventory : null;
        if (inv == null) return false;
        return inv.RemoveOne(gem);
    }

    private void ApplyState()
    {
        if (insertedVisual != null)
            insertedVisual.SetActive(isInserted);

        if (renderersToHideWhenInserted != null)
        {
            bool visible = !isInserted;
            for (int i = 0; i < renderersToHideWhenInserted.Length; i++)
            {
                if (renderersToHideWhenInserted[i] != null)
                    renderersToHideWhenInserted[i].enabled = visible;
            }
        }
    }
}

