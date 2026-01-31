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

        // Graph 特判
        if (asset is GraphDialogueAsset g)
        {
            graph = g;
            nodeId = string.IsNullOrEmpty(g.startNodeId) ? "start" : g.startNodeId;
            StepGraph();
            return;
        }

        // Repeat/Count：走你已有的 BuildSession
        DialogueSession session = asset.BuildSession(npcId, DialogueState);
        if (session == null || session.lines == null || session.lines.Length == 0) return;
        ui.Open(session.lines);
    }

    public void Close()
    {
        if (!IsOpen) return;
        ui.Close();
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

    // ===== Graph 解释器（无选项版）=====
    private void StepGraph()
    {
        if (graph == null) { Close(); return; }

        // ✅ 如果 nodeId 为空，默认结束（不再强制 End 节点）
        if (string.IsNullOrEmpty(nodeId))
        {
            Close();
            return;
        }

        var node = graph.Find(nodeId);
        if (node == null)
        {
            Debug.LogError($"GraphDialogue: 找不到节点 id={nodeId}", graph);
            Close();
            return;
        }

        var global = GameRoot.I != null ? GameRoot.I.Global : null;
        if (global == null)
        {
            Debug.LogError("GraphDialogue: GameRoot.Global 未就绪（Boot 是否加载？）");
            Close();
            return;
        }

        switch (node.type)
        {
            case GraphDialogueAsset.NodeType.Say:
            {
                DialogueLine[] linesToPlay = null;

                if (node.lines != null && node.lines.Length > 0)
                {
                    linesToPlay = node.lines;
                }
                else if (!string.IsNullOrEmpty(node.speakerKey) || !string.IsNullOrEmpty(node.textKey))
                {
                    linesToPlay = new DialogueLine[]
                    {
                        new DialogueLine { speakerKey = node.speakerKey, textKey = node.textKey }
                    };
                }

                // ✅ Say 没内容：如果 nextId 为空就结束，否则继续走 next
                if (linesToPlay == null || linesToPlay.Length == 0)
                {
                    Debug.LogWarning($"GraphDialogue: Say 节点没有内容 id={node.id}", graph);
                    nodeId = node.nextId;

                    if (string.IsNullOrEmpty(nodeId)) { Close(); return; }
                    StepGraph();
                    return;
                }

                // 记录下一步要去哪（播完再继续）
                nodeId = node.nextId;

                // ✅ 如果 nextId 为空：播完这段就结束（不再挂 ContinueAfterSay）
                if (string.IsNullOrEmpty(nodeId))
                {
                    // 直接播放，播完用户按到最后一句会触发 UI Close；
                    // 我们不需要继续图，所以不要 HookContinueOnClose。
                    ui.Open(linesToPlay);
                    return;
                }

                HookContinueOnClose();
                ui.Open(linesToPlay);
                break;
            }

            case GraphDialogueAsset.NodeType.IfBool:
            {
                bool v = global.GetBool(node.boolKey);
                nodeId = v ? node.trueNextId : node.falseNextId;

                // ✅ 分支结果为空：结束；否则继续
                if (string.IsNullOrEmpty(nodeId)) { Close(); return; }
                StepGraph();
                break;
            }

            case GraphDialogueAsset.NodeType.SetBool:
            {
                global.SetBool(node.boolKey, node.boolValue);
                nodeId = node.nextId;

                // ✅ nextId 为空：结束
                if (string.IsNullOrEmpty(nodeId)) { Close(); return; }
                StepGraph();
                break;
            }

            case GraphDialogueAsset.NodeType.AddInt:
            {
                global.AddInt(node.intKey, node.intValue);
                nodeId = node.nextId;

                if (string.IsNullOrEmpty(nodeId)) { Close(); return; }
                StepGraph();
                break;
            }

            case GraphDialogueAsset.NodeType.SetInt:
            {
                global.SetInt(node.intKey, node.intValue);
                nodeId = node.nextId;

                if (string.IsNullOrEmpty(nodeId)) { Close(); return; }
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

        // 如果中途手动 Close，graph 会被 Clear 掉
        if (graph == null) return;

        // ✅ nodeId 为空：结束（不再强制 End）
        if (string.IsNullOrEmpty(nodeId))
        {
            Close();
            return;
        }

        // 继续下一节点
        StepGraph();
    }
}
