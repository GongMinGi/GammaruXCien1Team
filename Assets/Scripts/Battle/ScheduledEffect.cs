using UnityEngine;

public enum MovementPresentation
{
    Slide,
    Teleport
}

public enum EffectType
{
    Move,
    Stay,
    DealDamage,
    Heal,
    Dodge,
    Cast,
    IncomingDamageModifier,
    CounterStance,
    ApplyBurn,
    DamageSpread,
    PlaceTower,
    CutTangle
}

[System.Serializable]
public struct ScheduledEffect
{
    public EffectType Type;
    public MovementPresentation MovementPresentation;
    public Vector2Int Direction;
    public int BaseValue;
    public float SpellPowerCoefficient;
    public int AdditionalEffectValue;
    public DamageElement Element;
    public ArcanaData SourceCard;
}
