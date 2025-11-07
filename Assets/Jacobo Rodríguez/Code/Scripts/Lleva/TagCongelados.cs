using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

// Modalidad "Congelados":
// - Un jugador es el "freezer" de la ronda (rota entre rondas).
// - El freezer congela a otros al tocarlos; los no congelados pueden descongelar a los congelados.
// - El freezer gana si congela a todos los demás antes de que termine el tiempo.
// - Si el tiempo se agota, ganan los no congelados.
// - Reutiliza Movimiento para moverse y PlayerCongelados para lógica de congelar/descongelar.
[DisallowMultipleComponent]
public class TagCongelados : MonoBehaviour
{
    public static TagCongelados Instance { get; private set; }

    // Evento propio de inicio de ronda para que otros componentes puedan escuchar si lo requieren
    public static System.Action OnRoundStarted;

    // Nuevo: Evento de fin de ronda (para UI)
    public enum RoundEndType { FreezerWin, NonFreezersWin, FinalVictory }
    public struct RoundEndData
    {
        public RoundEndType type;
        public int freezerIndex1Based; // válido si type=FreezerWin (quién fue el freezer ganador)
        public List<int> winners1Based; // ganadores 1..4 (no-freezers sobrevivientes o solo el freezer)
    }
    public static System.Action<RoundEndData> OnRoundEnded;

    [Header("Popup Inicio de Ronda")]
    [SerializeField] private TagRoundStartPopup roundStartPopup; // opcional
    [SerializeField, Tooltip("Colores 1..4 para mostrar en popup")] private Color[] startColors = new Color[]{ new Color(0.85f,0.25f,0.25f), new Color(0.25f,0.45f,0.9f), new Color(0.95f,0.85f,0.2f), new Color(0.3f,0.85f,0.4f)};
    [SerializeField, Tooltip("Nombres 1..4 de los jugadores para popup")] private string[] startColorNames = new string[]{ "rojo", "azul", "amarillo", "verde" };

    [Header("Duración Ronda")]
    [SerializeField, Tooltip("Segundos por ronda")] private float roundDuration = 20f;
    [SerializeField, Tooltip("Texto del cronómetro (muestra con una décima)")] private TMP_Text timerText;

    [Header("Rondas totales (final al completar)")]
    [SerializeField, Tooltip("Número total de rondas antes de declarar victoria final.")] private int totalRounds = 4;
    private int _roundsPlayed = 0;

    [Header("Panel Victoria (final)")]
    [SerializeField, Tooltip("Panel de victoria reutilizado con modo congelados.")] private VictoryTagPanel victoryPanel;

    [Header("Inicio Diferido por Gate")]
    [Tooltip("Si está activo, la ronda inicia cuando este objeto se desactiva (activeInHierarchy=false)." )]
    [SerializeField] private GameObject objetoGateInicio;
    [SerializeField] private bool esperarDesactivacionGate = true;
    [SerializeField, Tooltip("Timeout máximo esperando desactivación del gate; si se excede se inicia igualmente.")] private float gateTimeoutSeconds = 10f;

    [Header("Audio")]
    [SerializeField] private string sfxCountdownKey = "lleva:cronometro";
    [SerializeField] private float countdownSfxVolume = 0.44f;
    [SerializeField, Tooltip("Reproducir nuevamente el SFX de cronómetro al cruzar este umbral de tiempo restante (seg)")] private float countdownReplayThreshold = 5f;
    [SerializeField] private string sfxVictoryKey = "lleva:victoria";

    [Header("Integración / Seguridad")]
    [SerializeField, Tooltip("Si se encuentra un TagManager en escena, lo deshabilita para evitar conflictos con el modo Tag clásico")] private bool autoDisableTagManagerIfFound = true;
    [SerializeField, Tooltip("Deshabilita automáticamente componentes PlayerTag si están presentes en los jugadores")] private bool autoDisablePlayerTagIfFound = true;

    [Header("Velocidades Jugadores (Congelados)")]
    [SerializeField] private float moveSpeedNormal = 3f;
    [SerializeField] private float moveSpeedFreezer = 5f;
    public float MoveSpeedNormal => moveSpeedNormal;
    public float MoveSpeedFreezer => moveSpeedFreezer;

    [Header("Debug")] [SerializeField] private bool debugLogs = true;

    private readonly List<PlayerCongelados> _players = new List<PlayerCongelados>();
    private int _currentFreezerIndex1Based = 1; // rota 1..4 entre rondas
    private bool _roundActive = false;
    private float _timeRemaining = 0f;
    private bool _countdownThresholdPlayed = false;
    private bool _finalized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (autoDisableTagManagerIfFound)
        {
            var tm = FindFirstObjectByType<TagManager>();
            if (tm != null)
            {
                tm.enabled = false; // prevenir lógica del modo Tag clásico
                if (debugLogs) Debug.Log("[TagCongelados] TagManager detectado y deshabilitado para evitar conflictos.");
            }
        }
    }

    private void Start()
    {
        CollectPlayers();
        PrunePlayersByRoundData();
        // Opcional: deshabilitar PlayerTag en los jugadores si existe
        if (autoDisablePlayerTagIfFound)
        {
            foreach (var p in _players)
            {
                if (!p) continue;
                var pt = p.GetComponent<PlayerTag>();
                if (pt) pt.enabled = false;
            }
        }

        if (timerText) timerText.text = roundDuration.ToString("F1");

        if (esperarDesactivacionGate && objetoGateInicio != null)
        {
            StartCoroutine(CoEsperarDesactivacionEIniciar());
        }
        else
        {
            if (debugLogs && esperarDesactivacionGate && objetoGateInicio == null)
                Debug.LogWarning("[TagCongelados] 'esperarDesactivacionGate' activo pero 'objetoGateInicio' no asignado. Inicia inmediatamente.");
            StartNewRound();
        }
    }

    private void Update()
    {
        if (!_roundActive || _finalized) return;
        _timeRemaining -= Time.deltaTime;
        if (timerText)
        {
            float clamped = Mathf.Max(0f, _timeRemaining);
            timerText.text = clamped.ToString("F1");
        }

        // Reproducir de nuevo el cronómetro al cruzar el umbral (una sola vez)
        if (!_countdownThresholdPlayed && _timeRemaining <= countdownReplayThreshold && _timeRemaining > 0f)
        {
            _countdownThresholdPlayed = true;
            if (!string.IsNullOrEmpty(sfxCountdownKey))
            {
                var sm2 = SoundManager.instance;
                if (sm2 != null) sm2.PlaySfxRateLimited(sfxCountdownKey, 0.2f, countdownSfxVolume);
            }
            if (debugLogs) Debug.Log($"[TagCongelados] Umbral de {countdownReplayThreshold}s alcanzado: SFX cronómetro reproducido.");
        }

        if (_timeRemaining <= 0f)
        {
            HandleTimeExpired();
        }
    }

    private void CollectPlayers()
    {
        _players.Clear();
        var found = UnityEngine.Object.FindObjectsByType<PlayerCongelados>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++) if (found[i] != null) _players.Add(found[i]);
        if (debugLogs) Debug.Log($"[TagCongelados] Jugadores detectados: {_players.Count}");
    }

    private void PrunePlayersByRoundData()
    {
        var rd = RoundData.instance;
        if (rd == null) return;
        int maxPlayers = Mathf.Max(0, rd.numPlayers);
        int before = _players.Count;
        _players.RemoveAll(p => p == null || p.PlayerIndex > maxPlayers || p.PlayerIndex <= 0);
        if (debugLogs && before != _players.Count)
        {
            Debug.Log($"[TagCongelados] Filtrados {before - _players.Count} jugadores fuera de rango (numPlayers={maxPlayers}). Quedan {_players.Count} activos.");
        }
    }

    private System.Collections.IEnumerator CoEsperarDesactivacionEIniciar()
    {
        if (debugLogs) Debug.Log("[TagCongelados] Esperando desactivación de gate para iniciar ronda...");
        float elapsed = 0f;
        while (objetoGateInicio != null && objetoGateInicio.activeInHierarchy && elapsed < gateTimeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (objetoGateInicio != null && objetoGateInicio.activeInHierarchy)
        {
            if (debugLogs) Debug.LogWarning($"[TagCongelados] Gate no desactivado tras timeout {gateTimeoutSeconds}s -> iniciando igualmente.");
        }
        else if (debugLogs)
        {
            Debug.Log("[TagCongelados] Gate desactivado -> inicia ronda Congelados.");
        }
        StartNewRound();
    }

    private void StartNewRound()
    {
        if (_finalized) return;

        if (_players.Count == 0)
        {
            if (debugLogs) Debug.LogWarning("[TagCongelados] No hay jugadores.");
            return;
        }

        // Rotación garantizada del freezer para la nueva ronda
        _currentFreezerIndex1Based = GetNextExistingPlayerIndex(_currentFreezerIndex1Based);
        if (debugLogs) Debug.Log($"[TagCongelados] Freezer de esta ronda: Player{_currentFreezerIndex1Based}");

        // Preparar roles/estados
        foreach (var p in _players)
        {
            if (p == null) continue;
            bool isFreezer = p.PlayerIndex == _currentFreezerIndex1Based;
            p.SetFreezer(isFreezer);
            p.ForceUnfreeze(); // todos comienzan descongelados
        }

        // Resetear posiciones/velocidades de todos los Movimiento en escena
        var movs = UnityEngine.Object.FindObjectsByType<Movimiento>(FindObjectsSortMode.None);
        foreach (var m in movs) { if (m) m.ResetToInitialPosition(); }

        // Disparar inicio para obstáculos que aparecen con el tiempo
        var obsts = UnityEngine.Object.FindObjectsByType<Obstaculo>(FindObjectsSortMode.None);
        foreach (var o in obsts) { if (o) o.StartRound(); }

        _timeRemaining = roundDuration;
        _countdownThresholdPlayed = false;
        _roundActive = true;

        // Popup y SFX inicio
        ShowRoundStartPopup(_currentFreezerIndex1Based);
        if (!string.IsNullOrEmpty(sfxCountdownKey))
        {
            var sm = SoundManager.instance; if (sm != null) sm.PlaySfxRateLimited(sfxCountdownKey, 0.2f, countdownSfxVolume);
        }

        // Notificar inicio (evento propio)
        OnRoundStarted?.Invoke();

        if (debugLogs) Debug.Log($"[TagCongelados] Ronda iniciada. Freezer=Player{_currentFreezerIndex1Based}");
    }

    private int GetNextExistingPlayerIndex(int current)
    {
        var rd = RoundData.instance; int max = rd ? Mathf.Clamp(rd.numPlayers, 1, 4) : 4;
        // Si current es inválido, empezar en 1
        int next = (current >= 1 && current <= max) ? current : 1;
        // Avanza hasta encontrar un jugador presente
        for (int i = 0; i < 4; i++)
        {
            int candidate = ((next - 1 + i) % max) + 1; // 1..max
            if (_players.Exists(p => p != null && p.PlayerIndex == candidate))
                return candidate;
        }
        // fallback
        return _players[0].PlayerIndex;
    }

    private void HandleTimeExpired()
    {
        if (_finalized) return;
        _roundActive = false;
        if (debugLogs) Debug.Log("[TagCongelados] Tiempo agotado: Ganan los no congelados (todos los runners reciben punto, estén congelados o no).");
        var scorer = CongeladosScoreManager.Instance;
        if (scorer != null)
        {
            scorer.AddPointsToAllNonFreezers(_players);
        }
        var winners = new List<int>();
        foreach (var p in _players)
        {
            if (p != null && !p.IsFreezer) winners.Add(p.PlayerIndex);
        }
        OnRoundEnded?.Invoke(new RoundEndData
        {
            type = RoundEndType.NonFreezersWin,
            freezerIndex1Based = _currentFreezerIndex1Based,
            winners1Based = winners
        });
        var sm = SoundManager.instance; if (sm && !string.IsNullOrEmpty(sfxVictoryKey)) sm.PlaySfx(sfxVictoryKey, 0.7f);
        PostRoundAdvanceAndMaybeFinish();
    }

    private void PostRoundAdvanceAndMaybeFinish()
    {
        _roundsPlayed++;
        RotateFreezerForNextRound();
        if (_roundsPlayed >= totalRounds)
        {
            FinalizeSession();
        }
        else
        {
            Invoke(nameof(StartNewRound), 2f);
        }
    }

    private void RotateFreezerForNextRound()
    {
        var rd = RoundData.instance; int max = rd ? Mathf.Clamp(rd.numPlayers, 1, 4) : 4;
        _currentFreezerIndex1Based++;
        if (_currentFreezerIndex1Based > max) _currentFreezerIndex1Based = 1;
        _currentFreezerIndex1Based = GetNextExistingPlayerIndex(_currentFreezerIndex1Based);
        if (debugLogs) Debug.Log($"[TagCongelados] Próximo freezer será Player{_currentFreezerIndex1Based}");
    }

    public void CheckWinByAllFrozen()
    {
        if (!_roundActive || _finalized) return;
        // Si todos los no-freezer están congelados, gana el freezer
        bool anyNonFreezer = false;
        bool allFrozen = true;
        foreach (var p in _players)
        {
            if (p == null) continue;
            if (!p.IsFreezer)
            {
                anyNonFreezer = true;
                if (!p.IsFrozen)
                {
                    allFrozen = false; break;
                }
            }
        }
        if (anyNonFreezer && allFrozen)
        {
            _roundActive = false;
            if (debugLogs) Debug.Log($"[TagCongelados] ¡Todos congelados! Gana el freezer Player{_currentFreezerIndex1Based}.");

            // Otorgar punto al freezer
            var scorer = CongeladosScoreManager.Instance;
            if (scorer != null)
            {
                scorer.AddPointToFreezer(_currentFreezerIndex1Based);
            }

            // Notificar UI fin de ronda (freezer gana)
            OnRoundEnded?.Invoke(new RoundEndData
            {
                type = RoundEndType.FreezerWin,
                freezerIndex1Based = _currentFreezerIndex1Based,
                winners1Based = new List<int> { _currentFreezerIndex1Based }
            });

            var sm = SoundManager.instance; if (sm != null && !string.IsNullOrEmpty(sfxVictoryKey)) sm.PlaySfx(sfxVictoryKey, 0.9f);
            // Rotar freezer para la próxima ronda igualmente
            PostRoundAdvanceAndMaybeFinish();
        }
    }

    private void FinalizeSession()
    {
        _finalized = true;
        var winners = DetermineOverallWinners();
        if (victoryPanel)
        {
            if (winners != null && winners.Count > 1)
            {
                victoryPanel.EnableCongeladosModeAndShowMultiple(winners);
            }
            else if (winners != null && winners.Count == 1)
            {
                victoryPanel.EnableCongeladosModeAndShow(winners[0]);
            }
        }
        OnRoundEnded?.Invoke(new RoundEndData
        {
            type = RoundEndType.FinalVictory,
            freezerIndex1Based = _currentFreezerIndex1Based,
            winners1Based = winners
        });
    }

    private List<int> DetermineOverallWinners()
    {
        var scorer = CongeladosScoreManager.Instance; if (!scorer) return new List<int> { 1 };
        int[] snap = scorer.GetScoresSnapshot();
        // Encontrar máximo entre jugadores activos
        int maxScore = int.MinValue;
        var rd = RoundData.instance; int maxPlayers = rd ? Mathf.Clamp(rd.numPlayers, 1, 4) : 4;
        // Filtrar por jugadores presentes (_players) para robustez
        HashSet<int> present = new HashSet<int>();
        foreach (var p in _players) if (p != null && p.PlayerIndex >= 1 && p.PlayerIndex <= maxPlayers) present.Add(p.PlayerIndex);
        for (int i = 0; i < 4; i++)
        {
            int idx1 = i + 1;
            if (!present.Contains(idx1)) continue;
            maxScore = Mathf.Max(maxScore, snap[i]);
        }
        if (maxScore == int.MinValue)
        {
            // Fallback si no hay presentes
            return new List<int> { 1 };
        }
        var winners = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            int idx1 = i + 1;
            if (!present.Contains(idx1)) continue;
            if (snap[i] == maxScore) winners.Add(idx1);
        }
        if (debugLogs) Debug.Log($"[TagCongelados] Final de sesión -> ganadores: [{string.Join(",", winners)}] con scoreMax={maxScore}");
        return winners;
    }

    private void ShowRoundStartPopup(int playerIndex1Based)
    {
        if (!roundStartPopup) return;
        int idx = Mathf.Clamp(playerIndex1Based - 1, 0, 3);
        Color c = (startColors != null && startColors.Length > idx) ? startColors[idx] : Color.white;
        string nombre = (startColorNames != null && startColorNames.Length > idx) ? startColorNames[idx] : $"Jugador {playerIndex1Based}";
        roundStartPopup.Show(playerIndex1Based, nombre, c);
    }
}
