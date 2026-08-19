using System;
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
    [SerializeField] private ClownBossMechanic clownBossMechanic;  // 광대 보스일 때만 연결

    private BattlePhase currentPhase;
    private ArcanaBag bag;
    private CombatResolver combatResolver;
    private int turnNumber;
    private bool battleEnded;
    private bool heldPassiveReviveUsed;

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
        battleExecutor.ClearTowers();
        planningController.PlanningStateChanged += bossController.UpdatePlanningPreview;

        StartTurn();
    }

    private void OnDestroy()
    {
        if (planningController != null && bossController != null)
            planningController.PlanningStateChanged -= bossController.UpdatePlanningPreview;
    }


    private void StartTurn()
    {
        if (battleEnded)
            return;

        turnNumber++;
        currentPhase = BattlePhase.TurnStart;
        Debug.Log($"Turn {turnNumber} started.");

        bossStats.ClearBurn();
        playerHand.DrawToCapacity();

        if (!bossController.GenerateAndShow(playerDisplay.GridPosition))
        {
            Debug.LogError("Boss pattern generation failed. Battle stopped.", this);
            EndBattleDueToError();
            return;
        }

        EnterPlanning();
    }

    private void EnterPlanning()
    {
        currentPhase = BattlePhase.Planning;
        planningController.BeginPlanning(
            playerDisplay.GridPosition, OnPlanConfirmed, bag, selectedArcanaPool);
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

        // 턴 종료 훅 — 수레바퀴 회전 후 효과(피해·실타래) 발동.
        // 광대가 아닌 보스와 싸울 때는 오브젝트가 꺼져 있어 아무 일도 하지 않는다.
        if (clownBossMechanic != null && clownBossMechanic.isActiveAndEnabled &&
            !bossStats.IsDead)
            clownBossMechanic.ApplyTurnEnd();

        if (playerStats.IsDead)
        {
            if (TryHeldPassiveRevive())
            {
                StartTurn();
                return;
            }
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

    private bool TryHeldPassiveRevive()
    {
        if (heldPassiveReviveUsed) return false;

        for (int i = 0; i < playerHand.Cards.Count; i++)
        {
            if (playerHand.Cards[i].UsageType == ArcanaUsageType.HeldPassive)
            {
                playerHand.TryTakeCard(i, out _);
                int reviveHp = Mathf.Max(1, playerStats.MaxHp / 10);
                playerStats.Heal(reviveHp);
                heldPassiveReviveUsed = true;
                Debug.Log($"HeldPassive 발동: 체력 {reviveHp}으로 부활.");
                return true;
            }
        }
        return false;
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
        InstantModifierType pendingCostModifier = InstantModifierType.None;
        bool pendingElementBuff = false;
        DamageElement pendingElement = DamageElement.Neutral;
        DamageElement lastUsedElement = DamageElement.Neutral;

        foreach (PlannedAction action in actions)
        {
            if (action.Type == ActionType.MergeCards ||
                action.Type == ActionType.UseObservationCard)
            {
                if (action.Cost != 0)
                {
                    Debug.LogError(
                        $"{action.Type} 행동의 Cost가 0이 아님: {action.Cost}", this);
                    return null;
                }
                continue;
            }

            if (action.Type == ActionType.UseInstantCard)
            {
                if (action.Cost != 0)
                {
                    Debug.LogError(
                        $"UseInstantCard 행동의 Cost가 0이 아님: {action.Cost}", this);
                    return null;
                }

                switch (action.ModifierType)
                {
                    case InstantModifierType.DamageReduction:
                        InjectDamageReduction(timeline, slotCursor,
                            3, action.CardData);
                        break;
                    case InstantModifierType.DamageSpread:
                        InjectDamageSpread(timeline, slotCursor, 4);
                        break;
                    case InstantModifierType.CostReduction:
                    case InstantModifierType.EffectDuplication:
                        pendingCostModifier = action.ModifierType;
                        break;
                    case InstantModifierType.ElementBuff:
                        pendingElementBuff = true;
                        pendingElement = action.SelectedElement;
                        break;
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

                case ActionType.CutTangle:
                    if (action.Cost != 1)
                    {
                        Debug.LogError(
                            $"CutTangle 행동의 Cost가 1이 아님: {action.Cost}", this);
                        return null;
                    }
                    timeline[slotCursor].HasMainAction = true;
                    timeline[slotCursor].MainAction = action;
                    timeline[slotCursor].Effects.Add(new ScheduledEffect
                    {
                        Type = EffectType.CutTangle
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

                    ScheduledEffect[][] slotEffects = ExpandCard(action.CardData);
                    if (slotEffects == null)
                    {
                        Debug.LogError(
                            $"ExpandCard 실패: {action.CardData.Id}", this);
                        return null;
                    }

                    // 속성 계승: 이전 속성 공격의 원소 적용
                    if (action.CardData.EffectDefinition != null &&
                        action.CardData.EffectDefinition.InheritsElement &&
                        lastUsedElement != DamageElement.Neutral)
                    {
                        ReplaceElement(slotEffects, lastUsedElement);
                    }

                    // 속성 추적: 비-Neutral 카드 사용 시 갱신
                    if (action.CardData.DefaultElement != DamageElement.Neutral)
                        lastUsedElement = action.CardData.DefaultElement;

                    if (action.ConsumedHandCards != null)
                    {
                        int bonusPerCost = action.CardData.EffectDefinition.BonusDamagePerConsumedCost;
                        if (bonusPerCost > 0)
                        {
                            int totalCost = 0;
                            foreach (ArcanaData c in action.ConsumedHandCards)
                                totalCost += c.BaseCost;
                            int bonus = totalCost * bonusPerCost;
                            if (!InjectDealDamageBonus(slotEffects, bonus))
                            {
                                Debug.LogError(
                                    $"Arcana {action.CardData.Id}: ConsumesHand이나 DealDamage 없음.", this);
                                return null;
                            }
                        }
                    }

                    if (pendingCostModifier != InstantModifierType.None ||
                        pendingElementBuff)
                    {
                        bool cardConsumesHand = action.CardData.EffectDefinition != null
                            && action.CardData.EffectDefinition.ConsumesHand;
                        if (!cardConsumesHand)
                        {
                            slotEffects = ApplyInstantModifiers(
                                slotEffects, pendingCostModifier,
                                pendingElementBuff, pendingElement);
                        }
                        pendingCostModifier = InstantModifierType.None;
                        pendingElementBuff = false;
                    }

                    if (slotEffects == null || slotEffects.Length != action.Cost)
                    {
                        Debug.LogError(
                            $"ExpandCard 결과 불일치: expected {action.Cost}, " +
                            $"got {slotEffects?.Length}", this);
                        return null;
                    }

                    int placeTowerIndex = 0;
                    for (int i = 0; i < action.Cost; i++)
                    {
                        if (slotEffects[i] == null || slotEffects[i].Length == 0)
                        {
                            Debug.LogError(
                                $"ExpandCard 결과 슬롯 {i}이 null 또는 빈 배열.", this);
                            return null;
                        }

                        if (i == 0)
                        {
                            timeline[slotCursor + i].HasMainAction = true;
                            timeline[slotCursor + i].MainAction = action;
                        }
                        bool isDuplicatedSlot =
                            action.ConsumedCostModifier == InstantModifierType.EffectDuplication
                            && i == action.Cost - 1;
                        foreach (ScheduledEffect effect in slotEffects[i])
                        {
                            ScheduledEffect e = effect;
                            if (e.Type == EffectType.Move &&
                                action.Direction != Vector2Int.zero)
                            {
                                e.Direction = isDuplicatedSlot && action.DuplicatedDirection != Vector2Int.zero
                                    ? action.DuplicatedDirection
                                    : action.Direction;
                            }
                            else if (e.Type == EffectType.PlaceTower)
                            {
                                e.Direction = placeTowerIndex == 0
                                    ? action.Direction
                                    : action.DuplicatedDirection;
                                placeTowerIndex++;
                            }
                            timeline[slotCursor + i].Effects.Add(e);
                        }
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

    private ScheduledEffect[][] ExpandCard(ArcanaData card)
    {
        ArcanaEffectDefinition definition = card.EffectDefinition;

        if (definition == null)
        {
            Debug.LogWarning(
                $"Arcana {card.Id}({card.ArcanaName}): EffectDefinition 미구현. Cast 폴백.",
                card);
            return CreateCastFallback(card);
        }

        ScheduledEffect[][] effects = definition.Expand(card);

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

    private ScheduledEffect[][] CreateCastFallback(ArcanaData card)
    {
        ScheduledEffect[][] effects = new ScheduledEffect[card.BaseCost][];
        for (int i = 0; i < effects.Length; i++)
        {
            effects[i] = new[] { new ScheduledEffect
            {
                Type = EffectType.Cast,
                SourceCard = card,
                Element = card.DefaultElement
            }};
        }
        return effects;
    }

    private ScheduledEffect[][] ExpandCardWithModifiers(
        ArcanaData card, int effectiveCost,
        InstantModifierType costModifier, bool elementBuff,
        DamageElement element)
    {
        ScheduledEffect[][] effects = ExpandCard(card);
        if (effects == null)
            return null;

        return ApplyInstantModifiers(effects, costModifier, elementBuff, element);
    }

    public static ScheduledEffect[][] ApplyInstantModifiers(
        ScheduledEffect[][] effects,
        InstantModifierType costModifier, bool elementBuff,
        DamageElement element)
    {
        switch (costModifier)
        {
            case InstantModifierType.CostReduction:
                if (effects.Length > 1)
                {
                    ScheduledEffect[][] trimmed =
                        new ScheduledEffect[effects.Length - 1][];
                    Array.Copy(effects, 1, trimmed, 0, trimmed.Length);
                    effects = trimmed;
                }
                break;

            case InstantModifierType.EffectDuplication:
                ScheduledEffect[][] extended =
                    new ScheduledEffect[effects.Length + 1][];
                Array.Copy(effects, extended, effects.Length);
                extended[effects.Length] =
                    (ScheduledEffect[])effects[^1].Clone();
                effects = extended;
                break;
        }

        if (elementBuff)
        {
            for (int s = 0; s < effects.Length; s++)
            {
                for (int e = 0; e < effects[s].Length; e++)
                {
                    if (effects[s][e].Type == EffectType.DealDamage)
                    {
                        effects[s][e].Element = element;
                        return effects;
                    }
                }
            }
        }

        return effects;
    }

    private static void ReplaceElement(ScheduledEffect[][] effects, DamageElement element)
    {
        for (int s = 0; s < effects.Length; s++)
            for (int e = 0; e < effects[s].Length; e++)
            {
                ScheduledEffect eff = effects[s][e];
                eff.Element = element;
                effects[s][e] = eff;
            }
    }

    private static bool InjectDealDamageBonus(ScheduledEffect[][] effects, int bonus)
    {
        for (int s = effects.Length - 1; s >= 0; s--)
        {
            for (int e = 0; e < effects[s].Length; e++)
            {
                if (effects[s][e].Type == EffectType.DealDamage)
                {
                    ScheduledEffect effect = effects[s][e];
                    effect.BaseValue += bonus;
                    effects[s][e] = effect;
                    return true;
                }
            }
        }
        return false;
    }

    private void InjectDamageReduction(
        TimelineSlot[] timeline, int startSlot, int duration,
        ArcanaData sourceCard)
    {
        int reductionValue = sourceCard.InstantValue;
        for (int i = 0; i < duration && startSlot + i < timeline.Length; i++)
        {
            timeline[startSlot + i].Effects.Insert(0, new ScheduledEffect
            {
                Type = EffectType.IncomingDamageModifier,
                BaseValue = reductionValue,
                SourceCard = sourceCard
            });
        }
    }

    private void InjectDamageSpread(
        TimelineSlot[] timeline, int startSlot, int duration)
    {
        if (startSlot >= timeline.Length)
            return;

        int spreadSlots = Mathf.Min(duration, timeline.Length - startSlot);
        timeline[startSlot].Effects.Insert(0, new ScheduledEffect
        {
            Type = EffectType.DamageSpread,
            BaseValue = spreadSlots
        });
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
