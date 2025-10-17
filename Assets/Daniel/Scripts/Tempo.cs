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

        if (remaining <= 0f)
        {
            running = false;
            // Si hay sistemas de turnos activos, eliminar al jugador actual
            int current = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
            if (current >= 0 && GameRoundManager.instance != null)
            {
                GameRoundManager.instance.PlayerLose(current);
            }

            onTimerFinished?.Invoke();
            onGameFinished?.Invoke();
        }
    }

    // Inicia o reinicia el temporizador muestreando un nuevo límite gaussiano
    public void StartTimer()
    {
        limit = SampleGaussian(mean, stdDev);
        // Mantener tiempo mínimo positivo fijo para evitar 0 o negativos
        if (limit < 0.1f) limit = 0.1f;
        remaining = limit;
        running = true;
        UpdateText();
    }

    public void StopTimer()
    {
        running = false;
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
        // Mostrar solo el tiempo restante con sufijo "seg"
        timerText.text = $"{remaining:F2} seg";
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
}
