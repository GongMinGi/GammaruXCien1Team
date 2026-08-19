using System.Collections.Generic;
using UnityEngine;

public class BattleFlowController : MonoBehaviour
{
    [SerializeField] private PlanningController planningController;
    [SerializeField] private BattleExecutor battleExecutor;
    [SerializeField] private BossController bossController;
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private BossStats bossStats;
    [SerializeField] private PlayerDisplay playerDisplay;
    [SerializeField] private ArcanaCatalog arcanaCatalog;
    [SerializeField] private ArcanaData[] selectedArcanaPool;

    private BattlePhase currentPhase;
    private ArcanaBag bag;
    private CombatResolver combatResolver;
    private int turnNumber;
    private bool battleEnded;

    private void Start()
    {
        ArcanaData[] transferredArcanaCards = BattleLoadoutData.TakeSelectedArcanaCards();
        if (transferredArcanaCards != null)
            selectedArcanaPool = transferredArcanaCards;

        if (!ValidateReferences() || !ValidatePool())
        {
            enabled = false;
            return;
        }

        bag = new ArcanaBag(selectedArcanaPool);
        playerHand.Initialize(bag);
        combatResolver = new CombatResolver();
        battleExecutor.Initialize(combatResolver);
        StartTurn();
    }

    private void StartTurn()
    {
        if (battleEnded)
            return;

        turnNumber++;
        currentPhase = BattlePhase.TurnStart;
        Debug.Log($"턴 {turnNumber} 시작");

        playerHand.DrawToCapacity();
        bossController.GenerateAndShow();

        EnterObservation();
    }

    private void EnterObservation()
    {
        currentPhase = BattlePhase.Observation;
        // MVP: 빈 패스스루 — 관측 전용 카드 처리는 Phase E5에서 추가
        EnterPlanning();
    }

    private void EnterPlanning()
    {
        currentPhase = BattlePhase.Planning;
        planningController.BeginPlanning(playerDisplay.GridPosition, OnPlanConfirmed);
    }

    private void OnPlanConfirmed(List<PlannedAction> actions, Vector2Int startPos)
    {
        currentPhase = BattlePhase.Execution;

        TimelineSlot[] timeline = ConvertToTimeline(actions);

        IReadOnlyList<BossAction> pattern = bossController.LockedPattern;
        BossAction[] patternCopy = new BossAction[pattern.Count];
        for (int i = 0; i < pattern.Count; i++)
            patternCopy[i] = pattern[i];

        battleExecutor.Execute(timeline, patternCopy, startPos, OnExecutionComplete);
    }

    private void OnExecutionComplete()
    {
        currentPhase = BattlePhase.TurnEnd;

        if (playerStats.IsDead)
        {
            EndBattle(false);
            return;
        }

        if (bossStats.IsDead)
        {
            EndBattle(true);
            return;
        }

        StartTurn();
    }

    private void EndBattle(bool victory)
    {
        battleEnded = true;
        battleExecutor.ForceStop();

        if (victory)
            Debug.Log("전투 승리!");
        else
            Debug.Log("전투 패배...");
    }

    private TimelineSlot[] ConvertToTimeline(List<PlannedAction> actions)
    {
        TimelineSlot[] timeline = new TimelineSlot[ActionBar.SlotCount];
        for (int i = 0; i < timeline.Length; i++)
            timeline[i] = new TimelineSlot(i);

        int slotCursor = 0;

        foreach (PlannedAction action in actions)
        {
            if (action.Type == ActionType.MergeCards)
                continue;

            switch (action.Type)
            {
                case ActionType.Move:
                    timeline[slotCursor].HasMainAction = true;
                    timeline[slotCursor].MainAction = action;
                    timeline[slotCursor].Effects.Add(new ScheduledEffect
                    {
                        Type = EffectType.Move,
                        Direction = action.Direction
                    });
                    slotCursor++;
                    break;

                case ActionType.Stay:
                    timeline[slotCursor].HasMainAction = true;
                    timeline[slotCursor].MainAction = action;
                    timeline[slotCursor].Effects.Add(new ScheduledEffect
                    {
                        Type = EffectType.Stay
                    });
                    slotCursor++;
                    break;

                case ActionType.UseCard:
                    ScheduledEffect[] effects = ExpandCard(action.CardData);
                    for (int i = 0; i < action.Cost; i++)
                    {
                        if (i == 0)
                        {
                            timeline[slotCursor + i].HasMainAction = true;
                            timeline[slotCursor + i].MainAction = action;
                        }

                        if (i < effects.Length)
                            timeline[slotCursor + i].Effects.Add(effects[i]);
                    }
                    slotCursor += action.Cost;
                    break;
            }
        }

        Debug.Assert(slotCursor == ActionBar.SlotCount,
            $"Timeline slot count mismatch: expected {ActionBar.SlotCount}, got {slotCursor}");

        return timeline;
    }

    private ScheduledEffect[] ExpandCard(ArcanaData card)
    {
        // MVP: 전부 Cast 반환. Phase D에서 카드별 효과 추가.
        ScheduledEffect[] effects = new ScheduledEffect[card.BaseCost];
        for (int i = 0; i < effects.Length; i++)
        {
            effects[i] = new ScheduledEffect
            {
                Type = EffectType.Cast,
                SourceCard = card,
                Element = card.DefaultElement
            };
        }
        return effects;
    }

    private bool ValidateReferences()
    {
        if (planningController == null || battleExecutor == null ||
            bossController == null || playerHand == null ||
            playerStats == null || bossStats == null ||
            playerDisplay == null || arcanaCatalog == null)
        {
            Debug.LogError("BattleFlowController references are not assigned.", this);
            return false;
        }

        if (!planningController.ValidateReferences())
            return false;

        if (!battleExecutor.ValidateReferences())
            return false;

        if (!bossController.ValidateReferences())
            return false;

        return true;
    }

    private bool ValidatePool()
    {
        if (selectedArcanaPool == null || selectedArcanaPool.Length == 0)
        {
            Debug.LogError("Arcana pool is empty.", this);
            return false;
        }

        HashSet<int> ids = new();
        foreach (ArcanaData a in selectedArcanaPool)
        {
            if (a == null)
            {
                Debug.LogError("Pool has null entry.", this);
                return false;
            }

            if (!a.CanEnterPool)
            {
                Debug.LogError($"Arcana {a.DisplayNumber} cannot enter pool.", this);
                return false;
            }

            if (!ids.Add(a.Id))
            {
                Debug.LogError($"Duplicate ID: {a.Id}", this);
                return false;
            }

            if (a.CanPlaceOnTimeline &&
                (a.BaseCost < 1 || a.BaseCost > ActionBar.SlotCount))
            {
                Debug.LogError($"Arcana {a.Id} invalid cost: {a.BaseCost}", this);
                return false;
            }

            ArcanaData catalogEntry = arcanaCatalog.GetById(a.Id);
            if (catalogEntry == null)
            {
                Debug.LogError($"Arcana {a.Id} missing from catalog.", this);
                return false;
            }

            if (catalogEntry != a)
            {
                Debug.LogError($"Pool Arcana {a.Id} doesn't match catalog asset.", this);
                return false;
            }
        }

        return true;
    }
}
