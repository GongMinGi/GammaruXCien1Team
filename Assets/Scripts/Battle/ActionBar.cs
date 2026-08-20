using DG.Tweening;
using UnityEngine;

public class ActionBar : MonoBehaviour
{
    [Header("Slot Sprites")]
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite filledSlotSprite;
    [SerializeField] private Sprite currentTurnSprite;

    [Header("Slot Layout")]
    [SerializeField] private float slotSize = 0.6f;
    [SerializeField] private float spacing = 0.08f;

    [Header("Current Turn Blink")]
    [SerializeField] private float blinkDuration = 0.5f;
    [SerializeField] private float blinkMinAlpha = 0.2f;

    [Header("Execution")]
    [SerializeField, Min(0f)] private float shakeMagnitude = 0.02f;
    [SerializeField, Min(0.01f)] private float shakeCycleDuration = 0.5f;

    public const int SlotCount = 10;

    public float VisualWidth =>
        (SlotCount - 1) * (slotSize + spacing) + slotSize;

    private SpriteRenderer[] slotRenderers;

    private SpriteRenderer currentTurnRenderer;
    private Tween blinkTween;

    private int executingSlotIndex = -1;
    private Tween shakeTween;
    private Vector3 executingSlotOriginalPosition;

    private void Awake()
    {
        GenerateSlots();
        GenerateCurrentTurnMark();
    }

    public void FillSlot(int index)
    {
        slotRenderers[index].sprite = filledSlotSprite;
    }

    public void FillRange(int startIndex, int cost)
    {
        for (int i = startIndex; i < startIndex + cost; i++)
            slotRenderers[i].sprite = filledSlotSprite;
    }

    public void ClearSlot(int index)
    {
        slotRenderers[index].sprite = emptySlotSprite;
    }

    public void ClearRange(int startIndex, int cost)
    {
        for (int i = startIndex; i < startIndex + cost; i++)
            slotRenderers[i].sprite = emptySlotSprite;
    }

    public void ClearAll()
    {
        ClearExecutingSlot();
        HideCurrentTurnMark();

        for (int i = 0; i < SlotCount; i++)
            slotRenderers[i].sprite = emptySlotSprite;
    }

    public void ShowCurrentTurnMark(int slotIndex)
    {
        if (slotIndex < 0)
        {
            HideCurrentTurnMark();
            return;
        }

        currentTurnRenderer.transform.localPosition =
            slotRenderers[slotIndex].transform.localPosition;
        currentTurnRenderer.gameObject.SetActive(true);
        blinkTween.Play();
    }

    public void HideCurrentTurnMark()
    {
        blinkTween.Rewind();
        currentTurnRenderer.gameObject.SetActive(false);
    }

    public void MarkExecutingSlot(int index)
    {
        ClearExecutingSlot();

        executingSlotIndex = index;
        Transform slotTransform = slotRenderers[index].transform;
        executingSlotOriginalPosition = slotTransform.localPosition;
        shakeTween = slotTransform
            .DOShakePosition(shakeCycleDuration, shakeMagnitude, 20, 90f, false, false)
            .SetLoops(-1, LoopType.Restart);
    }

    public void ClearExecutingSlot()
    {
        if (executingSlotIndex < 0)
            return;

        shakeTween.Kill();
        shakeTween = null;
        slotRenderers[executingSlotIndex].transform.localPosition =
            executingSlotOriginalPosition;
        executingSlotIndex = -1;
    }

    private void GenerateSlots()
    {
        float step = slotSize + spacing;
        float startX = -(SlotCount - 1) * step * 0.5f;
        float slotScale = slotSize / emptySlotSprite.bounds.size.x;

        slotRenderers = new SpriteRenderer[SlotCount];

        for (int i = 0; i < SlotCount; i++)
        {
            GameObject slotObject = new GameObject("Slot (" + (i + 1) + ")");
            slotObject.transform.SetParent(transform, false);
            slotObject.transform.localPosition = new Vector3(startX + i * step, 0f, 0f);
            slotObject.transform.localScale = new Vector3(slotScale, slotScale, 1f);

            SpriteRenderer renderer = slotObject.AddComponent<SpriteRenderer>();
            renderer.sprite = emptySlotSprite;
            renderer.sortingOrder = 1;

            slotRenderers[i] = renderer;
        }
    }

    private void GenerateCurrentTurnMark()
    {
        float markScale = slotSize / currentTurnSprite.bounds.size.x;

        GameObject markObject = new GameObject("CurrentTurnMark");
        markObject.transform.SetParent(transform, false);
        markObject.transform.localScale = new Vector3(markScale, markScale, 1f);

        currentTurnRenderer = markObject.AddComponent<SpriteRenderer>();
        currentTurnRenderer.sprite = currentTurnSprite;
        currentTurnRenderer.sortingOrder = 2;

        blinkTween = currentTurnRenderer
            .DOFade(blinkMinAlpha, blinkDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .Pause();

        markObject.SetActive(false);
    }

    private void OnDisable()
    {
        ClearExecutingSlot();
    }

    private void OnDestroy()
    {
        blinkTween.Kill();
    }
}
