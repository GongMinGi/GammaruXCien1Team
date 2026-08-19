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
    [SerializeField] private TangleField tangleField;
    [SerializeField] private float slotDuration = 0.5f;
    [SerializeField] private Color attackHighlightColor = new Color(1f, 0.2f, 0.2f, 0.8f);

    /// 슬롯 시작. 구독자가 boss pattern의 targetCells를 그 자리에서 갱신할 수 있다.
    public event Action<int, BossAction[]> SlotStarted;
    /// 보스 액션이 플레이어에게 명중했을 때
    public event Action<BossAction> BossActionHit;

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
        public GameObject Visual;
    }

    private readonly List<TowerState> towers = new();

    private Texture2D cachedSquareTexture;
    private Texture2D cachedTriangleTexture;
    private Sprite cachedTowerSprite;
    private Sprite cachedDebrisSprite;

    private static readonly Color TowerColor = new Color(0.55f, 0.35f, 0.15f, 0.8f);
    private static readonly Color DebrisColor = new Color(0.45f, 0.3f, 0.15f, 0.6f);

    private struct SlotModifiers
    {
        public int IncomingDamageModifier;
        public bool CounterStanceActive;
        public int CounterStanceBonusPerHit;
        public int AccumulatedBonusDamage;

        // DamageSpread — cross-slot state, NOT reset per slot
        public bool DamageSpreadActive;
        public int DamageSpreadWindowRemaining;
        public bool DamageSpreadHitCaptured;
        public int DamageSpreadPartsRemaining;
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
        EnsureSprites();
    }

    public void ClearTowers()
    {
        foreach (TowerState tower in towers)
            if (tower.Visual != null)
                Destroy(tower.Visual);
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
                SlotStarted?.Invoke(slot, bossPattern);

                if (bossStats.IsDead)
                    break;

                bool isDodgingThisSlot = false;

                ProcessPlayerEffects(timeline[slot], ref currentPos, ref isDodgingThisSlot, ref modifiers);

                if (bossStats.IsDead)
                    break;

                ProcessBossActions(slot, bossPattern, currentPos, isDodgingThisSlot, ref modifiers);
                TickTowerDebris();
                DestroyTowersHitByBoss(slot, bossPattern);

                if (playerStats.IsDead)
                    break;

                if (modifiers.DamageSpreadActive)
                {
                    if (modifiers.DamageSpreadHitCaptured)
                    {
                        int perSlot = modifiers.DeferredDamage /
                                      modifiers.DamageSpreadPartsRemaining;
                        playerStats.TakeDamage(perSlot);
                        modifiers.DeferredDamage -= perSlot;
                        modifiers.DamageSpreadPartsRemaining--;
                        if (modifiers.DamageSpreadPartsRemaining <= 0)
                        {
                            modifiers.DamageSpreadActive = false;
                            if (modifiers.DeferredDamage > 0)
                                playerStats.TakeDamage(modifiers.DeferredDamage);
                            modifiers.DeferredDamage = 0;
                        }
                    }
                    else
                    {
                        modifiers.DamageSpreadWindowRemaining--;
                        if (modifiers.DamageSpreadWindowRemaining <= 0)
                            modifiers.DamageSpreadActive = false;
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
                    // 실타래 위에서는 전차 돌진을 포함해 모든 이동이 막힌다
                    if (tangleField != null && tangleField.Contains(currentPos))
                        break;

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

                case EffectType.CutTangle:
                    if (tangleField != null)
                        tangleField.RemoveContaining(currentPos);
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
                    modifiers.DamageSpreadWindowRemaining = effect.BaseValue;
                    modifiers.DamageSpreadHitCaptured = false;
                    modifiers.DamageSpreadPartsRemaining = effect.AdditionalEffectValue;
                    modifiers.DeferredDamage = 0;
                    break;

                case EffectType.PlaceTower:
                    Vector2Int towerPos = currentPos + effect.Direction;
                    if (gridManager.IsValidCoordinate(towerPos.x, towerPos.y)
                        && !HasTowerOrDebris(towerPos))
                    {
                        towers.Add(new TowerState
                        {
                            Position = towerPos,
                            BonusDamage = effect.BaseValue,
                            Visual = CreateTowerVisual(towerPos, false)
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

            bool playerOnTangle =
                tangleField != null && tangleField.Contains(playerPos);

            // 나이프가 실타래 칸에 명중 — 피해는 무효가 되고 실타래만 소멸한다
            if (bossAction.blockedByTangle && tangleField != null)
            {
                foreach (Vector2Int cell in bossAction.targetCells)
                {
                    if (tangleField.Contains(cell))
                        tangleField.RemoveContaining(cell);
                }
            }

            if (combatResolver.IsHit(playerPos, bossAction.targetCells, isDodging,
                    bossAction.ignoresDodge, bossAction.blockedByTangle, playerOnTangle))
            {
                BossActionHit?.Invoke(bossAction);

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
                    if (modifiers.DamageSpreadActive && !modifiers.DamageSpreadHitCaptured)
                    {
                        modifiers.DeferredDamage = finalDamage;
                        modifiers.DamageSpreadHitCaptured = true;
                    }
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
                        if (tw.Visual != null)
                            Destroy(tw.Visual);
                        tw.Visual = CreateTowerVisual(tw.Position, true);
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
            {
                if (tw.Visual != null)
                    Destroy(tw.Visual);
                towers.RemoveAt(t);
            }
            else
                towers[t] = tw;
        }
    }

    public HashSet<Vector2Int> GetTowerPositions()
    {
        var result = new HashSet<Vector2Int>();
        foreach (TowerState tower in towers)
            result.Add(tower.Position);
        return result;
    }

    private void EnsureSprites()
    {
        if (cachedTowerSprite != null) return;

        int texSize = 16;

        cachedSquareTexture = new Texture2D(texSize, texSize);
        cachedSquareTexture.filterMode = FilterMode.Point;
        for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
                cachedSquareTexture.SetPixel(x, y, Color.white);
        cachedSquareTexture.Apply();
        cachedTowerSprite = Sprite.Create(
            cachedSquareTexture, new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0f), texSize);

        cachedTriangleTexture = new Texture2D(texSize, texSize);
        cachedTriangleTexture.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
                cachedTriangleTexture.SetPixel(x, y, clear);
        for (int y = 0; y < texSize; y++)
        {
            int halfW = (texSize - y) / 2;
            int cx = texSize / 2;
            for (int x = cx - halfW; x < cx + halfW; x++)
                if (x >= 0 && x < texSize)
                    cachedTriangleTexture.SetPixel(x, y, Color.white);
        }
        cachedTriangleTexture.Apply();
        cachedDebrisSprite = Sprite.Create(
            cachedTriangleTexture, new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0f), texSize);
    }

    private GameObject CreateTowerVisual(Vector2Int gridPos, bool isDebris)
    {
        GameObject obj = new GameObject(isDebris ? "TowerDebris" : "TowerVisual");
        obj.transform.SetParent(transform, false);
        obj.transform.position = gridManager.GridToWorldPosition(gridPos.x, gridPos.y);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(obj.transform, false);
        visual.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = isDebris ? cachedDebrisSprite : cachedTowerSprite;
        renderer.color = isDebris ? DebrisColor : TowerColor;
        renderer.sortingOrder = 2;

        return obj;
    }

    private void OnDestroy()
    {
        if (cachedTowerSprite != null) Destroy(cachedTowerSprite);
        if (cachedDebrisSprite != null) Destroy(cachedDebrisSprite);
        if (cachedSquareTexture != null) Destroy(cachedSquareTexture);
        if (cachedTriangleTexture != null) Destroy(cachedTriangleTexture);
    }
}
