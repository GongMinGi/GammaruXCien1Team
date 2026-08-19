using System.Collections.Generic;
using UnityEngine;

public static class BossTargetResolver
{
    public static Vector2Int[] Center(Vector2Int origin)
    {
        return FilterToGrid(new[] { origin });
    }

    public static Vector2Int[] CardinalNeighbors(Vector2Int origin)
    {
        return FilterToGrid(new[]
        {
            origin + Vector2Int.up,
            origin + Vector2Int.down,
            origin + Vector2Int.left,
            origin + Vector2Int.right
        });
    }

    public static Vector2Int[] Cross()
    {
        List<Vector2Int> cells = new();
        for (int coordinate = -2; coordinate <= 2; coordinate++)
        {
            cells.Add(new Vector2Int(coordinate, 0));
            cells.Add(new Vector2Int(0, coordinate));
        }
        return FilterToGrid(cells);
    }

    public static Vector2Int[] DiagonalCross()
    {
        List<Vector2Int> cells = new();
        for (int coordinate = -2; coordinate <= 2; coordinate++)
        {
            cells.Add(new Vector2Int(coordinate, coordinate));
            cells.Add(new Vector2Int(coordinate, -coordinate));
        }
        return FilterToGrid(cells);
    }

    public static Vector2Int[] DiamondPerimeter()
    {
        return ManhattanRing(Vector2Int.zero, 2);
    }

    public static Vector2Int[] ManhattanRing(Vector2Int origin, int distance)
    {
        List<Vector2Int> cells = new();
        for (int y = -distance; y <= distance; y++)
        {
            for (int x = -distance; x <= distance; x++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) == distance)
                    cells.Add(origin + new Vector2Int(x, y));
            }
        }
        return FilterToGrid(cells);
    }

    private static Vector2Int[] FilterToGrid(IEnumerable<Vector2Int> candidates)
    {
        HashSet<Vector2Int> unique = new();
        foreach (Vector2Int cell in candidates)
        {
            if (cell.x >= -GridManager.Columns / 2 &&
                cell.x <= GridManager.Columns / 2 &&
                cell.y >= -GridManager.Rows / 2 &&
                cell.y <= GridManager.Rows / 2)
            {
                unique.Add(cell);
            }
        }

        Vector2Int[] result = new Vector2Int[unique.Count];
        unique.CopyTo(result);
        return result;
    }
}
