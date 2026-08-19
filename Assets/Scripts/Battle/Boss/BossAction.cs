using UnityEngine;

/// 실행 중 수레바퀴 위치를 참조해 목표를 다시 계산하는 방식
public enum WheelTargeting { None, Column, Bomb }

[System.Serializable]
public struct BossAction
{
    public int timingSlot;
    public int[] arcanaIds;
    public Vector2Int[] targetCells;
    public int baseDamage;
    public bool isInstantKill;
    public DamageElement element;

    /// 시전이 시작되는 슬롯. wheelTargeting이 None이 아닐 때만 의미가 있다.
    public int castSlot;
    /// 은둔자 회피를 무시한다 (반드시 명중)
    public bool ignoresDodge;
    /// 실타래 안에 있으면 막힌다 (나이프 투척)
    public bool blockedByTangle;
    /// 명중 시 수레바퀴가 1칸 우회전한다
    public bool rotatesWheelOnHit;
    /// 시전 시작 슬롯에서 수레바퀴 위치로 targetCells를 덮어쓴다
    public WheelTargeting wheelTargeting;
}
