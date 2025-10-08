using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PooledEffect : MonoBehaviour
{
    private Animator animator;
    private Coroutine fadeRoutine;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Llamado por el pool al activar
    public void Play(string trigger, float delay, float fadeDuration)
    {
        gameObject.SetActive(true);

        if (animator)
        {
            animator.ResetTrigger("Launch");
            animator.ResetTrigger("Impact");
            animator.SetTrigger(trigger);
        }

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeAndRecycle(delay, fadeDuration));
    }

    private IEnumerator FadeAndRecycle(float delay, float fadeDuration)
    {
        yield return new WaitForSeconds(delay);
        yield return new WaitForSeconds(fadeDuration);
        EffectPool.Instance.Recycle(this);
    }
}
