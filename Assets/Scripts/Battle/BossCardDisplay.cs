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

    private readonly List<Transform> cardTransforms = new();
    private readonly List<SpriteRenderer> cardRenderers = new();
    private readonly List<Vector3> restPositions = new();
    private readonly List<Vector3> restScales = new();
    private readonly List<int> restSortingOrders = new();
    private readonly List<GameObject> allObjects = new();

    private Sprite cachedCardSprite;

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
            cachedCardSprite = CreateCardSprite();

        float totalWidth = CalculateTotalWidth(actions);
        float currentX = -totalWidth * 0.5f;

        for (int g = 0; g < actions.Length; g++)
        {
            BossAction action = actions[g];
            int displayTiming = action.timingSlot + 1;

            // 그룹 중앙 계산
            int cardCount = action.arcanaIds.Length;
            float groupWidth = cardCount * cardWidth + (cardCount - 1) * cardInGroupSpacing;
            float groupCenter = currentX + groupWidth * 0.5f;

            // 타이밍 번호 텍스트
            GameObject timingObj = new GameObject($"Timing ({displayTiming})");
            timingObj.transform.SetParent(transform);
            timingObj.transform.localPosition = new Vector3(groupCenter, cardHeight * 0.5f + 0.15f, -0.01f);

            TextMesh timingText = timingObj.AddComponent<TextMesh>();
            timingText.text = displayTiming.ToString();
            timingText.anchor = TextAnchor.MiddleCenter;
            timingText.alignment = TextAlignment.Center;
            timingText.fontSize = 32;
            timingText.characterSize = 0.06f;
            timingText.color = timingTextColor;

            MeshRenderer timingRenderer = timingObj.GetComponent<MeshRenderer>();
            timingRenderer.sortingOrder = 10;

            allObjects.Add(timingObj);

            // 아르카나 카드들
            float cardStartX = currentX + cardWidth * 0.5f;

            for (int c = 0; c < cardCount; c++)
            {
                int arcanaId = action.arcanaIds[c];
                ArcanaData data = catalog.GetById(arcanaId);

                float cx = cardStartX + c * (cardWidth + cardInGroupSpacing);

                GameObject cardObj = new GameObject($"BossCard ({arcanaId})");
                cardObj.transform.SetParent(transform);
                cardObj.transform.localPosition = new Vector3(cx, 0f, 0f);
                cardObj.transform.localScale = new Vector3(cardWidth, cardHeight, 1f);

                SpriteRenderer renderer = cardObj.AddComponent<SpriteRenderer>();
                renderer.sprite = cachedCardSprite;
                renderer.sortingOrder = 2 + cardTransforms.Count;

                cardObj.AddComponent<BoxCollider2D>();

                // 로마숫자 텍스트
                GameObject numObj = new GameObject("Number");
                numObj.transform.SetParent(cardObj.transform);
                numObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                numObj.transform.localScale = new Vector3(1f / cardWidth, 1f / cardHeight, 1f);

                TextMesh numText = numObj.AddComponent<TextMesh>();
                numText.text = data != null ? data.DisplayNumber : arcanaId.ToString();
                numText.anchor = TextAnchor.MiddleCenter;
                numText.alignment = TextAlignment.Center;
                numText.fontSize = 32;
                numText.characterSize = 0.08f;
                numText.color = Color.white;

                MeshRenderer numRenderer = numObj.GetComponent<MeshRenderer>();
                numRenderer.sortingOrder = renderer.sortingOrder + 1;

                cardTransforms.Add(cardObj.transform);
                cardRenderers.Add(renderer);
                restPositions.Add(cardObj.transform.localPosition);
                restScales.Add(cardObj.transform.localScale);
                restSortingOrders.Add(renderer.sortingOrder);
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
        restPositions.Clear();
        restScales.Clear();
        restSortingOrders.Clear();
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
