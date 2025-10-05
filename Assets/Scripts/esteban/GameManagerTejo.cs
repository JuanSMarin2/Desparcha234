using UnityEngine;
using UnityEngine.UI;
using System.Collections; // <- importante para usar coroutines

public class GameManagerTejo : MonoBehaviour
{
    public static GameManagerTejo instance;

    [Header("Puntajes")]
    public int[] puntajes = new int[4]; // 4 jugadores
    public int puntajeMaximo = 21;

    [Header("UI Puntajes")]
    public Text[] puntajeTextos; // Arrastra 4 Text de UI en el inspector

    [Header("Ronda / Turnos")]
    [SerializeField] private int maxTiros = 3;          // editable desde inspector
    [SerializeField] private float delayCambioTurno = 2f; // configurable
    [SerializeField] private float delayMoverCentro = 2f; // configurable para que no se mueva de inmediato

    [Header("Control de Input")]
    [SerializeField] private GameObject blocker; // Asigna aquí el objeto bloqueador desde el inspector

    private int tirosRealizados = 0;
    private int cambiosDeTurno = 0;   // cuenta cuántas veces se llamó NextTurn
    private bool esperandoCambioTurno = false;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // Buscar una instancia de TutorialManagerTejo en la escena antes de llamar a MostrarPanel
        TutorialManagerTejo tutorialManager = FindObjectOfType<TutorialManagerTejo>();
        if (tutorialManager != null)
        {
            int numJugador = TurnManager.instance.CurrentTurn();
            switch 
                (numJugador)
            {
                case 1:
                    tutorialManager.MostrarPanel(4);
                    break;
                case 2:
                    tutorialManager.MostrarPanel(5);
                    break;
                case 3:
                    tutorialManager.MostrarPanel(6);
                    break;
                case 4:
                    tutorialManager.MostrarPanel(7);
                    break;                
            }
            tutorialManager.MostrarPanel(0);
        }
        else
        {
            Debug.LogWarning("No se encontró TutorialManagerTejo en la escena.");
        }
    }

    public void SumarPuntos(int jugadorID, int puntos)
    {
        Debug.Log($"Jugador {jugadorID} gana {puntos} puntos");
        puntajes[jugadorID] += puntos;
        if (puntajeTextos != null && jugadorID >= 0 && jugadorID < puntajeTextos.Length)
            puntajeTextos[jugadorID].text = $"J{jugadorID + 1}: {puntajes[jugadorID]}";

        // Victoria inmediata por puntaje máximo
        if (puntajes[jugadorID] >= puntajeMaximo)
        {
            GameRoundManager.instance.PlayerWin(jugadorID);

            // Los demás pierden
            for (int i = 0; i < puntajes.Length; i++)
            {
                if (i != jugadorID)
                    GameRoundManager.instance.PlayerLose(i);
            }
        }
    }

    public void RestarPuntos(int jugadorID, int puntos)
    {
        puntajes[jugadorID] -= puntos;
        if (puntajeTextos != null && jugadorID >= 0 && jugadorID < puntajeTextos.Length)
            puntajeTextos[jugadorID].text = $"J{jugadorID + 1}: {puntajes[jugadorID]}";
    }

    // Caso especial: cuando nadie cae en centro ni toca papeletas
    public void DarPuntoAlMasCercano(Vector3[] posicionesTejos, Vector3 centro)
    {
        float distanciaMin = float.MaxValue;
        int jugadorCercano = -1;

        for (int i = 0; i < posicionesTejos.Length; i++)
        {
            float dist = Vector3.Distance(posicionesTejos[i], centro);
            if (dist < distanciaMin)
            {
                distanciaMin = dist;
                jugadorCercano = i;
            }
        }

        if (jugadorCercano >= 0)
        {
            SumarPuntos(jugadorCercano, 1);
        }
    }

    public void AvisarCambioTurno()
    {
        cambiosDeTurno++;
        Debug.Log($"Cambio de turno #{cambiosDeTurno}");

        int numPlayers = (RoundData.instance != null) ? RoundData.instance.numPlayers : puntajes.Length;

        // cuando se completan 'numPlayers' ciclos se define ganador
        if (cambiosDeTurno >= numPlayers)
        {
            DefinirGanadorPorMayorPuntaje();
        }
    }

    private void DefinirGanadorPorMayorPuntaje()
    {
        // Convertir puntajes (int[]) a long[] para el método
        long[] scores = new long[puntajes.Length];
        for (int i = 0; i < puntajes.Length; i++)
            scores[i] = puntajes[i];

        Debug.Log("Finalizando ronda por mayor puntaje tras 4 rondas...");
        GameRoundManager.instance.FinalizeRoundFromScores(scores);
    }

    // Llamado cada vez que se instancia un tejo (desde JoystickControl)
    public void RegistrarTejoLanzado()
    {
        tirosRealizados++;

        if (tirosRealizados >= maxTiros)
        {
            // Bloqueamos los joysticks activando el Blocker
            if (blocker != null) blocker.SetActive(true);

            // Marcamos que el próximo TejoTermino debe cambiar el turno
            esperandoCambioTurno = true;
        }
    }

    // Compatibilidad si ya lo llamabas en otro flujo
    public void UltimoTejoLanzado()
    {
        esperandoCambioTurno = true;
    }

    // Llamado desde Tejo cuando su Rigidbody2D prácticamente se detuvo
    public void TejoTermino(Tejo tejo)
    {
        //  mover centro pero con un delay para que no se teletransporte de inmediato
        StartCoroutine(MoverCentroConDelay(delayMoverCentro));

        // si todavía no se cumplió el máximo de tiros, no hacemos nada más
        if (!esperandoCambioTurno) return;

        // si ya era el último tiro, iniciamos el cambio de turno con delay
        StartCoroutine(CambiarTurnoDespuesDeRetraso(delayCambioTurno));
        esperandoCambioTurno = false;
    }

    private IEnumerator MoverCentroConDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        CentroController centro = FindObjectOfType<CentroController>();
        if (centro != null)
            centro.MoverCentro();
    }

    private IEnumerator CambiarTurnoDespuesDeRetraso(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Avanzar turno
        if (TurnManager.instance != null)
            TurnManager.instance.NextTurn();

        AvisarCambioTurno();

        // Preparar siguiente ronda (activa joysticks pequeños, limpia tejos, etc.)
        MultiJoystickControl multi = FindObjectOfType<MultiJoystickControl>();
        if (multi != null)
            multi.PrepareForNextRound();

        // Rehabilitar y mover el Centro al comienzo de la nueva ronda
        CentroController centro = FindObjectOfType<CentroController>();
        if (centro != null)
        {
            var sr = centro.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            var col = centro.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            centro.MoverCentro();
        }

        // Reset para la siguiente ronda
        tirosRealizados = 0;

        //  Desbloquear joysticks al iniciar turno
        if (blocker != null) blocker.SetActive(false);
    }
}