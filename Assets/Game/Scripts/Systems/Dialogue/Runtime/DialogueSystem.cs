using System.Collections;
using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
    public DialogueUI ui;
    public DialogueState DialogueState { get; private set; } = new DialogueState();

    public bool IsOpen => ui != null && ui.IsOpen;

    private string currentNpcId;
    private GraphDialogueAsset graph;
    private string nodeId;
    private bool waitingContinue;

    public void Open(string npcId, DialogueAsset asset)
    {
        if (IsOpen || ui == null || asset == null) return;

        currentNpcId = npcId;

        // Graph 模式
        if (asset is GraphDialogueAsset g)
        {
            graph = g;
            nodeId = string.IsNullOrEmpty(g.startNodeId) ? "start" : g.startNodeId;
            StepGraph();
            return;
        }

        // 简单对话模式 (Repeat/Count)
        DialogueSession session = asset.BuildSession(npcId, DialogueState);
        if (session == null || session.lines == null || session.lines.Length == 0) return;
        ui.OnNodeEnd -= Close;
        ui.OnNodeEnd += Close;

        ui.Open(session.lines);
    }

    public void Close()
    {
        // 1) 先把 UI 关掉（玩家看到的立刻关）
        if (ui != null && ui.IsOpen)
            ui.Close();

        // 2) 关键：如果是 Graph 对话，先把 Say 后面的“纯逻辑节点”刷完
        if (graph != null)
        {
            DrainGraphActionsUntilSayOrEnd();
        }

        // 3) 最后再清理 runtime（以前你是先清理，导致后面跑不动）
        ClearGraphRuntime();
    }

    private void DrainGraphActionsUntilSayOrEnd()
    {
        // 防死循环保护（节点写错时避免卡死）
        const int maxSteps = 256;
        int steps = 0;

        var global = GameRoot.I != null ? GameRoot.I.Global : null;
        var inventory = GameRoot.I != null ? GameRoot.I.Inventory : null;

        while (graph != null && !string.IsNullOrEmpty(nodeId) && steps++ < maxSteps)
        {
            var node = graph.Find(nodeId);
            if (node == null)
            {
                Debug.LogError($"GraphDialogue: 找不到节点 id={nodeId}（Close flush 时）", graph);
                break;
            }

            switch (node.type)
            {
                // ✅ 遇到 Say：停止刷，因为 Say 需要 UI/玩家推进
                case GraphDialogueAsset.NodeType.Say:
                    return;

                case GraphDialogueAsset.NodeType.IfBool:
                    {
                        bool v = global != null && global.GetBool(node.boolKey);
                        nodeId = v ? node.trueNextId : node.falseNextId;
                        break;
                    }

                case GraphDialogueAsset.NodeType.IfCondition:
                    {
                        bool ok = node.condition != null && node.condition.Evaluate(null);
                        nodeId = ok ? node.trueNextId : node.falseNextId;
                        break;
                    }

                case GraphDialogueAsset.NodeType.SetBool:
                    {
                        if (global != null) global.SetBool(node.boolKey, node.boolValue);
                        nodeId = node.nextId;
                        break;
                    }

                case GraphDialogueAsset.NodeType.AddInt:
                    {
                        if (global != null) global.AddInt(node.intKey, node.intValue);
                        nodeId = node.nextId;
                        break;
                    }

                case GraphDialogueAsset.NodeType.SetInt:
                    {
                        if (global != null) global.SetInt(node.intKey, node.intValue);
                        nodeId = node.nextId;
                        break;
                    }

                case GraphDialogueAsset.NodeType.GiveItem:
                    {
                        if (inventory == null)
                        {
                            Debug.LogError("GiveItem 失败: GameRoot.I.Inventory 为空！（Close flush 时）");
                            nodeId = node.successNextId;
                            if (string.IsNullOrEmpty(nodeId)) return;
                            break;
                        }

                        if (node.itemToGive == null)
                        {
                            Debug.LogWarning($"GiveItem 节点 {node.id} 未配置 itemToGive（Close flush 时）");
                            nodeId = node.failNextId;
                            break;
                        }

                        bool success = inventory.TryAdd(node.itemToGive);
                        Debug.Log($"[Dialogue] GiveItem (Close flush): {node.itemToGive.name} | Result: {success}");

                        nodeId = success ? node.successNextId : node.failNextId;
                        break;
                    }

                case GraphDialogueAsset.NodeType.End:
                default:
                    // End 或未知：停止
                    return;
            }
        }

        if (steps >= maxSteps)
            Debug.LogError("DrainGraphActionsUntilSayOrEnd: exceeded maxSteps, possible graph loop.");
    }

    private void ClearGraphRuntime()
    {
        graph = null;
        nodeId = null;
        currentNpcId = null;
        waitingContinue = false;

        if (ui != null) ui.OnNodeEnd -= ContinueAfterSay;
    }


    // ===== Graph 核心解释器 =====
    private void StepGraph()
    {
        // 1. 基础检查
        if (graph == null) { Close(); return; }
        if (string.IsNullOrEmpty(nodeId)) { Close(); return; }

        var node = graph.Find(nodeId);
        if (node == null)
        {
            Debug.LogError($"GraphDialogue: 找不到节点 id={nodeId}", graph);
            Close();
            return;
        }

        var global = GameRoot.I != null ? GameRoot.I.Global : null;

        // 2. 根据节点类型执行逻辑
        switch (node.type)
        {
            case GraphDialogueAsset.NodeType.Say:
                {
                    DialogueLine[] linesToPlay = null;
                    if (node.lines != null && node.lines.Length > 0)
                        linesToPlay = node.lines;

                    if (linesToPlay == null || linesToPlay.Length == 0)
                    {
                        nodeId = node.nextId;
                        StepGraph();
                        return;
                    }

                    // 预设好下一站 ID
                    nodeId = node.nextId;

                    // ✅ 无论有没有下一站，都要 Hook！
                    // 这样当这节话说完时，才会触发 ContinueAfterSay
                    HookContinueOnClose();

                    ui.Open(linesToPlay);
                    break;
                }

            case GraphDialogueAsset.NodeType.IfBool:
                {
                    bool v = false;
                    if (global != null) v = global.GetBool(node.boolKey);

                    nodeId = v ? node.trueNextId : node.falseNextId;
                    StepGraph(); // 递归继续
                    break;
                }

            case GraphDialogueAsset.NodeType.SetBool:
                {
                    if (global != null) global.SetBool(node.boolKey, node.boolValue);

                    nodeId = node.nextId;
                    StepGraph(); // 递归继续
                    break;
                }

            case GraphDialogueAsset.NodeType.AddInt:
                {
                    if (global != null) global.AddInt(node.intKey, node.intValue);

                    nodeId = node.nextId;
                    StepGraph(); // 递归继续
                    break;
                }

            case GraphDialogueAsset.NodeType.SetInt:
                {
                    if (global != null) global.SetInt(node.intKey, node.intValue);

                    nodeId = node.nextId;
                    StepGraph(); // 递归继续
                    break;
                }
            case GraphDialogueAsset.NodeType.IfCondition:
                {
                    bool ok = false;

                    // 你的 Condition 大多是读 GameRoot 状态，不依赖 evt，这里传 null 就行
                    if (node.condition != null)
                        ok = node.condition.Evaluate(null);

                    nodeId = ok ? node.trueNextId : node.falseNextId;
                    StepGraph();
                    break;
                }


            case GraphDialogueAsset.NodeType.GiveItem:
                {
                    // ⚠️ 这里是之前可能出问题的地方：Inventory 引用和逻辑流

                    // 1. 获取 Inventory (加上判空保护)
                    var inventory = GameRoot.I != null ? GameRoot.I.Inventory : null;
                    if (inventory == null)
                    {
                        Debug.LogError("GiveItem 失败: GameRoot.I.Inventory 为空！请检查 GameRoot 是否初始化了 Inventory。");
                        // 为了不卡死游戏，尝试直接跳到 success 或 close
                        nodeId = node.successNextId;
                        if (string.IsNullOrEmpty(nodeId)) Close();
                        else StepGraph();
                        return;
                    }

                    // 2. 检查物品配置
                    if (node.itemToGive == null)
                    {
                        Debug.LogWarning($"GiveItem 节点 {node.id} 未配置 itemToGive");
                        nodeId = node.failNextId; // 没配置这就当失败处理
                        StepGraph();
                        return;
                    }

                    // 3. 执行添加逻辑
                    bool success = inventory.TryAdd(node.itemToGive);
                    Debug.Log($"[Dialogue] GiveItem: {node.itemToGive.name} | Result: {success}");

                    // 4. 根据结果跳转
                    if (success)
                    {
                        nodeId = node.successNextId;
                    }
                    else
                    {
                        nodeId = node.failNextId;
                    }

                    // 5. 关键：必须递归执行下一步！
                    StepGraph();
                    break;
                }

            case GraphDialogueAsset.NodeType.End:
            default:
                Close();
                break;
        }
    }

    private void HookContinueOnClose()
    {
        if (waitingContinue) return;
        waitingContinue = true;

        // ✅ 关键：改监听 OnNodeEnd (代表文字播完了)
        ui.OnNodeEnd -= ContinueAfterSay;
        ui.OnNodeEnd += ContinueAfterSay;
    }

    private void ContinueAfterSay()
    {
        ui.OnNodeEnd -= ContinueAfterSay; // 解绑
        waitingContinue = false;

        if (graph == null) return;

        // ✅ 核心逻辑：如果没有下一站了，由 System 负责彻底关闭 UI
        if (string.IsNullOrEmpty(nodeId))
        {
            Close(); // 这里会调用 ui.Close()，真正隐藏面板
            return;
        }

        // 如果还有下一站（比如下一个 Say 或 GiveItem），继续走
        StartCoroutine(StepGraphNextFrame());
    }

    private IEnumerator StepGraphNextFrame()
    {
        // 关键：等待帧结束，让 Input.GetKeyDown 状态失效
        yield return null;

        // 等待回来后，再次确认 graph 还在（防止等待期间被意外关闭）
        if (graph != null)
        {
            StepGraph();
        }
    }
}