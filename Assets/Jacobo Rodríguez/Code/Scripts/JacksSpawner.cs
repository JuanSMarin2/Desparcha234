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

    [System.Serializable] private struct StageJackCounts { public int normal; public int special; public int bomb; }

    [Header("Conteos por etapa (directos, NO pesos)")] 
    [SerializeField] private StageJackCounts stage1Counts = new StageJackCounts { normal = 12, special = 3, bomb = 0 }; 
    [SerializeField] private StageJackCounts stage2Counts = new StageJackCounts { normal = 10, special = 4, bomb = 1 }; 
    [SerializeField] private StageJackCounts stage3Counts = new StageJackCounts { normal = 8, special = 5, bomb = 2 }; 
    [SerializeField] private StageJackCounts defaultCounts = new StageJackCounts { normal = 12, special = 3, bomb = 0 };

    private static int _fallbackBoxUses = 0; // diagnóstico fallback puntos

    private void Start()
    {
        if (!spawnAreaBox)
        {
            Debug.LogWarning("[JackSpawner] BoxCollider2D no asignado (spawn deshabilitado).");
        }
    }

    public void SpawnJacks()
    {
        if (!spawnAreaBox)
        {
            Debug.LogWarning("[JackSpawner] Asigna un BoxCollider2D antes de spawnear.");
            return;
        }
        // Limpiar jacks previos
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.GetComponentInChildren<Jack>(true) != null)
                Destroy(child.gameObject);
        }

        int stage = 1; var prog = FindAnyObjectByType<Progression>(); if (prog) stage = prog.stage;
        StageJackCounts cfg = GetCountsForStage(stage);
        int normalCount = Mathf.Max(0, cfg.normal); int specialCount = Mathf.Max(0, cfg.special); int bombCount = Mathf.Max(0, cfg.bomb);
        int total = normalCount + specialCount + bombCount;
        if (total <= 0) { Debug.LogWarning("[JackSpawner] Total=0, nada que instanciar."); return; }

        var prefabs = new System.Collections.Generic.List<GameObject>(total);
        for (int i = 0; i < normalCount; i++) prefabs.Add(normalJackPrefab);
        for (int i = 0; i < specialCount; i++) prefabs.Add(specialJackPrefab);
        for (int i = 0; i < bombCount; i++) prefabs.Add(bombJackPrefab);
        int antes = prefabs.Count; prefabs.RemoveAll(p => p == null);
        if (antes != prefabs.Count) Debug.LogWarning($"[JackSpawner] Prefabs nulos removidos ({prefabs.Count}/{antes}).");

        // Shuffle simple
        for (int i = 0; i < prefabs.Count; i++) { int j = Random.Range(i, prefabs.Count); (prefabs[i], prefabs[j]) = (prefabs[j], prefabs[i]); }

        for (int i = 0; i < prefabs.Count; i++)
        {
            var prefab = prefabs[i]; if (!prefab) continue;
            Vector3 pos = GetRandomPointInBox(spawnAreaBox, spawnExclude);
            Quaternion rot = randomRotation ? Quaternion.Euler(0, 0, Random.Range(0f, 360f)) : Quaternion.identity;
            var instance = Instantiate(prefab, pos, rot, transform);
            // Color / estado inicial
            var jackComponents = instance.GetComponentsInChildren<Jack>(true);
            int turno = TurnManager.instance != null ? TurnManager.instance.CurrentTurn() : 1;
            foreach (var jack in jackComponents)
            {
                if (!jack) continue;
                jack.HabilitarColor();
                jack.updateColor(turno);
                jack.Transparentar();
            }
        }
        Debug.Log($"[JackSpawner] Spawn stage={stage} total={prefabs.Count} (N={normalCount}, S={specialCount}, B={bombCount}) sin-preventOverlap (física gestionará colisiones)");
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
        var jacks = GetComponentsInChildren<Jack>(true);
        foreach (var jack in jacks) jack.disable();
    }

    public void EnableJacks()
    {
        var jackComponents = GetComponentsInChildren<Jack>(true);
        int turno = TurnManager.instance != null ? TurnManager.instance.CurrentTurn() : 1;
        foreach (var jack in jackComponents)
        {
            if (!jack) continue;
            jack.HabilitarColor();
        }
        Debug.Log($"[JackSpawner] Jacks enabled for Player {turno}. fallbackBox={_fallbackBoxUses} totalJacks={jackComponents.Length}");
    }

    private static bool IsExcludedAt(Vector2 p, Collider2D[] excludes)
    {
        if (excludes == null || excludes.Length == 0) return false;
        for (int i = 0; i < excludes.Length; i++) { var c = excludes[i]; if (c && c.OverlapPoint(p)) return true; }
        return false;
    }

    private static Vector3 GetRandomPointInBox(BoxCollider2D area, Collider2D[] excludes)
    {
        if (!area) return Vector3.zero;
        const int maxAttempts = 64; Bounds b = area.bounds;
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float y = Random.Range(b.min.y, b.max.y);
            Vector2 p = new Vector2(x, y);
            if (area.OverlapPoint(p) && !IsExcludedAt(p, excludes)) return new Vector3(x, y, 0f);
        }
        _fallbackBoxUses++; Vector3 c = area.bounds.center;
        Debug.LogWarning($"[JackSpawner][Fallback][Box] Usando centro tras {maxAttempts} intentos. center={c} excl={(excludes!=null?excludes.Length:0)} fallbackUses={_fallbackBoxUses}");
        return new Vector3(c.x, c.y, 0f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!spawnAreaBox) return; Gizmos.color = Color.green; Gizmos.DrawWireCube(spawnAreaBox.bounds.center, spawnAreaBox.bounds.size);
    }
#endif
}
