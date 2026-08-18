using UnityEngine;

public struct PlannedAction
{
    public ActionType Type;
    public Vector2Int Direction;
    public int Cost;
    public ArcanaData CardData;
    public int OriginalHandIndex;

    // 합치기 전용
    public ArcanaData MergeSource1;
    public int MergeSourceIndex1;
    public ArcanaData MergeSource2;
    public int MergeSourceIndex2;
    public int MergeResultIndex;
}
