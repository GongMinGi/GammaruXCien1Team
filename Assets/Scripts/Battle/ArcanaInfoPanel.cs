using UnityEngine;
using UnityEngine.UI;

public class ArcanaInfoPanel : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Font koreanFont;
    [SerializeField] private Color bgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
    [SerializeField] private Vector2 panelSize = new Vector2(650f, 280f);
    [SerializeField] private Vector2 panelOffset = new Vector2(20f, 20f);
    [SerializeField] private int nameFontSize = 24;
    [SerializeField] private int costFontSize = 18;
    [SerializeField] private int descFontSize = 18;

    private GameObject panelRoot;
    private Text nameText;
    private Text costText;
    private Text descText;

    private void Awake()
    {
        if (koreanFont == null)
            Debug.LogError("[ArcanaInfoPanel] koreanFont가 할당되지 않았습니다.");

        BuildPanel();
        Hide();
    }

    public void Show(ArcanaData card)
    {
        nameText.text = $"{card.DisplayNumber} {card.KoreanName}";
        costText.text = $"코스트 {card.BaseCost} | {UsageLabel(card.UsageType)}";
        descText.text = card.EffectDescription;
        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void BuildPanel()
    {
        panelRoot = new GameObject("ArcanaInfoPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        panelRect.anchoredPosition = panelOffset;
        panelRect.sizeDelta = panelSize;

        Image bg = panelRoot.AddComponent<Image>();
        bg.color = bgColor;
        bg.raycastTarget = false;

        float innerWidth = panelSize.x - 20f;
        float nameY = panelSize.y - 50f;
        float costY = nameY - 35f;

        nameText = CreateText(panelRoot.transform, "NameText",
            new Vector2(10f, nameY), new Vector2(innerWidth, 40f), nameFontSize);

        costText = CreateText(panelRoot.transform, "CostText",
            new Vector2(10f, costY), new Vector2(innerWidth, 30f), costFontSize);
        costText.color = new Color(0.7f, 0.7f, 0.7f);

        descText = CreateText(panelRoot.transform, "DescText",
            new Vector2(10f, 10f), new Vector2(innerWidth, costY - 10f), descFontSize);
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private Text CreateText(Transform parent, string name,
        Vector2 position, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = obj.AddComponent<Text>();
        text.font = koreanFont;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private static string UsageLabel(ArcanaUsageType type)
    {
        return type switch
        {
            ArcanaUsageType.Timeline => "타임라인",
            ArcanaUsageType.Observation => "관측",
            ArcanaUsageType.Instant => "즉시",
            ArcanaUsageType.HeldPassive => "보유 효과",
            ArcanaUsageType.AlwaysAvailable => "기본",
            ArcanaUsageType.EventOnly => "이벤트",
            _ => ""
        };
    }
}
