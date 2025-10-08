using UnityEngine;
using System.Collections.Generic;

public class EffectPool : MonoBehaviour
{
    public static EffectPool Instance { get; private set; }

    [Header("Pool")]
    [SerializeField] private PooledEffect effectPrefab;
    [SerializeField] private int initialSize = 5;

    private readonly Queue<PooledEffect> pool = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Precalentar
        for (int i = 0; i < initialSize; i++)
        {
            var e = Instantiate(effectPrefab, transform);
            e.gameObject.SetActive(false);
            pool.Enqueue(e);
        }
    }

    /// <summary>
    /// Spawnea un efecto que reproduce un trigger del Animator (Launch, Impact, etc.).
    /// </summary>
    public PooledEffect SpawnTrigger(Vector3 position, string trigger, float delay = 0.2f, float fade = 0.3f)
    {
        var effect = GetAvailable();
        if (effect == null) return null;

        effect.transform.position = position;
        effect.Play(trigger, delay, fade);
        return effect;
    }

    /// <summary>
    /// Obtiene un efecto libre del pool o crea uno nuevo si se agotaron.
    /// </summary>
    private PooledEffect GetAvailable()
    {
        if (pool.Count == 0)
        {
            var extra = Instantiate(effectPrefab, transform);
            extra.gameObject.SetActive(false);
            pool.Enqueue(extra);
        }
        return pool.Dequeue();
    }

    /// <summary>
    /// Devuelve un efecto al pool.
    /// </summary>
    public void Recycle(PooledEffect effect)
    {
        if (effect == null) return;
        effect.gameObject.SetActive(false);
        pool.Enqueue(effect);
    }
}
