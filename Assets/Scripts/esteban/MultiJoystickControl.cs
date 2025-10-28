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
    public GameObject[] playerSets;

    [Header("Prefab del centro")]
    public GameObject centroPrefab;
    public Transform centroSpawnPoint;

    public bool finished { get; private set; }
    private bool initialized = false;

    private MoverIconoTejo moverIconoTejo; // referencia a tu clase de iconos

    int NumPlayers()
    {
        int n = (RoundData.instance != null) ? RoundData.instance.numPlayers : units.Length;
        return Mathf.Clamp(n, 1, units.Length);
    }

    void Start()
    {
        finished = false;
        moverIconoTejo = FindObjectOfType<MoverIconoTejo>();

        if (mainJoystickControlObject != null)
            mainJoystickControlObject.SetActive(false);

        EnforcePlayersActiveState();
        InicializarUnidadesDeArranque();
    }

    void EnforcePlayersActiveState()
    {
        int n = NumPlayers();

        for (int i = 0; i < units.Length; i++)
        {
            bool jugadorExiste = (i < n);

            if (playerSets != null && i < playerSets.Length && playerSets[i] != null)
                playerSets[i].SetActive(jugadorExiste);

            if (units[i] != null)
            {
                units[i].gameObject.SetActive(jugadorExiste);
                if (units[i].targetObject != null)
                    units[i].targetObject.gameObject.SetActive(jugadorExiste);
            }
        }
    }

    void InicializarUnidadesDeArranque()
    {
        if (TurnManager.instance == null) return;
        int n = NumPlayers();
        int blockedIndex = Mathf.Clamp(TurnManager.instance.CurrentTurn() - 1, 0, units.Length - 1);

        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] == null) continue;

            units[i].OnFinished -= HandleUnitFinished;
            units[i].OnFinished += HandleUnitFinished;

            bool shouldBeActive = (i < n) && (i != blockedIndex);
            units[i].ResetUnit(shouldBeActive);
        }
    }

    public void PrepareForNextRound()
    {
        int n = NumPlayers();
        finished = false;

        // Reinicia el centro si es necesario
        CentroController centro = FindObjectOfType<CentroController>();
        if (centro == null && centroPrefab != null)
        {
            GameObject centroObj = Instantiate(
                centroPrefab,
                centroSpawnPoint != null ? centroSpawnPoint.position : Vector3.zero,
                Quaternion.identity
            );
            centro = centroObj.GetComponent<CentroController>();
        }

        if (centro != null)
        {
            SpriteRenderer sr = centro.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            Collider2D col = centro.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            centro.MoverCentro();
        }

        foreach (GameObject proj in GameObject.FindGameObjectsWithTag("Tejo"))
            Destroy(proj);

        int blockedIndex = Mathf.Clamp(TurnManager.instance.CurrentTurn() - 1, 0, units.Length - 1);

        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] == null) continue;

            bool shouldBeActive = (i < n) && (i != blockedIndex);
            units[i].ResetUnit(shouldBeActive);

            if (playerSets != null && i < playerSets.Length && playerSets[i] != null)
                playerSets[i].SetActive(i < n);
        }

        if (mainJoystickControlObject != null)
            mainJoystickControlObject.SetActive(false);

        if (bloqueador != null)
            bloqueador.SetActive(false);

        //  Evita que los iconos se activen aquí al cambiar turno
        // Solo los activaremos al cerrar el panel correspondiente
    }

    void Update()
    {
        if (!initialized && TurnManager.instance != null && TurnManager.instance.CurrentTurn() > 0)
        {
            initialized = true;
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
            Debug.Log("MultiJoystickManager: todos los joysticks activos terminaron.");

            if (mainJoystickControlObject != null)
            {
                mainJoystickControlObject.SetActive(true);
                TutorialManagerTejo tutorialManager = FindObjectOfType<TutorialManagerTejo>();

                if (tutorialManager != null)
                {
                    int numJugador = TurnManager.instance.CurrentTurn();

                    switch (numJugador)
                    {
                        case 1: tutorialManager.MostrarPanel(8); break;
                        case 2: tutorialManager.MostrarPanel(9); break;
                        case 3: tutorialManager.MostrarPanel(10); break;
                        case 4: tutorialManager.MostrarPanel(11); break;
                    }

                    
                }
                else
                {
                    Debug.LogWarning("No se encontró TutorialManagerTejo en la escena.");
                }
            }
        }
        else
        {
            Debug.Log("Aún faltan joysticks por terminar.");
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
