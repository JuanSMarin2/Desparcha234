using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Componente independiente para hacer aparecer un objeto cuando la partida alcanza cierta etapa
// (la etapa incrementa cuando un jugador es eliminado, ver TagManager.CurrentStageInRound / OnStageChanged).
[DisallowMultipleComponent]
public class AparecerEnStage : MonoBehaviour
{
    public enum VisibilityMode { SetActive, Components }

    [Header("Visibilidad por etapa (0 inicial, 1 y 2 tras eliminaciones)")]
    [SerializeField] private bool visibleEnEtapa0 = false;
    [SerializeField] private bool visibleEnEtapa1 = false;
    [SerializeField] private bool visibleEnEtapa2 = false;

    [Header("Targets a habilitar/visibilizar")]
    [Tooltip("Si se deja vacío, afecta a este GameObject")]
    public GameObject[] targets;

    [Header("Modo de visibilidad")]
    [Tooltip("SetActive: activa/desactiva GameObjects (cuidado si incluye este mismo). Components: activa/desactiva SpriteRenderers/Colliders.")]
    public VisibilityMode visibilityMode = VisibilityMode.Components;

    private bool _visible;

    // Cache para modo Components
    private SpriteRenderer[] _cachedRenderers;
    private Collider2D[] _cachedColliders;

    // Posición inicial y física opcional
    private Vector3 _initialPosition;
    private Rigidbody2D _rb2d;

    private void Awake()
    {
        if (targets == null || targets.Length == 0)
        {
            targets = new GameObject[] { this.gameObject };
        }
        CacheComponentsIfNeeded();
        // Capturar posición inicial al cargar escena
        _initialPosition = transform.position;
        _rb2d = GetComponent<Rigidbody2D>();
    }

    private void CacheComponentsIfNeeded()
    {
        if (visibilityMode != VisibilityMode.Components) return;
        _cachedRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        _cachedColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
    }

    private void OnEnable()
    {
        TagManager.OnStageChanged += OnStageChanged;
        TagManager.OnRoundStarted += OnRoundStarted;

        // Aplicar visibilidad según la etapa actual al habilitar
        var tm = TagManager.Instance;
        ApplyStage(tm ? tm.CurrentStageInRound : 0);
    }

    private void OnDisable()
    {
        TagManager.OnStageChanged -= OnStageChanged;
        TagManager.OnRoundStarted -= OnRoundStarted;
    }

    private void OnRoundStarted()
    {
        // Re-evaluar visibilidad con la etapa actual al iniciar ronda (sin reposicionar)
        var tm = TagManager.Instance;
        ApplyStage(tm ? tm.CurrentStageInRound : 0);
    }

    private void OnStageChanged(int stage)
    {
        ApplyStage(stage);
    }

    private void ApplyStage(int stage)
    {
        int s = Mathf.Clamp(stage, 0, 2);
        bool desired = s == 0 ? visibleEnEtapa0 : (s == 1 ? visibleEnEtapa1 : visibleEnEtapa2);
        SetVisible(desired);
        Debug.Log($"[AparecerEnStage] '{gameObject.name}' etapa={s} -> visible={desired}");
    }

    private void SetVisible(bool v)
    {
        _visible = v;
        if (visibilityMode == VisibilityMode.SetActive)
        {
            if (targets == null) return;
            foreach (var t in targets)
            {
                if (!t) continue;
                // Evitar desactivar el mismo GameObject que contiene este script (se perderían suscripciones)
                if (t == this.gameObject && v == false)
                {
                    // Fallback a Components para este target específico
                    ApplyComponentsVisibility(false);
                    continue;
                }
                t.SetActive(v);
            }
        }
        else // Components
        {
            ApplyComponentsVisibility(v);
        }
    }

    private void ApplyComponentsVisibility(bool v)
    {
        // En modo Components, aplicar a cada target y todos sus hijos
        if (targets == null) return;
        foreach (var root in targets)
        {
            if (!root) continue;

            // Renderers (SpriteRenderer, MeshRenderer, LineRenderer, TrailRenderer, SkinnedMeshRenderer, etc.)
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (var r in renderers) if (r) r.enabled = v;

            // UI Graphics (Image, RawImage, Text, TextMeshProUGUI, etc.)
            var graphics = root.GetComponentsInChildren<Graphic>(includeInactive: true);
            foreach (var g in graphics) if (g) g.enabled = v;
            // TextMeshPro 3D (TMP_Text que no es Graphic)
            var tmpTexts = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
            foreach (var t in tmpTexts) if (t) t.enabled = v;

            // Colliders 2D (para no interactuar físicamente)
            var coll2d = root.GetComponentsInChildren<Collider2D>(includeInactive: true);
            foreach (var c in coll2d) if (c) c.enabled = v;

            // Partículas (emisión/visual)
            var particles = root.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            foreach (var ps in particles)
            {
                if (!ps) continue;
                var em = ps.emission; em.enabled = v;
                if (v) { if (!ps.isPlaying) ps.Play(); }
                else { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
            }
        }
    }
}
