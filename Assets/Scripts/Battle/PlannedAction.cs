using UnityEngine;

public struct PlannedAction
{
    public ActionType Type;
    public Vector2Int Direction;
    public int Cost;
    public ArcanaData CardData;
    public int OriginalHandIndex;

    // UseInstantCard용
    public InstantModifierType ModifierType;
    public DamageElement SelectedElement;

    // UseCard용 — 소비 추적 (언두 복원용)
    public InstantModifierType ConsumedCostModifier;
    public bool ConsumedElementBuff;
    public DamageElement ConsumedElement;
    public Vector2Int PreCardPosition;  // 카드 사용 전 위치 (이동 카드 undo용)

    // 합치기 전용
    public ArcanaData MergeSource1;
    public int MergeSourceIndex1;
    public ArcanaData MergeSource2;
    public int MergeSourceIndex2;
    public int MergeResultIndex;

    // 관측 카드 전용 (undo용)
    public ObservationActionType ObservationAction;
    public ArcanaData DrawnCard;           // DrawCard: 드로우된 카드
    public int DrawnCardIndex;             // DrawCard: 드로우된 카드 핸드 인덱스
    public int TransformTargetIndex;       // TransformCard: 교체 대상 위치
    public ArcanaData TransformOriginal;   // TransformCard: 교체 전 원본 카드
    public ArcanaData TransformReplacement; // TransformCard: 교체된 카드
}
