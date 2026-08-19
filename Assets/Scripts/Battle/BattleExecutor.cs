using System;
using System.Collections;
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

            for (int slot = 0; slot < timeline.Length; slot++)
            {
                bool isDodgingThisSlot = false;

                ProcessPlayerEffects(timeline[slot], ref currentPos, ref isDodgingThisSlot);

                if (bossStats.IsDead)
                    break;

                ProcessBossActions(slot, bossPattern, currentPos, isDodgingThisSlot);

                if (playerStats.IsDead)
                    break;

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
        ref bool isDodging)
    {
        foreach (ScheduledEffect effect in slot.Effects)
        {
            switch (effect.Type)
            {
                case EffectType.Move:
                    currentPos += effect.Direction;
                    playerDisplay.UpdateGridPosition(currentPos.x, currentPos.y);
                    break;

                case EffectType.Stay:
                    break;

                case EffectType.Dodge:
                    isDodging = true;
                    break;

                case EffectType.DealDamage:
                    int damage = combatResolver.ResolvePlayerDamage(
                        effect, playerStats.SpellPower, bossStats.Weakness);
                    bossStats.TakeDamage(damage);
                    break;

                case EffectType.Heal:
                    playerStats.Heal(effect.BaseValue);
                    break;

                case EffectType.Cast:
                    break;

            }
        }
    }

    private void ProcessBossActions(
        int slotIndex,
        BossAction[] bossPattern,
        Vector2Int playerPos,
        bool isDodging)
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
                if (bossAction.isInstantKill)
                    playerStats.InstantKill();
                else
                    playerStats.TakeDamage(combatResolver.ResolveBossDamage(bossAction));
            }
        }
    }
}
