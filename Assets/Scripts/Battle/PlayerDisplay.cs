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

    private void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager is not assigned.", this);
            return;
        }

        transform.position = gridManager.GridToWorldPosition(gridX, gridY);
        GenerateVisual();
    }

    public void UpdateGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
        Vector3 target = gridManager.GridToWorldPosition(gridX, gridY);
        transform.DOKill();
        transform.DOMove(target, moveDuration).SetEase(moveEase);
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
}
