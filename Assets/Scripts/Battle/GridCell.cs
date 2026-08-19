using System.Collections;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    [SerializeField] private int x;
    [SerializeField] private int y;

    private SpriteRenderer visualRenderer;
    private Color originalColor;
    private Coroutine blinkCoroutine;

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

    public void StartBlink(Color colorA, Color colorB, float interval = 0.35f)
    {
        StopBlink();
        blinkCoroutine = StartCoroutine(BlinkLoop(colorA, colorB, interval));
    }

    public void StopBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        ClearHighlight();
    }

    private IEnumerator BlinkLoop(Color colorA, Color colorB, float interval)
    {
        bool toggle = false;
        while (true)
        {
            visualRenderer.color = toggle ? colorA : colorB;
            toggle = !toggle;
            yield return new WaitForSeconds(interval);
        }
    }
}
