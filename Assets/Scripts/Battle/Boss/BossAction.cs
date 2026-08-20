using UnityEngine;

/// 실행 중 수레바퀴 위치를 참조해 목표를 다시 계산하는 방식
public enum WheelTargeting { None, Column, Bomb }

/// 보스 몸체 애니메이션 종류
public enum BossAnimationCue { None, Primary, Heavy, Ultimate }

/// 타격 지점 이펙트 종류
public enum BossImpactEffect { None, FallingStone, GroundBurst, ShockwaveRing }

[System.Serializable]
public struct BossAnimationEvent
{
    public int slot;
    public BossAnimationCue cue;
}

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

    /// 몸체 애니메이션 이벤트 목록 (null이면 연출 없음)
    public BossAnimationEvent[] animationEvents;
    /// 타격 지점에 생성할 이펙트 (None이면 이펙트 없음)
    public BossImpactEffect impactEffect;
}
