using System.Collections.Generic;
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

    [Header("Pattern Display")]
    [SerializeField] private float groupSpacing = 0.4f;
    [SerializeField] private float cardInGroupSpacing = 0.05f;
    [SerializeField] private Color timingTextColor = new Color(0.9f, 0.8f, 0.3f);

    [Header("Text Size")]
    [SerializeField] private float timingCharSize = 0.06f;
    [SerializeField] private float numberCharSize = 0.08f;

    [Header("Instant Kill")]
    [SerializeField] private Color instantKillCardColor = new Color(0.6f, 0.05f, 0.05f);
    [SerializeField] private Color instantKillBorderColor = new Color(1f, 0.15f, 0.15f);
    [SerializeField] private float glowSpeed = 3f;
    [SerializeField] private float glowIntensity = 0.3f;

    private readonly List<Transform> cardTransforms = new();
    private readonly List<SpriteRenderer> cardRenderers = new();
    private readonly List<SpriteRenderer> artworkRenderers = new();
    private readonly List<MeshRenderer> numberRenderers = new();
    private readonly List<SpriteMask> cardMasks = new();
    private readonly List<Vector3> restPositions = new();
    private readonly List<Vector3> restScales = new();
    private readonly List<int> restSortingOrders = new();
    private readonly List<bool> cardIsInstantKill = new();
    private readonly List<GameObject> allObjects = new();

    private Sprite cachedCardSprite;
    private Texture2D cachedCardTexture;
    private Sprite cachedInstantKillSprite;
    private Texture2D cachedInstantKillTexture;

    private void Update()
    {
        if (cardTransforms.Count == 0)
            return;

        int hovered = DetectHover();
        AnimateCards(hovered);
    }

    public void ShowPattern(BossAction[] actions, ArcanaCatalog catalog)
    {
        ClearCards();

        if (cachedCardSprite == null)
            cachedCardSprite = CreateCardSprite(cardColor, borderColor, out cachedCardTexture);
        if (cachedInstantKillSprite == null)
            cachedInstantKillSprite = CreateCardSprite(instantKillCardColor, instantKillBorderColor, out cachedInstantKillTexture);

        float totalWidth = CalculateTotalWidth(actions);
        float currentX = -totalWidth * 0.5f;

        for (int g = 0; g < actions.Length; g++)
        {
            BossAction action = actions[g];
            string timingLabel = action.timingSlot < 0
                ? "종료"
                : (action.timingSlot + 1).ToString();

            int cardCount = action.arcanaIds.Length;
            float groupWidth = cardCount * cardWidth + (cardCount - 1) * cardInGroupSpacing;
            float groupCenter = currentX + groupWidth * 0.5f;

            GameObject timingObj = new GameObject($"Timing ({timingLabel})");
            timingObj.transform.SetParent(transform);
            timingObj.transform.localPosition = new Vector3(groupCenter, cardHeight * 0.5f + 0.15f, -0.01f);

            TextMesh timingText = timingObj.AddComponent<TextMesh>();
            timingText.text = timingLabel;
            timingText.anchor = TextAnchor.MiddleCenter;
            timingText.alignment = TextAlignment.Center;
            timingText.fontSize = 32;
            timingText.characterSize = timingCharSize;
            timingText.color = timingTextColor;

            MeshRenderer timingRenderer = timingObj.GetComponent<MeshRenderer>();
            timingRenderer.sortingOrder = 10;

            allObjects.Add(timingObj);

            float cardStartX = currentX + cardWidth * 0.5f;

            for (int c = 0; c < cardCount; c++)
            {
                int arcanaId = action.arcanaIds[c];
                ArcanaData data = catalog.GetById(arcanaId);

                float cx = cardStartX + c * (cardWidth + cardInGroupSpacing);
                int baseSortOrder = 2 + cardTransforms.Count * 3;

                GameObject cardObj = new GameObject($"BossCard ({arcanaId})");
                cardObj.transform.SetParent(transform);
                cardObj.transform.localPosition = new Vector3(cx, 0f, 0f);
                cardObj.transform.localScale = new Vector3(cardWidth, cardHeight, 1f);

                cardObj.AddComponent<BoxCollider2D>();

                // Visual (배경 + 마스크)
                GameObject visualObj = new GameObject("Visual");
                visualObj.transform.SetParent(cardObj.transform, false);

                SpriteRenderer renderer = visualObj.AddComponent<SpriteRenderer>();
                renderer.sprite = action.isInstantKill ? cachedInstantKillSprite : cachedCardSprite;
                renderer.sortingOrder = baseSortOrder;

                SpriteMask mask = visualObj.AddComponent<SpriteMask>();
                mask.sprite = renderer.sprite;
                mask.isCustomRangeActive = true;
                mask.frontSortingOrder = baseSortOrder + 2;
                mask.backSortingOrder = baseSortOrder;
                mask.frontSortingLayerID = renderer.sortingLayerID;
                mask.backSortingLayerID = renderer.sortingLayerID;

                // Artwork
                GameObject artworkObj = new GameObject("Artwork");
                artworkObj.transform.SetParent(visualObj.transform, false);

                SpriteRenderer artworkRenderer = artworkObj.AddComponent<SpriteRenderer>();
                artworkRenderer.sortingOrder = baseSortOrder + 1;
                artworkRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

                Sprite cardImage = data?.CardImage;
                if (cardImage != null)
                {
                    artworkRenderer.sprite = cardImage;
                    Vector2 spriteSize = cardImage.bounds.size;
                    Vector2 targetSize = renderer.sprite.bounds.size;
                    float fitScale = Mathf.Max(
                        targetSize.x / spriteSize.x,
                        targetSize.y / spriteSize.y);
                    artworkObj.transform.localScale = new Vector3(fitScale, fitScale, 1f);
                    Vector3 cardCenter = renderer.sprite.bounds.center;
                    Vector3 artCenter = cardImage.bounds.center * fitScale;
                    artworkObj.transform.localPosition = new Vector3(
                        cardCenter.x - artCenter.x,
                        cardCenter.y - artCenter.y,
                        -0.01f);
                }
                else
                {
                    artworkRenderer.enabled = false;
                }

                // Number
                GameObject numObj = new GameObject("Number");
                numObj.transform.SetParent(visualObj.transform);
                numObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                numObj.transform.localScale = new Vector3(1f / cardWidth, 1f / cardHeight, 1f);

                TextMesh numText = numObj.AddComponent<TextMesh>();
                numText.text = data != null ? data.DisplayNumber : arcanaId.ToString();
                numText.anchor = TextAnchor.MiddleCenter;
                numText.alignment = TextAlignment.Center;
                numText.fontSize = 32;
                numText.characterSize = numberCharSize;
                numText.color = Color.white;

                MeshRenderer numRenderer = numObj.GetComponent<MeshRenderer>();
                numRenderer.sortingOrder = baseSortOrder + 2;

                cardTransforms.Add(cardObj.transform);
                cardRenderers.Add(renderer);
                artworkRenderers.Add(artworkRenderer);
                numberRenderers.Add(numRenderer);
                cardMasks.Add(mask);
                restPositions.Add(cardObj.transform.localPosition);
                restScales.Add(cardObj.transform.localScale);
                restSortingOrders.Add(baseSortOrder);
                cardIsInstantKill.Add(action.isInstantKill);
                allObjects.Add(cardObj);
            }

            currentX += groupWidth + groupSpacing;
        }
    }

    private void ClearCards()
    {
        foreach (GameObject obj in allObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        allObjects.Clear();
        cardTransforms.Clear();
        cardRenderers.Clear();
        artworkRenderers.Clear();
        numberRenderers.Clear();
        cardMasks.Clear();
        restPositions.Clear();
        restScales.Clear();
        restSortingOrders.Clear();
        cardIsInstantKill.Clear();
    }

    private float CalculateTotalWidth(BossAction[] actions)
    {
        float total = 0f;
        for (int i = 0; i < actions.Length; i++)
        {
            int count = actions[i].arcanaIds.Length;
            total += count * cardWidth + (count - 1) * cardInGroupSpacing;

            if (i < actions.Length - 1)
                total += groupSpacing;
        }
        return total;
    }

    private int DetectHover()
    {
        if (Mouse.current == null || cardTransforms.Count == 0)
            return -1;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;

        for (int i = cardTransforms.Count - 1; i >= 0; i--)
        {
            BoxCollider2D col = cardTransforms[i].GetComponent<BoxCollider2D>();
            if (col != null && col.OverlapPoint(worldPos))
                return i;
        }

        return -1;
    }

    private void AnimateCards(int hoveredIndex)
    {
        float dt = hoverSpeed * Time.deltaTime;

        for (int i = 0; i < cardTransforms.Count; i++)
        {
            Vector3 targetPos;
            Vector3 targetScale;

            if (i == hoveredIndex)
            {
                targetPos = new Vector3(restPositions[i].x, hoverRise, 0f);
                targetScale = new Vector3(cardWidth * hoverScale, cardHeight * hoverScale, 1f);

                cardRenderers[i].sortingOrder = 100;
                artworkRenderers[i].sortingOrder = 101;
                numberRenderers[i].sortingOrder = 102;
                cardMasks[i].frontSortingOrder = 102;
                cardMasks[i].backSortingOrder = 100;
            }
            else
            {
                targetPos = restPositions[i];
                targetScale = restScales[i];

                cardRenderers[i].sortingOrder = restSortingOrders[i];
                artworkRenderers[i].sortingOrder = restSortingOrders[i] + 1;
                numberRenderers[i].sortingOrder = restSortingOrders[i] + 2;
                cardMasks[i].frontSortingOrder = restSortingOrders[i] + 2;
                cardMasks[i].backSortingOrder = restSortingOrders[i];
            }

            cardTransforms[i].localPosition = Vector3.Lerp(
                cardTransforms[i].localPosition, targetPos, dt);
            cardTransforms[i].localScale = Vector3.Lerp(
                cardTransforms[i].localScale, targetScale, dt);

            if (cardIsInstantKill[i])
            {
                float glow = Mathf.Sin(Time.time * glowSpeed) * glowIntensity;
                cardRenderers[i].color = new Color(
                    Mathf.Clamp01(1f + glow),
                    Mathf.Clamp01(0f + glow * 0.3f),
                    Mathf.Clamp01(0f + glow * 0.3f),
                    1f);
                float pulse = (Mathf.Sin(Time.time * glowSpeed) + 1f) * 0.5f;
                artworkRenderers[i].color = Color.Lerp(
                    Color.white, new Color(1f, 0.35f, 0.35f), pulse * glowIntensity);
            }
            else
            {
                cardRenderers[i].color = Color.white;
                artworkRenderers[i].color = Color.white;
            }
        }
    }

    private Sprite CreateCardSprite(Color fill, Color border, out Texture2D texture)
    {
        int w = 16;
        int h = 24;
        texture = new Texture2D(w, h);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                texture.SetPixel(x, y, isBorder ? border : fill);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
    }

    private void OnDestroy()
    {
        ClearCards();
        if (cachedCardSprite != null) Destroy(cachedCardSprite);
        if (cachedCardTexture != null) Destroy(cachedCardTexture);
        if (cachedInstantKillSprite != null) Destroy(cachedInstantKillSprite);
        if (cachedInstantKillTexture != null) Destroy(cachedInstantKillTexture);
    }
}
