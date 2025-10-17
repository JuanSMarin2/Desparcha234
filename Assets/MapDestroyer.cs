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

    // Deteccion de fin de ciclo de turnos
    private HashSet<int> seenThisCycle = new HashSet<int>();
    private int lastObservedPlayer = -99;   // para detectar cambio de jugador

    // Progreso de destruccion
    private int currentSlotIndex = 0;

    void Start()
    {
        // Si ya hay TurnManager, arrancamos el set con el jugador actual
        if (TurnManager.instance != null)
        {
            int cur = TurnManager.instance.GetCurrentPlayerIndex();
            if (cur >= 0)
            {
                seenThisCycle.Clear();
                seenThisCycle.Add(cur);
                lastObservedPlayer = cur;
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
        if (activeCount <= 1) return; // nada que ciclar con 0/1 jugadores

        int cur = tm.GetCurrentPlayerIndex();
        if (cur < 0) return;

        // Si es la primera vez que tomamos referencia (Start pudo llegar antes que TM)
        if (lastObservedPlayer == -99)
        {
            seenThisCycle.Clear();
            seenThisCycle.Add(cur);
            lastObservedPlayer = cur;
            return;
        }

        // ¿cambió el jugador?
        if (cur != lastObservedPlayer)
        {
            lastObservedPlayer = cur;

            // Mantener el set solo con jugadores activos (por si alguien fue eliminado)
            TrimSeenToActive(tm.GetActivePlayerIndices());

            // Añadir el actual a los vistos del ciclo
            seenThisCycle.Add(cur);

            // Si ya vimos a TODOS los activos al menos una vez, el ciclo está completo -> destruir slot
            if (seenThisCycle.Count >= activeCount)
            {
                OnFullTurnCycle();

                // Reiniciar ciclo empezando desde el jugador actual (el que acaba de entrar)
                seenThisCycle.Clear();
                seenThisCycle.Add(cur);
            }
        }
    }

    private void TrimSeenToActive(List<int> active)
    {
        if (active == null) return;
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
            // Puedes deshabilitar el componente si no quieres más destrucciones:
            // enabled = false;
            currentSlotIndex = slots.Length - 1;
        }
    }

    private int GetActiveMapIndex(GameObject[] perMap)
    {
        // Deducimos el mapa activo por cuál de estos objetos está activo en jerarquía
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
