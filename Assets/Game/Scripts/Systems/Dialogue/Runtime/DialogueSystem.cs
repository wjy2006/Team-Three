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
        ui.Open(session.lines);
    }

    public void Close()
    {
        // 无论 UI 状态如何，只要调用了 Close，就强制清理数据
        if (ui != null && ui.IsOpen) 
        {
            ui.Close();
        }
        
        ClearGraphRuntime();
    }

    private void ClearGraphRuntime()
    {
        graph = null;
        nodeId = null;
        currentNpcId = null;
        waitingContinue = false;
        if (ui != null) ui.OnClosed -= ContinueAfterSay;
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

                    // 如果 Say 节点没内容，直接跳下一个
                    if (linesToPlay == null || linesToPlay.Length == 0)
                    {
                        nodeId = node.nextId;
                        StepGraph(); // 递归
                        return;
                    }

                    // 预设好下一站 ID
                    nodeId = node.nextId;

                    // 如果没有下一站了，就不需要 Hook 关闭事件，播完即止
                    if (string.IsNullOrEmpty(nodeId))
                    {
                        ui.Open(linesToPlay);
                        return;
                    }

                    // 还有下一站，Hook 关闭事件
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
                        if(string.IsNullOrEmpty(nodeId)) Close();
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
        ui.OnClosed -= ContinueAfterSay;
        ui.OnClosed += ContinueAfterSay;
    }

    private void ContinueAfterSay()
    {
        ui.OnClosed -= ContinueAfterSay;
        waitingContinue = false;

        // 如果中途被关闭了，graph 会为空
        if (graph == null) return;

        // 启动协程，等待一帧后再执行，解决按键穿透问题
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