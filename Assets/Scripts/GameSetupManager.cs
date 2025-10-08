using UnityEngine;

public class GameSetupManager : MonoBehaviour
{
    public static GameSetupManager Instance { get; private set; }

    [Header("Configuración de Objetos por Número de Jugadores")]
   
    [Space(10)]
    [Header("Objetos que solo se activan si el numero de jugadores es 3 o 4. ")]
    [Tooltip("Objetos que solo se activan si el numero de jugadores es 3 o 4. ")]
    [SerializeField] private GameObject[] objectsFor3Players;

    [Space(10)]
    [Header("Objetos que solo se activan si el numero de jugadores es 4. ")]
    [Tooltip("Objetos que solo se activan si el numero de jugadores es 4. ")]
    [SerializeField] private GameObject[] objectsFor4Players;

    public int NumActivePlayers { get; private set; } = 0; // leído por TagManager

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        int numPlayers = (RoundData.instance != null) ? RoundData.instance.numPlayers : 0;
        NumActivePlayers = numPlayers;

        switch (numPlayers)
        {
            case 2:
                SetObjectsActive(objectsFor3Players, false);
                SetObjectsActive(objectsFor4Players, false);
                break;
            case 3:
                SetObjectsActive(objectsFor3Players, true);
                SetObjectsActive(objectsFor4Players, false);
                break;
            case 4:
                SetObjectsActive(objectsFor3Players, true);
                SetObjectsActive(objectsFor4Players, true);
                break;
            default:
                Debug.LogError("Número de jugadores no válido: " + numPlayers);
                break;
        }

        Debug.Log("[GameSetupManager] NumActivePlayers=" + NumActivePlayers);
    }

    private void SetObjectsActive(GameObject[] objects, bool isActive)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }
        }
    }
}