using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BossCardDisplay : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private SpriteRenderer[] slabRenderers;
    [SerializeField] private SpriteRenderer[] slabCardRenderers;
    [SerializeField] private SpriteRenderer[] sunRenderers;
    [SerializeField] private Text[] sunTurnTexts;

    [Header("Card Fit")]
    [SerializeField] private float cardFillRatio = 0.9f;

    [Header("Hover")]
    [SerializeField] private float hoverRise = 0.3f;
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float hoverSpeed = 10f;

    [Header("Instant Kill")]
    [SerializeField] private float glowSpeed = 3f;
    [SerializeField] private float glowIntensity = 0.3f;

    private const int HoveredSlabSortingOrder = 100;

    private BoxCollider2D[] slabColliders;
    private Vector3[] slabRestPositions;
    private Vector3[] slabRestScales;
    private int[] slabRestSortingOrders;
    private int[] cardRestSortingOrders;
    private bool[] slabIsInstantKill;

    private void Awake()
    {
        slabColliders = new BoxCollider2D[slabRenderers.Length];
        slabRestPositions = new Vector3[slabRenderers.Length];
        slabRestScales = new Vector3[slabRenderers.Length];
        slabRestSortingOrders = new int[slabRenderers.Length];
        cardRestSortingOrders = new int[slabRenderers.Length];
        slabIsInstantKill = new bool[slabRenderers.Length];

        for (int i = 0; i < slabRenderers.Length; i++)
        {
            Transform slabTransform = slabRenderers[i].transform;
            slabColliders[i] = slabRenderers[i].GetComponent<BoxCollider2D>();
            slabRestPositions[i] = slabTransform.localPosition;
            slabRestScales[i] = slabTransform.localScale;
            slabRestSortingOrders[i] = slabRenderers[i].sortingOrder;
            cardRestSortingOrders[i] = slabCardRenderers[i].sortingOrder;
            slabCardRenderers[i].enabled = false;
        }

        for (int i = 0; i < sunRenderers.Length; i++)
            sunRenderers[i].gameObject.SetActive(false);
    }

    public void ShowPattern(BossAction[] actions, ArcanaCatalog catalog)
    {
        int slabIndex = 0;
        int sunIndex = 0;

        for (int a = 0; a < actions.Length; a++)
        {
            BossAction action = actions[a];
            int cardCount = action.arcanaIds.Length;

            if (slabIndex + cardCount > slabRenderers.Length)
                break;

            for (int c = 0; c < cardCount; c++)
            {
                ArcanaData arcana = catalog.GetById(action.arcanaIds[c]);
                PlaceCardOnSlab(slabIndex + c, arcana == null ? null : arcana.CardImage);
                slabIsInstantKill[slabIndex + c] = action.isInstantKill;
            }

            // 연계 공격이면 차지한 석판들의 가운데, 단일 공격이면 그 석판 위에 태양 하나.
            // Y는 씬에서 잡아둔 값을 그대로 두고 X만 옮긴다.
            Transform sunTransform = sunRenderers[sunIndex].transform;
            Vector3 sunPosition = sunTransform.localPosition;
            sunPosition.x = (slabRestPositions[slabIndex].x +
                slabRestPositions[slabIndex + cardCount - 1].x) * 0.5f;
            sunTransform.localPosition = sunPosition;

            sunTurnTexts[sunIndex].text = action.timingSlot < 0
                ? "종료"
                : (action.timingSlot + 1).ToString();
            sunRenderers[sunIndex].gameObject.SetActive(true);

            slabIndex += cardCount;
            sunIndex++;
        }

        for (int i = slabIndex; i < slabRenderers.Length; i++)
        {
            slabCardRenderers[i].enabled = false;
            slabIsInstantKill[i] = false;
        }

        for (int i = sunIndex; i < sunRenderers.Length; i++)
            sunRenderers[i].gameObject.SetActive(false);
    }

    private void PlaceCardOnSlab(int slabIndex, Sprite cardImage)
    {
        SpriteRenderer cardRenderer = slabCardRenderers[slabIndex];
        cardRenderer.sprite = cardImage;
        cardRenderer.enabled = cardImage != null;

        if (cardImage == null)
            return;

        Vector2 slabSize = slabRenderers[slabIndex].sprite.bounds.size;
        Vector2 cardSize = cardImage.bounds.size;
        float fitScale = Mathf.Min(slabSize.x / cardSize.x, slabSize.y / cardSize.y)
            * cardFillRatio;

        cardRenderer.transform.localScale = new Vector3(fitScale, fitScale, 1f);
        cardRenderer.transform.localPosition = new Vector3(
            -cardImage.bounds.center.x * fitScale,
            -cardImage.bounds.center.y * fitScale,
            -0.01f);
    }

    private void Update()
    {
        int hoveredSlab = DetectHoveredSlab();
        float lerpRate = hoverSpeed * Time.deltaTime;

        for (int i = 0; i < slabRenderers.Length; i++)
        {
            bool isHovered = i == hoveredSlab && slabCardRenderers[i].enabled;

            Vector3 targetPosition = slabRestPositions[i];
            Vector3 targetScale = slabRestScales[i];

            if (isHovered)
            {
                targetPosition.y += hoverRise;
                targetScale *= hoverScale;
            }

            Transform slabTransform = slabRenderers[i].transform;
            slabTransform.localPosition = Vector3.Lerp(
                slabTransform.localPosition, targetPosition, lerpRate);
            slabTransform.localScale = Vector3.Lerp(
                slabTransform.localScale, targetScale, lerpRate);

            slabRenderers[i].sortingOrder = isHovered
                ? HoveredSlabSortingOrder
                : slabRestSortingOrders[i];
            slabCardRenderers[i].sortingOrder = isHovered
                ? HoveredSlabSortingOrder + 1
                : cardRestSortingOrders[i];

            Color tint = Color.white;
            if (slabIsInstantKill[i])
            {
                float pulse = (Mathf.Sin(Time.time * glowSpeed) + 1f) * 0.5f;
                tint = Color.Lerp(Color.white,
                    new Color(1f, 0.35f, 0.35f), pulse * glowIntensity);
            }

            slabRenderers[i].color = tint;
            slabCardRenderers[i].color = tint;
        }
    }

    private int DetectHoveredSlab()
    {
        if (Mouse.current == null)
            return -1;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, 0f));
        worldPosition.z = 0f;

        for (int i = 0; i < slabRenderers.Length; i++)
        {
            if (slabColliders[i].OverlapPoint(worldPosition))
                return i;
        }

        return -1;
    }
}
