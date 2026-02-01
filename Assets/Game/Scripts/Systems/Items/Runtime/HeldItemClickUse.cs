using UnityEngine;
using Assets.Game.Scripts.Systems.Items;

public class HeldItemClickUse : MonoBehaviour
{
    private PlayerInputReader input;
    private Game.Gameplay.Player.HeldItem held;

    void Awake()
    {
        held = GetComponent<Game.Gameplay.Player.HeldItem>();
    }

    void Update()
    {
        // ✅ 延迟获取 input（避免启动顺序问题）
        if (input == null)
        {
            if (GameRoot.I != null) input = GameRoot.I.playerInput;
            if (input == null) return;
        }

        if (held == null) return;

        if (!input.ConsumeClickDown()) return; // ✅ Click Action
        var item = held.held;
        if (item == null)
        {
            Debug.Log("Click: 手上是空的");
            return;
        }

        if (item.Effect == null)
        {
            Debug.LogWarning($"Click: 手持物品 {item.DisplayName} 没有绑定 Effect，无法使用");
            return;
        }

        bool consume = item.Effect.Apply(gameObject);

        if (consume)
        {
            held.held = null; // ✅ 消耗品：用完手上清空
        }
    }
}
