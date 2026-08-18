using UnityEngine;

public class PlayerStatsDisplay : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ActionBar actionBar;

    [Header("HP Bar")]
    [SerializeField] private float barHeight = 0.25f;
    [SerializeField] private Color bgColor = new Color(0.2f, 0.2f, 0.2f);

    private float barWidth;
    private Transform hpFillTransform;
    private SpriteRenderer hpFillRenderer;
    private TextMesh hpText;
    private TextMesh spellPowerText;

    private void Start()
    {
        if (playerStats == null || actionBar == null)
        {
            Debug.LogError("References not assigned.", this);
            enabled = false;
            return;
        }

        barWidth = actionBar.VisualWidth;
        GenerateVisuals();
        playerStats.StatsChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.StatsChanged -= Refresh;
    }

    private void GenerateVisuals()
    {
        Sprite barSprite = CreateBarSprite();

        // Background
        GameObject bgObj = new GameObject("HpBarBg");
        bgObj.transform.SetParent(transform, false);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        SpriteRenderer bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = barSprite;
        bgRenderer.color = bgColor;
        bgRenderer.sortingOrder = 1;

        // Fill (left-aligned pivot)
        Sprite fillSprite = CreateFillSprite();

        GameObject fillObj = new GameObject("HpBarFill");
        fillObj.transform.SetParent(transform, false);
        fillObj.transform.localPosition = new Vector3(-barWidth * 0.5f, 0f, 0f);
        fillObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        hpFillRenderer = fillObj.AddComponent<SpriteRenderer>();
        hpFillRenderer.sprite = fillSprite;
        hpFillRenderer.color = Color.green;
        hpFillRenderer.sortingOrder = 2;
        hpFillTransform = fillObj.transform;

        // HP Text
        GameObject hpTextObj = new GameObject("HpText");
        hpTextObj.transform.SetParent(transform, false);
        hpTextObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        hpText = hpTextObj.AddComponent<TextMesh>();
        hpText.anchor = TextAnchor.MiddleCenter;
        hpText.alignment = TextAlignment.Center;
        hpText.fontSize = 32;
        hpText.characterSize = 0.06f;
        hpText.color = Color.white;

        MeshRenderer hpTextRenderer = hpTextObj.GetComponent<MeshRenderer>();
        hpTextRenderer.sortingOrder = 3;

        // Spell Power Text
        GameObject spObj = new GameObject("SpellPowerText");
        spObj.transform.SetParent(transform, false);
        spObj.transform.localPosition = new Vector3(barWidth * 0.5f + 0.5f, 0f, -0.01f);

        spellPowerText = spObj.AddComponent<TextMesh>();
        spellPowerText.anchor = TextAnchor.MiddleLeft;
        spellPowerText.alignment = TextAlignment.Left;
        spellPowerText.fontSize = 32;
        spellPowerText.characterSize = 0.08f;
        spellPowerText.color = new Color(0.6f, 0.7f, 1f);

        MeshRenderer spRenderer = spObj.GetComponent<MeshRenderer>();
        spRenderer.sortingOrder = 3;
    }

    private void Refresh()
    {
        float ratio = Mathf.Clamp01(
            (float)playerStats.CurrentHp / Mathf.Max(1, playerStats.MaxHp));

        hpFillTransform.localScale = new Vector3(ratio * barWidth, barHeight, 1f);
        hpFillRenderer.color = Color.Lerp(Color.red, Color.green, ratio);
        hpText.text = $"{playerStats.CurrentHp}/{playerStats.MaxHp}";
        spellPowerText.text = $"SP {playerStats.SpellPower}";
    }

    private Sprite CreateBarSprite()
    {
        int size = 4;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.white);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Sprite CreateFillSprite()
    {
        int size = 4;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.white);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0f, 0.5f), size);
    }
}
