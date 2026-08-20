using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsDisplay : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ActionBar actionBar;
    [SerializeField] private Slider hpSlider;

    private float barWidth;
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
        GenerateTexts();
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

    private void GenerateTexts()
    {
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

        hpSlider.value = ratio;
        hpText.text = $"{playerStats.CurrentHp}/{playerStats.MaxHp}";
        spellPowerText.text = $"SP {playerStats.SpellPower}";
    }

    private void ShowDamage(int amount)
    {
        DamagePopup.Spawn(transform, amount, new Vector3(0f, 0.55f, -0.02f));
    }
}
