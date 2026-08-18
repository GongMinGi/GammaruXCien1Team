using UnityEngine;

[CreateAssetMenu(fileName = "Arcana", menuName = "Battle/Arcana Data")]
public class ArcanaData : ScriptableObject
{
    [SerializeField, Range(0, 21)] private int id;
    [SerializeField] private string displayNumber;
    [SerializeField] private string arcanaName;
    [SerializeField] private string koreanName;
    [SerializeField] private ArcanaUsageType usageType;
    [SerializeField, Min(0)] private int baseCost;
    [SerializeField] private Color cardColor = Color.white;
    [SerializeField] private Color timelineColor = new Color(0.7f, 0.3f, 0.5f);
    [SerializeField] private DamageElement defaultElement = DamageElement.Neutral;
    [SerializeField, TextArea(2, 5)] private string effectDescription;

    public int Id => id;
    public string DisplayNumber => displayNumber;
    public string ArcanaName => arcanaName;
    public string KoreanName => koreanName;
    public ArcanaUsageType UsageType => usageType;
    public int BaseCost => baseCost;
    public Color CardColor => cardColor;
    public Color TimelineColor => timelineColor;
    public DamageElement DefaultElement => defaultElement;
    public string EffectDescription => effectDescription;

    public bool CanEnterPool =>
        usageType != ArcanaUsageType.AlwaysAvailable &&
        usageType != ArcanaUsageType.EventOnly;

    public bool CanPlaceOnTimeline =>
        usageType == ArcanaUsageType.Timeline;
}
