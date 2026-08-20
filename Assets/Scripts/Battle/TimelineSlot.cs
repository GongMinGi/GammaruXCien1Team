using System.Collections.Generic;

public sealed class TimelineSlot
{
    public int Index;
    public bool HasMainAction;
    public bool IsActionStart;
    public PlannedAction MainAction;
    public List<ArcanaData> Modifiers;
    public List<ScheduledEffect> Effects;

    public TimelineSlot(int index)
    {
        Index = index;
        HasMainAction = false;
        IsActionStart = false;
        Modifiers = new List<ArcanaData>();
        Effects = new List<ScheduledEffect>();
    }
}
