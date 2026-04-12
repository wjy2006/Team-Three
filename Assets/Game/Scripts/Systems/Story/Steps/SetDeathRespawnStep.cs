using System;
using System.Collections;
using Game.Gameplay.Player;

[Serializable]
public class SetDeathRespawnStep : StoryStep
{
    public string sceneName = "Room_Lab_Reviving";
    public string spawnId = "Left";
    public bool clearOverride;

    public override IEnumerator Play(StoryContext ctx)
    {
        var global = ctx?.Global;
        if (global == null) yield break;

        if (clearOverride)
        {
            global.Clear(PlayerStats.STATE_DEATH_RESPAWN_SCENE);
            global.Clear(PlayerStats.STATE_DEATH_RESPAWN_SPAWN);
            yield break;
        }

        global.SetString(PlayerStats.STATE_DEATH_RESPAWN_SCENE, sceneName ?? string.Empty);
        global.SetString(PlayerStats.STATE_DEATH_RESPAWN_SPAWN, spawnId ?? string.Empty);
    }
}
