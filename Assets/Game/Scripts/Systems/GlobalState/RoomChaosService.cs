using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoomChaosService
{
    public const string RoomKeyPrefix = "room.chaos.";
    public const string CurrentRoomKeyStateKey = "world.chaos.current_room_key";
    public const string CurrentRoomLevelMirrorKey = "world.chaos.level";

    public static event Action<int, string> OnCurrentRoomLevelChanged;

    public static void BindRoom(GlobalState global, string roomId, int defaultLevel = 0)
    {
        if (global == null) return;

        string roomKey = ResolveRoomKey(roomId);
        if (!global.HasKey(roomKey))
            global.SetInt(roomKey, defaultLevel);

        int level = global.GetInt(roomKey);
        ApplyCurrentRoom(global, roomKey, level, notify: true);
    }

    public static string GetCurrentRoomKey(GlobalState global, int defaultLevel = 0)
    {
        if (global == null) return null;

        string roomKey = global.GetString(CurrentRoomKeyStateKey);
        if (!string.IsNullOrWhiteSpace(roomKey))
            return roomKey;

        string sceneName = SceneManager.GetActiveScene().name;
        BindRoom(global, sceneName, defaultLevel);
        return global.GetString(CurrentRoomKeyStateKey);
    }

    public static int GetCurrentRoomLevel(GlobalState global, int defaultLevel = 0)
    {
        if (global == null) return defaultLevel;

        string roomKey = GetCurrentRoomKey(global, defaultLevel);
        if (string.IsNullOrWhiteSpace(roomKey)) return defaultLevel;

        if (!global.HasKey(roomKey))
            global.SetInt(roomKey, defaultLevel);

        return global.GetInt(roomKey);
    }

    public static bool TryShiftCurrentRoomLevel(
        GlobalState global,
        int delta,
        int minLevel,
        int maxLevel,
        int defaultLevel,
        out int before,
        out int after)
    {
        before = defaultLevel;
        after = defaultLevel;

        if (global == null) return false;

        int min = Mathf.Min(minLevel, maxLevel);
        int max = Mathf.Max(minLevel, maxLevel);
        int clampedDefault = Mathf.Clamp(defaultLevel, min, max);

        string roomKey = GetCurrentRoomKey(global, clampedDefault);
        if (string.IsNullOrWhiteSpace(roomKey)) return false;

        if (!global.HasKey(roomKey))
            global.SetInt(roomKey, clampedDefault);

        before = global.GetInt(roomKey);
        after = Mathf.Clamp(before + delta, min, max);

        if (after == before)
        {
            ApplyCurrentRoom(global, roomKey, before, notify: false);
            return false;
        }

        global.SetInt(roomKey, after);
        ApplyCurrentRoom(global, roomKey, after, notify: true);
        return true;
    }

    private static string ResolveRoomKey(string roomId)
    {
        string id = string.IsNullOrWhiteSpace(roomId) ? SceneManager.GetActiveScene().name : roomId.Trim();
        if (id.StartsWith(RoomKeyPrefix, StringComparison.Ordinal))
            return id;
        return RoomKeyPrefix + id;
    }

    private static void ApplyCurrentRoom(GlobalState global, string roomKey, int level, bool notify)
    {
        if (global == null || string.IsNullOrWhiteSpace(roomKey)) return;

        global.SetString(CurrentRoomKeyStateKey, roomKey);
        global.SetInt(CurrentRoomLevelMirrorKey, level);
        // Full-screen glitch is enabled only at the highest chaos tier.
        global.SetBool(GameRoot.STATE_GLITCH_WORLD, level == 2);

        if (notify)
            OnCurrentRoomLevelChanged?.Invoke(level, roomKey);
    }
}
