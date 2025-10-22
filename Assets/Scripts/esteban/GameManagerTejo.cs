using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManagerTejo : MonoBehaviour
{
    public static GameManagerTejo instance;

    [Header("Puntajes")]
    public int[] puntajes = new int[4];
    public int puntajeMaximo = 21;

    [Header("UI Puntajes")]
    public Text[] puntajeTextos;

    [Header("Ronda / Turnos")]
    [SerializeField] private int maxTiros = 3;
    [SerializeField] private float delayCambioTurno = 2f;
    [SerializeField] private float delayMoverCentro = 2f;

    [Header("Control de Input")]
    [SerializeField] private GameObject blocker;

    

    private int tirosRealizados = 0;
    private int cambiosDeTurno = 0;
    private bool esperandoCambioTurno = false;

    private void Awake()
    {
        if (instance == null) instance = this;
    }   

    void Start()
    {
        

        TutorialManagerTejo tutorialManager = FindObjectOfType<TutorialManagerTejo>();
        if (tutorialManager != null)
        {
            int numJugador = TurnManager.instance.CurrentTurn();
            int numPlayers = (RoundData.instance != null) ? RoundData.instance.numPlayers : 0;

            switch (numJugador)
            {
                case 1:
                    switch (numPlayers)
                    {
                        case 2: tutorialManager.MostrarPanel(12); break;
                        case 3: tutorialManager.MostrarPanel(1); break;
                        case 4: tutorialManager.MostrarPanel(4); break;
                        default: Debug.LogError("Número de jugadores no válido: " + numPlayers); break;
                    }
                    break;

                case 2:
                    switch (numPlayers)
                    {
                        case 2: tutorialManager.MostrarPanel(0); break;
                        case 3: tutorialManager.MostrarPanel(2); break;
                        case 4: tutorialManager.MostrarPanel(5); break;
                        default: Debug.LogError("Número de jugadores no válido: " + numPlayers); break;
                    }
                    break;

                case 3:
                    switch (numPlayers)
                    {
                        case 3: tutorialManager.MostrarPanel(3); break;
                        case 4: tutorialManager.MostrarPanel(6); break;
                        default: Debug.LogError("Número de jugadores no válido: " + numPlayers); break;
                    }
                    break;

                case 4:
                    if (numPlayers == 4) tutorialManager.MostrarPanel(7);
                    else Debug.LogError("Número de jugadores no válido: " + numPlayers);
                    break;
            }
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
            puntajeTextos[jugadorID].text = $"{puntajes[jugadorID]}";

        if (puntajes[jugadorID] >= puntajeMaximo)
        {
            GameRoundManager.instance.PlayerWin(jugadorID);
            for (int i = 0; i < puntajes.Length; i++)
                if (i != jugadorID) GameRoundManager.instance.PlayerLose(i);
        }
    }

    public void RestarPuntos(int jugadorID, int puntos)
    {
        puntajes[jugadorID] -= puntos;
        if (puntajeTextos != null && jugadorID >= 0 && jugadorID < puntajeTextos.Length)
            puntajeTextos[jugadorID].text = $"{puntajes[jugadorID]}";
    }

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
            SumarPuntos(jugadorCercano, 1);
    }

    public void AvisarCambioTurno()
    {
        cambiosDeTurno++;
        Debug.Log($"Cambio de turno #{cambiosDeTurno}");

        int numPlayers = (RoundData.instance != null) ? RoundData.instance.numPlayers : puntajes.Length;
        if (cambiosDeTurno >= numPlayers)
            DefinirGanadorPorMayorPuntaje();
    }

    private void DefinirGanadorPorMayorPuntaje()
    {
        long[] scores = new long[puntajes.Length];
        for (int i = 0; i < puntajes.Length; i++)
            scores[i] = puntajes[i];

        Debug.Log("Finalizando ronda por mayor puntaje tras 4 rondas...");
        GameRoundManager.instance.FinalizeRoundFromScores(scores);
    }

    public void RegistrarTejoLanzado()
    {
        tirosRealizados++;
        var jc = FindObjectOfType<JoystickControl>();
        if (jc != null) jc.RefreshTirosPanel();

        if (tirosRealizados >= maxTiros)
        {
            if (blocker != null) blocker.SetActive(true);
            esperandoCambioTurno = true;
        }
    }

    public void UltimoTejoLanzado() => esperandoCambioTurno = true;

    public void TejoTermino(Tejo tejo)
    {
        StartCoroutine(MoverCentroConDelay(delayMoverCentro));

        if (!esperandoCambioTurno) return;
        StartCoroutine(CambiarTurnoDespuesDeRetraso(delayCambioTurno));
        esperandoCambioTurno = false;
    }

    private IEnumerator MoverCentroConDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        var centro = FindObjectOfType<CentroController>();
        if (centro != null) centro.MoverCentro();
    }

    private IEnumerator CambiarTurnoDespuesDeRetraso(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (TurnManager.instance != null)
            TurnManager.instance.NextTurn();

        AvisarCambioTurno();

        var multi = FindObjectOfType<MultiJoystickControl>();
        if (multi != null)
            multi.PrepareForNextRound();

        var centro = FindObjectOfType<CentroController>();
        if (centro != null)
        {
            var sr = centro.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            var col = centro.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            centro.MoverCentro();
        }

        tirosRealizados = 0;

        if (blocker != null) blocker.SetActive(false);

        
        

        var tutorial = FindObjectOfType<TutorialManagerTejo>();
        if (tutorial != null && TurnManager.instance != null)
        {
            int jugador = TurnManager.instance.CurrentTurn();
            int numPlayers = (RoundData.instance != null) ? RoundData.instance.numPlayers : 0;
            tutorial.MostrarPanelPorJugador(jugador, numPlayers);
        }
    }

    public int ShotsRemaining() => Mathf.Max(0, maxTiros - tirosRealizados);
    public int MaxTiros => maxTiros;

    

    

}