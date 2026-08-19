using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "실타래에 꿰인 광대"의 수레바퀴 계산. 상태 없이 순수 계산만 한다.
/// 칸 번호는 넘패드 배열(5 없음)을 따른다.
/// </summary>
public static class ClownWheel
{
    /// 시계방향 순서: 7→8→9→6→3→2→1→4→7
    private static readonly int[] Clockwise = { 7, 8, 9, 6, 3, 2, 1, 4 };

    private static readonly int[] DiagonalColumns = { -2, 0, 2 };
    private static readonly int[] OrthogonalColumns = { -1, 1 };

    /// 전투 시작 위치
    public const int StartCell = 1;

    /// 1칸 우측(시계방향) 회전
    public static int Rotate(int cell, int steps = 1)
    {
        int index = System.Array.IndexOf(Clockwise, cell);
        if (index < 0)
            return cell;

        return Clockwise[((index + steps) % Clockwise.Length + Clockwise.Length) % Clockwise.Length];
    }

    /// 점대칭 반대편. 1↔9, 2↔8, 3↔7, 4↔6
    public static int Opposite(int cell)
    {
        return 10 - cell;
    }

    /// 넘패드 칸 번호 → 방향 벡터
    public static Vector2Int ToDirection(int cell)
    {
        return new Vector2Int((cell - 1) % 3 - 1, (cell - 1) / 3 - 1);
    }

    /// 대각 칸(1, 3, 7, 9) 여부
    public static bool IsDiagonal(int cell)
    {
        Vector2Int direction = ToDirection(cell);
        return direction.x != 0 && direction.y != 0;
    }

    /// VII: 대각 칸이면 세로 1·3·5줄, 직교 칸이면 세로 2·4줄
    public static Vector2Int[] ColumnCells(int wheel)
    {
        int[] columns = IsDiagonal(wheel) ? DiagonalColumns : OrthogonalColumns;

        List<Vector2Int> cells = new();
        foreach (int x in columns)
            for (int y = -GridManager.Rows / 2; y <= GridManager.Rows / 2; y++)
                cells.Add(new Vector2Int(x, y));

        return BossTargetResolver.FilterToGrid(cells);
    }

    /// VIII: 수레바퀴 방향 벡터를 중심으로 한 3×3
    public static Vector2Int[] BombCells(int wheel)
    {
        Vector2Int center = ToDirection(wheel);

        List<Vector2Int> cells = new();
        for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                cells.Add(center + new Vector2Int(x, y));

        return BossTargetResolver.FilterToGrid(cells);
    }

    /// II / II,II: 지정한 가로줄(y) 전체
    public static Vector2Int[] RowCells(params int[] rows)
    {
        List<Vector2Int> cells = new();
        foreach (int y in rows)
            for (int x = -GridManager.Columns / 2; x <= GridManager.Columns / 2; x++)
                cells.Add(new Vector2Int(x, y));

        return BossTargetResolver.FilterToGrid(cells);
    }

    /// 실타래 십자 5칸. 중심은 항상 (±1,±1)이라 그리드를 벗어나지 않는다.
    public static Vector2Int[] CrossCells(Vector2Int center)
    {
        return new[]
        {
            center,
            center + Vector2Int.up,
            center + Vector2Int.down,
            center + Vector2Int.left,
            center + Vector2Int.right
        };
    }

    /// <summary>
    /// 실타래 중심: 칸 7/9/17/19 = (±1,±1) 중 플레이어의 반대편 구역.
    /// 플레이어가 3번째 가로줄/세로줄이면 그 축만 랜덤, 정중앙이면 4곳 랜덤.
    /// </summary>
    public static Vector2Int PickTangleCenter(Vector2Int playerPos, System.Random rng)
    {
        int signX = playerPos.x == 0 ? 0 : (playerPos.x > 0 ? 1 : -1);
        int signY = playerPos.y == 0 ? 0 : (playerPos.y > 0 ? 1 : -1);

        int x = signX != 0 ? -signX : (rng.Next(2) == 0 ? 1 : -1);
        int y = signY != 0 ? -signY : (rng.Next(2) == 0 ? 1 : -1);

        return new Vector2Int(x, y);
    }
}
