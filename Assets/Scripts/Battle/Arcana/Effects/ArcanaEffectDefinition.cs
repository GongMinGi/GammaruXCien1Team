using UnityEngine;

public abstract class ArcanaEffectDefinition : ScriptableObject
{
    /// <summary>
    /// card.BaseCost 길이의 ScheduledEffect[][] 반환 (슬롯당 복수 효과).
    /// card가 null이거나 BaseCost가 유효하지 않으면 예외.
    /// </summary>
    public abstract ScheduledEffect[][] Expand(ArcanaData card);

    public virtual bool RequiresDirection => false;
    public virtual int AllowedDirections => 4;
    public virtual bool InheritsElement => false;
    public virtual bool ConsumesHand => false;
    public virtual int BonusDamagePerConsumedCost => 0;
}
