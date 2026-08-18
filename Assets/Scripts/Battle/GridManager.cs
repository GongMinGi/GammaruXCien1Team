using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private Sprite cellSprite;
    [SerializeField, Min(0.01f)] private float tileWidth = 1.2f;
    [SerializeField, Min(0.01f)] private float tileHeight = 0.6f;

    private const int Rows = 5;
    private const int Columns = 5;

    private GridCell[,] cells;
    private bool isGenerated;

    private void Start()
    {
        GenerateGrid();
    }

    public Vector3 GridToWorldPosition(int x, int y)
    {
        return transform.TransformPoint(GridToLocalPosition(x, y));
    }

    private Vector3 GridToLocalPosition(int x, int y)
    {
        return new Vector3(
            (x - y) * tileWidth * 0.5f,
            (x + y) * tileHeight * 0.5f,
            0f);
    }

    private void GenerateGrid()
    {
        if (isGenerated)
            return;

        if (cellSprite == null)
        {
            Debug.LogError("Grid cell sprite is not assigned.", this);
            return;
        }

        int halfRows = Rows / 2;
        int halfColumns = Columns / 2;
        cells = new GridCell[Columns, Rows];

        for (int y = -halfRows; y <= halfRows; y++)
        {
            for (int x = -halfColumns; x <= halfColumns; x++)
            {
                Vector3 localPosition = GridToLocalPosition(x, y);

                GameObject cellRoot = new GameObject($"Cell ({x}, {y})");
                cellRoot.transform.SetParent(transform);
                cellRoot.transform.localPosition = localPosition;

                GridCell cell = cellRoot.AddComponent<GridCell>();
                cell.Initialize(x, y);

                GameObject compression = new GameObject("VisualCompression");
                compression.transform.SetParent(cellRoot.transform);
                compression.transform.localPosition = Vector3.zero;
                compression.transform.localScale = new Vector3(1f, tileHeight / tileWidth, 1f);

                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(compression.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                float visualScale = tileWidth / Mathf.Sqrt(2f);
                visual.transform.localScale = new Vector3(visualScale, visualScale, 1f);

                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = cellSprite;

                cells[x + halfColumns, y + halfRows] = cell;
            }
        }

        isGenerated = true;
    }
}
