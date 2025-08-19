using UnityEngine;

// PowerUpRegistry.cs


public static class PowerUpRegistry
{
    // Cuántos power-ups hay activos en escena
    public static int ActiveCount = 0;

    // Candado simple para evitar carreras de spawn en el mismo frame
    public static bool SpawnLock = false;
}



public class PowerUpSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField, Range(0f, 1f)] private float spawnChance = 0.5f;
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Exactamente 3 prefabs, uno por poder.")]
    [SerializeField] private GameObject[] powerUpPrefabs = new GameObject[3];

    [Header("Control")]
    [SerializeField] private bool onePerTurn = true;
    [SerializeField] private bool spawnOnStart = false;

    private int lastTurnIndex = -1;
    private bool spawnedThisTurn = false;

    void Start()
    {
        if (TurnManager.instance != null)
        {
            lastTurnIndex = TurnManager.instance.GetCurrentPlayerIndex();
            if (spawnOnStart) TrySpawn();
        }
    }

    void Update()
    {
        if (TurnManager.instance == null) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        if (powerUpPrefabs == null || powerUpPrefabs.Length != 3) return;

        int currentTurnIndex = TurnManager.instance.GetCurrentPlayerIndex();
        if (currentTurnIndex != lastTurnIndex)
        {
            spawnedThisTurn = false; // nuevo turno
            TrySpawn();
            lastTurnIndex = currentTurnIndex;
        }
    }

    private void TrySpawn()
    {
        if (onePerTurn && spawnedThisTurn) return;

        // Si ya hay alguno, no spawnear
        if (PowerUpRegistry.ActiveCount > 0) return;

        // Evitar carrera si hay varios spawners en el mismo frame
        if (PowerUpRegistry.SpawnLock) return;

        PowerUpRegistry.SpawnLock = true;
        try
        {
            // Rechequeo por si otro alcanzó a instanciar en este frame
            if (PowerUpRegistry.ActiveCount > 0) return;

            if (Random.value <= spawnChance)
            {
                Transform p = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (!p) return;

                int prefabIdx = Random.Range(0, powerUpPrefabs.Length); // 0..2
                GameObject prefab = powerUpPrefabs[prefabIdx];
                if (!prefab) return;

                Instantiate(prefab, p.position, p.rotation);
                spawnedThisTurn = true;
            }
        }
        finally
        {
            PowerUpRegistry.SpawnLock = false;
        }
    }
}
