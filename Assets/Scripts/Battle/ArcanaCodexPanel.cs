using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 전투 중 언제든 열람 가능한 아르카나 도감 패널.
/// 좌측 스크롤 목록에서 아르카나를 고르면 우측에 설명이 뜬다.
/// UI는 전부 코드로 조립한다 (배틀 씬 관례).
/// </summary>
public class ArcanaCodexPanel : MonoBehaviour
{
    private const int FirstArcanaId = 1;
    private const int LastArcanaId = 20;

    [SerializeField] private Canvas canvas;
    [SerializeField] private Font koreanFont;
    [SerializeField] private ArcanaCatalog arcanaCatalog;

    [Header("색")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color windowColor = new Color(0.1f, 0.1f, 0.15f, 0.98f);
    [SerializeField] private Color viewportColor = new Color(0.05f, 0.05f, 0.08f, 1f);
    [SerializeField] private Color itemColor = new Color(0.18f, 0.18f, 0.24f, 1f);
    [SerializeField] private Color selectedItemColor = new Color(0.45f, 0.35f, 0.6f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.25f, 0.2f, 0.35f, 1f);
    [SerializeField] private Color subTextColor = new Color(0.7f, 0.7f, 0.7f);

    [Header("크기")]
    [SerializeField] private Vector2 windowSize = new Vector2(1200f, 700f);
    [SerializeField] private float listWidth = 420f;
    [SerializeField] private float itemHeight = 44f;
    [SerializeField] private float padding = 20f;
    [SerializeField] private float scrollSensitivity = 30f;
    [SerializeField] private Vector2 toggleButtonSize = new Vector2(160f, 50f);
    [SerializeField] private Vector2 toggleButtonOffset = new Vector2(-20f, -20f);

    [Header("폰트 크기")]
    [SerializeField] private int itemFontSize = 20;
    [SerializeField] private int nameFontSize = 30;
    [SerializeField] private int costFontSize = 25;
    [SerializeField] private int descFontSize = 25;
    [SerializeField] private int buttonFontSize = 24;

    private GameObject codexRoot;
    private Text nameText;
    private Text costText;
    private Text descText;

    private readonly List<Image> itemBackgrounds = new();
    private readonly List<int> itemIds = new();
    private int selectedId = FirstArcanaId;

    public bool IsOpen => codexRoot != null && codexRoot.activeSelf;

    /// <summary>좌측 목록 항목 표기: "I - The Magician / 마법사"</summary>
    public static string FormatListLabel(ArcanaData card)
    {
        if (card == null)
            return "";

        return $"{card.DisplayNumber} - {card.ArcanaName} / {card.KoreanName}";
    }

    private void Awake()
    {
        if (koreanFont == null)
            Debug.LogError("[ArcanaCodexPanel] koreanFont가 할당되지 않았습니다.");
        if (canvas == null)
        {
            Debug.LogError("[ArcanaCodexPanel] canvas가 할당되지 않았습니다.");
            return;
        }
        if (arcanaCatalog == null)
            Debug.LogError("[ArcanaCodexPanel] arcanaCatalog가 할당되지 않았습니다.");

        BuildPanel();
        Close();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // ` 키는 열기·닫기 양쪽, Esc는 열려 있을 때만 소비한다.
        if (Keyboard.current.backquoteKey.wasPressedThisFrame)
            Toggle();
        else if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void Open()
    {
        if (codexRoot == null)
            return;

        codexRoot.SetActive(true);
        codexRoot.transform.SetAsLastSibling();
        SelectArcana(selectedId);
    }

    public void Close()
    {
        if (codexRoot != null)
            codexRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void SelectArcana(int id)
    {
        ArcanaData card = arcanaCatalog != null ? arcanaCatalog.GetById(id) : null;
        if (card == null)
            return;

        selectedId = id;

        nameText.text = $"{card.DisplayNumber} {card.KoreanName}";
        costText.text = $"코스트 {card.BaseCost} | {ArcanaInfoPanel.UsageLabel(card.UsageType)}";
        descText.text = card.EffectDescription;

        for (int i = 0; i < itemBackgrounds.Count; i++)
            itemBackgrounds[i].color = itemIds[i] == id ? selectedItemColor : itemColor;
    }

    private void BuildPanel()
    {
        // 토글 버튼은 패널과 별개다. 항상 화면에 남아 있어야 하므로 codexRoot 바깥에 만든다.
        RectTransform toggleRect = CreateButton(canvas.transform, "CodexToggleButton",
            "도감 (`)", toggleButtonSize, Toggle);
        toggleRect.anchorMin = Vector2.one;
        toggleRect.anchorMax = Vector2.one;
        toggleRect.pivot = Vector2.one;
        toggleRect.anchoredPosition = toggleButtonOffset;

        codexRoot = new GameObject("ArcanaCodexRoot");
        codexRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = codexRoot.AddComponent<RectTransform>();
        Stretch(rootRect);
        Image dim = codexRoot.AddComponent<Image>();
        dim.color = dimColor;
        dim.raycastTarget = true;  // 패널 뒤쪽 클릭 차단

        GameObject window = new GameObject("Window");
        window.transform.SetParent(codexRoot.transform, false);
        RectTransform windowRect = window.AddComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = Vector2.zero;
        windowRect.sizeDelta = windowSize;
        Image windowBg = window.AddComponent<Image>();
        windowBg.color = windowColor;
        windowBg.raycastTarget = true;

        RectTransform closeRect = CreateButton(window.transform, "CloseButton",
            "X", new Vector2(44f, 44f), Close);
        closeRect.anchorMin = Vector2.one;
        closeRect.anchorMax = Vector2.one;
        closeRect.pivot = Vector2.one;
        closeRect.anchoredPosition = new Vector2(-10f, -10f);

        BuildList(window.transform);
        BuildDetail(window.transform);
    }

    private void BuildList(Transform parent)
    {
        float listHeight = windowSize.y - padding * 2f;

        GameObject scrollObj = new GameObject("ListScroll");
        scrollObj.transform.SetParent(parent, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        BottomLeft(scrollRect, new Vector2(padding, padding), new Vector2(listWidth, listHeight));
        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = scrollSensitivity;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        Stretch(viewportRect);
        viewportRect.pivot = new Vector2(0f, 1f);
        viewport.AddComponent<RectMask2D>();
        Image viewportBg = viewport.AddComponent<Image>();
        viewportBg.color = viewportColor;
        viewportBg.raycastTarget = true;  // 빈 공간 드래그 스크롤용

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        // 상단 스트레치여야 ContentSizeFitter가 아래로 자란다.
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRect;
        scroll.content = contentRect;

        for (int id = FirstArcanaId; id <= LastArcanaId; id++)
        {
            ArcanaData card = arcanaCatalog != null ? arcanaCatalog.GetById(id) : null;
            if (card == null)
                continue;

            CreateListItem(content.transform, id, FormatListLabel(card));
        }
    }

    private void CreateListItem(Transform parent, int id, string label)
    {
        GameObject item = new GameObject($"Item{id:00}");
        item.transform.SetParent(parent, false);

        RectTransform rect = item.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, itemHeight);

        Image bg = item.AddComponent<Image>();
        bg.color = itemColor;
        bg.raycastTarget = true;

        Button button = item.AddComponent<Button>();
        button.targetGraphic = bg;
        int capturedId = id;
        button.onClick.AddListener(() => SelectArcana(capturedId));

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(item.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        Stretch(textRect);
        textRect.offsetMin = new Vector2(10f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);

        Text text = textObj.AddComponent<Text>();
        text.font = koreanFont;
        text.fontSize = itemFontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = label;
        text.raycastTarget = false;  // 클릭은 부모 Button이 받는다

        itemBackgrounds.Add(bg);
        itemIds.Add(id);
    }

    private void BuildDetail(Transform parent)
    {
        GameObject detail = new GameObject("Detail");
        detail.transform.SetParent(parent, false);
        RectTransform detailRect = detail.AddComponent<RectTransform>();
        float detailX = padding + listWidth + padding;
        float detailWidth = windowSize.x - detailX - padding;
        BottomLeft(detailRect, new Vector2(detailX, padding),
            new Vector2(detailWidth, windowSize.y - padding * 2f));

        float innerHeight = windowSize.y - padding * 2f;
        float nameY = innerHeight - 80f;
        float costY = nameY - 45f;

        nameText = CreateText(detail.transform, "NameText",
            new Vector2(0f, nameY), new Vector2(detailWidth, 45f), nameFontSize);

        costText = CreateText(detail.transform, "CostText",
            new Vector2(0f, costY), new Vector2(detailWidth, 35f), costFontSize);
        costText.color = subTextColor;

        descText = CreateText(detail.transform, "DescText",
            new Vector2(0f, 0f), new Vector2(detailWidth, costY), descFontSize);
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private Text CreateText(Transform parent, string name,
        Vector2 position, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        BottomLeft(rect, position, size);

        Text text = obj.AddComponent<Text>();
        text.font = koreanFont;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private RectTransform CreateButton(Transform parent, string name, string label,
        Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        Image bg = obj.AddComponent<Image>();
        bg.color = buttonColor;
        bg.raycastTarget = true;

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        Stretch(textRect);

        Text text = textObj.AddComponent<Text>();
        text.font = koreanFont;
        text.fontSize = buttonFontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;
        text.raycastTarget = false;

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

    private static void BottomLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
