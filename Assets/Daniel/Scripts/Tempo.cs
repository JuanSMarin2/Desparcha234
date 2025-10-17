using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class Tempo : MonoBehaviour
{
    [Header("Gaussian timer settings")]
    [SerializeField, Tooltip("Media para el tiempo límite (segundos)")]
    private float mean = 50f;

    [SerializeField, Tooltip("Desviación estándar para la distribución Gaussiana")]
    private float stdDev = 10f;

    [SerializeField, Tooltip("TextMeshPro Text (opcional) para mostrar el tiempo restante")]
    private TMP_Text timerText;

    [Header("Texto y retardo de finalización")]
    [SerializeField, Tooltip("Texto a mostrar mientras el tiempo corre (>0)")]
    private string tingoText = "¡¡Tingo!!";
    [SerializeField, Tooltip("Texto a mostrar cuando llega a 0")]
    private string tangoText = "¡¡Tango!!";
    [SerializeField, Tooltip("Segundos a esperar mostrando '¡¡Tango!!' antes de avanzar")]
    private float finishHoldSeconds = 2f;

    [Header("Eventos")]
    [SerializeField, Tooltip("Evento que se invoca cuando termina el temporizador")]
    private UnityEvent onTimerFinished;

    [Tooltip("Evento estándar para integración con la secuencia de minijuegos")]
    public UnityEvent onGameFinished;

    [Tooltip("Si es true, el GameSequenceController NO llamará a NextTurn cuando este minijuego termine.")]
    public bool skipNextTurnOnFinish = true;

    [Header("Inicio")]
    [SerializeField, Tooltip("Si está activo, el temporizador inicia automáticamente al activar el objeto.")]
    private bool autoStart = false;

    // Estado interno
    private float limit;
    // Tiempo restante (countdown). Se inicializa a `limit` en StartTimer y decrementa cada frame
    private float remaining;
    private bool running = false;
    private bool finishing = false; // evitando doble finalización
    private int eliminatedPlayerIndex = -1; // se usa al finalizar tras el hold
    // Hitos de log (múltiplos de 10s)
    private int nextLogMilestone = -1;

    [Header("Animación 'Tingo' (pulso)")]
    [SerializeField, Tooltip("Si está activo, el texto '¡¡Tingo!!' hará un pulso de escala en bucle mientras el tiempo corre.")]
    private bool enableTingoPulse = true;
    [SerializeField, Tooltip("Velocidad del pulso (ciclos por segundo).")]
    private float tingoPulseSpeed = 1.6f;
    [SerializeField, Tooltip("Escala mínima del pulso.")]
    private float tingoScaleMin = 0.92f;
    [SerializeField, Tooltip("Escala máxima del pulso.")]
    private float tingoScaleMax = 1.08f;
    private float tingoPulsePhase = 0f;
    private Vector3 timerTextOriginalScale = Vector3.one;

    [Header("Bloqueo y pausa durante '¡¡Tango!!'")]
    [SerializeField, Tooltip("GameObject UI (por ejemplo, una Image full-screen con Raycast Target) que se activa para bloquear toda interacción mientras se muestra '¡¡Tango!!'.")]
    private GameObject interactionBlocker;
    [SerializeField, Tooltip("Pausar el juego ajustando Time.timeScale a 0 durante el hold de '¡¡Tango!!'.")]
    private bool pauseWithTimeScale = true;
    private float prevTimeScale = 1f;

    [Header("Panel de eliminación (durante '¡¡Tango!!')")]
    [SerializeField, Tooltip("Panel que muestra al jugador eliminado durante el hold de '¡¡Tango!!'.")]
    private GameObject eliminationPanel;
    [SerializeField, Tooltip("Lista de objetos de imagen (uno por jugador, ya con su sprite). Se activará solo el del jugador eliminado.")]
    private GameObject[] iconObjectsByPlayer;
    [SerializeField, Tooltip("Texto para el encabezado (se formatea como 'Jugador {n} eliminado').")]
    private TMP_Text eliminationHeaderText;
    [SerializeField, Tooltip("Texto para mostrar una frase de lástima aleatoria.")]
    private TMP_Text eliminationPhraseText;
    [SerializeField, TextArea, Tooltip("Lista de frases de lástima; se elegirá una aleatoriamente cuando sea '¡¡Tango!!'.")]
    private string[] pityPhrases;

    // RNG para Box-Muller
    private System.Random rng = new System.Random();

    void Start()
    {
        // Si no se asignó el TMP_Text por inspector, intentar obtenerlo del mismo GameObject
        if (timerText == null)
        {
            timerText = GetComponent<TMP_Text>();
            if (timerText == null)
                Debug.LogWarning("Tempo: timerText no asignado y no se encontró TMP_Text en el mismo GameObject.");
        }

        // Guardar escala original para restaurar al finalizar
        if (timerText != null)
            timerTextOriginalScale = timerText.rectTransform.localScale;
        else
            timerTextOriginalScale = transform.localScale;

        // Iniciar automáticamente si está habilitado; sino, usar PlayMiniGamen()/StartTimer desde fuera
        if (autoStart) StartTimer();
    }

    void Update()
    {
        if (!running) return;

        // Decrementar tiempo restante
        remaining -= Time.deltaTime;
        if (remaining < 0f) remaining = 0f;
        UpdateText();

        // Pulso de 'Tingo' mientras el tiempo corre (> 0)
        if (enableTingoPulse && timerText != null && remaining > 0f)
        {
            tingoPulsePhase += Time.deltaTime * Mathf.Max(0f, tingoPulseSpeed) * Mathf.PI * 2f; // rad/s
            float s = Mathf.Lerp(tingoScaleMin, tingoScaleMax, (Mathf.Sin(tingoPulsePhase) + 1f) * 0.5f);
            timerText.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        if (remaining <= 0f)
        {
            if (!finishing)
            {
                running = false;
                finishing = true;
                // Mostrar "¡¡Tango!!" inmediatamente
                SetText(tangoText);
                // Restaurar escala del texto para 'Tango'
                ResetTextScale();

                // Registrar jugador a eliminar, pero diferir la eliminación hasta después del hold
                int current = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
                eliminatedPlayerIndex = current;

                // Bloquear interacción y pausar si procede
                BeginHoldState();

                // Mostrar panel de eliminación con icono y frase
                ShowEliminationUI(current);

                // Esperar unos segundos antes de notificar fin y permitir avanzar turno
                StartCoroutine(FinishAfterDelay());
            }
        }
        else
        {
            // Log cuando pasamos por múltiplos de 10 (50, 40, 30, ...)
            while (nextLogMilestone >= 10 && remaining <= nextLogMilestone)
            {
                Debug.Log($"[Tempo] Quedan {nextLogMilestone} segundos.");
                nextLogMilestone -= 10;
            }
        }
    }

    // Inicia o reinicia el temporizador muestreando un nuevo límite gaussiano
    public void StartTimer()
    {
        // Ajustar media según cantidad de jugadores activos
        float adjustedMean = mean;
        int players = Dificultad.GetActivePlayersCount();
        switch (players)
        {
            case 4: adjustedMean = 60f; break;
            case 3: adjustedMean = 50f; break;
            case 2: adjustedMean = 35f; break;
            default: adjustedMean = mean; break;
        }

        limit = SampleGaussian(adjustedMean, stdDev);
        // Mantener tiempo mínimo positivo fijo para evitar 0 o negativos
        if (limit < 0.1f) limit = 0.1f;
        remaining = limit;
        running = true;
        finishing = false;
        tingoPulsePhase = 0f;
        // Preparar siguiente múltiplo de 10 a reportar (50,40,30,...)
        nextLogMilestone = Mathf.FloorToInt((remaining - 0.0001f) / 10f) * 10;
        if (nextLogMilestone < 10) nextLogMilestone = -1; // desactivar si menor a 10
        ResetTextScale();
        UpdateText();
    }

    public void StopTimer()
    {
        running = false;
        finishing = false;
        EndHoldState();
        nextLogMilestone = -1;
        ResetTextScale();
    }

    public void ResetTimer()
    {
        // Reinicia el tiempo restante al límite actual (no muestrea uno nuevo)
        remaining = limit;
        UpdateText();
    }

    private void UpdateText()
    {
        if (timerText == null) return;
        // Mientras corre (> 0), mostrar "¡¡Tingo!!" en vez de números
        if (remaining > 0f)
        {
            timerText.text = tingoText;
        }
        else
        {
            timerText.text = tangoText;
        }
    }

    private void SetText(string text)
    {
        if (timerText != null) timerText.text = text;
    }

    private void ResetTextScale()
    {
        if (timerText != null)
            timerText.rectTransform.localScale = timerTextOriginalScale;
        else
            transform.localScale = timerTextOriginalScale;
    }

    // (sin throttling) mantenerlo simple

    // Muestra una muestra de una normal con media mu y desviación sigma (Box-Muller)
    private float SampleGaussian(float mu, float sigma)
    {
        // Generar dos uniformes en (0,1]
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        double stdNormal = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Sin(2.0 * System.Math.PI * u2);
        double value = mu + sigma * stdNormal;
        return (float)value;
    }

    // (Sin integración de minijuegos) Este componente actúa solo como temporizador.
    [ContextMenu("PlayMiniGamen")]
    public void PlayMiniGamen()
    {
        StartTimer();
    }

    private System.Collections.IEnumerator FinishAfterDelay()
    {
        // Espera en tiempo real para que no dependa de Time.timeScale
        float wait = Mathf.Max(0f, finishHoldSeconds);
        if (wait > 0f)
        {
            yield return new WaitForSecondsRealtime(wait);
        }
        // Ejecutar eliminación del jugador ahora, tras mostrar el panel el tiempo requerido
        if (eliminatedPlayerIndex >= 0 && GameRoundManager.instance != null)
        {
            GameRoundManager.instance.PlayerLose(eliminatedPlayerIndex);
        }

        // Restaurar interacción/pausa antes de notificar
        EndHoldState();
        onTimerFinished?.Invoke();
        onGameFinished?.Invoke();
        finishing = false;
        eliminatedPlayerIndex = -1;
    }

    private void BeginHoldState()
    {
        // Activar overlay de bloqueo si está asignado
        if (interactionBlocker != null)
        {
            interactionBlocker.SetActive(true);
        }
        // Pausar tiempo de juego si está habilitado
        if (pauseWithTimeScale)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    private void EndHoldState()
    {
        if (interactionBlocker != null)
        {
            interactionBlocker.SetActive(false);
        }
        if (pauseWithTimeScale)
        {
            Time.timeScale = prevTimeScale;
        }
        if (eliminationPanel != null)
        {
            eliminationPanel.SetActive(false);
        }
    }

    private void ShowEliminationUI(int playerIndex)
    {
        if (eliminationPanel != null)
        {
            eliminationPanel.SetActive(true);
        }

        // Encabezado con número de jugador (1-based)
        if (eliminationHeaderText != null)
        {
            if (playerIndex >= 0)
                eliminationHeaderText.text = $"Jugador {playerIndex + 1} eliminado";
            else
                eliminationHeaderText.text = "Jugador eliminado";
        }

        // Icono por GameObject en lista: activar solo el del índice
        if (iconObjectsByPlayer != null && iconObjectsByPlayer.Length > 0)
        {
            for (int i = 0; i < iconObjectsByPlayer.Length; i++)
            {
                if (iconObjectsByPlayer[i] != null)
                    iconObjectsByPlayer[i].SetActive(playerIndex >= 0 && i == playerIndex);
            }
        }

        // Frase aleatoria
        if (eliminationPhraseText != null && pityPhrases != null && pityPhrases.Length > 0)
        {
            int idx = rng.Next(0, pityPhrases.Length);
            eliminationPhraseText.text = pityPhrases[idx];
        }
    }
}
