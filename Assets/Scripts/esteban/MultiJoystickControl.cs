using UnityEngine;
using System.Linq;

public class MultiJoystickControl : MonoBehaviour
{
    [Header("Joystick units (ordenados 0..3)")]
    public JoystickUnit[] units;

    [Header("Joystick principal (punto de lanzamiento)")]
    public GameObject mainJoystickControlObject;

    [Header("Bloqueador del joystick")]
    public GameObject bloqueador;

    [Header("Conjuntos por jugador (opcional)")]
    [Tooltip("Raíz por jugador (0..3): grupo que contiene joystick, papeletas y UI de ese jugador.")]
    public GameObject[] playerSets;   // opcional

    [Header("Prefab del centro")]
    public GameObject centroPrefab;
    public Transform centroSpawnPoint; // opcional, si quieres controlar la posición

    public bool finished { get; private set; }
    private bool initialized = false;

    int NumPlayers()
    {
        // Si RoundData no existe aún, asume el tamaño del array de units
        int n = (RoundData.instance != null) ? RoundData.instance.numPlayers : units.Length;
        return Mathf.Clamp(n, 1, units.Length);
    }

    void Start()
    {
        finished = false;
        if (mainJoystickControlObject != null)
            mainJoystickControlObject.SetActive(false);

        // Asegura el estado correcto ANTES del primer Update
        EnforcePlayersActiveState();
        InicializarUnidadesDeArranque();
    }

    // Apaga todo lo que sea de jugadores inexistentes (joystick, target/papeleta y sets opcionales)
    void EnforcePlayersActiveState()
    {
        int n = NumPlayers();

        for (int i = 0; i < units.Length; i++)
        {
            bool jugadorExiste = (i < n);

            // Apaga/enciende grupo opcional por jugador
            if (playerSets != null && i < playerSets.Length && playerSets[i] != null)
                playerSets[i].SetActive(jugadorExiste);

            // Apaga/enciende JoystickUnit GameObject
            if (units[i] != null)
            {
                // Ojo: no llamamos ResetUnit aquí para no decidir turnos todavía.
                units[i].gameObject.SetActive(jugadorExiste);

                // También podemos apagar el target (papeleta) si el jugador no existe
                if (units[i].targetObject != null)
                    units[i].targetObject.gameObject.SetActive(jugadorExiste);
            }
        }
    }

    // Solo una vez: suscribe eventos y aplica el estado activo/inactivo correcto para el primer turno
    void InicializarUnidadesDeArranque()
    {
        if (TurnManager.instance == null) return;
        int n = NumPlayers();
        int blockedIndex = Mathf.Clamp(TurnManager.instance.CurrentTurn() - 1, 0, units.Length - 1);

        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] == null) continue;

            units[i].OnFinished -= HandleUnitFinished; // evita doble suscripción si se llama dos veces
            units[i].OnFinished += HandleUnitFinished;

            bool shouldBeActive = (i < n) && (i != blockedIndex);
            units[i].ResetUnit(shouldBeActive);
        }
    }


    // --- utilidad: llamar al comenzar nueva ronda/turno ---
    public void PrepareForNextRound()
    {
        int n = NumPlayers();

        // Buscar o instanciar el centro
        CentroController centro = FindObjectOfType<CentroController>();
        if (centro == null && centroPrefab != null)
        {
            GameObject centroObj = Instantiate(centroPrefab, centroSpawnPoint != null ? centroSpawnPoint.position : Vector3.zero, Quaternion.identity);
            centro = centroObj.GetComponent<CentroController>();
        }

        if (centro != null)
        {
            SpriteRenderer sr = centro.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            Collider2D col = centro.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            centro.MoverCentro(); // mover al inicio de ronda
        }

        // limpiar proyectiles
        GameObject[] projectiles = GameObject.FindGameObjectsWithTag("Tejo");
        foreach (GameObject proj in projectiles)
        {
            Destroy(proj);
        }

        finished = false;

        // calcular qué joystick se bloquea
        int blockedIndex = Mathf.Clamp(TurnManager.instance.CurrentTurn() - 1, 0, units.Length - 1);

        // MUY IMPORTANTE: respeta numPlayers aquí también
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] == null) continue;

            bool shouldBeActive = (i < n) && (i != blockedIndex);
            units[i].ResetUnit(shouldBeActive);

            // sincroniza sets opcionales por jugador
            if (playerSets != null && i < playerSets.Length && playerSets[i] != null)
                playerSets[i].SetActive(i < n);
        }

        if (mainJoystickControlObject != null)
            mainJoystickControlObject.SetActive(false);

        if (bloqueador != null)
            bloqueador.SetActive(false);  
    }

    void Update()
    {
        // Esperar hasta que TurnManager esté listo y solo inicializar una vez
        if (!initialized && TurnManager.instance != null && TurnManager.instance.CurrentTurn() > 0)
        {
            initialized = true;

            // Asegura coherencia al inicio: respeta numPlayers
            EnforcePlayersActiveState();
            InicializarUnidadesDeArranque();
        }
    }

    void HandleUnitFinished(JoystickUnit unit)
    {
        Debug.Log($"JoystickUnit {unit.name} terminó. Revisando si todos acabaron...");

        if (AllActiveFinished())
        {
            finished = true;
            Debug.Log(" MultiJoystickManager: todos los joysticks activos terminaron.");

            if (mainJoystickControlObject != null)
            {
                mainJoystickControlObject.SetActive(true);
                // Buscar una instancia de TutorialManagerTejo en la escena antes de llamar a MostrarPanel
                TutorialManagerTejo tutorialManager = FindObjectOfType<TutorialManagerTejo>();
                if (tutorialManager != null)
                {
                    int numJugador = TurnManager.instance.CurrentTurn();
                    switch
                        (numJugador)
                    {
                        case 1:
                            tutorialManager.MostrarPanel(8);
                            break;
                        case 2:
                            tutorialManager.MostrarPanel(9);
                            break;
                        case 3:
                            tutorialManager.MostrarPanel(10);
                            break;
                        case 4:
                            tutorialManager.MostrarPanel(11);
                            break;
                    }

                }
                else
                {
                    Debug.LogWarning("No se encontró TutorialManagerTejo en la escena.");
                }
                Debug.Log("¿Activo?: " + mainJoystickControlObject.activeSelf);
                Debug.Log(" Activado Joystick principal (zona de tiro).");
            }
        }
        else
        {
            Debug.Log(" Aún faltan joysticks por terminar.");
        }
    }

    bool AllActiveFinished()
    {
        foreach (var u in units)
        {
            if (u == null) continue;
            if (u.IsActive && !u.IsFinished) return false;
        }
        return true;
    }
}
