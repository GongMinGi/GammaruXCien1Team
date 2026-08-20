using UnityEngine;
using UnityEngine.UI;

public class BossStatsDisplay : MonoBehaviour
{
    [SerializeField] private BossStats bossStats;
    [SerializeField] private Slider hpSlider;

    private TextMesh hpText;

    private void Start()
    {
        if (bossStats == null)
        {
            Debug.LogError("BossStats reference not assigned.", this);
            enabled = false;
            return;
        }

        GenerateTexts();
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
    }

    private void Refresh()
    {
        float ratio = Mathf.Clamp01(
            (float)bossStats.CurrentHp / Mathf.Max(1, bossStats.MaxHp));

        hpSlider.value = ratio;
        hpText.text = $"{bossStats.CurrentHp}/{bossStats.MaxHp}";
    }

    private void ShowDamage(int amount)
    {
        DamagePopup.Spawn(transform, amount, new Vector3(0f, 0.55f, -0.02f));
    }
}
