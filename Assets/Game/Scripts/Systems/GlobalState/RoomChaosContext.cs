using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class RoomChaosContext : MonoBehaviour
{
    [Header("Room Identity")]
    [Tooltip("Optional stable room id. Empty = current scene name.")]
    public string roomId;

    [Header("Default Level")]
    [Tooltip("Initial chaos level when this room is first encountered.")]
    [Range(0, 2)]
    public int defaultLevel = 0;

    [Header("Dialogue Overrides (Optional)")]
    [Tooltip("Overrides dialogue shown when chaos level increases in this room.")]
    public DialogueAsset chaosLevelUpDialogueOverride;
    [Tooltip("Overrides dialogue shown when chaos level decreases in this room.")]
    public DialogueAsset chaosLevelDownDialogueOverride;
    [Tooltip("Overrides dialogue shown when trying to increase at max level in this room.")]
    public DialogueAsset chaosAlreadyMaxDialogueOverride;
    [Tooltip("Overrides dialogue shown when trying to decrease at min level in this room.")]
    public DialogueAsset chaosAlreadyMinDialogueOverride;

    [Header("Lab Chaos Gate (Optional)")]
    [Tooltip("When enabled, positive chaos shift is blocked while scene name starts with the prefix and unlock bool is false.")]
    public bool gateIncreaseByScenePrefix = true;
    [Tooltip("Scene name prefix to block chaos increase, for example: Room_Lab.")]
    public string gatedScenePrefix = "Room_Lab";
    [Tooltip("When this global bool becomes true, chaos increase is allowed again.")]
    public string chaosIncreaseUnlockBoolKey = GameRoot.STATE_ADMIN_DISABLED;
    [Tooltip("Optional dialogue override shown when chaos increase is blocked by this gate.")]
    public DialogueAsset chaosIncreaseBlockedDialogueOverride;

    public string GetEffectiveRoomId(Scene scene)
    {
        if (!string.IsNullOrWhiteSpace(roomId))
            return roomId;
        return scene.name;
    }
}
