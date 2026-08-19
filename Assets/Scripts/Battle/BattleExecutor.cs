using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleExecutor : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerDisplay playerDisplay;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private BossStats bossStats;
    [SerializeField] private float slotDuration = 0.5f;
    [SerializeField] private Color attackHighlightColor = new Color(1f, 0.2f, 0.2f, 0.8f);

    private CombatResolver combatResolver;
    private Coroutine executionCoroutine;
    private Action completionCallback;
    private bool isExecuting;

    private struct TowerState
    {
        public Vector2Int Position;
        public int BonusDamage;
        public bool IsDebris;
        public int DebrisRemainingSlots;
    }

    private readonly List<TowerState> towers = new();

    private struct SlotModifiers
    {
        public int IncomingDamageModifier;
        public bool CounterStanceActive;
        public int CounterStanceBonusPerHit;
        public int AccumulatedBonusDamage;

        // DamageSpread — cross-slot state, NOT reset per slot
        public bool DamageSpreadActive;
        public int DamageSpreadRemainingSlots;
        public int DeferredDamage;

        public void ResetPerSlot()
        {
            IncomingDamageModifier = 0;
            CounterStanceActive = false;
            CounterStanceBonusPerHit = 0;
        }
    }

    public bool ValidateReferences()
    {
        if (gridManager == null || playerDisplay == null ||
            playerStats == null || bossStats == null)
        {
            Debug.LogError("BattleExecutor references are not assigned.", this);
            return false;
        }

        return true;
    }

    public void Initialize(CombatResolver resolver)
    {
        combatResolver = resolver;
    }

    public void ClearTowers()
    {
        towers.Clear();
    }

    public bool Execute(
        TimelineSlot[] timeline,
        BossAction[] bossPattern,
        Vector2Int startPos,
        Action onComplete)
    {
        if (executionCoroutine != null)
        {
            Debug.LogWarning("BattleExecutor: 이미 실행 중. 중복 호출 무시.", this);
            return false;
        }

        if (timeline == null || timeline.Length != ActionBar.SlotCount)
        {
            Debug.LogError("BattleExecutor: 유효하지 않은 timeline.", this);
            return false;
        }

        if (bossPattern == null)
        {
            Debug.LogError("BattleExecutor: boss pattern이 null.", this);
            return false;
        }

        if (combatResolver == null)
        {
            Debug.LogError("BattleExecutor: Initialize() 미호출.", this);
            return false;
        }

        isExecuting = true;
        completionCallback = onComplete;
        executionCoroutine = StartCoroutine(
            RunTimeline(timeline, bossPattern, startPos));
        return true;
    }

    public void ForceStop()
    {
        Coroutine coroutine = executionCoroutine;
        CleanupExecution();

        if (coroutine != null)
            StopCoroutine(coroutine);
    }

    private void CleanupExecution()
    {
        executionCoroutine = null;
        completionCallback = null;
        isExecuting = false;
        if (gridManager != null)
            gridManager.ClearAllHighlights();
    }

    private void CompleteExecution()
    {
        Action callback = completionCallback;
        CleanupExecution();
        callback?.Invoke();
    }

    private IEnumerator RunTimeline(
        TimelineSlot[] timeline,
        BossAction[] bossPattern,
        Vector2Int startPos)
    {
        bool completedNormally = false;
        try
        {
            Vector2Int currentPos = startPos;
            playerDisplay.UpdateGridPosition(startPos.x, startPos.y);
            yield return new WaitForSeconds(0.3f);

            SlotModifiers modifiers = default;

            for (int slot = 0; slot < timeline.Length; slot++)
            {
                modifiers.ResetPerSlot();

                bossStats.ProcessBurn();
                if (bossStats.IsDead)
                    break;

                bool isDodgingThisSlot = false;

                ProcessPlayerEffects(timeline[slot], ref currentPos, ref isDodgingThisSlot, ref modifiers);

                if (bossStats.IsDead)
                    break;

                ProcessBossActions(slot, bossPattern, currentPos, isDodgingThisSlot, ref modifiers);
                DestroyTowersHitByBoss(slot, bossPattern);
                TickTowerDebris();

                if (playerStats.IsDead)
                    break;

                if (modifiers.DamageSpreadActive)
                {
                    int perSlot = modifiers.DeferredDamage /
                                  modifiers.DamageSpreadRemainingSlots;
                    playerStats.TakeDamage(perSlot);
                    modifiers.DeferredDamage -= perSlot;
                    modifiers.DamageSpreadRemainingSlots--;
                    if (modifiers.DamageSpreadRemainingSlots <= 0)
                    {
                        modifiers.DamageSpreadActive = false;
                        if (modifiers.DeferredDamage > 0)
                            playerStats.TakeDamage(modifiers.DeferredDamage);
                        modifiers.DeferredDamage = 0;
                    }

                    if (playerStats.IsDead)
                        break;
                }

                yield return new WaitForSeconds(slotDuration);
                gridManager.ClearAllHighlights();
            }

            completedNormally = true;
        }
        finally
        {
            if (!completedNormally)
                CleanupExecution();
        }

        CompleteExecution();
    }

    private void ProcessPlayerEffects(
        TimelineSlot slot,
        ref Vector2Int currentPos,
        ref bool isDodging,
        ref SlotModifiers modifiers)
    {
        foreach (ScheduledEffect effect in slot.Effects)
        {
            switch (effect.Type)
            {
                case EffectType.Move:
                    int moveDist = effect.BaseValue > 0 ? effect.BaseValue : 1;
                    Vector2Int moveTarget = currentPos;
                    for (int step = 1; step <= moveDist; step++)
                    {
                        Vector2Int check = currentPos + effect.Direction * step;
                        if (!gridManager.IsValidCoordinate(check.x, check.y))
                            break;
                        if (HasTowerOrDebris(check))
                            break;
                        moveTarget = check;
                    }
                    if (moveTarget != currentPos)
                    {
                        currentPos = moveTarget;
                        playerDisplay.UpdateGridPosition(currentPos.x, currentPos.y);
                    }
                    break;

                case EffectType.Stay:
                    break;

                case EffectType.Dodge:
                    isDodging = true;
                    break;

                case EffectType.DealDamage:
                    int damage = combatResolver.ResolvePlayerDamage(
                        effect, playerStats.SpellPower, bossStats.Weakness);
                    damage += modifiers.AccumulatedBonusDamage;
                    modifiers.AccumulatedBonusDamage = 0;
                    damage += CalculateTowerBonus(effect.Element);
                    bossStats.TakeDamage(damage);
                    break;

                case EffectType.Heal:
                    playerStats.Heal(effect.BaseValue);
                    break;

                case EffectType.Cast:
                    break;

                case EffectType.IncomingDamageModifier:
                    modifiers.IncomingDamageModifier += effect.BaseValue;
                    break;

                case EffectType.CounterStance:
                    modifiers.CounterStanceActive = true;
                    modifiers.CounterStanceBonusPerHit = effect.BaseValue;
                    break;

                case EffectType.ApplyBurn:
                    bossStats.AddBurnStacks(effect.BaseValue);
                    break;

                case EffectType.DamageSpread:
                    modifiers.DamageSpreadActive = true;
                    modifiers.DamageSpreadRemainingSlots = effect.BaseValue;
                    modifiers.DeferredDamage = 0;
                    break;

                case EffectType.PlaceTower:
                    Vector2Int towerPos = currentPos + effect.Direction;
                    if (gridManager.IsValidCoordinate(towerPos.x, towerPos.y)
                        && !HasTowerAt(towerPos))
                    {
                        towers.Add(new TowerState
                        {
                            Position = towerPos,
                            BonusDamage = effect.BaseValue
                        });
                    }
                    break;
            }
        }
    }

    private void ProcessBossActions(
        int slotIndex,
        BossAction[] bossPattern,
        Vector2Int playerPos,
        bool isDodging,
        ref SlotModifiers modifiers)
    {
        foreach (BossAction bossAction in bossPattern)
        {
            if (bossAction.timingSlot != slotIndex)
                continue;

            if (bossAction.targetCells == null)
                continue;

            foreach (Vector2Int cell in bossAction.targetCells)
            {
                GridCell gridCell = gridManager.GetCell(cell.x, cell.y);
                gridCell?.SetHighlight(attackHighlightColor);
            }

            if (combatResolver.IsHit(playerPos, bossAction.targetCells, isDodging))
            {
                if (modifiers.CounterStanceActive)
                    modifiers.AccumulatedBonusDamage += modifiers.CounterStanceBonusPerHit;

                if (bossAction.isInstantKill)
                {
                    playerStats.InstantKill();
                }
                else
                {
                    int rawDamage = combatResolver.ResolveBossDamage(bossAction);
                    int finalDamage = Mathf.Max(0, rawDamage + modifiers.IncomingDamageModifier);
                    if (modifiers.DamageSpreadActive)
                        modifiers.DeferredDamage += finalDamage;
                    else
                        playerStats.TakeDamage(finalDamage);
                }
            }
        }
    }

    private bool HasTowerOrDebris(Vector2Int pos)
    {
        foreach (TowerState tower in towers)
            if (tower.Position == pos)
                return true;
        return false;
    }

    private bool HasTowerAt(Vector2Int pos)
    {
        foreach (TowerState tower in towers)
            if (tower.Position == pos && !tower.IsDebris)
                return true;
        return false;
    }

    private int CalculateTowerBonus(DamageElement attackElement)
    {
        int bonus = 0;
        foreach (TowerState tower in towers)
        {
            if (tower.IsDebris) continue;
            int towerDmg = tower.BonusDamage;
            if (attackElement != DamageElement.Neutral
                && attackElement == bossStats.Weakness)
                towerDmg = Mathf.RoundToInt(towerDmg * 1.2f);
            bonus += towerDmg;
        }
        return bonus;
    }

    private void DestroyTowersHitByBoss(int slotIndex, BossAction[] pattern)
    {
        foreach (BossAction action in pattern)
        {
            if (action.timingSlot != slotIndex || action.targetCells == null)
                continue;

            for (int t = 0; t < towers.Count; t++)
            {
                if (towers[t].IsDebris) continue;

                foreach (Vector2Int cell in action.targetCells)
                {
                    if (cell == towers[t].Position)
                    {
                        TowerState tw = towers[t];
                        tw.IsDebris = true;
                        tw.DebrisRemainingSlots = 3;
                        towers[t] = tw;
                    }
                }
            }
        }
    }

    private void TickTowerDebris()
    {
        for (int t = towers.Count - 1; t >= 0; t--)
        {
            if (!towers[t].IsDebris) continue;

            TowerState tw = towers[t];
            tw.DebrisRemainingSlots--;
            if (tw.DebrisRemainingSlots <= 0)
                towers.RemoveAt(t);
            else
                towers[t] = tw;
        }
    }
}
