using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

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

    [Header("Panel 'Siguiente jugador'")]
    [Tooltip("Panel raíz (GameObject único) que contiene icono y texto del siguiente turno.")]
    [SerializeField] private GameObject nextTurnPanel;
    [Tooltip("(Opcional) Imagen del ícono del jugador. Si no se asigna, se buscará automáticamente en hijos.")]
    [SerializeField] private Image nextTurnPlayerIconImage;
    [Header("Fuentes de icono por índice (por Image, no por Sprite)")]
    [Tooltip("Imágenes en la escena (0-based) que ya tienen asignado el sprite del jugador. Se copiará su sprite y opcionalmente su color.")]
    [SerializeField] private Image[] playerSpriteSourcesByIndex;
    [Tooltip("Copiar también el color de la imagen fuente cuando esté disponible.")]
    [SerializeField] private bool copySourceColor = true;
    [Tooltip("(Opcional) TMP_Text para la cuenta regresiva. Si no se asigna, se buscará en hijos.")]
    [SerializeField] private TMP_Text nextTurnCountdownText;
    [Tooltip("(Opcional) TMP_Text para el encabezado: 'Siguiente jugador {n}'. Si no se asigna, se buscará en hijos.")]
    [SerializeField] private TMP_Text nextTurnHeaderText;
    [Tooltip("(Opcional) TMP_Text para el mensaje de pasar el celular. Si no se asigna, se buscará en hijos.")]
    [SerializeField] private TMP_Text passDeviceText;
    [Tooltip("Duración en segundos de la cuenta regresiva antes de iniciar el siguiente minijuego")]
    [SerializeField] private float nextTurnCountdownSeconds = 2f;
    [Tooltip("Cuenta regresiva cuando solo hay 2 jugadores.")]
    [SerializeField] private float twoPlayersCountdownSeconds = 1f;
    [Tooltip("Pausar el juego (Time.timeScale=0) mientras el panel de siguiente jugador está activo")]
    [SerializeField] private bool pauseWithTimeScale = true;
    private Coroutine _nextTurnRoutine;
    private float _prevTimeScale = 1f;
    // Control de freeze del Tempo durante el panel de siguiente jugador
    private int _nextTurnFreezeDepth = 0;

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
        int idxRand = UnityEngine.Random.Range(0, minigames.Count);
        while (idxRand == _lastPlayedIndex && tries-- > 0)
        {
            idxRand = UnityEngine.Random.Range(0, minigames.Count);
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

        // Mostrar panel de siguiente jugador y, al terminar, continuar con el próximo minijuego
        ShowNextTurnPanelThen(() =>
        {
            PlayNext();
        });
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

        // Pausar/reiniciar el cronómetro en transición por timeout
        if (turnTimer != null)
        {
            turnTimer.StopTimer();
        }

        UnsubscribeCurrent();

        // Mostrar panel de siguiente jugador y, al finalizar, reiniciar cronómetro y avanzar
        ShowNextTurnPanelThen(() =>
        {
            if (turnTimer != null)
            {
                turnTimer.StartTimer();
            }
            PlayNext();
        });
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
            int j = UnityEngine.Random.Range(0, i + 1);
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

    // ================= Panel de siguiente jugador =================
    private void ShowNextTurnPanelThen(Action onCompleted)
    {
        if (nextTurnPanel == null)
        {
            // Si no está configurado el panel, continuar de inmediato
            onCompleted?.Invoke();
            return;
        }

        // Resolver referencias perezosamente si no fueron asignadas
        AutoResolveNextTurnChildren();

        if (_nextTurnRoutine != null)
        {
            // Asegurar que cualquier freeze anterior se libere antes de reiniciar la rutina
            PopAllNextTurnFreezes();
            StopCoroutine(_nextTurnRoutine);
            _nextTurnRoutine = null;
        }
        int idx = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        float seconds = ResolveNextTurnSeconds();
        _nextTurnRoutine = StartCoroutine(Co_ShowNextTurnPanel(idx, seconds, onCompleted));
    }

    // Determina la duración de la cuenta regresiva, usando 1s cuando solo hay 2 jugadores.
    private float ResolveNextTurnSeconds()
    {
        int count = GetPlayerCountSafe();
        if (count == 2) return Mathf.Max(0f, twoPlayersCountdownSeconds);
        return Mathf.Max(0f, nextTurnCountdownSeconds);
    }

    // Intenta obtener la cantidad de jugadores desde TurnManager sin acoplarse a su API concreta.
    private int GetPlayerCountSafe()
    {
        if (TurnManager.instance == null) return -1;
        var tm = TurnManager.instance;
        var t = tm.GetType();

        // 1) Métodos comunes que devuelvan int
        string[] methodNames = { "GetAlivePlayerCount", "GetPlayerCount", "GetTotalPlayers", "GetPlayersCount" };
        foreach (var name in methodNames)
        {
            var m = t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (m != null && m.GetParameters().Length == 0 && m.ReturnType == typeof(int))
            {
                try { return (int)m.Invoke(tm, null); } catch { }
            }
        }

        // 2) Propiedades tipo int
        string[] propNames = { "AlivePlayerCount", "PlayerCount", "TotalPlayers" };
        foreach (var name in propNames)
        {
            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.CanRead && p.PropertyType == typeof(int))
            {
                try { return (int)p.GetValue(tm); } catch { }
            }
        }

        // 3) Campos/colecciones comunes
        string[] fieldNames = { "players", "playerList", "activePlayers", "Players" };
        foreach (var name in fieldNames)
        {
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) continue;
            try
            {
                var val = f.GetValue(tm);
                if (val == null) continue;
                if (val is Array arr) return arr.Length;
                if (val is System.Collections.ICollection col) return col.Count;
            }
            catch { }
        }

        // 4) Fallback desconocido
        return -1;
    }

    private IEnumerator Co_ShowNextTurnPanel(int playerIndex, float seconds, Action onCompleted)
    {
        // Preparar imagen del jugador si se asignó
        EnsurePlayerSourcesFromPreGamePanel();
        ApplyNextTurnIconFromSources(playerIndex);

        // Preparar textos
        ApplyNextTurnTexts(playerIndex);

        // Activar panel y bloquear interacción
        nextTurnPanel.SetActive(true);

        // Congelar el Tempo durante la espera, independientemente de timeScale
        PushNextTurnFreeze();

        // Pausar juego con timeScale (y así detener el cronómetro) si está habilitado
        if (pauseWithTimeScale)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

    float remaining = Mathf.Max(0f, seconds);
    UpdateNextTurnCountdownText(Mathf.CeilToInt(remaining));

        // Usar tiempo real para no depender de Time.timeScale
        float start = Time.realtimeSinceStartup;
        float end = start + remaining;
        while (Time.realtimeSinceStartup < end)
        {
            float left = Mathf.Max(0f, end - Time.realtimeSinceStartup);
            UpdateNextTurnCountdownText(Mathf.CeilToInt(left));
            yield return null;
        }

        // Ocultar panel
        nextTurnPanel.SetActive(false);

        // Restaurar timeScale
        if (pauseWithTimeScale)
        {
            Time.timeScale = _prevTimeScale;
        }
        // Liberar el freeze aplicado por el panel
        PopNextTurnFreeze();
        _nextTurnRoutine = null;
        onCompleted?.Invoke();
    }

    private void UpdateNextTurnCountdownText(int secondsLeft)
    {
        if (nextTurnCountdownText != null)
        {
            nextTurnCountdownText.text = secondsLeft.ToString();
        }
    }

    private void ApplyNextTurnIconFromSources(int playerIndex)
    {
        if (nextTurnPlayerIconImage == null)
            return;

        // Reset defaults
        nextTurnPlayerIconImage.enabled = false;
        nextTurnPlayerIconImage.sprite = null;
        nextTurnPlayerIconImage.color = Color.white;

        if (playerIndex >= 0)
        {
            // Usar únicamente fuentes de Image (no Sprite[])
            if (playerSpriteSourcesByIndex != null && playerIndex < playerSpriteSourcesByIndex.Length)
            {
                var src = playerSpriteSourcesByIndex[playerIndex];
                if (src != null && src.sprite != null)
                {
                    nextTurnPlayerIconImage.enabled = true;
                    nextTurnPlayerIconImage.sprite = src.sprite;
                    if (copySourceColor) nextTurnPlayerIconImage.color = src.color;
                    return;
                }
            }
            // Si no hay fuente válida, mantener desactivada la imagen y registrar aviso
            Debug.LogWarning($"[NextTurnPanel] No se encontró Image fuente para el jugador {playerIndex}.");
        }
    }

    private void AutoResolveNextTurnChildren()
    {
        if (nextTurnPanel == null) return;
        // Buscar una Image para icono si falta
        if (nextTurnPlayerIconImage == null)
        {
            nextTurnPlayerIconImage = nextTurnPanel.GetComponentInChildren<Image>(true);
        }
        // Buscar un TMP_Text para countdown si falta
        if (nextTurnCountdownText == null)
        {
            nextTurnCountdownText = nextTurnPanel.GetComponentInChildren<TMP_Text>(true);
        }
        // Resolver header y mensaje de pasar dispositivo si faltan (busca por nombre parcial)
        if (nextTurnHeaderText == null || passDeviceText == null)
        {
            var texts = nextTurnPanel.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                if (t == null) continue;
                var n = t.gameObject.name.ToLowerInvariant();
                if (nextTurnHeaderText == null && (n.Contains("header") || n.Contains("titulo") || n.Contains("title") || n.Contains("siguiente")))
                {
                    nextTurnHeaderText = t;
                    continue;
                }
                if (passDeviceText == null && (n.Contains("pass") || n.Contains("celular") || n.Contains("telefono") || n.Contains("device") || n.Contains("rapido")))
                {
                    passDeviceText = t;
                    continue;
                }
            }
        }
    }

    private void EnsurePlayerSourcesFromPreGamePanel()
    {
        // Si ya tenemos fuentes válidas, no hacer nada
        if (playerSpriteSourcesByIndex != null && playerSpriteSourcesByIndex.Length > 0)
        {
            bool any = false;
            for (int i = 0; i < playerSpriteSourcesByIndex.Length; i++)
            {
                if (playerSpriteSourcesByIndex[i] != null)
                {
                    any = true; break;
                }
            }
            if (any) return;
        }

        // Intentar localizar PreGameOrderPanel incluso si está inactivo
        PreGameOrderPanel srcPanel = null;
        try
        {
            srcPanel = FindFirstObjectByType<PreGameOrderPanel>();
        }
        catch { /* Unity versions older than 2023 may throw; ignore */ }

        if (srcPanel == null)
        {
            var all = Resources.FindObjectsOfTypeAll(typeof(PreGameOrderPanel));
            if (all != null && all.Length > 0)
            {
                srcPanel = all[0] as PreGameOrderPanel;
            }
        }

        if (srcPanel == null) return;

        // Usar reflexión para leer su arreglo privado playerSpriteSourcesByIndex
        var f = typeof(PreGameOrderPanel).GetField("playerSpriteSourcesByIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null)
        {
            var val = f.GetValue(srcPanel) as Image[];
            if (val != null && val.Length > 0)
            {
                playerSpriteSourcesByIndex = val;
            }
        }

        // Copiar también el flag copySourceColor si existe
        var fColor = typeof(PreGameOrderPanel).GetField("copySourceColor", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fColor != null)
        {
            try
            {
                var v = (bool)fColor.GetValue(srcPanel);
                copySourceColor = v;
            }
            catch { }
        }
    }

    private void ApplyNextTurnTexts(int playerIndex)
    {
        // 'Siguiente jugador {n}' donde n es 1-based
        if (nextTurnHeaderText != null)
        {
            int n = (playerIndex >= 0 ? playerIndex + 1 : 0);
            nextTurnHeaderText.text = (n > 0) ? $"Siguiente jugador {n}" : "Siguiente jugador";
        }
        // Mensaje fijo: 'Pasa el celular rapido'
        if (passDeviceText != null)
        {
            passDeviceText.text = "Pasa el celular rapido";
        }
    }

    // ================ Freeze helpers (Tempo) ================
    private void PushNextTurnFreeze()
    {
        if (Tempo.instance != null)
        {
            Tempo.instance.PushExternalFreeze("NextTurnPanel");
            _nextTurnFreezeDepth++;
        }
    }

    private void PopNextTurnFreeze()
    {
        if (_nextTurnFreezeDepth > 0 && Tempo.instance != null)
        {
            Tempo.instance.PopExternalFreeze();
            _nextTurnFreezeDepth--;
        }
    }

    private void PopAllNextTurnFreezes()
    {
        while (_nextTurnFreezeDepth > 0)
        {
            PopNextTurnFreeze();
        }
    }
}
