using UnityEngine;
using Game.UI.Menu; // 引用菜单命名空间
using Game.UI.Shop; // 引用商店命名空间

public class DoorInteractTrigger : MonoBehaviour
{
    public KeyDoor2D door;
    public DialogueAsset cantOpen;

    private PlayerInputReader input;
    private bool inRange;

    // 缓存 UI 引用以提高性能
    private FixedMenuController menu;

    private void Awake()
    {
        if (door == null)
            door = GetComponentInParent<KeyDoor2D>();
    }

    private void Update()
    {
        if (!inRange) return;

        // 延迟获取引用
        if (input == null)
        {
            input = GameRoot.I != null ? GameRoot.I.playerInput : null;
            if (input == null) return;
        }
        
        // 1. 检查全局输入锁和对话框（原有逻辑）
        if (GameRoot.I != null && (GameRoot.I.InputLocked || (GameRoot.I.Dialogue != null && GameRoot.I.Dialogue.IsOpen)))
            return;

        // 2. 检查背包/主菜单是否打开 (从 FixedMenuController 获取)
        menu = FixedMenuController.Instance;
        if (menu != null && menu.menuPanel != null && menu.menuPanel.activeInHierarchy)
            return;


        // --- 结束检查 ---

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
}