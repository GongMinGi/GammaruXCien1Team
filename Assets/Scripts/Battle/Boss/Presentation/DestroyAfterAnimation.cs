using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DestroyAfterAnimation : MonoBehaviour
{
    [SerializeField, Min(0f)] private float destroyDelay = 0.05f;

    private IEnumerator Start()
    {
        Animator animator = GetComponent<Animator>();

        yield return null;

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning(
                $"{name}: Animator Controller가 없어 이펙트를 제거합니다.",
                this);
            Destroy(gameObject);
            yield break;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float speed = Mathf.Abs(
            animator.speed * state.speed * state.speedMultiplier);
        float duration =
            speed > Mathf.Epsilon ? state.length / speed : state.length;

        Destroy(gameObject, Mathf.Max(0f, duration) + destroyDelay);
    }
}
