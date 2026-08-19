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

    private void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager is not assigned.", this);
            return;
        }

        transform.position = gridManager.GridToWorldPosition(gridX, gridY);
        GenerateVisual();
        CreateDustParticle();

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

    private void GenerateVisual()
    {
        if (isGenerated)
            return;

        int texSize = 16;
        Texture2D tex = new Texture2D(texSize, texSize);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
                tex.SetPixel(x, y, Color.white);

        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0f), texSize);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = playerColor;
        renderer.sortingOrder = 3;

        isGenerated = true;
    }

    private void CreateDustParticle()
    {
        GameObject particleObj = new GameObject("DustParticle");
        particleObj.transform.SetParent(transform, false);
        particleObj.transform.localPosition = Vector3.zero;

        dustParticle = particleObj.AddComponent<ParticleSystem>();
        dustParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = dustParticle.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = 0.3f;
        main.startSpeed = 0.8f;
        main.startSize = 0.08f;
        main.startColor = new Color(0.8f, 0.7f, 0.5f, 0.7f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;

        ParticleSystem.EmissionModule emission = dustParticle.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0f, 6, 10)
        });

        ParticleSystem.ShapeModule shape = dustParticle.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = dustParticle.colorOverLifetime;
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

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = dustParticle.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0.3f);

        ParticleSystemRenderer psRenderer = particleObj.GetComponent<ParticleSystemRenderer>();
        psRenderer.sortingOrder = 4;
    }
}
