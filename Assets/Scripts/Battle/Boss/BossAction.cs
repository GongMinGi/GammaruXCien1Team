using UnityEngine;

[System.Serializable]
public struct BossAction
{
    public int timingSlot;
    public int[] arcanaIds;
    public Vector2Int[] targetCells;
}
