using UnityEngine;

public struct PlannedAction
{
    public ActionType Type;
    public Vector2Int Direction;
    public int Cost;
    public ArcanaData CardData;
    public int OriginalHandIndex;
}
