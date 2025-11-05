using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GhostManager
/// - Gestiona quién tiene el power-up Ghost (papeletas) y aplica fades de alpha a las papeletas.
/// - Reactiva y reposiciona pickups Ghost al cambiar de turno.
/// - API:
///     * ApplyGhost(Transform papeleta)
///     * FadeAllToZeroOnLaunch()
/// </summary>
public class GhostManager : MonoBehaviour
{
    public static GhostManager Instance { get; private set; }

    [Header("Fade Durations")]
    [SerializeField] private float fadeDurationPickup = 0.35f;     // 1 -> 0.5 en pickup
    [SerializeField] private float fadeDurationLaunch = 0.35f;     // 0.5 -> 0 en lanzamiento
    [SerializeField] private float fadeDurationTurnReset = 0.35f;  // 0/0.5 -> 1 en cambio de turno

    // Estado: papeletas con ghost activo
    private readonly HashSet<Transform> holders = new HashSet<Transform>();
    private readonly Dictionary<Transform, Coroutine> fadeByHolder = new Dictionary<Transform, Coroutine>();

    // Pickups Ghost a reactivar en próximo cambio de turno
    private readonly List<Ghost> pickupsToReactivate = new List<Ghost>();

    private int lastObservedTurn = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // opcional
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (TurnManager.instance != null)
            lastObservedTurn = TurnManager.instance.CurrentTurn();
    }

    private void Update()
    {
        if (TurnManager.instance == null) return;
        int current = TurnManager.instance.CurrentTurn();
        if (lastObservedTurn != -1 && current != lastObservedTurn)
        {
            OnTurnChanged();
        }
        lastObservedTurn = current;
    }

    public void ApplyGhost(Transform papeleta)
    {
        if (papeleta == null) return;

        // Respetar bloqueo global de power-ups
        if (PowerUpStateManager.Instance != null)
        {
            if (!PowerUpStateManager.Instance.CanPickup(papeleta)) return;
            PowerUpStateManager.Instance.MarkHasPowerUp(papeleta);
        }

        holders.Add(papeleta);
        // Fade 1 -> 0.5 en la papeleta
        StartFadeOnTarget(papeleta, 0.5f, fadeDurationPickup);
    }

    public void FadeAllToZeroOnLaunch()
    {
        // Llamar en el momento del lanzamiento para 0.5 -> 0
        foreach (var h in holders)
        {
            StartFadeOnTarget(h, 0f, fadeDurationLaunch);
        }
    }

    public void RegisterConsumable(Ghost ghost)
    {
        if (ghost != null && !pickupsToReactivate.Contains(ghost))
            pickupsToReactivate.Add(ghost);
    }

    private void OnTurnChanged()
    {
        // Volver a alpha 1 en las papeletas, limpiar estado
        foreach (var h in holders)
        {
            StartFadeOnTarget(h, 1f, fadeDurationTurnReset);
        }
        holders.Clear();

        // Reactivar y reposicionar pickups Ghost
        for (int i = 0; i < pickupsToReactivate.Count; i++)
        {
            var g = pickupsToReactivate[i];
            if (g != null)
            {
                // Asegurar que el objeto está activo después de posicionar
                g.gameObject.SetActive(false);
                g.ReactivateForNewTurn();
            }
        }
        pickupsToReactivate.Clear();

        // Limpiar bloqueo global de power-ups
        if (PowerUpStateManager.Instance != null)
        {
            PowerUpStateManager.Instance.ClearAll();
        }
    }

    private void StartFadeOnTarget(Transform target, float toAlpha, float duration)
    {
        if (target == null) return;
        if (fadeByHolder.TryGetValue(target, out var existing) && existing != null)
        {
            StopCoroutine(existing);
        }
        var co = StartCoroutine(FadeRoutine(target, toAlpha, duration));
        fadeByHolder[target] = co;
    }

    private IEnumerator FadeRoutine(Transform target, float toAlpha, float duration)
    {
        if (target == null) yield break;
        var renderers = target.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        if (renderers == null || renderers.Length == 0) yield break;

        float startA = renderers[0].color.a;
        float t = 0f;
        duration = Mathf.Max(0.001f, duration);
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float a = Mathf.Lerp(startA, toAlpha, t);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var c = renderers[i].color;
                c.a = a;
                renderers[i].color = c;
            }
            yield return null;
        }

        // Asegurar alpha final exacto y quitar el handle del diccionario
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            var c = renderers[i].color;
            c.a = toAlpha;
            renderers[i].color = c;
        }

        fadeByHolder.Remove(target);
    }
}
