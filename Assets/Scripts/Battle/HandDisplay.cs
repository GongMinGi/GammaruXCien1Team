using UnityEngine;
using UnityEngine.InputSystem;

public class HandDisplay : MonoBehaviour
{
    [Header("Card Appearance")]
    [SerializeField] private float cardWidth = 0.8f;
    [SerializeField] private float cardHeight = 1.2f;
    [SerializeField] private Color cardColor = new Color(0.25f, 0.28f, 0.4f);
    [SerializeField] private Color borderColor = new Color(0.45f, 0.5f, 0.65f);

    [Header("Fan Layout")]
    [SerializeField] private float totalWidth = 4f;
    [SerializeField] private float maxFanAngle = 15f;
    [SerializeField] private float arcHeight = 0.4f;

    [Header("Hover")]
    [SerializeField] private float hoverRise = 0.5f;
    [SerializeField] private float hoverScale = 1.3f;
    [SerializeField] private float hoverSpeed = 10f;

    private const int CardCount = 5;

    private Transform[] cardTransforms;
    private SpriteRenderer[] cardRenderers;
    private Vector3[] restPositions;
    private Quaternion[] restRotations;
    private Vector3[] restScales;
    private int[] restSortingOrders;
    private bool isGenerated;

    private void Start()
    {
        GenerateHand();
    }

    private void Update()
    {
        if (!isGenerated)
            return;

        int hovered = DetectHover();
        AnimateCards(hovered);
    }

    private void GenerateHand()
    {
        if (isGenerated)
            return;

        Sprite sprite = CreateCardSprite();

        cardTransforms = new Transform[CardCount];
        cardRenderers = new SpriteRenderer[CardCount];
        restPositions = new Vector3[CardCount];
        restRotations = new Quaternion[CardCount];
        restScales = new Vector3[CardCount];
        restSortingOrders = new int[CardCount];

        float spacing = CardCount > 1 ? totalWidth / (CardCount - 1) : 0f;

        for (int i = 0; i < CardCount; i++)
        {
            float centerOffset = i - (CardCount - 1) * 0.5f;
            float t = CardCount > 1 ? centerOffset / ((CardCount - 1) * 0.5f) : 0f;

            float x = centerOffset * spacing;
            float y = -arcHeight * t * t;
            float angle = -maxFanAngle * t;

            GameObject cardObj = new GameObject($"HandCard ({i + 1})");
            cardObj.transform.SetParent(transform);
            cardObj.transform.localPosition = new Vector3(x, y, 0f);
            cardObj.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            cardObj.transform.localScale = new Vector3(cardWidth, cardHeight, 1f);

            SpriteRenderer renderer = cardObj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10 + i;

            cardObj.AddComponent<BoxCollider2D>();

            cardTransforms[i] = cardObj.transform;
            cardRenderers[i] = renderer;
            restPositions[i] = cardObj.transform.localPosition;
            restRotations[i] = cardObj.transform.localRotation;
            restScales[i] = cardObj.transform.localScale;
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
            Quaternion targetRot;
            Vector3 targetScale;

            if (i == hoveredIndex)
            {
                targetPos = new Vector3(restPositions[i].x, hoverRise, 0f);
                targetRot = Quaternion.identity;
                targetScale = new Vector3(cardWidth * hoverScale, cardHeight * hoverScale, 1f);
                cardRenderers[i].sortingOrder = 100;
            }
            else
            {
                targetPos = restPositions[i];
                targetRot = restRotations[i];
                targetScale = restScales[i];
                cardRenderers[i].sortingOrder = restSortingOrders[i];
            }

            cardTransforms[i].localPosition = Vector3.Lerp(
                cardTransforms[i].localPosition, targetPos, dt);
            cardTransforms[i].localRotation = Quaternion.Lerp(
                cardTransforms[i].localRotation, targetRot, dt);
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
                bool isInner = x == 1 || x == w - 2 || y == 1 || y == h - 2;
                Color c = isBorder ? borderColor
                    : isInner ? Color.Lerp(cardColor, borderColor, 0.3f)
                    : cardColor;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), w);
    }
}
