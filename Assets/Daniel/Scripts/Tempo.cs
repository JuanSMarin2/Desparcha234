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

    [SerializeField, Tooltip("TextMeshPro Text para mostrar el tiempo transcurrido")]
    private TMP_Text timerText;

    [SerializeField, Tooltip("Si se activa, el temporizador reinicia automáticamente con un nuevo límite cuando termina")]
    private bool autoRestart = false;

    [SerializeField, Tooltip("Forzar límite mínimo positivo (evita valores negativos o cero)")]
    private bool clampPositive = true;

    [SerializeField, Tooltip("Evento que se invoca cuando termina el temporizador")]
    private UnityEvent onTimerFinished;

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

        // Iniciar temporizador automáticamente con un límite muestreado
        StartTimer();
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
            onTimerFinished?.Invoke();
            if (autoRestart) StartTimer();
        }
    }

    // Inicia o reinicia el temporizador muestreando un nuevo límite gaussiano
    public void StartTimer()
    {
        limit = SampleGaussian(mean, stdDev);
        if (clampPositive && limit < 0.1f) limit = 0.1f;
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
}
