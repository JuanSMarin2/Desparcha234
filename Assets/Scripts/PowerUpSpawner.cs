using System.Linq;
using UnityEngine;

public static class PowerUpRegistry
{
    public static int ActiveCount = 0;
    public static bool SpawnLock = false;
}

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField, Range(0f, 1f)] private float spawnChance = 0.5f;
    [SerializeField] private Transform[] spawnPoints;
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
            spawnedThisTurn = false;
            TrySpawn();
            lastTurnIndex = currentTurnIndex;
        }
    }

    private void TrySpawn()
    {
        if (onePerTurn && spawnedThisTurn) return;
        if (PowerUpRegistry.ActiveCount > 0) return;
        if (PowerUpRegistry.SpawnLock) return;

        PowerUpRegistry.SpawnLock = true;
        try
        {
            if (PowerUpRegistry.ActiveCount > 0) return;

            if (Random.value <= spawnChance)
            {
                var activeSpawnPoints = spawnPoints.Where(p => p.gameObject.activeInHierarchy).ToArray();
                if (activeSpawnPoints.Length == 0) return;

                Transform p = activeSpawnPoints[Random.Range(0, activeSpawnPoints.Length)];
                if (!p) return;

                int prefabIdx = Random.Range(0, powerUpPrefabs.Length);
                GameObject prefab = powerUpPrefabs[prefabIdx];
                if (!prefab) return;

                GameObject powerUp = Instantiate(prefab, p.position, p.rotation);

                // AÑADE ESTO: Configura el power-up para que actualice el registro
                PowerUpController powerUpController = powerUp.GetComponent<PowerUpController>();
                if (powerUpController == null)
                {
                    powerUpController = powerUp.AddComponent<PowerUpController>();
                }

                spawnedThisTurn = true;
            }
        }
        finally
        {
            PowerUpRegistry.SpawnLock = false;
        }
    }
}