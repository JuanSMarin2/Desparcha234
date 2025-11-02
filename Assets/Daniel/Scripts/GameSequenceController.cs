using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

// Controlador sencillo: toma una lista de minijuegos, los baraja (opcional) y los ejecuta uno a uno
[DisallowMultipleComponent]
public class GameSequenceController : MonoBehaviour
{
    [Header("Minijuegos a jugar")]
    [Tooltip("Arrastra aquí los componentes de tus minijuegos (JOrden, BotonReducible, CircleGameManagerUI, AccelerometerGame, etc.)")]
    public List<MonoBehaviour> minigames = new();

    [Header("Ejecución")]
    public bool randomizeOrder = true;
    public bool autoStartOnAwake = false;

    [Header("Turn Timer (Tempo)")]
    [Tooltip("Referencia al componente Tempo que controla el cronómetro de cada turno.")]
    public Tempo turnTimer;

    [Header("Eventos")]
    public UnityEvent onAllMinigamesFinished;

    // Estado interno
    private UnityEvent _currentFinishEvent;
    private MonoBehaviour _current;
    private int _lastPlayedIndex = -1; // para no repetir consecutivo
    private int _lastPlayerIndex = -2; // rastrear cambios de jugador para logs
    private MinigameInstructionUI _instructionUI; // cache perezoso
    private bool _hasStartedSequence = false; // evita dobles inicios
    private bool _firstPickDone = false;       // para forzar el primer minijuego

    void Awake()
    {
        if (autoStartOnAwake)
        {
            PlaySequence();
        }

        // Suscribirse al temporizador de turnos (si existe)
        if (turnTimer != null)
        {
            // Evitar múltiples suscripciones
            turnTimer.onGameFinished.RemoveListener(OnTurnTimerFinished);
            turnTimer.onGameFinished.AddListener(OnTurnTimerFinished);
        }
    }

    [ContextMenu("Play Sequence")]
    public void PlaySequence()
    {
        if (_hasStartedSequence)
        {
            Debug.Log("[Sequence] PlaySequence ignorado: la secuencia ya fue iniciada.");
            return;
        }
        _hasStartedSequence = true;
        // Registrar jugador actual al inicio para logs
        if (TurnManager.instance != null)
        {
            _lastPlayerIndex = TurnManager.instance.GetCurrentPlayerIndex();
            if (_lastPlayerIndex >= 0)
                Debug.Log($"[Sequence] Inicia turno del Jugador {_lastPlayerIndex + 1}");
        }
        // El cronómetro se inicia desde PreGameOrderPanel al pulsar Iniciar
        PlayNext();
    }

    private int PickRandomIndex()
    {
        if (minigames == null || minigames.Count == 0) return -1;
        // Forzar que el primer minijuego sea JOrden, BotonReducible o JProyectikes
        if (!_firstPickDone)
        {
            _firstPickDone = true;
            for (int i = 0; i < minigames.Count; i++)
            {
                var m = minigames[i];
                if (m == null) continue;
                string n = m.GetType().Name;
                if (n == nameof(JOrden) || n == nameof(BotonReducible) || n == nameof(JProyectikes))
                {
                    return i;
                }
            }
            // Si no se encontró ninguno, cae a la selección normal
        }
        if (!randomizeOrder)
        {
            // modo determinista: avanzar circular evitando repetir si es posible
            int idx = (_lastPlayedIndex + 1) % minigames.Count;
            if (minigames.Count > 1 && idx == _lastPlayedIndex)
                idx = (idx + 1) % minigames.Count;
            return idx;
        }

        if (minigames.Count == 1) return 0;

        int tries = 5;
        int idxRand = Random.Range(0, minigames.Count);
        while (idxRand == _lastPlayedIndex && tries-- > 0)
        {
            idxRand = Random.Range(0, minigames.Count);
        }
        return idxRand;
    }

    private void PlayNext()
    {
        UnsubscribeCurrent();

        int idx = PickRandomIndex();
        if (idx < 0)
        {
            onAllMinigamesFinished?.Invoke();
            return;
        }

        var comp = minigames[idx];
        _lastPlayedIndex = idx;
        _current = comp;
        var t = comp.GetType();

        // No tocar el cronómetro aquí: se controla al inicio y cuando llega a 0

        // Mostrar instrucciones para el minijuego actual (si hay UI presente)
    TryShowInstructionsFor(comp);
    // Si aún no hay índice válido, intentar refrescar el icono en breve
    TryScheduleIconRefresh();
    // Incluso si hay índice, hacer un refresco corto por si el binding de sprites tarda un frame
    TryScheduleLateIconRefresh();

        // 1) Escuchar onGameFinished si existe
        var finishField = t.GetField("onGameFinished", BindingFlags.Public | BindingFlags.Instance);
        if (finishField != null && typeof(UnityEvent).IsAssignableFrom(finishField.FieldType))
        {
            _currentFinishEvent = (UnityEvent)finishField.GetValue(comp);
            _currentFinishEvent?.AddListener(OnMinigameFinished);
        }
        else
        {
            _currentFinishEvent = null; // si no existe, avanzamos de inmediato tras intentar iniciar
        }

        // 2) Intentar iniciar por PlayMiniGamen(); si no, usar Play() como fallback
        var playMethod = t.GetMethod("PlayMiniGamen", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                         ?? t.GetMethod("Play", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (playMethod != null)
        {
            playMethod.Invoke(comp, null);
        }
        else
        {
            Debug.LogWarning($"[GameSequenceController] '{t.Name}' no tiene PlayMiniGamen() ni Play(). Se omitirá.");
        }

        // Si no tenemos evento para detectar el final, avanzar inmediatamente
        if (_currentFinishEvent == null)
        {
            OnMinigameFinished();
        }
    }

    private void OnMinigameFinished()
    {
        // No detener ni reiniciar el cronómetro en final normal de minijuego
        HideInstructions();
        TryNextTurnFor(_current);
        UnsubscribeCurrent();
        PlayNext();
    }

    // Handler cuando el cronómetro (Tempo) termina y elimina al jugador actual
    private void OnTurnTimerFinished()
    {
        // Intentar detener el minijuego actual inmediatamente
        if (_current != null)
        {
            var t = _current.GetType();
            var stopNames = new string[] { "StopGame", "Stop", "Abort", "Cancel", "ClearAllCircles" };
            bool stopped = false;
            foreach (var name in stopNames)
            {
                var m = t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (m != null)
                {
                    m.Invoke(_current, null);
                    stopped = true;
                    break;
                }
            }
            if (!stopped)
            {
                // Como fallback, desactivar el componente para detener Update/inputs
                _current.enabled = false;
            }
        }

        // Registrar cambio de jugador sin NextTurn (eliminación ajustó el puntero)
        if (TurnManager.instance != null)
        {
            int now = TurnManager.instance.GetCurrentPlayerIndex();
            if (now != _lastPlayerIndex && now >= 0)
            {
                Debug.Log($"[Turn] Cambio de jugador por timeout: ahora juega el Jugador {now + 1}");
                _lastPlayerIndex = now;
            }
        }

        // Ocultar instrucciones del minijuego que terminó por timeout
        HideInstructions();

        // Avanzar inmediatamente al siguiente minijuego y reiniciar cronómetro para el nuevo turno
        if (turnTimer != null)
        {
            turnTimer.StopTimer();
            turnTimer.StartTimer();
        }
        UnsubscribeCurrent();
        PlayNext();
    }

    private void UnsubscribeCurrent()
    {
        if (_currentFinishEvent != null)
        {
            _currentFinishEvent.RemoveListener(OnMinigameFinished);
            _currentFinishEvent = null;
        }
    }

    // Intenta llamar a NextTurn() si corresponde y no está marcado para saltarse
    private void TryNextTurnFor(MonoBehaviour comp)
    {
        if (comp == null) return;
        var t = comp.GetType();
        // Si el minijuego expone skipNextTurnOnFinish = true, no avanzar turno
        var skipField = t.GetField("skipNextTurnOnFinish", BindingFlags.Public | BindingFlags.Instance);
        bool skip = false;
        if (skipField != null && skipField.FieldType == typeof(bool))
        {
            skip = (bool)skipField.GetValue(comp);
        }

        if (!skip && TurnManager.instance != null)
        {
            int prev = TurnManager.instance.GetCurrentPlayerIndex();
            TurnManager.instance.NextTurn();
            int now = TurnManager.instance.GetCurrentPlayerIndex();
            if (prev != now && now >= 0)
            {
                Debug.Log($"[Turn] Cambio de jugador: Jugador {prev + 1} -> Jugador {now + 1}");
                _lastPlayerIndex = now;
            }
        }
        else if (TurnManager.instance != null)
        {
            // En minijuegos que eliminan al jugador actual (p.ej., Tempo), el puntero ya apunta al siguiente
            int now = TurnManager.instance.GetCurrentPlayerIndex();
            if (now != _lastPlayerIndex && now >= 0)
            {
                Debug.Log($"[Turn] Cambio de jugador (sin NextTurn): ahora juega el Jugador {now + 1}");
                _lastPlayerIndex = now;
            }
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ================= Instrucciones centralizadas =================
    private void TryShowInstructionsFor(MonoBehaviour comp)
    {
        if (comp == null) return;
        var ui = GetInstructionUI();
        if (ui == null) return;

        // Obtener índice si está disponible; si no, usar -1 para que al menos se actualice el texto
        int idx = -1;
        if (TurnManager.instance != null)
        {
            idx = TurnManager.instance.GetCurrentPlayerIndex();
        }

        if (TryResolveKind(comp, out var kind))
        {
            ui.Show(kind, idx);
        }
    }

    private void HideInstructions()
    {
        var ui = GetInstructionUI();
        if (ui != null) ui.Hide();
    }

    private MinigameInstructionUI GetInstructionUI()
    {
        if (_instructionUI == null)
        {
            _instructionUI = FindFirstObjectByType<MinigameInstructionUI>();
        }
        return _instructionUI;
    }

    private bool TryResolveKind(MonoBehaviour comp, out MinigameKind kind)
    {
        // Mapear tipos concretos a MinigameKind. Ampliar según nuevos minijuegos.
        var type = comp.GetType();
        string name = type.Name;
        // Comparar por nombre para evitar dependencias de namespaces
        if (name == nameof(JProyectikes)) { kind = MinigameKind.Proyectiles; return true; }
        if (name == nameof(JOrden)) { kind = MinigameKind.Orden; return true; }
        if (name == nameof(CircleGameManagerUI) || name.Contains("Circulo")) { kind = MinigameKind.Circulos; return true; }
        if (name == nameof(BotonReducible) || name.Contains("Reduce")) { kind = MinigameKind.Reducir; return true; }
        if (name == nameof(RecordarInsame) || name.Contains("Recordar") || name.Contains("Ritmo")) { kind = MinigameKind.Ritmo; return true; }

        kind = default;
        return false;
    }

    // Si el índice no está listo al mostrar las instrucciones, refrescar icono cuando lo esté
    private void TryScheduleIconRefresh()
    {
        if (TurnManager.instance == null) return;
        if (TurnManager.instance.GetCurrentPlayerIndex() >= 0) return; // ya es válido
        StopCoroutineSafe(nameof(RefreshIconWhenReady));
        StartCoroutine(RefreshIconWhenReady());
    }

    private IEnumerator RefreshIconWhenReady()
    {
        float waited = 0f;
        const float maxWait = 1.0f;
        while (TurnManager.instance != null && TurnManager.instance.GetCurrentPlayerIndex() < 0 && waited < maxWait)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        var ui = GetInstructionUI();
        if (ui != null && TurnManager.instance != null)
        {
            int idx = TurnManager.instance.GetCurrentPlayerIndex();
            if (idx >= 0)
            {
                ui.UpdatePlayerIcon(idx);
            }
        }
    }

    private void StopCoroutineSafe(string routineName)
    {
        try { StopCoroutine(routineName); } catch { /* ignore */ }
    }

    private void TryScheduleLateIconRefresh()
    {
        StopCoroutineSafe(nameof(LateIconRefresh));
        StartCoroutine(LateIconRefresh());
    }

    private IEnumerator LateIconRefresh()
    {
        // Espera un frame en tiempo de juego para asegurar que los bindings de UI estén listos
        yield return null;
        if (TurnManager.instance != null)
        {
            int idx = TurnManager.instance.GetCurrentPlayerIndex();
            if (idx >= 0)
            {
                var ui = GetInstructionUI();
                if (ui != null)
                {
                    ui.UpdatePlayerIcon(idx);
                }
            }
        }
    }
}
