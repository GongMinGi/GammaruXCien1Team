using UnityEngine;

public class PlayerPresentation : MonoBehaviour
{
    [SerializeField] private BattleExecutor battleExecutor;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private PlayerDisplay playerDisplay;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AidSkillHash = Animator.StringToHash("AidSkill");
    private static readonly int DamageHash = Animator.StringToHash("Damage");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");

    private void OnEnable()
    {
        if (battleExecutor == null || playerAnimator == null || playerDisplay == null)
        {
            Debug.LogError("PlayerPresentation references are not assigned.", this);
            enabled = false;
            return;
        }

        battleExecutor.PlayerAnimationRequested += OnPlayerAnimationRequested;
        battleExecutor.BossActionHit += OnBossActionHit;
        playerDisplay.MovementStateChanged += OnMovementStateChanged;
    }

    private void OnDisable()
    {
        if (battleExecutor != null)
            battleExecutor.PlayerAnimationRequested -= OnPlayerAnimationRequested;
        if (battleExecutor != null)
            battleExecutor.BossActionHit -= OnBossActionHit;
        if (playerDisplay != null)
            playerDisplay.MovementStateChanged -= OnMovementStateChanged;
    }

    private void OnPlayerAnimationRequested(PlayerAnimationCue cue)
    {
        switch (cue)
        {
            case PlayerAnimationCue.Attack:
                playerAnimator.SetTrigger(AttackHash);
                break;
            case PlayerAnimationCue.AidSkill:
                playerAnimator.SetTrigger(AidSkillHash);
                break;
        }
    }

    private void OnBossActionHit(BossAction action)
    {
        playerAnimator.SetTrigger(DamageHash);
    }

    private void OnMovementStateChanged(bool moving)
    {
        playerAnimator.SetBool(IsRunningHash, moving);
    }
}
