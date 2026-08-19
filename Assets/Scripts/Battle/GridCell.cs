using System.Collections;
using DG.Tweening;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    [SerializeField] private int x;
    [SerializeField] private int y;

    private SpriteRenderer visualRenderer;
    private Color originalColor;
    private Coroutine blinkCoroutine;
    private Tween pulseTween;

    public int X => x;
    public int Y => y;
    public Color OriginalColor => originalColor;

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

    public void StartPulse()
    {
        StopPulse();
        if (visualRenderer == null) return;
        visualRenderer.color = originalColor;
        pulseTween = visualRenderer
            .DOFade(0.6f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void StopPulse()
    {
        if (pulseTween != null)
        {
            pulseTween.Kill();
            pulseTween = null;
        }
        if (visualRenderer != null)
            visualRenderer.color = originalColor;
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
