using UnityEngine;

public class BossAI : MonoBehaviour
{
    public BossAction[] GeneratePattern()
    {
        return new BossAction[]
        {
            new BossAction
            {
                timingSlot = 2,
                arcanaIds = new[] { 2 },
                targetCells = new[] { new Vector2Int(1, 0) }
            },
            new BossAction
            {
                timingSlot = 5,
                arcanaIds = new[] { 3 },
                targetCells = new[]
                {
                    new Vector2Int(0, -1),
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1)
                }
            },
            new BossAction
            {
                timingSlot = 8,
                arcanaIds = new[] { 2, 4 },
                targetCells = new[]
                {
                    new Vector2Int(-1, 1),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                }
            }
        };
    }
}
