using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManagerTejo : MonoBehaviour
{
    public static GameManagerTejo instance;

    [Header("Puntajes")]
    public int[] puntajes = new int[4];
    public int puntajeMaximo = 21;

    [Header("UI Puntajes")]
    public Text[] puntajeTextos;

    [Header("UI Feedback por jugador (-2 / +2)")]
    [SerializeField] private TextMeshProUGUI[] playerFeedbackTexts = new TextMeshProUGUI[4];
    [SerializeField] private float feedbackDuration = 0.5f;
    [SerializeField] private Vector3 feedbackBaseScale = Vector3.one;
    [SerializeField] private float feedbackPulseAmplitude = 0.2f;
    [SerializeField] private float feedbackPulseFrequency = 10f;

    private Coroutine[] feedbackCoroutines = new Coroutine[4];

    [Header("Ronda / Turnos")]
    [SerializeField] private int maxTiros = 3;
    [SerializeField] private float delayCambioTurno = 2f;
    [SerializeField] private float delayMoverCentro = 0.5f;

    [Header("Control de Input")]
    [SerializeField] private GameObject blocker;

    private int tirosRealizados = 0;
    private int cambiosDeTurno = 0;
    private bool esperandoCambioTurno = false;

    public static event Action<int> OnPapeletaDestruida;

    // ... resto del c�digo del GameManagerTejo ...

    //  M�todo seguro para notificar (llamado desde Tejo)

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void NotificarPapeletaDestruida(int idJugador)
    {
        Debug.Log($"[GameManagerTejo] Papeleta del jugador {idJugador + 1} destruida — mostrando icono triste y notificando evento");

        OnPapeletaDestruida?.Invoke(idJugador);

        MoverIconoTejo moverIconos = FindAnyObjectByType<MoverIconoTejo>();
        if (moverIconos != null)
            moverIconos.MostrarYMoverIconoTriste(idJugador);
        else
            Debug.LogWarning("No se encontró MoverIconoTejo en la escena para mostrar el icono triste.");

        //  Feedback visual para el jugador afectado (-2)
        MostrarFeedbackJugador(idJugador, "-2");
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
                        default: Debug.LogError("N�mero de jugadores no v�lido: " + numPlayers); break;
                    }
                    break;

                case 2:
                    switch (numPlayers)
                    {
                        case 2: tutorialManager.MostrarPanel(0); break;
                        case 3: tutorialManager.MostrarPanel(2); break;
                        case 4: tutorialManager.MostrarPanel(5); break;
                        default: Debug.LogError("N�mero de jugadores no v�lido: " + numPlayers); break;
                    }
                    break;

                case 3:
                    switch (numPlayers)
                    {
                        case 3: tutorialManager.MostrarPanel(3); break;
                        case 4: tutorialManager.MostrarPanel(6); break;
                        default: Debug.LogError("N�mero de jugadores no v�lido: " + numPlayers); break;
                    }
                    break;

                case 4:
                    if (numPlayers == 4) tutorialManager.MostrarPanel(7);
                    else Debug.LogError("N�mero de jugadores no v�lido: " + numPlayers);
                    break;
            }
        }
        else
        {
            Debug.LogWarning("No se encontr� TutorialManagerTejo en la escena.");
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

    // === NUEVO: Llamar cuando una papeleta es destruida ===
    public void PapeletaDestruidaPorJugador(int jugadorIndex)
    {
        Debug.Log($"[GameManagerTejo] Papeleta destruida del jugador {jugadorIndex}");
        OnPapeletaDestruida?.Invoke(jugadorIndex);

        // Refuerzo: mostrar feedback también si este es el flujo usado
        int currentTurnIndex0 = -1;
        if (TurnManager.instance != null)
            currentTurnIndex0 = TurnManager.instance.CurrentTurn() - 1; // 0-based

        MostrarFeedbackJugador(jugadorIndex, "-2");
        if (currentTurnIndex0 >= 0 && currentTurnIndex0 < 4)
            MostrarFeedbackJugador(currentTurnIndex0, "+2");
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
        // Reaparecer el centro después de 0.5 segundos
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

    // === API pública para mostrar feedback +2 para un jugador concreto (índice 0..3) ===
    // === API pública para mostrar feedback +3 para un jugador concreto ===
    public void MostrarPlusTresParaJugador(int jugadorIndex0)
    {
        MostrarFeedbackJugador(jugadorIndex0, "+3");
    }

    // === MÉTODO BASE: muestra cualquier texto de feedback (+3, -2, +6, etc.) ===
    public void MostrarFeedbackJugador(int index, string texto)
    {
        if (playerFeedbackTexts == null || index < 0 || index >= playerFeedbackTexts.Length)
            return;

        var tmp = playerFeedbackTexts[index];
        if (tmp == null) return;

        // Reiniciamos cualquier animación anterior del mismo jugador
        if (feedbackCoroutines[index] != null)
        {
            StopCoroutine(feedbackCoroutines[index]);
            feedbackCoroutines[index] = null;
        }

        feedbackCoroutines[index] = StartCoroutine(PulseFeedback(tmp, texto, feedbackDuration));
    }

    // === NUEVO: muestra +2, +3, +9, etc. según papeletas destruidas ===
    public void MostrarFeedbackPapeletas(int jugadorIndex0, int papeletasDestruidas)
    {
        if (papeletasDestruidas <= 0) return;
        int puntaje = papeletasDestruidas * 2;
        MostrarFeedbackJugador(jugadorIndex0, $"+{puntaje}");
    }

    private IEnumerator PulseFeedback(TextMeshProUGUI tmp, string content, float duration)
    {
        tmp.text = content;
        tmp.gameObject.SetActive(true);

        float t = 0f;
        tmp.rectTransform.localScale = feedbackBaseScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * Mathf.PI * 2f * feedbackPulseFrequency) * feedbackPulseAmplitude;
            tmp.rectTransform.localScale = feedbackBaseScale * s;
            yield return null;
        }

        tmp.rectTransform.localScale = feedbackBaseScale;
        tmp.gameObject.SetActive(false);
    }

    public int ShotsRemaining() => Mathf.Max(0, maxTiros - tirosRealizados);
    public int MaxTiros => maxTiros;


    


}