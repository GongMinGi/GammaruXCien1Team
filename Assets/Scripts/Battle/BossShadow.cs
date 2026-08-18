using UnityEngine;

public class BossShadow : MonoBehaviour
{
    [SerializeField] private float shadowWidth = 2.2f;
    [SerializeField] private float shadowHeight = 0.6f;
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.4f);

    private bool isGenerated;

    private void Start()
    {
        GenerateShadow();
    }

    private void GenerateShadow()
    {
        if (isGenerated)
            return;

        Sprite sprite = CreateEllipseSprite();

        GameObject shadowObject = new GameObject("Shadow");
        shadowObject.transform.SetParent(transform);
        shadowObject.transform.localPosition = Vector3.zero;
        shadowObject.transform.localScale = new Vector3(shadowWidth, shadowHeight, 1f);

        SpriteRenderer renderer = shadowObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;

        isGenerated = true;
    }

    private Sprite CreateEllipseSprite()
    {
        int w = 32;
        int h = 16;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;

        float cx = (w - 1) * 0.5f;
        float cy = (h - 1) * 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / cx;
                float dy = (y - cy) / cy;
                float dist = dx * dx + dy * dy;

                if (dist <= 1f)
                {
                    float alpha = shadowColor.a * (1f - dist);
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32);
    }
}
