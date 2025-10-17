using System.Collections.Generic;
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
        // Registrar jugador actual al inicio para logs
        if (TurnManager.instance != null)
        {
            _lastPlayerIndex = TurnManager.instance.GetCurrentPlayerIndex();
            if (_lastPlayerIndex >= 0)
                Debug.Log($"[Sequence] Inicia turno del Jugador {_lastPlayerIndex + 1}");
        }
        // Iniciar cronómetro solo al inicio de la secuencia
        if (turnTimer != null)
        {
            turnTimer.StopTimer();
            turnTimer.StartTimer();
        }
        PlayNext();
    }

    private int PickRandomIndex()
    {
        if (minigames == null || minigames.Count == 0) return -1;
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
}
