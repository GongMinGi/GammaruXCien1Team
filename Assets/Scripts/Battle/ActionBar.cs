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

    public const int SlotCount = 10;

    private SpriteRenderer[] slotRenderers;
    private bool isGenerated;

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
        if (slotRenderers == null)
            return;

        for (int i = 0; i < slotRenderers.Length; i++)
            slotRenderers[i].color = Color.white;
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
}
