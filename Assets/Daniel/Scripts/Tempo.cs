using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Reflection;

public class Tempo : MonoBehaviour
{
    public static Tempo instance;
    [Header("Tiempo fijo por dificultad (compatibilidad)")]
    [SerializeField, Tooltip("Media base (ya no se usa con tiempos fijos por jugador). Se mantiene por compatibilidad.")]
    private float mean = 20f;
    [SerializeField, Tooltip("Desviación (ya no se usa). Se mantiene por compatibilidad.")]
    private float stdDev = 5f;

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
    // Tiempo restante (countdown). Se inicializa a limit en StartTimer y decrementa cada frame
    private float remaining;
    private bool running = false;
    private bool finishing = false; // evitando doble finalización
    private int eliminatedPlayerIndex = -1; // se usa al finalizar tras el hold
    // Hitos de log (múltiplos de 10s)
    private int nextLogMilestone = -1;
    private int lastTickPlayerIndex = -1;
    // Congelamiento externo (p. ej., durante reproducción de patrón en minijuegos)
    private int externalFreezeCount = 0;

    [Header("UI de sliders por jugador")]
    [SerializeField, Tooltip("Contenedor donde se instancian los sliders (uno por jugador).")]
    private RectTransform slidersParent;
    [SerializeField, Tooltip("Prefab por defecto para el slider.")]
    private GameObject defaultSliderPrefab;
    [SerializeField, Tooltip("Prefab por índice de jugador (0-based). Si existe, se usará en lugar del prefab por defecto.")]
    private GameObject[] sliderPrefabsByPlayer;
    [SerializeField, Tooltip("Segundos extra otorgados a cada jugador activo cuando alguien es eliminado.")]
    private float bonusOnEliminationSeconds = 10f;

    private class PlayerTimer
    {
        public int playerIndex;
        public float remainingSeconds;
        public float maxSeconds;
        public float initialMaxSeconds; // tiempo original por dificultad
        public Slider slider;
        public GameObject go;
    }

    private readonly System.Collections.Generic.Dictionary<int, PlayerTimer> timers = new System.Collections.Generic.Dictionary<int, PlayerTimer>();
    private bool timersInitialized = false;

    [Header("Reloj de arena (animación)")]
    [SerializeField, Tooltip("RectTransform/Imagen del reloj de arena a animar")] 
    private RectTransform sandClock;
    [SerializeField, Tooltip("Velocidad base de giro en grados/seg.")] 
    private float sandClockBaseRotSpeed = 180f;
    [SerializeField, Tooltip("Velocidad de giro urgente (<10s) en grados/seg.")] 
    private float sandClockUrgentRotSpeed = 360f;
    [SerializeField, Tooltip("Frecuencia de rebote (ciclos/seg.)")] 
    private float sandClockBounceSpeed = 3.5f;
    [SerializeField, Tooltip("Amplitud de rebote base (escala adicional)")] 
    private float sandClockBounceAmp = 0.06f;
    [SerializeField, Tooltip("Amplitud de rebote urgente (<10s)")] 
    private float sandClockUrgentBounceAmp = 0.12f;
    [SerializeField, Tooltip("Habilitar animación del reloj de arena")] 
    private bool enableSandClock = true;
    private float sandClockBouncePhase = 0f;
    private Vector3 sandClockOriginalScale = Vector3.one;
    private Quaternion sandClockOriginalRotation = Quaternion.identity;
    private bool sandClockWasActive = false;

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
    private Color timerTextOriginalColor = Color.white;

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

    [Header("Sonidos del temporizador")]
    [SerializeField, Tooltip("Clave SFX para el tic del cronómetro mientras corre (usar prefijo 'Tingo:').")]
    private string timerTickSfxKey = "Tingo:TimerTick";
    [SerializeField, Tooltip("Intervalo entre tics del cronómetro en segundos.")]
    private float timerTickInterval = 1.0f;
    [SerializeField, Tooltip("Volumen del tic del cronómetro (0..1)")]
    [Range(0f,1f)] private float timerTickVolume = 1f;
    [SerializeField, Tooltip("Clave SFX para reproducir cuando se muestra la pantalla de eliminación (Tango).")]
    private string eliminationSfxKey = "Tingo:TANGO";

    [Header("IconManager (opcional)")]
    [SerializeField, Tooltip("Referencia directa al IconManager. Si no se asigna, se buscará en la escena.")]
    private IconManager iconManager;

    // RNG para Box-Muller
    private System.Random rng = new System.Random();

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

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
        {
            timerTextOriginalScale = timerText.rectTransform.localScale;
            timerTextOriginalColor = timerText.color;
        }
        else
            timerTextOriginalScale = transform.localScale;

        // Guardar estado original del reloj de arena
        if (sandClock != null)
        {
            sandClockOriginalScale = sandClock.localScale;
            sandClockOriginalRotation = sandClock.localRotation;
            sandClockWasActive = sandClock.gameObject.activeInHierarchy;
        }

        // Iniciar automáticamente si está habilitado; sino, usar PlayMiniGamen()/StartTimer desde fuera
        if (autoStart) StartTimer();
    }

    void Update()
    {
        if (!running) return;

        int currentPlayer = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (currentPlayer < 0) return;

        if (!timersInitialized) SetupTimersIfNeeded();

        if (!timers.TryGetValue(currentPlayer, out var pt))
        {
            pt = CreatePlayerTimer(currentPlayer, GetInitialSecondsByDifficulty());
            timers[currentPlayer] = pt;
        }

        // Si el reloj de arena acaba de activarse, recalibrar su escala/rotación base desde la del inspector
        if (sandClock != null)
        {
            bool nowActive = sandClock.gameObject.activeInHierarchy;
            if (nowActive && !sandClockWasActive)
            {
                sandClockOriginalScale = sandClock.localScale;
                sandClockOriginalRotation = sandClock.localRotation;
                sandClockBouncePhase = 0f;
            }
            sandClockWasActive = nowActive;
        }

        if (lastTickPlayerIndex != currentPlayer)
        {
            lastTickPlayerIndex = currentPlayer;
            nextLogMilestone = Mathf.FloorToInt((pt.remainingSeconds - 0.0001f) / 10f) * 10;
            if (nextLogMilestone < 10) nextLogMilestone = -1;
            // Actualizar visibilidad de sliders: mostrar solo el del jugador actual
            UpdateSlidersVisibility(currentPlayer);
        }

        // Sincronizar con compatibilidad de variables
        limit = pt.maxSeconds;
        remaining = pt.remainingSeconds;

        // Decrementar tiempo restante solo si no está congelado externamente
        if (externalFreezeCount <= 0)
        {
            remaining -= Time.deltaTime;
        }
        if (remaining < 0f) remaining = 0f;
        UpdateText();

        // Actualizar slider
        if (pt.slider != null)
        {
            pt.slider.maxValue = pt.maxSeconds;
            pt.slider.value = remaining;
        }

        // Pulso de 'Tingo' mientras el tiempo corre (> 0)
        if (enableTingoPulse && timerText != null && remaining > 0f)
        {
            tingoPulsePhase += Time.deltaTime * Mathf.Max(0f, tingoPulseSpeed) * Mathf.PI * 2f; // rad/s
            float s = Mathf.Lerp(tingoScaleMin, tingoScaleMax, (Mathf.Sin(tingoPulsePhase) + 1f) * 0.5f);
            timerText.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        // Titilar color de 'Tingo' cuando falten pocos segundos
        UpdateUrgentBlinkColor();

        // Animación del reloj de arena mientras el tiempo corre
        if (enableSandClock && sandClock != null && remaining > 0f)
        {
            float rotSpeed = remaining <= 10f ? sandClockUrgentRotSpeed : sandClockBaseRotSpeed;
            sandClock.Rotate(0f, 0f, -rotSpeed * Time.deltaTime);

            sandClockBouncePhase += Time.deltaTime * Mathf.Max(0f, sandClockBounceSpeed) * Mathf.PI * 2f;
            float amp = remaining <= 10f ? sandClockUrgentBounceAmp : sandClockBounceAmp;
            float bounce = Mathf.Abs(Mathf.Sin(sandClockBouncePhase)); // rebote (solo positivo)
            float scale = 1f + amp * bounce;
            sandClock.localScale = sandClockOriginalScale * scale;
        }

        if (remaining <= 0f)
        {
            if (!finishing)
            {
                running = false;
                finishing = true;
                // Detener el tic del cronómetro al entrar en 'Tango'
                StopTickLoop();
                // Mostrar "¡¡Tango!!" inmediatamente
                SetText(tangoText);
                // Restaurar escala del texto para 'Tango'
                ResetTextScale();
                // Restaurar color para 'Tango'
                ResetTextColor();

                // Registrar jugador a eliminar, pero diferir la eliminación hasta después del hold
                int current = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
                eliminatedPlayerIndex = current;

                // Persistir el 0 del jugador actual
                pt.remainingSeconds = remaining;

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

        // Guardar back en el timer del jugador actual
        pt.remainingSeconds = remaining;
    }

    // Inicia o reinicia el temporizador muestreando un nuevo límite gaussiano
    public void StartTimer()
    {
        // Inicializar timers si no existen y reanudar temporizador del jugador actual
        SetupTimersIfNeeded();
        int currentPlayer = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (currentPlayer >= 0 && timers.TryGetValue(currentPlayer, out var pt))
        {
            limit = pt.maxSeconds;
            remaining = pt.remainingSeconds;
            nextLogMilestone = Mathf.FloorToInt((remaining - 0.0001f) / 10f) * 10;
            if (nextLogMilestone < 10) nextLogMilestone = -1;
            lastTickPlayerIndex = currentPlayer;
            UpdateSlidersVisibility(currentPlayer);
        }
        running = true;
        finishing = false;
        tingoPulsePhase = 0f;
        ResetTextScale();
        UpdateText();
        ResetTextColor();
        // Iniciar bucle de tics de cronómetro
        StartTickLoop();
    }

    public void StopTimer()
    {
        running = false;
        finishing = false;
        StopTickLoop();
        EndHoldState();
        nextLogMilestone = -1;
        ResetTextScale();
        ResetTextColor();
        ResetSandClockVisuals();
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
    private float SampleGaussian(float mu, float sigma) { return mu; } // No-op (compatibilidad)

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
        if (eliminatedPlayerIndex >= 0)
        {
            // Eliminar slider y timer del jugador eliminado
            if (timers.TryGetValue(eliminatedPlayerIndex, out var eliminated))
            {
                if (eliminated.go != null) Destroy(eliminated.go);
                timers.Remove(eliminatedPlayerIndex);
            }

            // Aplicar bonus a jugadores restantes
            foreach (var kv in timers)
            {
                var t = kv.Value;
                t.maxSeconds += bonusOnEliminationSeconds;
                t.remainingSeconds += bonusOnEliminationSeconds;
                if (t.slider != null)
                {
                    t.slider.maxValue = t.maxSeconds;
                    t.slider.value = t.remainingSeconds;
                }
            }

            if (GameRoundManager.instance != null)
            {
                GameRoundManager.instance.PlayerLose(eliminatedPlayerIndex);
            }
        }

        // Restaurar interacción/pausa antes de notificar fin
        EndHoldState();

        // Notificar fin para que inicie el siguiente minijuego
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
        // Reset visuals for hourglass during hold
        ResetSandClockVisuals();
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

        // Sonido al mostrar la pantalla de eliminación (Tango)
        if (!string.IsNullOrWhiteSpace(eliminationSfxKey) && SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(eliminationSfxKey);
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

        // Mostrar icono triste en el HUD según el índice del jugador eliminado durante el hold de '¡¡Tango!!'
        TryShowSadIconForPlayer(playerIndex, Mathf.Max(0.1f, finishHoldSeconds));
    }

    // Intenta invocar IconManager.MostrarIconoTristeTemporal(int, float) sin modificar IconManager
    private void TryShowSadIconForPlayer(int playerIndex, float duration)
    {
        if (playerIndex < 0) return;
        IconManager mgr = iconManager;
        if (mgr == null)
        {
            // Buscar instancia en escena; usa la API disponible según versión de Unity
            #if UNITY_2023_1_OR_NEWER
            mgr = Object.FindFirstObjectByType<IconManager>();
            #else
            mgr = (IconManager)Object.FindObjectOfType(typeof(IconManager));
            #endif
        }
        if (mgr == null) return;

        // Usar reflexión para invocar el método privado IEnumerator MostrarIconoTristeTemporal(int, float)
        MethodInfo mi = typeof(IconManager).GetMethod(
            "MostrarIconoTristeTemporal",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        if (mi == null) return;
        try
        {
            var enumerator = mi.Invoke(mgr, new object[] { playerIndex, duration }) as IEnumerator;
            if (enumerator != null)
            {
                // Ejecutar la corrutina en el propio IconManager para mantener su ciclo de vida
                mgr.StartCoroutine(enumerator);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Tempo: No se pudo invocar MostrarIconoTristeTemporal en IconManager. {ex.Message}");
        }
    }

    // ===== Sonidos: tic del cronómetro =====
    private void StartTickLoop()
    {
        if (string.IsNullOrWhiteSpace(timerTickSfxKey)) return;
        // Evitar múltiples invocaciones
        CancelInvoke(nameof(PlayTimerTickSfx));
        float interval = Mathf.Max(0.05f, timerTickInterval);
        // Ejecutar un primer tic inmediato para feedback, luego repetir
        PlayTimerTickSfx();
        InvokeRepeating(nameof(PlayTimerTickSfx), interval, interval);
    }

    private void StopTickLoop()
    {
        CancelInvoke(nameof(PlayTimerTickSfx));
    }

    private void PlayTimerTickSfx()
    {
        var sm = SoundManager.instance;
        if (sm == null) return;
        if (string.IsNullOrWhiteSpace(timerTickSfxKey)) return;
        sm.PlaySfx(timerTickSfxKey, Mathf.Clamp01(timerTickVolume));
    }

    // ========= Timers por jugador =========
    private void SetupTimersIfNeeded()
    {
        if (timersInitialized) return;
        timersInitialized = true;

        float initialSeconds = GetInitialSecondsByDifficulty();
        int[] indices = ResolveInitialOrderIndices();
        if (indices == null || indices.Length == 0)
        {
            indices = new int[] { 0, 1, 2, 3 };
        }

        foreach (var idx in indices)
        {
            if (idx < 0) continue;
            if (!timers.ContainsKey(idx))
            {
                timers[idx] = CreatePlayerTimer(idx, initialSeconds);
            }
        }

        // Al inicializar, si ya hay jugador actual, ajustar visibilidad
        int currentPlayer = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (currentPlayer >= 0)
        {
            UpdateSlidersVisibility(currentPlayer);
        }
    }

    private float GetInitialSecondsByDifficulty()
    {
        int players = Dificultad.GetActivePlayersCount();
        if (players >= 4) return 15f;
        if (players == 3 || players == 2) return 20f;
        return 20f;
    }

    private PlayerTimer CreatePlayerTimer(int playerIndex, float seconds)
    {
        GameObject prefab = ResolveSliderPrefab(playerIndex);
        GameObject go = null;
        Slider slider = null;
        if (prefab != null && slidersParent != null)
        {
            go = Instantiate(prefab, slidersParent);
            slider = go.GetComponentInChildren<Slider>(true);
        }
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = seconds;
            slider.value = seconds;
        }
        return new PlayerTimer
        {
            playerIndex = playerIndex,
            remainingSeconds = seconds,
            maxSeconds = seconds,
            initialMaxSeconds = seconds,
            slider = slider,
            go = go
        };
    }

    private GameObject ResolveSliderPrefab(int playerIndex)
    {
        if (sliderPrefabsByPlayer != null && playerIndex >= 0 && playerIndex < sliderPrefabsByPlayer.Length)
        {
            var p = sliderPrefabsByPlayer[playerIndex];
            if (p != null) return p;
        }
        return defaultSliderPrefab;
    }

    private int[] ResolveInitialOrderIndices()
    {
        var tm = TurnManager.instance;
        if (tm != null)
        {
            var methods = new string[] { "GetPlayerOrderIndices", "GetPlayersOrder", "GetActivePlayerIndices", "GetInitialOrder" };
            foreach (var mName in methods)
            {
                var m = tm.GetType().GetMethod(mName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (m != null)
                {
                    var result = m.Invoke(tm, null);
                    if (result is int[] arr && arr.Length > 0) return arr;
                    if (result is System.Collections.Generic.List<int> list && list.Count > 0) return list.ToArray();
                }
            }
        }
        return null;
    }

    private void UpdateSlidersVisibility(int activePlayerIndex)
    {
        foreach (var kv in timers)
        {
            var t = kv.Value;
            if (t.go != null)
            {
                t.go.SetActive(t.playerIndex == activePlayerIndex);
            }
        }
    }

    // ===== Ventana de cuarto final y bonus por aciertos =====
    public bool IsInQuarterWindow(int playerIndex)
    {
        SetupTimersIfNeeded();
        if (!timers.TryGetValue(playerIndex, out var t)) return false;
        float threshold = t.initialMaxSeconds * 0.25f;
        return t.remainingSeconds > 0f && t.remainingSeconds <= threshold;
    }

    // Conveniencia: aplicar bonus al jugador actual solo si está en la ventana del 1/4
    public void TryBonusOnSuccess(float seconds, string source = null)
    {
        if (seconds <= 0f) return;
        int currentPlayer = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (currentPlayer < 0) return;
        if (IsInQuarterWindow(currentPlayer))
        {
            // Agregar solo al tiempo restante del slider actual, sin aumentar el máximo
            AddRemainingOnlyForPlayer(currentPlayer, seconds);
            if (timers.TryGetValue(currentPlayer, out var t))
            {
                Debug.Log($"[Tempo] Bonus +{seconds:0.###}s aplicado al Jugador {currentPlayer + 1} desde {source ?? "minijuego"}. Restante: {t.remainingSeconds:0.00}s / Max: {t.maxSeconds:0.00}s (umbral 1/4={t.initialMaxSeconds * 0.25f:0.00}s)");
            }
        }
    }

    // Sumar segundos solamente al tiempo restante del jugador, sin modificar el máximo del slider
    public void AddRemainingOnlyForPlayer(int playerIndex, float seconds)
    {
        if (playerIndex < 0 || seconds <= 0f) return;
        SetupTimersIfNeeded();
        if (!timers.TryGetValue(playerIndex, out var t))
        {
            // Si no existe aún, créalo con los segundos iniciales
            float init = GetInitialSecondsByDifficulty();
            t = CreatePlayerTimer(playerIndex, init);
            timers[playerIndex] = t;
        }

        t.remainingSeconds = Mathf.Min(t.maxSeconds, t.remainingSeconds + seconds);
        if (t.slider != null)
        {
            t.slider.value = t.remainingSeconds;
        }

        int currentPlayer = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (currentPlayer == playerIndex)
        {
            // Sin tocar el límite; solo sincronizamos el restante
            remaining = t.remainingSeconds;
        }
    }

    private void ResetSandClockVisuals()
    {
        if (sandClock != null)
        {
            sandClock.localScale = sandClockOriginalScale;
            sandClock.localRotation = sandClockOriginalRotation;
            sandClockBouncePhase = 0f;
        }
    }

    // ===== API pública: otorgar bonus a un jugador específico =====
    public void AddBonusForPlayer(int playerIndex, float seconds)
    {
        if (playerIndex < 0 || seconds <= 0f) return;
        SetupTimersIfNeeded();
        if (!timers.TryGetValue(playerIndex, out var t))
        {
            // Si el jugador no tiene timer aún, créalo con los segundos iniciales + bonus
            float init = GetInitialSecondsByDifficulty();
            t = CreatePlayerTimer(playerIndex, init + seconds);
            timers[playerIndex] = t;
        }
        else
        {
            t.maxSeconds += seconds;
            t.remainingSeconds += seconds;
            if (t.slider != null)
            {
                t.slider.maxValue = t.maxSeconds;
                t.slider.value = t.remainingSeconds;
            }
        }

        // Si el jugador bonificado es el que está corriendo, sincronizar variables de compatibilidad
        int currentPlayer = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (currentPlayer == playerIndex)
        {
            limit = t.maxSeconds;
            remaining = t.remainingSeconds;
            nextLogMilestone = Mathf.FloorToInt((remaining - 0.0001f) / 10f) * 10;
            if (nextLogMilestone < 10) nextLogMilestone = -1;
        }
    }

    [Header("Urgente: parpadeo de color (< umbral)")]
    [SerializeField, Tooltip("Habilitar parpadeo de color del texto '¡¡Tingo!!' cuando queden pocos segundos.")]
    private bool enableUrgentBlink = true;
    [SerializeField, Tooltip("Umbral en segundos para iniciar el parpadeo.")]
    private float urgentThresholdSeconds = 5f;
    [SerializeField, Tooltip("Color de parpadeo (por defecto #EC4949)")]
    private Color urgentBlinkColor = new Color(0.925f, 0.286f, 0.286f); // #EC4949
    [SerializeField, Tooltip("Velocidad del parpadeo (ciclos por segundo)")]
    private float urgentBlinkSpeed = 4f;
    private float urgentBlinkPhase = 0f;

    private void UpdateUrgentBlinkColor()
    {
        if (!enableUrgentBlink || timerText == null) return;
        if (remaining > 0f && remaining <= urgentThresholdSeconds)
        {
            urgentBlinkPhase += Time.deltaTime * Mathf.Max(0f, urgentBlinkSpeed) * Mathf.PI * 2f;
            float t = (Mathf.Sin(urgentBlinkPhase) + 1f) * 0.5f; // 0..1
            timerText.color = Color.Lerp(timerTextOriginalColor, urgentBlinkColor, t);
        }
        else
        {
            // fuera de umbral o ya en Tango: asegurar color original
            ResetTextColor();
        }
    }

    private void ResetTextColor()
    {
        if (timerText != null)
            timerText.color = timerTextOriginalColor;
        urgentBlinkPhase = 0f;
    }

    // ===== API de congelamiento externo =====
    public void PushExternalFreeze(string reason = null)
    {
        externalFreezeCount = Mathf.Max(0, externalFreezeCount) + 1;
        if (!string.IsNullOrEmpty(reason))
            Debug.Log($"[Tempo] Freeze++ ({externalFreezeCount}) por: {reason}");
    }

    public void PopExternalFreeze()
    {
        if (externalFreezeCount > 0)
        {
            externalFreezeCount--;
            Debug.Log($"[Tempo] Freeze-- ({externalFreezeCount})");
        }
        else
        {
            Debug.LogWarning("[Tempo] PopExternalFreeze sin freeze activo.");
        }
    }

    public void ClearExternalFreeze()
    {
        if (externalFreezeCount != 0)
        {
            externalFreezeCount = 0;
            Debug.Log("[Tempo] Freeze limpiado (ClearExternalFreeze)");
        }
    }
}