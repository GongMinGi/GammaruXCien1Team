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

    public Vector2Int GridPosition => new(gridX, gridY);

    private bool isGenerated;
    private ParticleSystem dustParticle;
    private Sprite cachedSprite;
    private Texture2D cachedTexture;

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

        GenerateVisual();
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

    public void UpdateGridPosition(int x, int y)
    {
        GridCell prevCell = gridManager.GetCell(gridX, gridY);
        prevCell?.StopPulse();

        gridX = x;
        gridY = y;
        Vector3 target = gridManager.GridToWorldPosition(gridX, gridY);
        transform.DOKill();
        transform.DOMove(target, moveDuration).SetEase(moveEase);

        GridCell newCell = gridManager.GetCell(gridX, gridY);
        newCell?.StartPulse();

        if (dustParticle != null)
            dustParticle.Play();
    }

    public void CreateGhost()
    {
        DestroyGhost();

        if (cachedSprite == null)
            return;

        ghostGridX = gridX;
        ghostGridY = gridY;

        ghostObject = new GameObject("PlayerGhost");
        ghostObject.transform.position = gridManager.GridToWorldPosition(gridX, gridY);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(ghostObject.transform, false);
        visual.transform.localScale = new Vector3(size, size, 1f);

        ghostRenderer = visual.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = cachedSprite;
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

    private void GenerateVisual()
    {
        if (isGenerated)
            return;

        int texSize = 16;
        cachedTexture = new Texture2D(texSize, texSize);
        cachedTexture.filterMode = FilterMode.Point;

        for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
                cachedTexture.SetPixel(x, y, Color.white);

        cachedTexture.Apply();
        cachedSprite = Sprite.Create(cachedTexture, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0f), texSize);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = cachedSprite;
        renderer.color = playerColor;
        renderer.sortingOrder = 3;

        isGenerated = true;
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

    private void OnDestroy()
    {
        DestroyGhost();
        if (cachedSprite != null) Destroy(cachedSprite);
        if (cachedTexture != null) Destroy(cachedTexture);
    }
}
