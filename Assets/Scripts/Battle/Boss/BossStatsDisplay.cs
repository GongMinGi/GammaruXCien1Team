using UnityEngine;

public class BossStatsDisplay : MonoBehaviour
{
    [SerializeField] private BossStats bossStats;

    [Header("HP Bar")]
    [SerializeField] private float barWidth = 4.45f;
    [SerializeField] private float barHeight = 0.25f;
    [SerializeField] private Color bgColor = new Color(0.2f, 0.2f, 0.2f);

    private Transform hpFillTransform;
    private SpriteRenderer hpFillRenderer;
    private TextMesh hpText;

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
        Sprite barSprite = CreateBarSprite();

        GameObject bgObj = new GameObject("HpBarBg");
        bgObj.transform.SetParent(transform, false);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        SpriteRenderer bgRenderer = bgObj.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = barSprite;
        bgRenderer.color = bgColor;
        bgRenderer.sortingOrder = 1;

        Sprite fillSprite = CreateFillSprite();

        GameObject fillObj = new GameObject("HpBarFill");
        fillObj.transform.SetParent(transform, false);
        fillObj.transform.localPosition = new Vector3(-barWidth * 0.5f, 0f, 0f);
        fillObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        hpFillRenderer = fillObj.AddComponent<SpriteRenderer>();
        hpFillRenderer.sprite = fillSprite;
        hpFillRenderer.color = Color.red;
        hpFillRenderer.sortingOrder = 2;
        hpFillTransform = fillObj.transform;

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
    }

    private void Refresh()
    {
        float ratio = Mathf.Clamp01(
            (float)bossStats.CurrentHp / Mathf.Max(1, bossStats.MaxHp));

        hpFillTransform.localScale = new Vector3(ratio * barWidth, barHeight, 1f);
        hpFillRenderer.color = Color.Lerp(Color.red, new Color(0.8f, 0.2f, 0.2f), ratio);
        hpText.text = $"{bossStats.CurrentHp}/{bossStats.MaxHp}";
    }

    private void ShowDamage(int amount)
    {
        DamagePopup.Spawn(transform, amount, new Vector3(0f, 0.55f, -0.02f));
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
