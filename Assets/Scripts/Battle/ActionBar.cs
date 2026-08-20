using DG.Tweening;
using UnityEngine;

public class ActionBar : MonoBehaviour
{
    [Header("Slot Appearance")]
    [SerializeField] private float slotSize = 0.4f;
    [SerializeField] private float spacing = 0.05f;
    [SerializeField] private Color slotColor = new Color(0.4f, 0.45f, 0.55f);
    [SerializeField] private Color borderColor = new Color(0.2f, 0.22f, 0.3f);

    [Header("Action Colors")]
    [SerializeField] private Color filledMoveColor = new Color(0.3f, 0.6f, 0.9f);
    [SerializeField] private Color filledStayColor = new Color(0.6f, 0.6f, 0.3f);

    [Header("Execution")]
    [SerializeField] private Color executionColor = Color.black;
    [SerializeField, Min(0f)] private float shakeMagnitude = 0.02f;
    [SerializeField, Min(0.01f)] private float shakeCycleDuration = 0.5f;

    [Header("Planning Indicator")]
    [SerializeField] private Color indicatorColor = Color.white;
    [SerializeField] private float indicatorSize = 0.15f;
    [SerializeField] private float indicatorGap = 0.08f;

    public const int SlotCount = 10;

    public float VisualWidth =>
        (SlotCount - 1) * (slotSize + spacing) + slotSize;

    private SpriteRenderer[] slotRenderers;
    private bool isGenerated;
    private int activeExecutionSlot = -1;
    private Tween activeShakeTween;
    private Vector3 savedSlotLocalPosition;

    private GameObject indicatorObject;
    private SpriteRenderer indicatorRenderer;
    private Sprite indicatorSprite;
    private Texture2D indicatorTexture;

    private void Awake()
    {
        GenerateSlots();
    }

    public void FillSlot(int index, ActionType type)
    {
        if (slotRenderers == null || index < 0 || index >= slotRenderers.Length)
            return;

        slotRenderers[index].color =
            type == ActionType.Move ? filledMoveColor : filledStayColor;
    }

    public void FillRange(int startIndex, int cost, Color color)
    {
        if (slotRenderers == null)
            return;

        for (int i = startIndex; i < startIndex + cost && i < slotRenderers.Length; i++)
        {
            if (i >= 0)
                slotRenderers[i].color = color;
        }
    }

    public void ClearSlot(int index)
    {
        if (slotRenderers == null || index < 0 || index >= slotRenderers.Length)
            return;

        slotRenderers[index].color = Color.white;
    }

    public void ClearRange(int startIndex, int cost)
    {
        if (slotRenderers == null)
            return;

        for (int i = startIndex; i < startIndex + cost && i < slotRenderers.Length; i++)
        {
            if (i >= 0)
                slotRenderers[i].color = Color.white;
        }
    }

    public void ClearAll()
    {
        ClearExecutingSlot();
        HideIndicator();

        if (slotRenderers == null)
            return;

        for (int i = 0; i < slotRenderers.Length; i++)
            slotRenderers[i].color = Color.white;
    }

    public void ShowIndicator(int slotIndex)
    {
        if (indicatorObject == null || slotRenderers == null ||
            slotIndex < 0 || slotIndex >= slotRenderers.Length)
        {
            HideIndicator();
            return;
        }

        Vector3 slotPos = slotRenderers[slotIndex].transform.localPosition;
        indicatorObject.transform.localPosition = new Vector3(
            slotPos.x,
            slotPos.y - slotSize * 0.5f - indicatorGap,
            0f);
        indicatorObject.SetActive(true);
    }

    public void HideIndicator()
    {
        if (indicatorObject != null)
            indicatorObject.SetActive(false);
    }

    public void MarkExecutingSlot(int index)
    {
        ClearExecutingSlot();

        if (slotRenderers == null || index < 0 || index >= slotRenderers.Length)
            return;

        activeExecutionSlot = index;
        SpriteRenderer renderer = slotRenderers[index];
        renderer.color = executionColor;

        Transform slotTransform = renderer.transform;
        savedSlotLocalPosition = slotTransform.localPosition;
        activeShakeTween = slotTransform
            .DOShakePosition(shakeCycleDuration, shakeMagnitude, 20, 90f, false, false)
            .SetLoops(-1, LoopType.Restart);
    }

    public void ClearExecutingSlot()
    {
        activeShakeTween?.Kill();
        activeShakeTween = null;

        if (slotRenderers != null && activeExecutionSlot >= 0 &&
            activeExecutionSlot < slotRenderers.Length)
        {
            slotRenderers[activeExecutionSlot].transform.localPosition =
                savedSlotLocalPosition;
        }

        activeExecutionSlot = -1;
    }

    private void OnDisable()
    {
        ClearExecutingSlot();
    }

    private void GenerateSlots()
    {
        if (isGenerated)
            return;

        Sprite sprite = CreateSlotSprite();
        float step = slotSize + spacing;
        float startX = -(SlotCount - 1) * step * 0.5f;

        slotRenderers = new SpriteRenderer[SlotCount];

        for (int i = 0; i < SlotCount; i++)
        {
            float x = startX + i * step;

            GameObject slotObject = new GameObject($"Slot ({i + 1})");
            slotObject.transform.SetParent(transform);
            slotObject.transform.localPosition = new Vector3(x, 0f, 0f);
            slotObject.transform.localScale = new Vector3(slotSize, slotSize, 1f);

            SpriteRenderer renderer = slotObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1;

            slotRenderers[i] = renderer;
        }

        indicatorSprite = CreateArrowSprite();
        indicatorObject = new GameObject("PlanningIndicator");
        indicatorObject.transform.SetParent(transform, false);
        indicatorRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        indicatorRenderer.sprite = indicatorSprite;
        indicatorRenderer.color = indicatorColor;
        indicatorRenderer.sortingOrder = 2;
        indicatorObject.transform.localScale = new Vector3(indicatorSize, indicatorSize, 1f);
        indicatorObject.SetActive(false);

        isGenerated = true;
    }

    private Sprite CreateSlotSprite()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                tex.SetPixel(x, y, isBorder ? borderColor : slotColor);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Sprite CreateArrowSprite()
    {
        int size = 16;
        indicatorTexture = new Texture2D(size, size);
        indicatorTexture.filterMode = FilterMode.Point;

        Color clear = Color.clear;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                indicatorTexture.SetPixel(x, y, clear);
        }

        for (int y = 0; y < size; y++)
        {
            float progress = (float)y / (size - 1);
            int halfWidth = Mathf.RoundToInt((1f - progress) * size * 0.5f);
            int cx = size / 2;
            for (int x = cx - halfWidth; x <= cx + halfWidth; x++)
            {
                if (x >= 0 && x < size)
                    indicatorTexture.SetPixel(x, y, Color.white);
            }
        }

        indicatorTexture.Apply();
        return Sprite.Create(indicatorTexture,
            new Rect(0, 0, size, size), new Vector2(0.5f, 1f), size);
    }

    private void OnDestroy()
    {
        if (indicatorSprite != null) Destroy(indicatorSprite);
        if (indicatorTexture != null) Destroy(indicatorTexture);
    }
}
