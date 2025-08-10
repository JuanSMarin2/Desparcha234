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

    private void SpawnJacks()
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("Asigna un BoxCollider2D como área de spawn.");
            return;
        }

        for (int i = 0; i < numberOfJacks; i++)
        {
            var prefab = PickPrefab();
            if (prefab == null) continue;

            Vector3 pos = RandomPointInArea(spawnArea);
            //Verificar si se rota o no.
            Quaternion rot = randomRotation ? Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)) : Quaternion.identity;

            Instantiate(prefab, pos, rot);
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
