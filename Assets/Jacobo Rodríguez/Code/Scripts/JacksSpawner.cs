using UnityEngine;

public class JackSpawner : MonoBehaviour
{

    // corregido SerializeField


    [Header("Prefabs")]
    [SerializeField] private GameObject normalJackPrefab;
    [SerializeField] private GameObject specialJackPrefab;
    [SerializeField] private GameObject bombJackPrefab;

    [Header("Spawn")]

    [SerializeField] private BoxCollider2D spawnArea; // Asigna un GameObject con BoxCollider2D (Is Trigger)

    [SerializeField] private bool randomRotation = true;

    [SerializeField] private int numberOfJacks = 10;

    private void Start()
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("Spawn area not assigned. Please assign a BoxCollider2D.");
            return;
        }

        SpawnJacks();
    }
    public void SpawnJacks()
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("Asigna un BoxCollider2D como área de spawn.");
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
        // Deshabilita todos los Jacks que estén bajo este spawner
        var jacks = GetComponentsInChildren<Jack>(true);
        foreach (var jack in jacks)
        {
            jack.disable();
        }
    }

    public void EnableAll()
    {
        var jacks = GetComponentsInChildren<Jack>(true);
        foreach (var jack in jacks)
        {
            jack.enable();
        }
    }

    private static Vector3 RandomPointInArea(BoxCollider2D area)
    {
        Bounds b = area.bounds;
        float x = Random.Range(b.min.x, b.max.x);
        float y = Random.Range(b.min.y, b.max.y);
        return new Vector3(x, y, 0f);
    }
    // Devuelve un prefab con pesos: normal 50%, special 25%, bomb 25%
    private GameObject PickPrefab()
    {
        float r = Random.value; // [0,1)
        if (r < 0.50f) return normalJackPrefab;   // 50%
        if (r < 0.75f) return specialJackPrefab;  // 25%
        return bombJackPrefab;                    // 25%
    }
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (spawnArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnArea.bounds.center, spawnArea.bounds.size);
        }
    }
#endif

}
