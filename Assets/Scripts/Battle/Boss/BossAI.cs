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
                targetCells = new[] { new Vector2Int(1, 0) },
                baseDamage = 100,
                isInstantKill = false,
                element = DamageElement.Neutral
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
                },
                baseDamage = 150,
                isInstantKill = false,
                element = DamageElement.Sun
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
                },
                baseDamage = 0,
                isInstantKill = true,
                element = DamageElement.Neutral
            }
        };
    }
}
