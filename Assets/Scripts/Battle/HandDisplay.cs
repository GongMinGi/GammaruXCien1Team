using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandDisplay : MonoBehaviour
{
    [Header("Card Appearance")]
    [SerializeField] private float cardWidth = 0.8f;
    [SerializeField] private float cardHeight = 1.2f;
    [SerializeField] private Color cardColor = new Color(0.25f, 0.28f, 0.4f);
    [SerializeField] private Color borderColor = new Color(0.45f, 0.5f, 0.65f);
    [SerializeField] private bool showNumberOverlay = false;

    [Header("Fan Layout")]
    [SerializeField, Min(0f)] private float cardSpacing = 0.67f;
    [SerializeField] private float maxFanAngle = 15f;
    [SerializeField] private float arcHeight = 0.4f;

    [Header("Resting Position")]
    [SerializeField] private float restingYOffset = -1.2f;

    [Header("Hover")]
    [SerializeField] private float hoverRise = 0.5f;
    [SerializeField] private float hoverScale = 1.3f;
    [SerializeField] private float hoverSpeed = 10f;

    [Header("DOTween Animation")]
    [SerializeField] private float layoutTweenDuration = 0.25f;
    [SerializeField] private Ease layoutEase = Ease.OutCubic;
    [SerializeField] private float drawSlideInDuration = 0.35f;
    [SerializeField] private Ease drawSlideInEase = Ease.OutBack;
    [SerializeField] private float drawOffscreenX = 6f;
    [SerializeField] private float useAnimDuration = 0.3f;
    [SerializeField] private float useDisplayDuration = 0.4f;
    [SerializeField] private float useConsumedDuration = 0.2f;
    [SerializeField] private Ease useAnimEase = Ease.OutQuad;
    [SerializeField] private Transform useTargetPoint;
    [SerializeField] private float useTargetScale = 1.5f;
    [SerializeField] private float drawStaggerDelay = 0.08f;

    private const int MaxCardCount = 7;

    private Transform[] cardTransforms;
    private Transform[] cardVisualTransforms;
    private SpriteRenderer[] cardRenderers;
    private SpriteRenderer[] artworkRenderers;
    private Transform[] artworkTransforms;
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

    [Header("Selection")]
    [SerializeField] private Color selectedHighlightColor = new Color(0.5f, 1f, 0.5f);
    private int selectedCardIndex = -1;

    private bool isDragging;
    private Vector3 dragWorldOffset;

    private bool isAnimating;
    private Sequence activeLayoutSequence;
    private Sequence activeUseSequence;
    private System.Action<bool> pendingUseCallback;
    private int previousActiveCount;
    private int[] previousCardIds;

    private Sprite baseCardSprite;
    private Texture2D cachedCardTexture;

    public bool IsAnimating => isAnimating;

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

        HashSet<int> newSlots = DetectNewSlots(cards);

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

                Sprite artwork = cards[i].CardImage;
                if (artwork != null)
                {
                    artworkRenderers[i].sprite = artwork;
                    artworkRenderers[i].enabled = true;
                    cardRenderers[i].color = Color.white;
                    cardBaseColors[i] = Color.white;

                    Vector2 spriteSize = artwork.bounds.size;
                    Vector2 targetSize = baseCardSprite.bounds.size;
                    float fitScale = Mathf.Max(
                        targetSize.x / spriteSize.x,
                        targetSize.y / spriteSize.y);
                    artworkTransforms[i].localScale =
                        new Vector3(fitScale, fitScale, 1f);

                    Vector3 cardCenter = baseCardSprite.bounds.center;
                    Vector3 artCenter = artwork.bounds.center * fitScale;
                    artworkTransforms[i].localPosition =
                        new Vector3(
                            cardCenter.x - artCenter.x,
                            cardCenter.y - artCenter.y,
                            -0.01f);
                }
                else
                {
                    artworkRenderers[i].sprite = null;
                    artworkRenderers[i].enabled = false;
                    artworkTransforms[i].localPosition = new Vector3(0f, 0f, -0.01f);
                    cardBaseColors[i] = cards[i].CardColor;
                    cardRenderers[i].color = cardBaseColors[i];
                }

                numberTexts[i].text = cards[i].DisplayNumber;
                numberTexts[i].gameObject.SetActive(showNumberOverlay || artwork == null);
            }
            else
            {
                cardTransforms[i].gameObject.SetActive(false);
            }
        }

        AnimateToLayoutPositions(newSlots);
        StoreCardIds(cards);
    }

    private void GenerateCardObjects()
    {
        if (isGenerated)
            return;

        baseCardSprite = CreateCardSprite();

        cardTransforms = new Transform[MaxCardCount];
        cardVisualTransforms = new Transform[MaxCardCount];
        cardRenderers = new SpriteRenderer[MaxCardCount];
        artworkRenderers = new SpriteRenderer[MaxCardCount];
        artworkTransforms = new Transform[MaxCardCount];
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

            BoxCollider2D hitbox = cardObj.AddComponent<BoxCollider2D>();
            hitbox.size = baseCardSprite.bounds.size;
            hitbox.offset = baseCardSprite.bounds.center;

            GameObject visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(cardObj.transform, false);

            SpriteRenderer renderer = visualObj.AddComponent<SpriteRenderer>();
            renderer.sprite = baseCardSprite;
            renderer.sortingOrder = 10 + i;

            SpriteMask mask = visualObj.AddComponent<SpriteMask>();
            mask.sprite = baseCardSprite;

            GameObject artworkObj = new GameObject("Artwork");
            artworkObj.transform.SetParent(visualObj.transform, false);

            SpriteRenderer artworkRenderer = artworkObj.AddComponent<SpriteRenderer>();
            artworkRenderer.sortingOrder = 11 + i;
            artworkRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            artworkRenderer.enabled = false;

            GameObject textObj = new GameObject("Number");
            textObj.transform.SetParent(visualObj.transform);
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
            meshRenderer.sortingOrder = 12 + i;

            cardTransforms[i] = cardObj.transform;
            cardVisualTransforms[i] = visualObj.transform;
            cardRenderers[i] = renderer;
            artworkRenderers[i] = artworkRenderer;
            artworkTransforms[i] = artworkObj.transform;
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

        for (int i = 0; i < count; i++)
        {
            float centerOffset = i - (count - 1) * 0.5f;
            float t = count > 1 ? centerOffset / ((count - 1) * 0.5f) : 0f;

            float x = centerOffset * cardSpacing;
            float y = restingYOffset - arcHeight * t * t;
            float angle = -maxFanAngle * t;

            restPositions[i] = new Vector3(x, y, 0f);
            restRotations[i] = Quaternion.Euler(0f, 0f, angle);
            restScales[i] = new Vector3(cardWidth, cardHeight, 1f);
            restSortingOrders[i] = 10 + i;
        }
    }

    public int GetHoveredCardIndex() => DetectHover();

    public void SetDragSource(int index)
    {
        if (Mouse.current == null || Camera.main == null ||
            index < 0 || index >= activeCount)
            return;

        dragSourceIndex = index;
        isDragging = true;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, 0f));
        mouseWorld.z = 0f;
        dragWorldOffset = cardVisualTransforms[index].position - mouseWorld;
    }

    public void SetSelectedCard(int index) { selectedCardIndex = index; }
    public void ClearSelectedCard() { selectedCardIndex = -1; }

    public void ClearDragSource()
    {
        dragSourceIndex = -1;
        isDragging = false;
    }

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
        if (isAnimating) return;

        float dt = hoverSpeed * Time.deltaTime;

        for (int i = 0; i < activeCount; i++)
        {
            // 드래그 중: Visual만 마우스 따라 이동, Root 고정
            if (i == dragSourceIndex && isDragging)
            {
                if (Mouse.current == null || Camera.main == null)
                {
                    ClearDragSource();
                    continue;
                }

                Vector2 screenPos = Mouse.current.position.ReadValue();
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
                    new Vector3(screenPos.x, screenPos.y, 0f));
                mouseWorld.z = 0f;
                Vector3 dragTargetWorld = mouseWorld + dragWorldOffset;

                Vector3 visualLocalTarget =
                    cardTransforms[i].InverseTransformPoint(dragTargetWorld);

                cardVisualTransforms[i].localPosition = visualLocalTarget;
                cardVisualTransforms[i].localRotation =
                    Quaternion.Inverse(cardTransforms[i].localRotation);
                cardVisualTransforms[i].localScale =
                    new Vector3(hoverScale, hoverScale, 1f);

                cardRenderers[i].sortingOrder = 100;
                artworkRenderers[i].sortingOrder = 101;
                numberRenderers[i].sortingOrder = 102;

                cardRenderers[i].color = dragHighlightColor;
                artworkRenderers[i].color = dragHighlightColor;
                continue;
            }

            Vector3 targetPos;
            Quaternion targetRot;
            Vector3 targetScale;

            if (i == hoveredIndex)
            {
                Vector3 hoverOffset = new Vector3(
                    0f, hoverRise - restPositions[i].y, 0f);
                Vector3 localOffset =
                    Quaternion.Inverse(restRotations[i]) * hoverOffset;
                targetPos = new Vector3(
                    localOffset.x / restScales[i].x,
                    localOffset.y / restScales[i].y,
                    0f);
                targetRot = Quaternion.Inverse(restRotations[i]);
                targetScale = new Vector3(hoverScale, hoverScale, 1f);
                cardRenderers[i].sortingOrder = 100;
                artworkRenderers[i].sortingOrder = 101;
                numberRenderers[i].sortingOrder = 102;
            }
            else
            {
                targetPos = Vector3.zero;
                targetRot = Quaternion.identity;
                targetScale = Vector3.one;
                cardRenderers[i].sortingOrder = restSortingOrders[i];
                artworkRenderers[i].sortingOrder = restSortingOrders[i] + 1;
                numberRenderers[i].sortingOrder = restSortingOrders[i] + 2;
            }

            if (i == selectedCardIndex)
            {
                cardRenderers[i].color = selectedHighlightColor;
                artworkRenderers[i].color = selectedHighlightColor;
            }
            else
            {
                if (cardBaseColors[i] != Color.white)
                    cardRenderers[i].color = cardBaseColors[i];
                else
                    cardRenderers[i].color = Color.white;
                artworkRenderers[i].color = Color.white;
            }

            cardVisualTransforms[i].localPosition = Vector3.Lerp(
                cardVisualTransforms[i].localPosition, targetPos, dt);
            cardVisualTransforms[i].localRotation = Quaternion.Lerp(
                cardVisualTransforms[i].localRotation, targetRot, dt);
            cardVisualTransforms[i].localScale = Vector3.Lerp(
                cardVisualTransforms[i].localScale, targetScale, dt);
        }
    }

    private Sprite CreateCardSprite()
    {
        int w = 16;
        int h = 28;
        cachedCardTexture = new Texture2D(w, h);
        cachedCardTexture.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                bool isInner = x == 1 || x == w - 2 || y == 1 || y == h - 2;
                Color c = isBorder ? borderColor
                    : isInner ? Color.Lerp(cardColor, borderColor, 0.3f)
                    : cardColor;
                cachedCardTexture.SetPixel(x, y, c);
            }
        }

        cachedCardTexture.Apply();
        return Sprite.Create(cachedCardTexture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), w);
    }

    private HashSet<int> DetectNewSlots(IReadOnlyList<ArcanaData> newCards)
    {
        var newSlots = new HashSet<int>();
        int newCount = Mathf.Min(newCards.Count, MaxCardCount);

        if (previousCardIds == null)
        {
            for (int i = 0; i < newCount; i++)
                newSlots.Add(i);
            return newSlots;
        }

        var oldCounts = new Dictionary<int, int>();
        for (int i = 0; i < previousActiveCount; i++)
        {
            int id = previousCardIds[i];
            oldCounts[id] = oldCounts.TryGetValue(id, out int c) ? c + 1 : 1;
        }

        for (int i = 0; i < newCount; i++)
        {
            int id = newCards[i].Id;
            if (oldCounts.TryGetValue(id, out int remaining) && remaining > 0)
                oldCounts[id] = remaining - 1;
            else
                newSlots.Add(i);
        }
        return newSlots;
    }

    private void StoreCardIds(IReadOnlyList<ArcanaData> cards)
    {
        int count = Mathf.Min(cards.Count, MaxCardCount);
        if (previousCardIds == null || previousCardIds.Length < MaxCardCount)
            previousCardIds = new int[MaxCardCount];
        for (int i = 0; i < count; i++)
            previousCardIds[i] = cards[i].Id;
        previousActiveCount = count;
    }

    private void AnimateToLayoutPositions(HashSet<int> newSlots)
    {
        activeLayoutSequence?.Kill();

        if (activeCount == 0)
        {
            activeLayoutSequence = null;
            isAnimating = false;
            return;
        }

        activeLayoutSequence = DOTween.Sequence();
        isAnimating = true;
        int drawIndex = 0;

        for (int i = 0; i < activeCount; i++)
        {
            cardTransforms[i].DOKill();
            cardVisualTransforms[i].DOKill();

            cardVisualTransforms[i].localPosition = Vector3.zero;
            cardVisualTransforms[i].localRotation = Quaternion.identity;
            cardVisualTransforms[i].localScale = Vector3.one;

            if (newSlots != null && newSlots.Contains(i))
            {
                cardTransforms[i].localPosition = new Vector3(
                    drawOffscreenX, restPositions[i].y, 0f);
                cardTransforms[i].localRotation = restRotations[i];
                cardTransforms[i].localScale = restScales[i];

                float delay = drawIndex * drawStaggerDelay;
                activeLayoutSequence.Insert(delay,
                    cardTransforms[i].DOLocalMove(restPositions[i], drawSlideInDuration)
                        .SetEase(drawSlideInEase));
                drawIndex++;
            }
            else
            {
                activeLayoutSequence.Insert(0f,
                    cardTransforms[i].DOLocalMove(restPositions[i], layoutTweenDuration)
                        .SetEase(layoutEase));
                activeLayoutSequence.Insert(0f,
                    cardTransforms[i].DOLocalRotateQuaternion(restRotations[i], layoutTweenDuration)
                        .SetEase(layoutEase));
                activeLayoutSequence.Insert(0f,
                    cardTransforms[i].DOScale(restScales[i], layoutTweenDuration)
                        .SetEase(layoutEase));
            }

            cardRenderers[i].sortingOrder = restSortingOrders[i];
            artworkRenderers[i].sortingOrder = restSortingOrders[i] + 1;
            numberRenderers[i].sortingOrder = restSortingOrders[i] + 2;
        }

        activeLayoutSequence.OnComplete(() => isAnimating = false);
    }

    public void PlayCardUseAnimation(int cardIndex, System.Action<bool> onFinished)
    {
        if (cardIndex < 0 || cardIndex >= activeCount)
        {
            onFinished?.Invoke(false);
            return;
        }

        CancelUseAnimation();

        pendingUseCallback = onFinished;
        isAnimating = true;

        Transform card = cardTransforms[cardIndex];
        Transform visual = cardVisualTransforms[cardIndex];

        card.DOKill();
        visual.DOKill();

        Vector3 visibleWorldPos = visual.position;
        Quaternion visibleWorldRot = visual.rotation;
        Vector3 visualScaleMultiplier = visual.localScale;

        card.position = visibleWorldPos;
        card.rotation = visibleWorldRot;
        card.localScale = Vector3.Scale(card.localScale, visualScaleMultiplier);

        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;

        cardRenderers[cardIndex].color = cardBaseColors[cardIndex];
        artworkRenderers[cardIndex].color = Color.white;

        cardRenderers[cardIndex].sortingOrder = 200;
        artworkRenderers[cardIndex].sortingOrder = 201;
        numberRenderers[cardIndex].sortingOrder = 202;

        Vector3 worldTarget = useTargetPoint != null
            ? useTargetPoint.position
            : transform.TransformPoint(new Vector3(0f, 3f, 0f));
        Vector3 localTarget = transform.InverseTransformPoint(worldTarget);

        activeUseSequence = DOTween.Sequence();

        activeUseSequence.Append(
            card.DOLocalMove(localTarget, useAnimDuration).SetEase(useAnimEase));
        activeUseSequence.Join(
            card.DOScale(restScales[cardIndex] * useTargetScale, useAnimDuration)
                .SetEase(useAnimEase));
        activeUseSequence.Join(
            card.DOLocalRotateQuaternion(Quaternion.identity, useAnimDuration));

        activeUseSequence.AppendInterval(useDisplayDuration);

        activeUseSequence.Append(
            card.DOScale(Vector3.zero, useConsumedDuration).SetEase(Ease.InBack));

        activeUseSequence.OnComplete(() =>
        {
            card.gameObject.SetActive(false);
            FinishUseAnimation(true);
        });
    }

    private void FinishUseAnimation(bool success)
    {
        var callback = pendingUseCallback;
        pendingUseCallback = null;
        activeUseSequence = null;
        isAnimating = false;
        callback?.Invoke(success);
    }

    private void CancelUseAnimation()
    {
        if (activeUseSequence != null)
        {
            activeUseSequence.Kill();
            FinishUseAnimation(false);
        }
    }

    private void OnDisable()
    {
        activeLayoutSequence?.Kill();
        activeLayoutSequence = null;
        CancelUseAnimation();
        isAnimating = false;
    }

    private void OnDestroy()
    {
        activeLayoutSequence?.Kill();
        CancelUseAnimation();
        if (baseCardSprite != null)
            Destroy(baseCardSprite);
        if (cachedCardTexture != null)
            Destroy(cachedCardTexture);
    }
}
