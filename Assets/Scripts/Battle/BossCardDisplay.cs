using UnityEngine;

public class BossCardDisplay : MonoBehaviour
{
    [SerializeField] private float cardWidth = 0.7f;
    [SerializeField] private float cardHeight = 1f;
    [SerializeField] private float spacing = 0.1f;
    [SerializeField] private Color cardColor = new Color(0.3f, 0.15f, 0.25f);
    [SerializeField] private Color borderColor = new Color(0.5f, 0.2f, 0.3f);

    private const int CardCount = 4;
    private bool isGenerated;

    private void Start()
    {
        GenerateCards();
    }

    private void GenerateCards()
    {
        if (isGenerated)
            return;

        Sprite sprite = CreateCardSprite();
        float step = cardHeight + spacing;
        float startY = (CardCount - 1) * step * 0.5f;

        for (int i = 0; i < CardCount; i++)
        {
            float y = startY - i * step;

            GameObject cardObject = new GameObject($"Card ({i + 1})");
            cardObject.transform.SetParent(transform);
            cardObject.transform.localPosition = new Vector3(0f, y, 0f);
            cardObject.transform.localScale = new Vector3(cardWidth, cardHeight, 1f);

            SpriteRenderer renderer = cardObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 2;
        }

        isGenerated = true;
    }

    private Sprite CreateCardSprite()
    {
        int w = 16;
        int h = 24;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                tex.SetPixel(x, y, isBorder ? borderColor : cardColor);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
    }
}
