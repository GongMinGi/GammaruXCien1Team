using UnityEngine;
using UnityEngine.InputSystem;

public class BossCardDisplay : MonoBehaviour
{
    [Header("Card Appearance")]
    [SerializeField] private float cardWidth = 0.7f;
    [SerializeField] private float cardHeight = 1f;
    [SerializeField] private float spacing = 0.1f;
    [SerializeField] private Color cardColor = new Color(0.3f, 0.15f, 0.25f);
    [SerializeField] private Color borderColor = new Color(0.5f, 0.2f, 0.3f);

    [Header("Hover")]
    [SerializeField] private float hoverRise = 0.3f;
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float hoverSpeed = 10f;

    private const int CardCount = 4;

    private Transform[] cardTransforms;
    private SpriteRenderer[] cardRenderers;
    private Vector3[] restPositions;
    private Vector3[] restScales;
    private int[] restSortingOrders;
    private bool isGenerated;

    private void Start()
    {
        GenerateCards();
    }

    private void Update()
    {
        if (!isGenerated)
            return;

        int hovered = DetectHover();
        AnimateCards(hovered);
    }

    private void GenerateCards()
    {
        if (isGenerated)
            return;

        Sprite sprite = CreateCardSprite();
        float step = cardWidth + spacing;
        float startX = -(CardCount - 1) * step * 0.5f;

        cardTransforms = new Transform[CardCount];
        cardRenderers = new SpriteRenderer[CardCount];
        restPositions = new Vector3[CardCount];
        restScales = new Vector3[CardCount];
        restSortingOrders = new int[CardCount];

        for (int i = 0; i < CardCount; i++)
        {
            float x = startX + i * step;

            GameObject cardObject = new GameObject($"Card ({i + 1})");
            cardObject.transform.SetParent(transform);
            cardObject.transform.localPosition = new Vector3(x, 0f, 0f);
            cardObject.transform.localScale = new Vector3(cardWidth, cardHeight, 1f);

            SpriteRenderer renderer = cardObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 2 + i;

            cardObject.AddComponent<BoxCollider2D>();

            cardTransforms[i] = cardObject.transform;
            cardRenderers[i] = renderer;
            restPositions[i] = cardObject.transform.localPosition;
            restScales[i] = cardObject.transform.localScale;
            restSortingOrders[i] = renderer.sortingOrder;
        }

        isGenerated = true;
    }

    private int DetectHover()
    {
        if (Mouse.current == null)
            return -1;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;

        for (int i = CardCount - 1; i >= 0; i--)
        {
            BoxCollider2D col = cardTransforms[i].GetComponent<BoxCollider2D>();
            if (col.OverlapPoint(worldPos))
                return i;
        }

        return -1;
    }

    private void AnimateCards(int hoveredIndex)
    {
        float dt = hoverSpeed * Time.deltaTime;

        for (int i = 0; i < CardCount; i++)
        {
            Vector3 targetPos;
            Vector3 targetScale;

            if (i == hoveredIndex)
            {
                targetPos = new Vector3(restPositions[i].x, hoverRise, 0f);
                targetScale = new Vector3(cardWidth * hoverScale, cardHeight * hoverScale, 1f);
                cardRenderers[i].sortingOrder = 100;
            }
            else
            {
                targetPos = restPositions[i];
                targetScale = restScales[i];
                cardRenderers[i].sortingOrder = restSortingOrders[i];
            }

            cardTransforms[i].localPosition = Vector3.Lerp(
                cardTransforms[i].localPosition, targetPos, dt);
            cardTransforms[i].localScale = Vector3.Lerp(
                cardTransforms[i].localScale, targetScale, dt);
        }
    }

    private Sprite CreateCardSprite()
    {
        int w = 16;
        int h = 24;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                tex.SetPixel(x, y, isBorder ? borderColor : cardColor);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
    }
}
