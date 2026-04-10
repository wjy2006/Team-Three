using UnityEngine;

[DisallowMultipleComponent]
public class ShowByRoomChaosLevel : MonoBehaviour
{
    [Tooltip("Target object to show/hide. Keep this controller object active.")]
    public GameObject target;

    [Tooltip("Visible when current room chaos level is within [minLevel, maxLevel].")]
    public int minLevel = 0;
    public int maxLevel = 0;

    [Tooltip("Invert visibility result.")]
    public bool invert;

    private void OnEnable()
    {
        RoomChaosService.OnCurrentRoomLevelChanged += OnRoomChaosChanged;
        Apply();
    }

    private void OnDisable()
    {
        RoomChaosService.OnCurrentRoomLevelChanged -= OnRoomChaosChanged;
    }

    [ContextMenu("Apply")]
    public void Apply()
    {
        GameObject go = target != null ? target : gameObject;
        if (GameRoot.I == null || GameRoot.I.Global == null || go == null) return;

        int level = RoomChaosService.GetCurrentRoomLevel(GameRoot.I.Global, defaultLevel: 0);
        int min = Mathf.Min(minLevel, maxLevel);
        int max = Mathf.Max(minLevel, maxLevel);

        bool visible = level >= min && level <= max;
        if (invert) visible = !visible;

        if (go.activeSelf != visible)
            go.SetActive(visible);
    }

    private void OnRoomChaosChanged(int level, string roomKey)
    {
        Apply();
    }
}
