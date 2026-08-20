using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class PlayerDisplay : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField, Range(-2, 2)] private int gridX = 0;
    [SerializeField, Range(-2, 2)] private int gridY = 0;
    [SerializeField, Min(0.01f)] private float size = 0.5f;
    [SerializeField] private Color playerColor = Color.white;
    [SerializeField] private float moveDuration = 0.15f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    [Header("Teleport")]
    [SerializeField, Min(0.01f)] private float teleportFadeDuration = 0.12f;
    [SerializeField, Min(0f)] private float teleportPause = 0.05f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private Sprite idleSprite;

    public Vector2Int GridPosition => new(gridX, gridY);
    public event Action<bool> MovementStateChanged;

    private ParticleSystem dustParticle;
    private Vector3 playerVisualBaseScale;
    private Color playerVisualBaseColor;
    private Coroutine teleportCoroutine;
    private Tween moveTween;
    private bool isMoving;

    private GameObject ghostObject;
    private SpriteRenderer ghostRenderer;
    private int ghostGridX, ghostGridY;
    private ParticleSystem ghostDustParticle;

    private void Awake()
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager is not assigned.", this);
            return;
        }

        if (playerRenderer == null || idleSprite == null)
        {
            Debug.LogError("playerRenderer or idleSprite is not assigned.", this);
            return;
        }

        playerRenderer.sprite = idleSprite;
        playerRenderer.color = playerColor;
        playerRenderer.sortingOrder = 3;
        playerVisualBaseScale = playerRenderer.transform.localScale;
        playerVisualBaseColor = playerColor;
    }

    private void Start()
    {
        if (gridManager == null)
            return;

        transform.position = gridManager.GridToWorldPosition(gridX, gridY);
        dustParticle = CreateDustParticle(transform);

        GridCell startCell = gridManager.GetCell(gridX, gridY);
        startCell?.StartPulse();
    }

    private void SetMoving(bool value)
    {
        if (isMoving == value) return;
        isMoving = value;
        MovementStateChanged?.Invoke(value);
    }

    private void StopMovement()
    {
        Tween tween = moveTween;
        moveTween = null;
        tween?.Kill();
        SetMoving(false);
    }

    public void CancelMovement()
    {
        CleanupTeleport();
        StopMovement();
    }

    public void UpdateGridPosition(int x, int y)
    {
        CleanupTeleport();
        StopMovement();
        if (x == gridX && y == gridY) return;

        GridCell prevCell = gridManager.GetCell(gridX, gridY);
        prevCell?.StopPulse();

        gridX = x;
        gridY = y;
        Vector3 target = gridManager.GridToWorldPosition(gridX, gridY);

        Tween tween = transform.DOMove(target, moveDuration).SetEase(moveEase);
        moveTween = tween;
        SetMoving(true);
        tween.OnComplete(() =>
        {
            if (moveTween != tween) return;
            moveTween = null;
            SetMoving(false);
        });

        GridCell newCell = gridManager.GetCell(gridX, gridY);
        newCell?.StartPulse();

        if (dustParticle != null)
            dustParticle.Play();
    }

    public void SetGridPositionImmediate(int x, int y)
    {
        CleanupTeleport();
        StopMovement();

        GridCell prevCell = gridManager.GetCell(gridX, gridY);
        prevCell?.StopPulse();

        gridX = x;
        gridY = y;
        transform.position = gridManager.GridToWorldPosition(gridX, gridY);

        GridCell newCell = gridManager.GetCell(gridX, gridY);
        newCell?.StartPulse();
    }

    public void TeleportToGridPosition(int x, int y)
    {
        StopMovement();

        GridCell prevCell = gridManager.GetCell(gridX, gridY);
        prevCell?.StopPulse();

        gridX = x;
        gridY = y;

        CleanupTeleport();
        teleportCoroutine = StartCoroutine(TeleportSequence());
    }

    private IEnumerator TeleportSequence()
    {
        Vector3 target = gridManager.GridToWorldPosition(gridX, gridY);
        Transform visual = playerRenderer.transform;

        float elapsed = 0f;
        while (elapsed < teleportFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / teleportFadeDuration);
            visual.localScale = playerVisualBaseScale * (1f - t);
            playerRenderer.color = new Color(
                playerVisualBaseColor.r, playerVisualBaseColor.g,
                playerVisualBaseColor.b, playerVisualBaseColor.a * (1f - t));
            yield return null;
        }

        transform.position = target;

        if (teleportPause > 0f)
            yield return new WaitForSeconds(teleportPause);

        elapsed = 0f;
        while (elapsed < teleportFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / teleportFadeDuration);
            visual.localScale = playerVisualBaseScale * t;
            playerRenderer.color = new Color(
                playerVisualBaseColor.r, playerVisualBaseColor.g,
                playerVisualBaseColor.b, playerVisualBaseColor.a * t);
            yield return null;
        }

        visual.localScale = playerVisualBaseScale;
        playerRenderer.color = playerVisualBaseColor;
        teleportCoroutine = null;

        GridCell newCell = gridManager.GetCell(gridX, gridY);
        newCell?.StartPulse();

        if (dustParticle != null)
            dustParticle.Play();
    }

    private void CleanupTeleport()
    {
        if (teleportCoroutine != null)
        {
            StopCoroutine(teleportCoroutine);
            teleportCoroutine = null;
        }
        if (playerRenderer != null)
        {
            playerRenderer.transform.localScale = playerVisualBaseScale;
            playerRenderer.color = playerVisualBaseColor;
        }
    }

    public void CreateGhost()
    {
        DestroyGhost();

        if (idleSprite == null)
            return;

        ghostGridX = gridX;
        ghostGridY = gridY;

        ghostObject = new GameObject("PlayerGhost");
        ghostObject.transform.position = gridManager.GridToWorldPosition(gridX, gridY);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(ghostObject.transform, false);
        visual.transform.localScale = playerVisualBaseScale;

        ghostRenderer = visual.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = idleSprite;
        ghostRenderer.flipX = playerRenderer.flipX;
        ghostRenderer.color = new Color(playerColor.r, playerColor.g, playerColor.b, 0.4f);
        ghostRenderer.sortingOrder = 2;

        ghostDustParticle = CreateDustParticle(ghostObject.transform);
    }

    public void DestroyGhost()
    {
        if (ghostObject == null)
            return;

        bool sameCell = ghostGridX == gridX && ghostGridY == gridY;
        if (!sameCell)
        {
            GridCell ghostCell = gridManager.GetCell(ghostGridX, ghostGridY);
            ghostCell?.StopPulse();
        }

        ghostObject.transform.DOKill();
        Destroy(ghostObject);
        ghostObject = null;
        ghostRenderer = null;
        ghostDustParticle = null;
    }

    public void UpdateGhostPosition(int x, int y)
    {
        if (ghostObject == null)
            return;

        bool prevSameAsPlayer = ghostGridX == gridX && ghostGridY == gridY;
        if (!prevSameAsPlayer)
        {
            GridCell prevCell = gridManager.GetCell(ghostGridX, ghostGridY);
            prevCell?.StopPulse();
        }

        ghostGridX = x;
        ghostGridY = y;

        Vector3 target = gridManager.GridToWorldPosition(x, y);
        ghostObject.transform.DOKill();
        ghostObject.transform.DOMove(target, moveDuration).SetEase(moveEase);

        bool newSameAsPlayer = ghostGridX == gridX && ghostGridY == gridY;
        if (!newSameAsPlayer)
        {
            GridCell newCell = gridManager.GetCell(ghostGridX, ghostGridY);
            newCell?.StartPulse();
        }

        if (ghostDustParticle != null)
            ghostDustParticle.Play();
    }

    private ParticleSystem CreateDustParticle(Transform parent)
    {
        GameObject particleObj = new GameObject("DustParticle");
        particleObj.transform.SetParent(parent, false);
        particleObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = 0.3f;
        main.startSpeed = 0.8f;
        main.startSize = 0.08f;
        main.startColor = new Color(0.8f, 0.7f, 0.5f, 0.7f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0f, 6, 10)
        });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.8f, 0.7f, 0.5f), 0f),
                new GradientColorKey(new Color(0.8f, 0.7f, 0.5f), 1f)
            },
            new[] {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0.3f);

        ParticleSystemRenderer psRenderer = particleObj.GetComponent<ParticleSystemRenderer>();
        psRenderer.sortingOrder = 4;

        return ps;
    }

    private void OnDisable()
    {
        CancelMovement();
    }

    private void OnDestroy()
    {
        CancelMovement();
        DestroyGhost();
    }
}
