using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsDisplay : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ActionBar actionBar;
    [SerializeField] private Font font;

    [Header("HP Bar")]
    [SerializeField] private float barHeight = 30f;
    [SerializeField] private Color bgColor = new Color(0.2f, 0.2f, 0.2f);

    private RectTransform hpFillRect;
    private Image hpFillImage;
    private Text hpText;
    private Text spellPowerText;
    private Canvas canvas;
    private float barWidth;

    private void Start()
    {
        if (playerStats == null || actionBar == null)
        {
            Debug.LogError("References not assigned.", this);
            enabled = false;
            return;
        }

        barWidth = actionBar.VisualWidth * 100f;
        GenerateVisuals();
        playerStats.StatsChanged += Refresh;
        playerStats.DamageTaken += ShowDamage;
        Refresh();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.StatsChanged -= Refresh;
            playerStats.DamageTaken -= ShowDamage;
        }
    }

    private void GenerateVisuals()
    {
        // Canvas (world space, same position as this transform)
        GameObject canvasObj = new GameObject("StatsCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = Vector3.zero;

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(barWidth + 120f, barHeight + 20f);
        canvasRect.localScale = Vector3.one * 0.01f;

        // Background
        GameObject bgObj = CreateUIObject("HpBarBg", canvasObj.transform);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(barWidth, barHeight);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = bgColor;
        bgImage.raycastTarget = false;

        // Fill
        GameObject fillObj = CreateUIObject("HpBarFill", canvasObj.transform);
        hpFillRect = fillObj.GetComponent<RectTransform>();
        hpFillRect.anchoredPosition = Vector2.zero;
        hpFillRect.sizeDelta = new Vector2(barWidth, barHeight);
        hpFillRect.pivot = new Vector2(0f, 0.5f);
        hpFillRect.anchoredPosition = new Vector2(-barWidth * 0.5f, 0f);

        hpFillImage = fillObj.AddComponent<Image>();
        hpFillImage.color = Color.green;
        hpFillImage.raycastTarget = false;

        // HP Text
        GameObject hpTextObj = CreateUIObject("HpText", canvasObj.transform);
        RectTransform hpTextRect = hpTextObj.GetComponent<RectTransform>();
        hpTextRect.anchoredPosition = Vector2.zero;
        hpTextRect.sizeDelta = new Vector2(barWidth, barHeight);

        hpText = hpTextObj.AddComponent<Text>();
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.fontSize = 20;
        hpText.color = Color.white;
        hpText.raycastTarget = false;
        if (font != null) hpText.font = font;

        // Spell Power Text
        GameObject spObj = CreateUIObject("SpellPowerText", canvasObj.transform);
        RectTransform spRect = spObj.GetComponent<RectTransform>();
        spRect.anchoredPosition = new Vector2(barWidth * 0.5f + 60f, 0f);
        spRect.sizeDelta = new Vector2(120f, barHeight);

        spellPowerText = spObj.AddComponent<Text>();
        spellPowerText.alignment = TextAnchor.MiddleLeft;
        spellPowerText.fontSize = 22;
        spellPowerText.color = new Color(0.6f, 0.7f, 1f);
        spellPowerText.raycastTarget = false;
        if (font != null) spellPowerText.font = font;
    }

    private void Refresh()
    {
        float ratio = Mathf.Clamp01(
            (float)playerStats.CurrentHp / Mathf.Max(1, playerStats.MaxHp));

        hpFillRect.sizeDelta = new Vector2(ratio * barWidth, barHeight);
        hpFillImage.color = Color.Lerp(Color.red, Color.green, ratio);
        hpText.text = $"{playerStats.CurrentHp}/{playerStats.MaxHp}";
        spellPowerText.text = $"SP {playerStats.SpellPower}";
    }

    private void ShowDamage(int amount)
    {
        DamagePopup.Spawn(canvas.transform, amount, new Vector3(0f, 55f, 0f), font);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }
}
