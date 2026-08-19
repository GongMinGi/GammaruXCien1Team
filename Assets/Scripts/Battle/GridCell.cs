using UnityEngine;

public class GridCell : MonoBehaviour
{
    [SerializeField] private int x;
    [SerializeField] private int y;

    private SpriteRenderer visualRenderer;
    private Color originalColor;

    public int X => x;
    public int Y => y;

    public void Initialize(int x, int y, SpriteRenderer renderer)
    {
        this.x = x;
        this.y = y;
        visualRenderer = renderer;
        originalColor = renderer.color;
    }

    public void SetHighlight(Color color)
    {
        if (visualRenderer != null)
            visualRenderer.color = color;
    }

    public void ClearHighlight()
    {
        if (visualRenderer != null)
            visualRenderer.color = originalColor;
    }
}
