// EffectPool.cs
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
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Precalentar
        for (int i = 0; i < initialSize; i++)
        {
            var e = Instantiate(effectPrefab, transform);
            e.gameObject.SetActive(false);
            pool.Enqueue(e);
        }
    }

    public void Spawn(Vector3 position, Sprite sprite, float delay = 0.2f, float fade = 0.3f)
    {
        if (pool.Count == 0)
        {
            // Si se agota, instanciamos uno extra (o puedes ignorar el spawn)
            var extra = Instantiate(effectPrefab, transform);
            extra.gameObject.SetActive(false);
            pool.Enqueue(extra);
        }

        var eff = pool.Dequeue();
        eff.transform.position = position;
        eff.Play(sprite, delay, fade);
    }

    public void Recycle(PooledEffect effect)
    {
        effect.gameObject.SetActive(false);
        pool.Enqueue(effect);
    }
}
