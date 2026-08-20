using UnityEngine;
using UnityEngine.UI;

public class DamagePopup : MonoBehaviour
{
    private const float Lifetime = 0.9f;
    private Text text;
    private RectTransform rectTransform;
    private Vector2 startPosition;
    private float age;
    private float phase;

    public static void Spawn(Transform parent, int amount, Vector3 localPosition, Font font = null)
    {
        if (parent == null || amount <= 0) return;
        GameObject obj = new GameObject(nameof(DamagePopup), typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(localPosition.x, localPosition.y);
        rt.sizeDelta = new Vector2(200f, 50f);
        obj.AddComponent<DamagePopup>().Initialize(amount, font);
    }

    private void Initialize(int amount, Font font)
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
        phase = Random.Range(0f, Mathf.PI * 2f);

        text = gameObject.AddComponent<Text>();
        text.text = "-" + amount;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.2f, 0.12f);
        text.raycastTarget = false;
        if (font != null) text.font = font;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / Lifetime);
        float shake = Mathf.Sin(age * 42f + phase) * 12f * (1f - t);
        rectTransform.anchoredPosition = startPosition + new Vector2(shake, t * 90f);
        float scale = t < 0.2f ? Mathf.Lerp(0.65f, 1.2f, t / 0.2f)
            : Mathf.Lerp(1.2f, 0.9f, (t - 0.2f) / 0.8f);
        transform.localScale = Vector3.one * scale;
        Color color = text.color;
        color.a = 1f - Mathf.SmoothStep(0f, 1f, t);
        text.color = color;
        if (age >= Lifetime) Destroy(gameObject);
    }
}
