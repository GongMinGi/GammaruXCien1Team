using UnityEngine;
using UnityEngine.UI;

public class BossStatsDisplay : MonoBehaviour
{
    [SerializeField] private BossStats bossStats;
    [SerializeField] private Font font;

    [Header("HP Bar")]
    [SerializeField] private float barWidth = 445f;
    [SerializeField] private float barHeight = 30f;
    [SerializeField] private Color bgColor = new Color(0.2f, 0.2f, 0.2f);

    private RectTransform hpFillRect;
    private Image hpFillImage;
    private Text hpText;
    private Canvas canvas;

    private void Start()
    {
        if (bossStats == null)
        {
            Debug.LogError("BossStats reference not assigned.", this);
            enabled = false;
            return;
        }

        GenerateVisuals();
        bossStats.StatsChanged += Refresh;
        bossStats.DamageTaken += ShowDamage;
        Refresh();
    }

    private void OnDestroy()
    {
        if (bossStats != null)
        {
            bossStats.StatsChanged -= Refresh;
            bossStats.DamageTaken -= ShowDamage;
        }
    }

    private void GenerateVisuals()
    {
        GameObject canvasObj = new GameObject("BossStatsCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = Vector3.zero;

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(barWidth + 20f, barHeight + 20f);
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
        hpFillRect.sizeDelta = new Vector2(barWidth, barHeight);
        hpFillRect.pivot = new Vector2(0f, 0.5f);
        hpFillRect.anchoredPosition = new Vector2(-barWidth * 0.5f, 0f);

        hpFillImage = fillObj.AddComponent<Image>();
        hpFillImage.color = Color.red;
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
    }

    private void Refresh()
    {
        float ratio = Mathf.Clamp01(
            (float)bossStats.CurrentHp / Mathf.Max(1, bossStats.MaxHp));

        hpFillRect.sizeDelta = new Vector2(ratio * barWidth, barHeight);
        hpFillImage.color = Color.Lerp(Color.red, new Color(0.8f, 0.2f, 0.2f), ratio);
        hpText.text = $"{bossStats.CurrentHp}/{bossStats.MaxHp}";
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
