using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private Sprite cellSprite;
    [SerializeField] private float cellSize = 1f;

    private const int Rows = 5;
    private const int Columns = 5;

    private GridCell[,] cells;
    private bool isGenerated;

    private void Start()
    {
        GenerateGrid();
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

        float diagonal = cellSize * 0.7071f; // cellSize / sqrt(2)

        for (int y = -halfRows; y <= halfRows; y++)
        {
            for (int x = -halfColumns; x <= halfColumns; x++)
            {
                float worldX = (x - y) * diagonal;
                float worldY = (x + y) * diagonal;
                Vector3 localPosition = new Vector3(worldX, worldY, 0f);

                GameObject cellObject = new GameObject($"Cell ({x}, {y})");
                cellObject.transform.SetParent(transform);
                cellObject.transform.localPosition = localPosition;
                cellObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

                SpriteRenderer renderer = cellObject.AddComponent<SpriteRenderer>();
                renderer.sprite = cellSprite;

                GridCell cell = cellObject.AddComponent<GridCell>();
                cell.Initialize(x, y);

                cells[x + halfColumns, y + halfRows] = cell;
            }
        }

        isGenerated = true;
    }
}
