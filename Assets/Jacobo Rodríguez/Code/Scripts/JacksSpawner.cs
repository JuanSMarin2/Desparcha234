using UnityEngine;

public class JackSpawner : MonoBehaviour
{

    // corregido SerializeField


    [Header("Prefabs")]
    [SerializeField] private GameObject normalJackPrefab;
    [SerializeField] private GameObject specialJackPrefab;
    [SerializeField] private GameObject bombJackPrefab;

    [Header("Spawn")]

    [SerializeField] private PolygonCollider2D spawnArea; // Asigna un GameObject con PolygonCollider2D (Is Trigger)

    [SerializeField] private bool randomRotation = true;

    [SerializeField] private int numberOfJacks = 10;

    [Header("Probabilidades de spawn")]
    [Tooltip("Pesos relativos para cada tipo de Jack. No es necesario que sumen 1.")]
    [SerializeField, Min(0f)] private float weightNormal = 0.5f;
    [SerializeField, Min(0f)] private float weightSpecial = 0.25f;
    [SerializeField, Min(0f)] private float weightBomb = 0.25f;

    private void Start()
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("Spawn area not assigned. Please assign a PolygonCollider2D.");
            return;
        }

        // Spawn is now triggered when the ball is thrown.
        // SpawnJacks();
    }
    public void SpawnJacks()
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("Asigna un PolygonCollider2D como área de spawn.");
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

            Vector3 pos = RandomPointInArea(spawnArea);
            //Verificar si se rota o no.
            Quaternion rot = randomRotation ? Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)) : Quaternion.identity;

            // Instanciar como hijo de este spawner, conservando posición/rotación en mundo
            var instance = Instantiate(prefab, pos, rot, transform);

            // Actualizar color para cada componente Jack del objeto instanciado (y sus hijos)
            if (TurnManager.instance != null)
            {
                int turno = TurnManager.instance.CurrentTurn();
                var jackComponents = instance.GetComponentsInChildren<Jack>(true);
                foreach (var jack in jackComponents)
                {
                    jack.updateColor(turno);
                }
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

    private static Vector3 RandomPointInArea(PolygonCollider2D area)
    {
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
        if (spawnArea != null)
        {
            Gizmos.color = Color.green;
            // Dibujar contorno del/los paths del PolygonCollider2D
            for (int i = 0; i < spawnArea.pathCount; i++)
            {
                var path = spawnArea.GetPath(i);
                for (int j = 0; j < path.Length; j++)
                {
                    Vector3 a = spawnArea.transform.TransformPoint(path[j]);
                    Vector3 b = spawnArea.transform.TransformPoint(path[(j + 1) % path.Length]);
                    Gizmos.DrawLine(a, b);
                }
            }
        }
    }
    #endif

}
