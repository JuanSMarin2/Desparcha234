using UnityEngine;

public class JackSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject normalJackPrefab;
    [SerializeField] private GameObject specialJackPrefab;
    [SerializeField] private GameObject bombJackPrefab;

    [Header("Área de Spawn (Box obligatorio)")]
    [Tooltip("Asigna un BoxCollider2D que define el área válida para spawnear jacks")]
    [SerializeField] private BoxCollider2D spawnAreaBox;

    [Header("Exclusiones (opcional)")]
    [Tooltip("Colliders donde NO se pueden spawnear jacks")]
    [SerializeField] private Collider2D[] spawnExclude;

    [SerializeField] private bool randomRotation = true;

    [System.Serializable]
    private struct StageJackCounts
    {
        public int normal;
        public int special;
        public int bomb;
    }

    [Header("Conteos por etapa (directos, NO pesos)")]
    [SerializeField] private StageJackCounts stage1Counts = new StageJackCounts { normal = 12, special = 3, bomb = 0 };
    [SerializeField] private StageJackCounts stage2Counts = new StageJackCounts { normal = 10, special = 4, bomb = 1 };
    [SerializeField] private StageJackCounts stage3Counts = new StageJackCounts { normal = 8, special = 5, bomb = 2 };
    [SerializeField] private StageJackCounts defaultCounts = new StageJackCounts { normal = 12, special = 3, bomb = 0 };

    [Header("Separación / Anti-Solape")]
    [SerializeField] private bool preventOverlap = true;
    [SerializeField] private float separationPadding = 0.05f;
    [Tooltip("Iteraciones máximas de resolución de solapes por jack (2-5 usual)")]
    [SerializeField] private int maxResolveIterations = 6;

    [Header("Refuerzo límites")]
    [SerializeField] private bool clampDuringResolve = true; // clampa cada iter mientras resuelve overlap
    [SerializeField] private bool postSpawnValidation = true; // pasada(s) extra tras spawn
    [SerializeField] [Range(1, 5)] private int postSpawnValidationPasses = 2; // pasadas de corrección
    [SerializeField] private bool logPostValidation = false;

    [Header("Refuerzo global extra")]
    [Tooltip("Aplica pases adicionales globales de separación después de spawnear para asegurar cero solapes residuales.")]
    [SerializeField] private bool extraGlobalRelaxation = true;
    [Tooltip("Cantidad de pases globales de separación.")]
    [SerializeField] [Range(1,8)] private int extraSeparationPasses = 3;
    [Tooltip("Máximo de iteraciones internas por pase (pares) antes de abortar para evitar costo excesivo).")]
    [SerializeField] private int maxPairIterationsPerPass = 600;
    [Tooltip("Log de los pases de relajación global.")]
    [SerializeField] private bool logGlobalRelaxation = false;

    private static int _fallbackBoxUses = 0; // diagnostico clustering

    // Debug counters (Overlap)
    private int _dbgAdjustedJacks = 0;          // jacks que requirieron >=1 iteración
    private int _dbgMaxIterations = 0;          // iteraciones máximas usadas por un jack
    private int _dbgClampCount = 0;             // veces que Clamp movió posición
    private float _dbgJackAreaSum = 0f;         // suma áreas aproximadas

    private void Start()
    {
        if (spawnAreaBox == null)
        {
            Debug.LogWarning("[JackSpawner] BoxCollider2D no asignado (spawn deshabilitado).");
            return;
        }
    }

    public void SpawnJacks()
    {
        if (spawnAreaBox == null)
        {
            Debug.LogWarning("[JackSpawner] Asigna un BoxCollider2D antes de spawnear.");
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
                Debug.LogWarning($"Child {child.name} no contiene Jack. Omitido.");
            }
        }

        int stage = 1;
        var prog = FindAnyObjectByType<Progression>();
        if (prog) stage = prog.stage;
        StageJackCounts cfg = GetCountsForStage(stage);
        int normalCount = Mathf.Max(0, cfg.normal);
        int specialCount = Mathf.Max(0, cfg.special);
        int bombCount = Mathf.Max(0, cfg.bomb);
        int total = normalCount + specialCount + bombCount;
        if (total <= 0)
        {
            Debug.LogWarning("[JackSpawner] Total=0, nada que instanciar.");
            return;
        }

        var prefabs = new System.Collections.Generic.List<GameObject>(total);
        for (int i = 0; i < normalCount; i++) prefabs.Add(normalJackPrefab);
        for (int i = 0; i < specialCount; i++) prefabs.Add(specialJackPrefab);
        for (int i = 0; i < bombCount; i++) prefabs.Add(bombJackPrefab);
        int antes = prefabs.Count;
        prefabs.RemoveAll(p => p == null);
        if (antes != prefabs.Count) Debug.LogWarning($"[JackSpawner] Prefabs nulos removidos ({prefabs.Count}/{antes}).");

        // Mezclar
        for (int i = 0; i < prefabs.Count; i++)
        {
            int j = Random.Range(i, prefabs.Count);
            (prefabs[i], prefabs[j]) = (prefabs[j], prefabs[i]);
        }

        var placedPos = new System.Collections.Generic.List<Vector2>(prefabs.Count);
        var placedRad = new System.Collections.Generic.List<float>(prefabs.Count);

        // Métricas de debug anti-overlap
        float totalAreaJacks = 0f;
        int jacksAjustados = 0;
        int maxIteracionesUsadas = 0;
        int clampsAplicados = 0;
        int clampsDuranteResolve = 0;

        for (int i = 0; i < prefabs.Count; i++)
        {
            var prefab = prefabs[i];
            if (!prefab) continue;
            float radius = EstimatePrefabRadius(prefab);
            totalAreaJacks += Mathf.PI * radius * radius;

            Vector3 pos = GetRandomPointInBox(spawnAreaBox, spawnExclude);
            Vector3 originalBeforeResolve = pos;
            int itersUsadas = 0;
            if (preventOverlap)
            {
                pos = ResolveOverlapPosition(pos, radius, placedPos, placedRad, out itersUsadas, ref clampsDuranteResolve);
                if (itersUsadas > 0 && (Vector2)pos != (Vector2)originalBeforeResolve)
                {
                    jacksAjustados++;
                    if (itersUsadas > maxIteracionesUsadas) maxIteracionesUsadas = itersUsadas;
                }
            }
            Vector3 beforeClamp = pos;
            pos = ClampCircleInsideBox(pos, radius, spawnAreaBox); // clamp final con radio
            if ((Vector2)beforeClamp != (Vector2)pos) clampsAplicados++;

            Quaternion rot = randomRotation ? Quaternion.Euler(0, 0, Random.Range(0f, 360f)) : Quaternion.identity;
            var instance = Instantiate(prefab, pos, rot, transform);
            if (preventOverlap)
            {
                placedPos.Add(pos);
                placedRad.Add(radius);
            }

            var jackComponents = instance.GetComponentsInChildren<Jack>(true);
            int turno = TurnManager.instance != null ? TurnManager.instance.CurrentTurn() : 1;
            foreach (var jack in jackComponents)
            {
                jack.HabilitarColor();
                jack.updateColor(turno);
                jack.Transparentar();
            }
        }
        Debug.Log($"[JackSpawner] Spawn stage={stage} total={prefabs.Count} (N={normalCount}, S={specialCount}, B={bombCount}) overlapPrevent={preventOverlap}");
        if (preventOverlap)
        {
            Bounds b = spawnAreaBox.bounds;
            float areaBox = b.size.x * b.size.y;
            float densidad = areaBox > 0 ? totalAreaJacks / areaBox : 0f;
            Debug.Log($"[JackSpawner][OverlapDebug] densidad={densidad:F3} areaJacks={totalAreaJacks:F3} areaBox={areaBox:F3} ajustados={jacksAjustados}/{prefabs.Count} maxIterUsed={maxIteracionesUsadas} clampsFinal={clampsAplicados} clampsDuringResolve={clampsDuranteResolve}");
        }
        if (postSpawnValidation)
        {
            PostSpawnClampValidation();
        }
        if (preventOverlap && extraGlobalRelaxation)
        {
            GlobalRelaxationPasses();
        }
    }

    private Vector3 ResolveOverlapPosition(
        Vector3 startPos,
        float radius,
        System.Collections.Generic.List<Vector2> placedPos,
        System.Collections.Generic.List<float> placedRad,
        out int iterUsed,
        ref int clampsDuranteResolve)
    {
        iterUsed = 0;
        if (placedPos.Count == 0) return startPos;
        Vector2 pos = startPos;
        for (int iter = 0; iter < maxResolveIterations; iter++)
        {
            Vector2 totalPush = Vector2.zero;
            bool anyOverlap = false;
            for (int i = 0; i < placedPos.Count; i++)
            {
                float needed = placedRad[i] + radius + separationPadding;
                Vector2 delta = pos - placedPos[i];
                float dist = delta.magnitude;
                if (dist < needed && dist > 1e-4f)
                {
                    anyOverlap = true;
                    float overlap = needed - dist;
                    totalPush += (delta / dist) * overlap;
                }
                else if (dist <= 1e-4f)
                {
                    anyOverlap = true;
                    totalPush += Random.insideUnitCircle.normalized * (needed);
                }
            }
            if (!anyOverlap) { iterUsed = iter; break; }
            pos += totalPush;
            if (clampDuringResolve)
            {
                Vector3 before = pos;
                Vector3 after = ClampCircleInsideBox(pos, radius, spawnAreaBox);
                if ((Vector2)before != (Vector2)after) clampsDuranteResolve++;
                pos = after;
            }
            iterUsed = iter + 1;
        }
        return new Vector3(pos.x, pos.y, startPos.z);
    }

    private float EstimatePrefabRadius(GameObject prefab)
    {
        float r = 0.25f;
        var circle = prefab.GetComponentInChildren<CircleCollider2D>(true);
        if (circle)
        {
            r = circle.radius * Mathf.Max(circle.transform.lossyScale.x, circle.transform.lossyScale.y);
        }
        else
        {
            var box = prefab.GetComponentInChildren<BoxCollider2D>(true);
            if (box)
            {
                var size = box.size;
                float maxDim = Mathf.Max(size.x * Mathf.Abs(box.transform.lossyScale.x), size.y * Mathf.Abs(box.transform.lossyScale.y));
                r = maxDim * 0.5f;
            }
        }
        return Mathf.Max(0.01f, r);
    }

    private Vector3 ClampCircleInsideBox(Vector3 worldPos, float radius, BoxCollider2D box)
    {
        if (!box) return worldPos;
        var tr = box.transform;
        Vector2 local = tr.InverseTransformPoint(worldPos);
        Vector2 center = box.offset;
        Vector2 half = box.size * 0.5f;
        float minX = center.x - half.x + radius;
        float maxX = center.x + half.x - radius;
        float minY = center.y - half.y + radius;
        float maxY = center.y + half.y - radius;
        if (minX > maxX) { float mid = (minX + maxX) * 0.5f; minX = maxX = mid; }
        if (minY > maxY) { float mid = (minY + maxY) * 0.5f; minY = maxY = mid; }
        local.x = Mathf.Clamp(local.x, minX, maxX);
        local.y = Mathf.Clamp(local.y, minY, maxY);
        Vector2 clampedWorld = tr.TransformPoint(local);
        return new Vector3(clampedWorld.x, clampedWorld.y, worldPos.z);
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
        Debug.Log($"[JackSpawner] Jacks enabled for Player {turno}. Fallbacks(Box={_fallbackBoxUses}) totalJacks={jackComponents.Length}");
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

    private static Vector3 GetRandomPointInBox(BoxCollider2D area, Collider2D[] excludes)
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
        Debug.LogWarning($"[JackSpawner][Fallback][Box] Usando centro tras {maxAttempts} intentos. center={c} exclCount={(excludes != null ? excludes.Length : 0)} fallbackUses={_fallbackBoxUses}");
        return new Vector3(c.x, c.y, 0f);
    }

    private void PostSpawnClampValidation()
    {
        if (!spawnAreaBox) return;
        int moved = 0;
        for (int pass = 0; pass < postSpawnValidationPasses; pass++)
        {
            var jacks = GetComponentsInChildren<Jack>(true);
            bool anyMovedThisPass = false;
            foreach (var jack in jacks)
            {
                if (!jack) continue;
                Transform t = jack.transform.root == transform ? jack.transform : jack.transform; // parented al spawner
                float r = EstimatePrefabRadius(t.gameObject);
                Vector3 before = t.position;
                Vector3 after = ClampCircleInsideBox(before, r, spawnAreaBox);
                if ((Vector2)before != (Vector2)after)
                {
                    t.position = after;
                    moved++;
                    anyMovedThisPass = true;
                }
            }
            if (!anyMovedThisPass) break; // converge
        }
        if (logPostValidation && moved > 0)
        {
            Debug.Log($"[JackSpawner][PostSpawnValidation] Ajustes finales aplicados moved={moved} passes={postSpawnValidationPasses}");
        }
    }

    private Vector3 ClampToOrientedBox(Vector3 worldPos, BoxCollider2D box)
    {
        // Transformar a espacio local del collider
        var tr = box.transform;
        Vector2 local = tr.InverseTransformPoint(worldPos);
        // Centro local del box = offset
        Vector2 center = box.offset;
        Vector2 half = box.size * 0.5f;
        local.x = Mathf.Clamp(local.x, center.x - half.x, center.x + half.x);
        local.y = Mathf.Clamp(local.y, center.y - half.y, center.y + half.y);
        Vector2 clampedWorld = tr.TransformPoint(local);
        return new Vector3(clampedWorld.x, clampedWorld.y, worldPos.z);
    }

    private void GlobalRelaxationPasses()
    {
        var jacks = GetComponentsInChildren<Jack>(true);
        int total = jacks.Length;
        if (total <= 1) return;
        // Pre-calcular radios y refs a transforms
        var trs = new System.Collections.Generic.List<Transform>(total);
        var radii = new System.Collections.Generic.List<float>(total);
        for (int i = 0; i < total; i++)
        {
            var j = jacks[i];
            if (!j) continue;
            trs.Add(j.transform);
            radii.Add(EstimatePrefabRadius(j.gameObject));
        }
        int movedTotal = 0;
        for (int pass = 0; pass < extraSeparationPasses; pass++)
        {
            int movedThisPass = 0;
            int pairIterations = 0;
            for (int i = 0; i < trs.Count; i++)
            {
                for (int k = i + 1; k < trs.Count; k++)
                {
                    if (pairIterations++ > maxPairIterationsPerPass) { break; }
                    Vector3 pi = trs[i].position;
                    Vector3 pk = trs[k].position;
                    float need = radii[i] + radii[k] + separationPadding;
                    Vector2 delta = (Vector2)(pk - pi);
                    float dist = delta.magnitude;
                    if (dist < need && dist > 1e-5f)
                    {
                        float overlap = need - dist;
                        Vector2 pushDir = delta / dist;
                        Vector2 halfPush = pushDir * (overlap * 0.5f);
                        Vector2 newPi = (Vector2)pi - halfPush;
                        Vector2 newPk = (Vector2)pk + halfPush;
                        // Clamp con radio individual
                        pi = ClampCircleInsideBox(new Vector3(newPi.x, newPi.y, pi.z), radii[i], spawnAreaBox);
                        pk = ClampCircleInsideBox(new Vector3(newPk.x, newPk.y, pk.z), radii[k], spawnAreaBox);
                        trs[i].position = pi;
                        trs[k].position = pk;
                        movedThisPass++;
                    }
                    else if (dist <= 1e-5f)
                    {
                        // Separación aleatoria para evitar colapso exacto
                        Vector2 rnd = Random.insideUnitCircle.normalized * need * 0.5f;
                        Vector2 newPi = (Vector2)pi - rnd;
                        Vector2 newPk = (Vector2)pk + rnd;
                        pi = ClampCircleInsideBox(new Vector3(newPi.x, newPi.y, pi.z), radii[i], spawnAreaBox);
                        pk = ClampCircleInsideBox(new Vector3(newPk.x, newPk.y, pk.z), radii[k], spawnAreaBox);
                        trs[i].position = pi;
                        trs[k].position = pk;
                        movedThisPass++;
                    }
                }
                if (pairIterations > maxPairIterationsPerPass) break;
            }
            movedTotal += movedThisPass;
            if (logGlobalRelaxation)
            {
                Debug.Log($"[JackSpawner][GlobalRelax] pass={pass+1}/{extraSeparationPasses} movedPairs={movedThisPass} pairIter={Mathf.Min(pairIterations,maxPairIterationsPerPass)}" );
            }
            if (movedThisPass == 0) break; // converge antes
        }
        if (logGlobalRelaxation)
        {
            Debug.Log($"[JackSpawner][GlobalRelax] totalMovedPairs={movedTotal}");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!spawnAreaBox) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnAreaBox.bounds.center, spawnAreaBox.bounds.size);
    }
#endif
}
