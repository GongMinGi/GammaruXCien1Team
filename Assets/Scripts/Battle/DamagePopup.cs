using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    private const float Lifetime = 0.9f;
    private TextMesh textMesh;
    private Vector3 startPosition;
    private float age;
    private float phase;

    public static void Spawn(Transform parent, int amount, Vector3 localPosition)
    {
        if (parent == null || amount <= 0) return;
        GameObject obj = new GameObject(nameof(DamagePopup));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.AddComponent<DamagePopup>().Initialize(amount);
    }

    private void Initialize(int amount)
    {
        startPosition = transform.localPosition;
        phase = Random.Range(0f, Mathf.PI * 2f);
        textMesh = gameObject.AddComponent<TextMesh>();
        textMesh.text = ((char)45).ToString() + amount;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.075f;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = new Color(1f, 0.2f, 0.12f);
        GetComponent<MeshRenderer>().sortingOrder = 20;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / Lifetime);
        float shake = Mathf.Sin(age * 42f + phase) * 0.12f * (1f - t);
        transform.localPosition = startPosition + new Vector3(shake, t * 0.9f, 0f);
        float scale = t < 0.2f ? Mathf.Lerp(0.65f, 1.2f, t / 0.2f)
            : Mathf.Lerp(1.2f, 0.9f, (t - 0.2f) / 0.8f);
        transform.localScale = Vector3.one * scale;
        Color color = textMesh.color;
        color.a = 1f - Mathf.SmoothStep(0f, 1f, t);
        textMesh.color = color;
        if (age >= Lifetime) Destroy(gameObject);
    }
}
