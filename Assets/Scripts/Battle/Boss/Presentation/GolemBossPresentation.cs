using System.Collections;
using UnityEngine;

public class GolemBossPresentation : MonoBehaviour
{
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private GameObject fallingStonePrefab;
    [SerializeField] private GameObject groundExplosionPrefab;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BattleExecutor battleExecutor;
    [SerializeField] private BossStats bossStats;
    [SerializeField] private BossData expectedBossData;
    [SerializeField] private Vector3 fallingStoneOffset = new Vector3(0f, 0.2f, 0f);

    [Header("Camera Shake")]
    [SerializeField] private float sustainedAmplitude = 0.03f;
    [SerializeField] private float sustainedDuration = 1.5f;
    [SerializeField] private float impactAmplitude = 0.15f;
    [SerializeField] private float impactDecaySpeed = 8f;

    private Vector3 lastShakeOffset;
    private float currentSustainedAmplitude;
    private float currentImpactAmplitude;
    private float sustainedEndTime;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (bossStats == null || bossStats.Data != expectedBossData)
        {
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (battleExecutor == null) return;
        battleExecutor.BossAnimationRequested += OnAnimationRequested;
        battleExecutor.BossImpactRequested += OnImpactRequested;
    }

    private void OnDisable()
    {
        if (battleExecutor == null) return;
        battleExecutor.BossAnimationRequested -= OnAnimationRequested;
        battleExecutor.BossImpactRequested -= OnImpactRequested;
        StopShake();
    }

    private void OnAnimationRequested(BossAnimationCue cue)
    {
        if (bossAnimator == null) return;

        switch (cue)
        {
            case BossAnimationCue.Primary:
                bossAnimator.SetTrigger("OneHand");
                break;
            case BossAnimationCue.Heavy:
                bossAnimator.SetTrigger("TwoHands");
                break;
            case BossAnimationCue.Ultimate:
                bossAnimator.SetTrigger("Roar");
                StartSustainedShake();
                break;
        }
    }

    private void OnImpactRequested(BossAction action)
    {
        if (action.targetCells == null) return;

        GameObject prefab = action.impactEffect switch
        {
            BossImpactEffect.FallingStone => fallingStonePrefab,
            BossImpactEffect.GroundBurst => groundExplosionPrefab,
            BossImpactEffect.ShockwaveRing => groundExplosionPrefab,
            _ => null
        };

        if (prefab == null) return;

        foreach (Vector2Int cell in action.targetCells)
        {
            Vector3 worldPos = gridManager.GridToWorldPosition(cell.x, cell.y);
            if (action.impactEffect == BossImpactEffect.FallingStone)
                worldPos += fallingStoneOffset;
            Instantiate(prefab, worldPos, Quaternion.identity);
        }

        if (action.impactEffect == BossImpactEffect.ShockwaveRing)
            TriggerImpactShake();
    }

    private void StartSustainedShake()
    {
        currentSustainedAmplitude = sustainedAmplitude;
        sustainedEndTime = Time.time + sustainedDuration;
        EnsureShakeRunning();
    }

    private void TriggerImpactShake()
    {
        currentImpactAmplitude = impactAmplitude;
        EnsureShakeRunning();
    }

    private void EnsureShakeRunning()
    {
        if (shakeCoroutine == null)
            shakeCoroutine = StartCoroutine(ShakeLoop());
    }

    private IEnumerator ShakeLoop()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            shakeCoroutine = null;
            yield break;
        }

        Transform camTransform = cam.transform;

        while (currentSustainedAmplitude > 0f ||
               currentImpactAmplitude > Mathf.Epsilon)
        {
            camTransform.position -= lastShakeOffset;

            if (Time.time >= sustainedEndTime)
                currentSustainedAmplitude = 0f;

            float totalAmp = currentSustainedAmplitude + currentImpactAmplitude;
            float time = Time.time;
            float x = (Mathf.PerlinNoise(time * 25f, 0f) - 0.5f) * 2f * totalAmp;
            float y = (Mathf.PerlinNoise(0f, time * 25f) - 0.5f) * 2f * totalAmp;

            lastShakeOffset = new Vector3(x, y, 0f);
            camTransform.position += lastShakeOffset;

            currentImpactAmplitude = Mathf.MoveTowards(
                currentImpactAmplitude, 0f, impactDecaySpeed * Time.deltaTime);

            yield return null;
        }

        camTransform.position -= lastShakeOffset;
        lastShakeOffset = Vector3.zero;
        shakeCoroutine = null;
    }

    private void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        Camera cam = Camera.main;
        if (cam != null)
            cam.transform.position -= lastShakeOffset;

        lastShakeOffset = Vector3.zero;
        currentSustainedAmplitude = 0f;
        currentImpactAmplitude = 0f;
    }
}
