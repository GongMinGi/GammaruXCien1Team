using UnityEngine;

/// 수레바퀴 8칸(넘패드 배열)과 현재 위치 하이라이트.
public class WheelDisplay : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float cellSize = 0.25f;
    [SerializeField] private float spacing = 0.05f;
    [SerializeField] private Color idleColor = new Color(0.35f, 0.3f, 0.42f);
    [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color borderColor = new Color(0.15f, 0.12f, 0.2f);

    /// 5를 제외한 넘패드 8칸
    private static readonly int[] Cells = { 7, 8, 9, 4, 6, 1, 2, 3 };

    private SpriteRenderer[] cellRenderers;
    private int currentCell = ClownWheel.StartCell;

    private void Awake()
    {
        GenerateCells();
        SetPosition(currentCell);
    }

    public void SetPosition(int cell)
    {
        currentCell = cell;

        if (cellRenderers == null)
            return;

        for (int i = 0; i < Cells.Length; i++)
            cellRenderers[i].color = Cells[i] == cell ? activeColor : idleColor;
    }

    private void GenerateCells()
    {
        if (cellRenderers != null)
            return;

        Sprite sprite = CreateCellSprite();
        float step = cellSize + spacing;
        cellRenderers = new SpriteRenderer[Cells.Length];

        for (int i = 0; i < Cells.Length; i++)
        {
            Vector2Int direction = ClownWheel.ToDirection(Cells[i]);

            GameObject cellObject = new GameObject($"Wheel ({Cells[i]})");
            cellObject.transform.SetParent(transform);
            cellObject.transform.localPosition =
                new Vector3(direction.x * step, direction.y * step, 0f);
            cellObject.transform.localScale = new Vector3(cellSize, cellSize, 1f);

            SpriteRenderer renderer = cellObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1;

            cellRenderers[i] = renderer;
        }
    }

    private Sprite CreateCellSprite()
    {
        int size = 16;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                texture.SetPixel(x, y, isBorder ? borderColor : Color.white);
            }
        }

        texture.Apply();
        return Sprite.Create(
            texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
