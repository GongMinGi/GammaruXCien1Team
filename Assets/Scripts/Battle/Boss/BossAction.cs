using UnityEngine;

[System.Serializable]
public struct BossAction
{
    public int timingSlot;
    public int[] arcanaIds;
    public Vector2Int[] targetCells;
    public int baseDamage;
    public bool isInstantKill;
    public DamageElement element;
}
