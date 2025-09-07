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

    [SerializeField] private bool randomRotation = true;

    [SerializeField] private int numberOfJacks = 10;

    [Header("Probabilidades de spawn")]
    [Tooltip("Pesos relativos para cada tipo de Jack. No es necesario que sumen 1.")]
    [SerializeField, Min(0f)] private float weightNormal = 0.5f;
    [SerializeField, Min(0f)] private float weightSpecial = 0.25f;
    [SerializeField, Min(0f)] private float weightBomb = 0.25f;

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

        for (int i = 0; i < numberOfJacks; i++)
        {
            var prefab = PickPrefab();
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
                // Para Normal, esto asigna sprite según color del jugador; otros tipos se mantienen
                jack.updateColor(turno);
                jack.Transparentar();
            }
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

    public void EnableAll()
    {
        // Ya no existe "enable" en Jack; restaurar = respawnear
        SpawnJacks();
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
        Debug.Log($"Jacks enabled for Player {turno}");
    }

    private Vector3 GetRandomPointInArea()
    {
        switch (spawnShape)
        {
            case SpawnAreaShape.Box:
                return RandomPointInBox(spawnAreaBox);
            case SpawnAreaShape.Polygon:
                return RandomPointInPolygon(spawnAreaPolygon);
            default:
                // Fallback al centro del spawner si no hay área válida
                return transform.position;
        }
    }

    private static Vector3 RandomPointInBox(BoxCollider2D area)
    {
        if (area == null) return Vector3.zero;
        Bounds b = area.bounds;
        float x = Random.Range(b.min.x, b.max.x);
        float y = Random.Range(b.min.y, b.max.y);
        return new Vector3(x, y, 0f);
    }

    private static Vector3 RandomPointInPolygon(PolygonCollider2D area)
    {
        if (area == null) return Vector3.zero;
        Bounds b = area.bounds;
        const int maxAttempts = 64;
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float y = Random.Range(b.min.y, b.max.y);
            Vector2 p = new Vector2(x, y);
            if (area.OverlapPoint(p))
            {
                return new Vector3(x, y, 0f);
            }
        }
        // Fallback: centro del polígono si no encontramos punto en intentos
        Vector3 c = area.bounds.center;
        return new Vector3(c.x, c.y, 0f);
    }
    // Devuelve un prefab con pesos configurables en el inspector
    private GameObject PickPrefab()
    {
        float n = Mathf.Max(0f, weightNormal);
        float s = Mathf.Max(0f, weightSpecial);
        float b = Mathf.Max(0f, weightBomb);
        float total = n + s + b;
        if (total <= 0f)
        {
            // Fallback: todo a normal si no hay pesos válidos
            n = 1f; s = 0f; b = 0f; total = 1f;
        }
        float r = Random.value * total;
        if (r < n) return normalJackPrefab;
        if (r < n + s) return specialJackPrefab;
        return bombJackPrefab;
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
