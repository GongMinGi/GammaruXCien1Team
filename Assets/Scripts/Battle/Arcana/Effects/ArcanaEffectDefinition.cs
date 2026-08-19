using UnityEngine;

public abstract class ArcanaEffectDefinition : ScriptableObject
{
    /// <summary>
    /// card.BaseCost 길이의 ScheduledEffect 배열 반환.
    /// card가 null이거나 BaseCost가 유효하지 않으면 예외.
    /// </summary>
    public abstract ScheduledEffect[] Expand(ArcanaData card);
}
