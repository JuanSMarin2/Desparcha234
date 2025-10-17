using System.Collections.Generic;
using UnityEngine;

public class MapDestroyer : MonoBehaviour
{
    [System.Serializable]
    public class SlotGroup
    {
        // El MISMO slot en cada mapa, en el mismo orden que RandomActivator
        public GameObject[] perMap;
    }

    [Header("Slots (uno por ronda de destruccion)")]
    [SerializeField] private SlotGroup[] slots;

    [Header("Explosion")]
    [SerializeField] private GameObject explosion;
    [SerializeField] private float explosionLifetime = 1.25f; // 0 = no auto-apagar

    // Seguimiento de ciclo
    private HashSet<int> seenThisCycle = new HashSet<int>();
    private int lastObservedPlayer = -99;   // para detectar cambios

    // Progreso de destruccion
    private int currentSlotIndex = 0;

    void Start()
    {
        var tm = TurnManager.instance;
        if (tm != null)
        {
            int cur = tm.GetCurrentPlayerIndex();
            if (cur >= 0)
            {
                lastObservedPlayer = cur;
                seenThisCycle.Clear();
                seenThisCycle.Add(cur); // arrancamos el ciclo “observando” al actual
            }
        }
        else
        {
            seenThisCycle.Clear();
            lastObservedPlayer = -99;
        }
    }

    void Update()
    {
        var tm = TurnManager.instance;
        if (tm == null) return;

        int activeCount = tm.GetActivePlayerCount();
        if (activeCount <= 1) return; // sin ciclo con 0/1

        int cur = tm.GetCurrentPlayerIndex();
        if (cur < 0) return;

        // Primer frame útil si Start no alcanzó a fijarlo
        if (lastObservedPlayer == -99)
        {
            lastObservedPlayer = cur;
            seenThisCycle.Clear();
            seenThisCycle.Add(cur);
            return;
        }

        // Solo actuamos ante cambio de jugador
        if (cur == lastObservedPlayer) return;
        lastObservedPlayer = cur;

        // Recortar a jugadores activos (si hubo eliminaciones en medio)
        TrimSeenToActive(tm.GetActivePlayerIndices());

        // ¿Ya vimos a todos los activos y estamos viendo un jugador repetido?
        bool setCompleto = seenThisCycle.Count >= activeCount;
        bool esRepetido = seenThisCycle.Contains(cur);

        if (setCompleto && esRepetido)
        {
            // Wrap robusto: cerró el ciclo
            OnFullTurnCycle();

            // Reiniciar ciclo arrancando con el jugador actual
            seenThisCycle.Clear();
            seenThisCycle.Add(cur);
        }
        else
        {
            // Aún no cerramos ciclo: marcamos al actual como visto
            seenThisCycle.Add(cur);
        }
    }

    private void TrimSeenToActive(List<int> active)
    {
        if (active == null)
        {
            seenThisCycle.Clear();
            return;
        }

        var kept = new HashSet<int>();
        for (int i = 0; i < active.Count; i++)
        {
            if (seenThisCycle.Contains(active[i]))
                kept.Add(active[i]);
        }
        seenThisCycle = kept;
    }

    private void OnFullTurnCycle()
    {
        if (slots == null || slots.Length == 0) return;
        if (currentSlotIndex < 0 || currentSlotIndex >= slots.Length) return;

        var group = slots[currentSlotIndex];
        if (group == null || group.perMap == null || group.perMap.Length == 0) return;

        int mapIdx = GetActiveMapIndex(group.perMap);
        if (mapIdx < 0) return; // no se pudo deducir mapa activo

        var target = group.perMap[mapIdx];
        if (target == null) return;

        // Si ese slot ya estaba desactivado, avanzar y salir
        if (!target.activeInHierarchy)
        {
            currentSlotIndex++;
            if (currentSlotIndex >= slots.Length) currentSlotIndex = slots.Length - 1;
            return;
        }

        // Explosión al centro
        if (explosion != null)
        {
            explosion.transform.position = GetObjectCenter(target);
            explosion.SetActive(false);
            explosion.SetActive(true);

            if (explosionLifetime > 0f)
                StartCoroutine(AutoDisable(explosion, explosionLifetime));
        }

        Debug.Log("Desactivando");
        target.SetActive(false);

        // Siguiente slot para la próxima vuelta
        currentSlotIndex++;
        if (currentSlotIndex >= slots.Length)
        {
            // enabled = false; // si no quieres más destrucciones
            currentSlotIndex = slots.Length - 1;
        }
    }

    private int GetActiveMapIndex(GameObject[] perMap)
    {
        // Deducimos el mapa activo por cuál de estos objetos está activo
        for (int i = 0; i < perMap.Length; i++)
        {
            var go = perMap[i];
            if (go != null && go.activeInHierarchy)
                return i;
        }
        return -1;
    }

    private Vector3 GetObjectCenter(GameObject go)
    {
        var rend = go.GetComponentInChildren<Renderer>();
        if (rend != null) return rend.bounds.center;

        var col = go.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.center;

        var col2d = go.GetComponentInChildren<Collider2D>();
        if (col2d != null) return (Vector2)col2d.bounds.center;

        return go.transform.position;
    }

    private System.Collections.IEnumerator AutoDisable(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        if (go != null) go.SetActive(false);
    }
}
