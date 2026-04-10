using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class RoomChaosContext : MonoBehaviour
{
    [Tooltip("Optional stable room id. Empty = current scene name.")]
    public string roomId;

    [Tooltip("Initial chaos level when this room is first encountered.")]
    [Range(0, 2)]
    public int defaultLevel = 0;

    public string GetEffectiveRoomId(Scene scene)
    {
        if (!string.IsNullOrWhiteSpace(roomId))
            return roomId;
        return scene.name;
    }
}
