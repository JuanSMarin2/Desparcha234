using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TejoShieldManager
/// - Mantiene y restaura el estado de papeletas "protegidas" por shield.
/// - Gestiona feedback visual (instancia/activa/desactiva) separado del consumible TejoShield.
/// - Detecta cambio de turno vía TurnManager.CurrentTurn() y, al ocurrir:
///     * Restaura isTrigger=true en las papeletas protegidas
///     * Desactiva/destruye feedbacks
///     * Reactiva consumibles TejoShield registrados
/// No modifica TurnManager.
/// </summary>
public class TejoShieldManager : MonoBehaviour
{
    public static TejoShieldManager Instance { get; private set; }

    [Header("Feedback visual")]
    [Tooltip("Prefab del feedback a colocar sobre la papeleta protegida.")]
    [SerializeField] private GameObject shieldFeedbackPrefab;
    [SerializeField] private Vector3 feedbackLocalOffset = Vector3.zero;
    [SerializeField] private bool destroyFeedbackOnTurnEnd = false;

    private class ShieldEntry
    {
        public Transform target;
        public PolygonCollider2D polygon;
        public bool previousIsTrigger;
        public GameObject feedback;
    }

    private readonly Dictionary<Transform, ShieldEntry> active = new Dictionary<Transform, ShieldEntry>();
    private readonly List<GameObject> consumablesToReactivate = new List<GameObject>();
    private int lastObservedTurn = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Opcional: DontDestroyOnLoad si quieres persistencia entre escenas
            // DontDestroyOnLoad(gameObject);
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
        if (TurnManager.instance != null)
        {
            int current = TurnManager.instance.CurrentTurn();
            if (lastObservedTurn != -1 && current != lastObservedTurn)
            {
                OnTurnChanged();
            }
            lastObservedTurn = current;
        }

        // Limpieza de entradas con targets destruidos
        if (active.Count > 0)
        {
            List<Transform> toRemove = null;
            foreach (var kv in active)
            {
                if (kv.Key == null)
                {
                    toRemove ??= new List<Transform>();
                    toRemove.Add(kv.Key);
                }
            }
            if (toRemove != null)
            {
                foreach (var tr in toRemove)
                {
                    active.Remove(tr);
                }
            }
        }
    }

    public void ApplyShield(Transform target, PolygonCollider2D polygon)
    {
        if (target == null || polygon == null) return;

        // Si ya está activo, no duplicamos
        if (!active.TryGetValue(target, out var entry))
        {
            entry = new ShieldEntry
            {
                target = target,
                polygon = polygon,
                previousIsTrigger = polygon.isTrigger,
                feedback = null
            };
            active[target] = entry;
        }

        // Desactivar el trigger para que ahora sea colisionable (como pidió el requerimiento)
        polygon.isTrigger = false;

        // Gestionar feedback visual
        if (shieldFeedbackPrefab != null)
        {
            if (entry.feedback == null)
            {
                entry.feedback = Instantiate(shieldFeedbackPrefab);
                entry.feedback.name = shieldFeedbackPrefab.name + "(ShieldFeedback)";
            }

            // Parent y posicionamiento local
            entry.feedback.transform.SetParent(target, worldPositionStays: false);
            entry.feedback.transform.localPosition = feedbackLocalOffset;
            entry.feedback.transform.localRotation = Quaternion.identity;
            entry.feedback.SetActive(true);
        }

        // Registrar power-up a nivel global para bloquear otros pickups en este turno
        if (PowerUpStateManager.Instance != null)
        {
            PowerUpStateManager.Instance.MarkHasPowerUp(target);
        }
    }

    public void RegisterConsumableForReset(GameObject consumable)
    {
        if (consumable != null && !consumablesToReactivate.Contains(consumable))
            consumablesToReactivate.Add(consumable);
    }

    private void OnTurnChanged()
    {
        // Restaurar cada objetivo
        foreach (var kv in active)
        {
            var e = kv.Value;
            if (e == null) continue;
            if (e.polygon != null)
            {
                e.polygon.isTrigger = e.previousIsTrigger;
            }
            if (e.feedback != null)
            {
                if (destroyFeedbackOnTurnEnd) Destroy(e.feedback); else e.feedback.SetActive(false);
            }
        }

        active.Clear();

        // Reactivar consumibles (shield pickups)
        for (int i = 0; i < consumablesToReactivate.Count; i++)
        {
            var go = consumablesToReactivate[i];
            if (go != null)
                go.SetActive(true);
        }
        consumablesToReactivate.Clear();

        // Limpiar estado global de power-ups por turno
        if (PowerUpStateManager.Instance != null)
        {
            PowerUpStateManager.Instance.ClearAll();
        }
    }
}
