// PooledEffect.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class PooledEffect : MonoBehaviour
{
    private SpriteRenderer sr;
    private Coroutine fadeRoutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Llamado por el pool al activar
    public void Play(Sprite sprite, float delay, float fadeDuration)
    {
        gameObject.SetActive(true);

        if (sr)
        {
            sr.sprite = sprite;
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeAndRecycle(delay, fadeDuration));
    }

    private IEnumerator FadeAndRecycle(float delay, float fadeDuration)
    {
        yield return new WaitForSeconds(delay);

        if (sr)
        {
            float t = 0f;
            Color c = sr.color;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
                sr.color = c;
                yield return null;
            }
        }

        // Devolver al pool sin destruir
        EffectPool.Instance.Recycle(this);
    }
}
