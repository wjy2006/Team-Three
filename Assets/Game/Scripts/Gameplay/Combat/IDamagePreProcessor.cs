using Game.Gameplay.Combat;

public interface IDamagePreprocessor
{
    /// <summary>
    /// return false 表示“这次伤害已被处理/吸收”，Health2D 不再继续扣血与触发后续逻辑
    /// return true  表示“继续走原本扣血流程”（info 可能被修改）
    /// </summary>
    bool PreprocessDamage(ref DamageInfo info);
}
