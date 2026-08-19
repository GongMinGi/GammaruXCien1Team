public sealed class BossPatternPlan
{
    public BossIntent[] Intents { get; }
    public BossAction[] Actions { get; }

    public BossPatternPlan(BossIntent[] intents, BossAction[] actions)
    {
        Intents = intents;
        Actions = actions;
    }
}
