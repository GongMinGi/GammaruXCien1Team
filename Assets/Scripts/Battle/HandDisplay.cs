using System.Collections.Generic;
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

    private const int MaxCardCount = 7;

    private Transform[] cardTransforms;
    private SpriteRenderer[] cardRenderers;
    private TextMesh[] numberTexts;
    private MeshRenderer[] numberRenderers;
    private Vector3[] restPositions;
    private Quaternion[] restRotations;
    private Vector3[] restScales;
    private int[] restSortingOrders;
    private Color[] cardBaseColors;
    private int activeCount;
    private bool isGenerated;
    private int dragSourceIndex = -1;
    private IReadOnlyList<ArcanaData> currentCards;
    private int lastHoveredIndex = -1;

    [Header("Info Panel")]
    [SerializeField] private ArcanaInfoPanel infoPanel;

    [Header("Drag")]
    [SerializeField] private Color dragHighlightColor = new Color(1f, 1f, 0.5f);

    private void Awake()
    {
        GenerateCardObjects();
    }

    private void Update()
    {
        if (!isGenerated || activeCount == 0)
            return;

        int hovered = DetectHover();
        AnimateCards(hovered);

        if (infoPanel != null && hovered != lastHoveredIndex)
        {
            lastHoveredIndex = hovered;
            if (hovered >= 0 && hovered < activeCount && currentCards[hovered] != null)
                infoPanel.Show(currentCards[hovered]);
            else
                infoPanel.Hide();
        }
    }

    public void UpdateHand(IReadOnlyList<ArcanaData> cards)
    {
        if (!isGenerated)
            return;

        currentCards = cards;
        lastHoveredIndex = -1;
        if (infoPanel != null)
            infoPanel.Hide();

        activeCount = Mathf.Min(cards.Count, MaxCardCount);
        RecalculateLayout(activeCount);

        for (int i = 0; i < MaxCardCount; i++)
        {
            if (i < activeCount)
            {
                cardTransforms[i].gameObject.SetActive(true);
                cardBaseColors[i] = cards[i].CardColor;
                cardRenderers[i].color = cardBaseColors[i];
                numberTexts[i].text = cards[i].DisplayNumber;
            }
            else
            {
                cardTransforms[i].gameObject.SetActive(false);
            }
        }
    }

    private void GenerateCardObjects()
    {
        if (isGenerated)
            return;

        Sprite sprite = CreateCardSprite();

        cardTransforms = new Transform[MaxCardCount];
        cardRenderers = new SpriteRenderer[MaxCardCount];
        numberTexts = new TextMesh[MaxCardCount];
        numberRenderers = new MeshRenderer[MaxCardCount];
        restPositions = new Vector3[MaxCardCount];
        restRotations = new Quaternion[MaxCardCount];
        restScales = new Vector3[MaxCardCount];
        restSortingOrders = new int[MaxCardCount];
        cardBaseColors = new Color[MaxCardCount];

        for (int i = 0; i < MaxCardCount; i++)
        {
            GameObject cardObj = new GameObject($"HandCard ({i + 1})");
            cardObj.transform.SetParent(transform);
            cardObj.transform.localPosition = Vector3.zero;
            cardObj.transform.localScale = new Vector3(cardWidth, cardHeight, 1f);

            SpriteRenderer renderer = cardObj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10 + i;

            cardObj.AddComponent<BoxCollider2D>();

            GameObject textObj = new GameObject("Number");
            textObj.transform.SetParent(cardObj.transform);
            textObj.transform.localPosition = new Vector3(0f, 0.5f, -0.01f);
            textObj.transform.localScale = new Vector3(
                1f / cardWidth, 1f / cardHeight, 1f);

            TextMesh textMesh = textObj.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.1f;
            textMesh.color = Color.white;

            MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = 11 + i;

            cardTransforms[i] = cardObj.transform;
            cardRenderers[i] = renderer;
            numberTexts[i] = textMesh;
            numberRenderers[i] = meshRenderer;
            cardObj.SetActive(false);
        }

        activeCount = 0;
        isGenerated = true;
    }

    private void RecalculateLayout(int count)
    {
        if (count == 0)
            return;

        float spacing = count > 1 ? totalWidth / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float centerOffset = i - (count - 1) * 0.5f;
            float t = count > 1 ? centerOffset / ((count - 1) * 0.5f) : 0f;

            float x = centerOffset * spacing;
            float y = -arcHeight * t * t;
            float angle = -maxFanAngle * t;

            restPositions[i] = new Vector3(x, y, 0f);
            restRotations[i] = Quaternion.Euler(0f, 0f, angle);
            restScales[i] = new Vector3(cardWidth, cardHeight, 1f);
            restSortingOrders[i] = 10 + i;

            cardTransforms[i].localPosition = restPositions[i];
            cardTransforms[i].localRotation = restRotations[i];
            cardTransforms[i].localScale = restScales[i];
            cardRenderers[i].sortingOrder = restSortingOrders[i];
            numberRenderers[i].sortingOrder = restSortingOrders[i] + 1;
        }
    }

    public int GetHoveredCardIndex() => DetectHover();

    public void SetDragSource(int index) { dragSourceIndex = index; }
    public void ClearDragSource() { dragSourceIndex = -1; }

    private int DetectHover()
    {
        if (Mouse.current == null)
            return -1;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;

        for (int i = activeCount - 1; i >= 0; i--)
        {
            if (!cardTransforms[i].gameObject.activeSelf)
                continue;

            BoxCollider2D col = cardTransforms[i].GetComponent<BoxCollider2D>();
            if (col.OverlapPoint(worldPos))
                return i;
        }

        return -1;
    }

    private void AnimateCards(int hoveredIndex)
    {
        float dt = hoverSpeed * Time.deltaTime;

        for (int i = 0; i < activeCount; i++)
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
                numberRenderers[i].sortingOrder = 101;
            }
            else
            {
                targetPos = restPositions[i];
                targetRot = restRotations[i];
                targetScale = restScales[i];
                cardRenderers[i].sortingOrder = restSortingOrders[i];
                numberRenderers[i].sortingOrder = restSortingOrders[i] + 1;
            }

            cardRenderers[i].color = i == dragSourceIndex
                ? dragHighlightColor
                : cardBaseColors[i];

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
