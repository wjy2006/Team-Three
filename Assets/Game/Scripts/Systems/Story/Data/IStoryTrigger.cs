public interface IStoryTrigger
{
    int Priority { get; }                 // 越大越先
    bool OnEvent(GameEvent evt);          // true = 已处理/消耗（可阻止后续）
}
