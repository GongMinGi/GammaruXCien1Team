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
        TimelineSlot[] timeline = ConvertToTimeline(actions);
        if (timeline == null)
        {
            Debug.LogError("Timeline 변환 실패. 전투 중단.", this);
            EndBattleDueToError();
            return;
        }

        IReadOnlyList<BossAction> pattern = bossController.LockedPattern;
        BossAction[] patternCopy = new BossAction[pattern.Count];
        for (int i = 0; i < pattern.Count; i++)
            patternCopy[i] = pattern[i];

        if (!battleExecutor.Execute(timeline, patternCopy, startPos, OnExecutionComplete))
        {
            Debug.LogError("전투 실행 시작 실패. 전투 중단.", this);
            EndBattleDueToError();
            return;
        }

        currentPhase = BattlePhase.Execution;
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

    private void EndBattleDueToError()
    {
        battleEnded = true;
        planningController.enabled = false;
        battleExecutor.ForceStop();
        Debug.LogError("전투가 오류로 인해 중단되었습니다.", this);
    }

    private TimelineSlot[] ConvertToTimeline(List<PlannedAction> actions)
    {
        if (actions == null)
        {
            Debug.LogError("Actions가 null.", this);
            return null;
        }

        TimelineSlot[] timeline = new TimelineSlot[ActionBar.SlotCount];
        for (int i = 0; i < timeline.Length; i++)
            timeline[i] = new TimelineSlot(i);

        int slotCursor = 0;

        foreach (PlannedAction action in actions)
        {
            if (action.Type == ActionType.MergeCards)
            {
                if (action.Cost != 0)
                {
                    Debug.LogError(
                        $"Merge 행동의 Cost가 0이 아님: {action.Cost}", this);
                    return null;
                }
                continue;
            }

            // 공통 범위 검증
            if (action.Cost < 0 || slotCursor + action.Cost > ActionBar.SlotCount)
            {
                Debug.LogError(
                    $"행동이 타임라인 범위 초과: cursor={slotCursor}, cost={action.Cost}", this);
                return null;
            }

            switch (action.Type)
            {
                case ActionType.Move:
                    if (action.Cost != 1)
                    {
                        Debug.LogError(
                            $"Move 행동의 Cost가 1이 아님: {action.Cost}", this);
                        return null;
                    }
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
                    if (action.Cost != 1)
                    {
                        Debug.LogError(
                            $"Stay 행동의 Cost가 1이 아님: {action.Cost}", this);
                        return null;
                    }
                    timeline[slotCursor].HasMainAction = true;
                    timeline[slotCursor].MainAction = action;
                    timeline[slotCursor].Effects.Add(new ScheduledEffect
                    {
                        Type = EffectType.Stay
                    });
                    slotCursor++;
                    break;

                case ActionType.UseCard:
                    if (action.CardData == null)
                    {
                        Debug.LogError("UseCard 행동에 CardData가 null.", this);
                        return null;
                    }
                    if (action.Cost <= 0)
                    {
                        Debug.LogError(
                            $"UseCard 행동의 Cost가 유효하지 않음: {action.Cost}", this);
                        return null;
                    }

                    ScheduledEffect[] effects = ExpandCard(action.CardData);
                    if (effects == null || effects.Length != action.Cost)
                    {
                        Debug.LogError(
                            $"ExpandCard 결과 불일치: expected {action.Cost}, " +
                            $"got {effects?.Length}", this);
                        return null;
                    }

                    for (int i = 0; i < action.Cost; i++)
                    {
                        if (i == 0)
                        {
                            timeline[slotCursor + i].HasMainAction = true;
                            timeline[slotCursor + i].MainAction = action;
                        }
                        timeline[slotCursor + i].Effects.Add(effects[i]);
                    }
                    slotCursor += action.Cost;
                    break;

                default:
                    Debug.LogError(
                        $"지원하지 않는 ActionType: {action.Type}", this);
                    return null;
            }
        }

        if (slotCursor != ActionBar.SlotCount)
        {
            Debug.LogError(
                $"Timeline 슬롯 수 불일치: expected {ActionBar.SlotCount}, " +
                $"got {slotCursor}", this);
            return null;
        }

        return timeline;
    }

    private ScheduledEffect[] ExpandCard(ArcanaData card)
    {
        ArcanaEffectDefinition definition = card.EffectDefinition;

        if (definition == null)
        {
            Debug.LogWarning(
                $"Arcana {card.Id}({card.ArcanaName}): EffectDefinition 미구현. Cast 폴백.",
                card);
            return CreateCastFallback(card);
        }

        ScheduledEffect[] effects = definition.Expand(card);

        if (effects == null || effects.Length != card.BaseCost)
        {
            Debug.LogError(
                $"Arcana {card.Id}: {definition.name} 확장 결과가 유효하지 않음. " +
                $"expected {card.BaseCost}, got {effects?.Length}.",
                definition);
            return null;
        }

        return effects;
    }

    private ScheduledEffect[] CreateCastFallback(ArcanaData card)
    {
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

            if (a.RequiresEffectDefinition && a.EffectDefinition == null)
            {
                Debug.LogError(
                    $"Arcana {a.Id}({a.ArcanaName}): " +
                    $"EffectDefinition이 필수이나 할당되지 않음.", a);
                return false;
            }
        }

        return true;
    }
}
