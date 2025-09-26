using UnityEngine;

public class JackSpawner : MonoBehaviour
{

    // corregido SerializeField


    [Header("Prefabs")]
    [SerializeField] private GameObject normalJackPrefab;
    [SerializeField] private GameObject specialJackPrefab;
    [SerializeField] private GameObject bombJackPrefab;

    public enum SpawnAreaShape { Box, Polygon }

    [Header("Spawn")]
    [Tooltip("Elige la forma del área de spawn")]
    [SerializeField] private SpawnAreaShape spawnShape = SpawnAreaShape.Box;
    [Tooltip("Asigna un BoxCollider2D para el área de spawn")]
    [SerializeField] private BoxCollider2D spawnAreaBox;
    [Tooltip("Opcional: Asigna un PolygonCollider2D para un área de spawn más compleja")]
    [SerializeField] private PolygonCollider2D spawnAreaPolygon;

    [Header("Spawn Exclusions")]
    [Tooltip("Colliders donde NO se pueden spawnear jacks (exclusiones)")]
    [SerializeField] private Collider2D[] spawnExclude;

    [SerializeField] private bool randomRotation = true;

    // Total fijo por spawn
    private const int TotalJacksPorSpawn = 15;

    [System.Serializable]
    private struct StageJackCounts
    {
        public int normal;
        public int special;
        public int bomb;
    }

    [Header("Conteos por etapa (deben sumar 15)")]
    [SerializeField] private StageJackCounts stage1Counts = new StageJackCounts { normal = 12, special = 3, bomb = 0 };
    [SerializeField] private StageJackCounts stage2Counts = new StageJackCounts { normal = 10, special = 4, bomb = 1 };
    [SerializeField] private StageJackCounts stage3Counts = new StageJackCounts { normal = 8, special = 5, bomb = 2 };
    [Tooltip("Usado si la etapa actual no es 1..3")]
    [SerializeField] private StageJackCounts defaultCounts = new StageJackCounts { normal = 12, special = 3, bomb = 0 };

    private static int _fallbackBoxUses = 0; // diagnostico clustering
    private static int _fallbackPolyUses = 0; // diagnostico clustering

    private void Start()
    {
        bool areaMissing = (spawnShape == SpawnAreaShape.Box && spawnAreaBox == null) ||
                           (spawnShape == SpawnAreaShape.Polygon && spawnAreaPolygon == null);

        if (areaMissing)
        {
            Debug.LogWarning($"Spawn area ({spawnShape}) not assigned. Please assign a {spawnShape}Collider2D.");
            return;
        }

        // El spawn se controla desde Progression (pendiente de lanzar o al lanzar).
        // SpawnJacks();
    }
    public void SpawnJacks()
    {
        bool areaMissing = (spawnShape == SpawnAreaShape.Box && spawnAreaBox == null) ||
                           (spawnShape == SpawnAreaShape.Polygon && spawnAreaPolygon == null);

        if (areaMissing)
        {
            Debug.LogWarning($"Asigna un {spawnShape}Collider2D como área de spawn.");
            return;
        }

        // Limpiar jacks previos: elimina hijos que tengan un componente Jack en su jerarquía
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.GetComponentInChildren<Jack>(true) != null)
            {
                Destroy(child.gameObject);
            }
            else
            {
                Debug.LogWarning($"Child {child.name} does not have a Jack component. Skipping.");
            }
        }

        // Determinar conteos por etapa actual
        int stage = 1;
        var prog = FindAnyObjectByType<Progression>();
        if (prog != null) stage = prog.stage;
        StageJackCounts cfg = GetCountsForStage(stage);
        AjustarConteosASuma(ref cfg, TotalJacksPorSpawn);

        // Construir lista a instanciar y mezclar para distribuir
        System.Collections.Generic.List<GameObject> prefabs = new System.Collections.Generic.List<GameObject>(TotalJacksPorSpawn);
        for (int i = 0; i < cfg.normal; i++) prefabs.Add(normalJackPrefab);
        for (int i = 0; i < cfg.special; i++) prefabs.Add(specialJackPrefab);
        for (int i = 0; i < cfg.bomb; i++) prefabs.Add(bombJackPrefab);

        // Eliminar posibles nulls (si faltan prefabs) conservando cantidad lo más posible
        int antes = prefabs.Count;
        prefabs.RemoveAll(p => p == null);
        if (prefabs.Count != antes)
        {
            Debug.LogWarning($"[JackSpawner] Algunos prefabs no están asignados. Se instanciarán {prefabs.Count}/{TotalJacksPorSpawn}.");
        }

        // Mezclar
        for (int i = 0; i < prefabs.Count; i++)
        {
            int j = Random.Range(i, prefabs.Count);
            var tmp = prefabs[i];
            prefabs[i] = prefabs[j];
            prefabs[j] = tmp;
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            var prefab = prefabs[i];
            if (prefab == null) continue;

            Vector3 pos = GetRandomPointInArea();
            //Verificar si se rota o no.
            Quaternion rot = randomRotation ? Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)) : Quaternion.identity;

            // Instanciar como hijo de este spawner, conservando posición/rotación en mundo
            var instance = Instantiate(prefab, pos, rot, transform);

            // Actualizar color por jugador y dejar en estado "pendiente" (transparente y sin collider)
            var jackComponents = instance.GetComponentsInChildren<Jack>(true);
            int turno = TurnManager.instance != null ? TurnManager.instance.CurrentTurn() : 1;
            foreach (var jack in jackComponents)
            {
                jack.HabilitarColor();
                // Para Normal, esto asigna sprite según color del jugador; otros tipos se mantienen
                jack.updateColor(turno);
                jack.Transparentar();
            }
        }

        Debug.Log($"[JackSpawner] Spawned {prefabs.Count} jacks for stage={stage} (N={cfg.normal}, S={cfg.special}, B={cfg.bomb}).");
    }

    private StageJackCounts GetCountsForStage(int stage)
    {
        switch (stage)
        {
            case 1: return stage1Counts;
            case 2: return stage2Counts;
            case 3: return stage3Counts;
            default: return defaultCounts;
        }
    }

    private static void AjustarConteosASuma(ref StageJackCounts cfg, int objetivo)
    {
        // Asegurar no-negativos
        cfg.normal = Mathf.Max(0, cfg.normal);
        cfg.special = Mathf.Max(0, cfg.special);
        cfg.bomb = Mathf.Max(0, cfg.bomb);

        int total = cfg.normal + cfg.special + cfg.bomb;
        if (total == objetivo) return;

        if (total < objetivo)
        {
            // Completar con normales por seguridad
            cfg.normal += (objetivo - total);
            return;
        }

        // Si nos pasamos, recortar en orden normal->special->bomb
        int sobre = total - objetivo;
        int rec = Mathf.Min(sobre, cfg.normal); cfg.normal -= rec; sobre -= rec;
        if (sobre > 0) { rec = Mathf.Min(sobre, cfg.special); cfg.special -= rec; sobre -= rec; }
        if (sobre > 0) { rec = Mathf.Min(sobre, cfg.bomb); cfg.bomb -= rec; sobre -= rec; }
    }

    public void DisableAll()
    {
        // Deshabilita (ahora destruye) todos los Jacks que estén bajo este spawner
        var jacks = GetComponentsInChildren<Jack>(true);
        foreach (var jack in jacks)
        {
            jack.disable(); // Destroy(gameObject)
        }
    }

    

    // Habilita colliders y color pleno de todos los jacks activos bajo este spawner
    public void EnableJacks()
    {
        var jackComponents = GetComponentsInChildren<Jack>(true);
        int turno = TurnManager.instance != null ? TurnManager.instance.CurrentTurn() : 1;
        foreach (var jack in jackComponents)
        {
            // Asegurar sprite correcto por turno para Normales y color pleno para todos
            if (jack != null)
            {
                if (TurnManager.instance != null && jack != null)
                {
                    //jack.updateColor(turno);
                }
                jack.HabilitarColor();
            }
        }
        Debug.Log($"[JackSpawner] Jacks enabled for Player {turno}. Fallbacks(Box={_fallbackBoxUses},Poly={_fallbackPolyUses}) totalJacks={jackComponents.Length}");
    }

    private Vector3 GetRandomPointInArea()
    {
        switch (spawnShape)
        {
            case SpawnAreaShape.Box:
                return RandomPointInBox(spawnAreaBox, spawnExclude);
            case SpawnAreaShape.Polygon:
                return RandomPointInPolygon(spawnAreaPolygon, spawnExclude);
            default:
                // Fallback al centro del spawner si no hay área válida
                return transform.position;
        }
    }

    private static bool IsExcludedAt(Vector2 p, Collider2D[] excludes)
    {
        if (excludes == null || excludes.Length == 0) return false;
        for (int i = 0; i < excludes.Length; i++)
        {
            var c = excludes[i];
            if (c == null) continue;
            if (c.OverlapPoint(p)) return true;
        }
        return false;
    }

    private static Vector3 RandomPointInBox(BoxCollider2D area, Collider2D[] excludes)
    {
        if (area == null) return Vector3.zero;
        const int maxAttempts = 64;
        Bounds b = area.bounds;
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float y = Random.Range(b.min.y, b.max.y);
            Vector2 p = new Vector2(x, y);
            // Validar que esté dentro de la caja (soporta rotación) y no en exclusión
            if (area.OverlapPoint(p) && !IsExcludedAt(p, excludes))
            {
                return new Vector3(x, y, 0f);
            }
        }
        // Fallback: centro del box si no encontramos punto válido
        _fallbackBoxUses++;
        Vector3 c = area.bounds.center;
        Debug.LogWarning($"[JackSpawner][Fallback][Box] Usando centro tras {maxAttempts} intentos. center={c} exclCount={(excludes!=null?excludes.Length:0)} fallbackUses={_fallbackBoxUses}");
        return new Vector3(c.x, c.y, 0f);
    }

    private static Vector3 RandomPointInPolygon(PolygonCollider2D area, Collider2D[] excludes)
    {
        if (area == null) return Vector3.zero;
        Bounds b = area.bounds;
        const int maxAttempts = 64;
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float y = Random.Range(b.min.y, b.max.y);
            Vector2 p = new Vector2(x, y);
            if (area.OverlapPoint(p) && !IsExcludedAt(p, excludes))
            {
                return new Vector3(x, y, 0f);
            }
        }
        // Fallback: centro del polígono si no encontramos punto en intentos
        _fallbackPolyUses++;
        Vector3 c2 = area.bounds.center;
        Debug.LogWarning($"[JackSpawner][Fallback][Poly] Usando centro tras {maxAttempts} intentos. center={c2} exclCount={(excludes!=null?excludes.Length:0)} fallbackUses={_fallbackPolyUses}");
        return new Vector3(c2.x, c2.y, 0f);
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (spawnShape == SpawnAreaShape.Box && spawnAreaBox != null)
        {
            Gizmos.DrawWireCube(spawnAreaBox.bounds.center, spawnAreaBox.bounds.size);
        }
        else if (spawnShape == SpawnAreaShape.Polygon && spawnAreaPolygon != null)
        {
            // Dibujar contorno del/los paths del PolygonCollider2D
            for (int i = 0; i < spawnAreaPolygon.pathCount; i++)
            {
                var path = spawnAreaPolygon.GetPath(i);
                for (int j = 0; j < path.Length; j++)
                {
                    Vector3 a = spawnAreaPolygon.transform.TransformPoint(path[j]);
                    Vector3 b = spawnAreaPolygon.transform.TransformPoint(path[(j + 1) % path.Length]);
                    Gizmos.DrawLine(a, b);
                }
            }
        }
    }
    #endif

}
