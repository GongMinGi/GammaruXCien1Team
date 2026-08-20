using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CardMergePanel : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Font koreanFont;
    [SerializeField] private PlanningController planningController;
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private HandDisplay handDisplay;

    [Header("색")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] private Color buttonColor = new Color(0.25f, 0.2f, 0.35f, 1f);

    [Header("크기")]
    [SerializeField] private Vector2 mergeButtonSize = new Vector2(160f, 50f);
    [SerializeField] private Vector2 mergeButtonOffset = new Vector2(-20f, 0f);

    [Header("연출")]
    [SerializeField, Min(0.01f)] private float cardMoveSpeed = 5f;
    [SerializeField, Min(0f)] private float resultDisplayTime = 1f;
    [SerializeField] private int promptFontSize = 36;
    [SerializeField] private int buttonFontSize = 24;

    private GameObject mergeButton;
    private GameObject mergeRoot;
    private GameObject animContainer;
    private Text promptText;
    private int selectedIndex1 = -1;
    private int selectedIndex2 = -1;
    private bool mergeActive;
    private bool animating;
    private Coroutine mergeCoroutine;

    public bool IsOpen => mergeActive;

    public void SetButtonVisible(bool visible)
    {
        if (!visible && mergeActive)
            Close();
        if (mergeButton != null)
            mergeButton.SetActive(visible);
    }

    private void Awake()
    {
        if (canvas == null || koreanFont == null)
        {
            Debug.LogError("[CardMergePanel] canvas 또는 koreanFont 미할당.");
            return;
        }

        BuildButton();
        BuildOverlay();
    }

    private void BuildButton()
    {
        mergeButton = new GameObject("MergeButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mergeButton.transform.SetParent(canvas.transform, false);

        RectTransform rect = mergeButton.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = mergeButtonOffset;
        rect.sizeDelta = mergeButtonSize;

        Image bg = mergeButton.GetComponent<Image>();
        bg.color = buttonColor;
        bg.raycastTarget = true;

        Button button = mergeButton.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(Open);

        GameObject textObj = new GameObject("Label",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObj.transform.SetParent(mergeButton.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        Stretch(textRect);

        Text text = textObj.GetComponent<Text>();
        text.font = koreanFont;
        text.fontSize = buttonFontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = "조합";
        text.raycastTarget = false;

        mergeButton.SetActive(false);
    }

    private void BuildOverlay()
    {
        mergeRoot = new GameObject("MergeOverlay",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mergeRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = mergeRoot.GetComponent<RectTransform>();
        Stretch(rootRect);

        Image dim = mergeRoot.GetComponent<Image>();
        dim.color = dimColor;
        dim.raycastTarget = true;

        // 프롬프트 텍스트
        GameObject promptObj = new GameObject("PromptText",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        promptObj.transform.SetParent(mergeRoot.transform, false);

        RectTransform promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 1f);
        promptRect.anchorMax = new Vector2(0.5f, 1f);
        promptRect.pivot = new Vector2(0.5f, 1f);
        promptRect.anchoredPosition = new Vector2(0f, -60f);
        promptRect.sizeDelta = new Vector2(800f, 60f);

        promptText = promptObj.GetComponent<Text>();
        promptText.font = koreanFont;
        promptText.fontSize = promptFontSize;
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.raycastTarget = false;

        // 연출 컨테이너
        animContainer = new GameObject("AnimContainer",
            typeof(RectTransform));
        animContainer.transform.SetParent(mergeRoot.transform, false);
        Stretch(animContainer.GetComponent<RectTransform>());

        mergeRoot.SetActive(false);
    }

    public void Open()
    {
        if (mergeActive) return;
        mergeActive = true;
        mergeRoot.SetActive(true);
        mergeRoot.transform.SetAsLastSibling();
        selectedIndex1 = selectedIndex2 = -1;
        animating = false;
        promptText.text = "조합할 카드를 고르시오";
        promptText.enabled = true;
        ClearAnimContainer();
    }

    public void Close()
    {
        if (mergeCoroutine != null)
        {
            StopCoroutine(mergeCoroutine);
            mergeCoroutine = null;
        }
        mergeActive = false;
        animating = false;
        mergeRoot.SetActive(false);
        handDisplay.ClearSelectedCard();
        ClearAnimContainer();
        selectedIndex1 = selectedIndex2 = -1;
    }

    private void ClearAnimContainer()
    {
        if (animContainer == null) return;
        for (int i = animContainer.transform.childCount - 1; i >= 0; i--)
            Destroy(animContainer.transform.GetChild(i).gameObject);
    }

    private void Update()
    {
        if (!mergeActive) return;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame &&
            !animating)
        {
            Close();
            return;
        }

        if (animating) return;

        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        int hovered = handDisplay.GetHoveredCardIndex();
        if (hovered < 0) return;

        if (selectedIndex1 < 0)
        {
            selectedIndex1 = hovered;
            handDisplay.SetSelectedCard(hovered);
            promptText.text = "두 번째 카드를 고르시오";
        }
        else if (hovered == selectedIndex1)
        {
            selectedIndex1 = -1;
            handDisplay.ClearSelectedCard();
            promptText.text = "조합할 카드를 고르시오";
        }
        else
        {
            selectedIndex2 = hovered;
            mergeCoroutine = StartCoroutine(PerformMerge());
        }
    }

    private IEnumerator PerformMerge()
    {
        animating = true;
        handDisplay.ClearSelectedCard();

        if (!planningController.TryGetMergeResult(
                selectedIndex1, selectedIndex2, out ArcanaData result))
        {
            promptText.text = "조합할 수 없는 조합입니다";
            yield return new WaitForSeconds(1f);
            selectedIndex1 = selectedIndex2 = -1;
            animating = false;
            promptText.text = "조합할 카드를 고르시오";
            mergeCoroutine = null;
            yield break;
        }

        promptText.enabled = false;

        playerHand.TryGetCard(selectedIndex1, out ArcanaData card1);
        playerHand.TryGetCard(selectedIndex2, out ArcanaData card2);

        RectTransform leftCard = CreateCardImage(card1, new Vector2(-400f, 0f));
        RectTransform rightCard = CreateCardImage(card2, new Vector2(400f, 0f));

        Vector2 center = Vector2.zero;
        float elapsed = 0f;
        Vector2 leftStart = leftCard.anchoredPosition;
        Vector2 rightStart = rightCard.anchoredPosition;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * cardMoveSpeed;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed));
            leftCard.anchoredPosition = Vector2.Lerp(leftStart, center, t);
            rightCard.anchoredPosition = Vector2.Lerp(rightStart, center, t);
            yield return null;
        }

        leftCard.gameObject.SetActive(false);
        rightCard.gameObject.SetActive(false);
        CreateCardImage(result, Vector2.zero);

        yield return new WaitForSeconds(resultDisplayTime);

        if (!planningController.CommitMerge(selectedIndex1, selectedIndex2, result))
        {
            ClearAnimContainer();
            promptText.enabled = true;
            promptText.text = "조합에 실패했습니다";
            yield return new WaitForSeconds(1f);
            selectedIndex1 = selectedIndex2 = -1;
            animating = false;
            mergeCoroutine = null;
            promptText.text = "조합할 카드를 고르시오";
            yield break;
        }

        ClearAnimContainer();
        animating = false;
        mergeCoroutine = null;
        Close();
    }

    private RectTransform CreateCardImage(ArcanaData card, Vector2 position)
    {
        GameObject obj = new GameObject("MergeCard",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(animContainer.transform, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(200f, 350f);

        Image img = obj.GetComponent<Image>();
        if (card.CardImage != null)
        {
            img.sprite = card.CardImage;
            img.preserveAspect = true;
        }
        else
        {
            img.color = card.CardColor;
        }
        img.raycastTarget = false;

        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
