using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class TagManager : MonoBehaviour
{
    public static TagManager Instance { get; private set; }

    [Header("Velocidades Jugador")]
    [SerializeField] private float moveSpeedNormal = 3f;
    [SerializeField] private float moveSpeedTagged = 5f;
    public float MoveSpeedNormal => moveSpeedNormal;
    public float MoveSpeedTagged => moveSpeedTagged;

    [Header("Popup Inicio de Ronda")]
    [SerializeField] private global::TagRoundStartPopup roundStartPopup; // asignar en inspector (panel/prefab en escena)
    [Tooltip("Colores 1..4 para el popup de inicio de ronda")] 
    [SerializeField] private Color[] startColors = new Color[]{ new Color(0.85f,0.25f,0.25f), new Color(0.25f,0.45f,0.9f), new Color(0.95f,0.85f,0.2f), new Color(0.3f,0.85f,0.4f)};
    [Tooltip("Nombres 1..4 de los colores (coinciden con startColors)")] 
    [SerializeField] private string[] startColorNames = new string[]{ "rojo", "azul", "amarillo", "verde" };

    [Header("Duración Ronda")] [SerializeField] private float roundDuration = 10f; // segundos
    [SerializeField] private TMP_Text timerText; // mostrar con una décima

    [Header("UI Jugadores")] 
    [Tooltip("Botones/slots UI por índice de jugador (1..4). Se desactivan al eliminar.")] 
    [SerializeField] private Button[] playerButtons = new Button[4];
    [SerializeField] private Color eliminatedColor = Color.gray;

    [Header("Paneles Tag")] 
    [Tooltip("Panel que muestra jugador eliminado y botón para siguiente ronda")] 
    [SerializeField] private global::PanelEliminacionTag panelEliminacion;
    [Tooltip("Panel/Contenedor de victoria final")] 
    [SerializeField] private global::VictoryTagPanel panelVictoria;
    [Tooltip("Iconos (tristes) de jugadores 1..4 para panel de eliminación")] 
    [SerializeField] private Sprite[] eliminationIcons = new Sprite[4];

    [Header("Reposicionamiento al eliminar")] 
    [Tooltip("Objetos con ReposicionarPorTurno que se ajustarán al target del jugador eliminado")] 
    [SerializeField] private ReposicionarPorTurno[] repositionersOnElimination;

    [Header("Inicio Diferido por Gate")]
    [Tooltip("Si se asigna y 'esperarDesactivacionGate' es true, la ronda inicia cuando este objeto se desactiva (activeInHierarchy=false)." )]
    [SerializeField] private GameObject objetoGateInicio; 
    [SerializeField] private bool esperarDesactivacionGate = true;
    [Tooltip("Timeout máximo esperando desactivación del gate; si se excede se inicia igualmente.")]
    [SerializeField] private float gateTimeoutSeconds = 10f;

    [Header("Audio")]
    [SerializeField] private string sfxCountdownKey = "lleva:cronometro";
    [SerializeField] private float countdownSfxVolume = 0.44f;
    [Tooltip("Reproducir nuevamente el SFX de cronómetro al cruzar este umbral de tiempo restante (seg)")]
    [SerializeField] private float countdownReplayThreshold = 5f;
    [SerializeField] private string sfxVictoryKey = "lleva:victoria";

    [Header("Debug")] [SerializeField] private bool debugLogs = true;

    private List<PlayerTag> _players = new List<PlayerTag>();
    private PlayerTag _currentTagged;
    private float _timeRemaining;
    private bool _roundActive = false;
    private int _pendingWinnerIndex0Based = -1; // almacenado hasta animación final
    private List<int> _eliminationOrder0Based = new List<int>(); // orden cronológico de eliminados (0-based)

    public System.Action<PlayerTag> OnPlayerEliminated; // callback externo (futuro panel)
    public System.Action<PlayerTag, PlayerTag> OnTagChanged; // old,new

    // Flag para reproducir una sola vez el SFX al cruzar el umbral
    private bool _countdownThresholdPlayed = false;

    public static event System.Action OnRoundStarted;
    public static event System.Action<int> OnStageChanged; // etapa++ cuando hay eliminación
    public int CurrentStageInRound { get; private set; } = 0; // ahora persiste entre rondas de esta partida

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CollectPlayers();
        timerText.text = roundDuration.ToString("F1");
        PrunePlayersByRoundData();
        // Nueva lógica: esperar desactivación de un GameObject gate
        if (esperarDesactivacionGate && objetoGateInicio != null)
        {
            StartCoroutine(CoEsperarDesactivacionEIniciar());
        }
        else
        {
            if (debugLogs && esperarDesactivacionGate && objetoGateInicio == null)
                Debug.LogWarning("[TagManager] 'esperarDesactivacionGate' activo pero 'objetoGateInicio' no asignado. Inicia inmediatamente.");
            StartNewRoundInitial();
        }
    }

    void Update()
    {
        if (!_roundActive) return;
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
            if (debugLogs) Debug.Log($"[TagManager] Umbral de {countdownReplayThreshold}s alcanzado: SFX cronómetro reproducido.");
        }

        if (_timeRemaining <= 0f)
        {
            HandleTimeExpired();
        }
    }

    private void CollectPlayers()
    {
        _players.Clear();
#pragma warning disable 618
        var found = UnityEngine.Object.FindObjectsOfType<PlayerTag>(); // compat legacy
#pragma warning restore 618
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null) _players.Add(found[i]);
        }
        if (debugLogs) Debug.Log($"[TagManager] Jugadores detectados: {_players.Count}");
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
            Debug.Log($"[TagManager] PrunePlayersByRoundData: filtrados {before - _players.Count} jugadores fuera de rango (numPlayers={maxPlayers}). Quedan {_players.Count} activos.");
        }
    }

    private void StartNewRoundInitial()
    {
        PrunePlayersByRoundData();
        if (_players.Count == 0)
        {
            if (debugLogs) Debug.LogWarning("[TagManager] No hay jugadores tras prune.");
            return;
        }
        PlayerTag chosen = DetermineInitialTaggedFromActive();
        if (chosen == null)
        {
            chosen = GetRandomActivePlayer();
            if (debugLogs) Debug.LogWarning("[TagManager] Fallback a random por chosen null");
        }
        AssignTag(chosen, null);
        _timeRemaining = roundDuration;
        _roundActive = true;
        _countdownThresholdPlayed = false;
        // Nota: NO reiniciamos CurrentStageInRound aquí; etapa avanza con eliminaciones y persiste.

        ShowRoundStartPopup(chosen.PlayerIndex);
        
        // Reproducir sonido una sola vez al inicio
        if (!string.IsNullOrEmpty(sfxCountdownKey))
        {
            var sm = SoundManager.instance;
            if (sm != null) sm.PlaySfxRateLimited(sfxCountdownKey, 0.2f, countdownSfxVolume);
        }
        
        // Notificar inicio de ronda
        OnRoundStarted?.Invoke();
        
        if (debugLogs) Debug.Log($"[TagManager] Ronda inicial comenzada. Tagged=Player{chosen.PlayerIndex}");
    }

    public void RestartRound()
    {
        PrunePlayersByRoundData();
        if (_players.Count <= 1)
        {
            if (debugLogs) Debug.Log("[TagManager] Juego terminado. Ganador pendiente de panel.");
            _roundActive = false;
            return;
        }
        _timeRemaining = roundDuration;
        _countdownThresholdPlayed = false;
        PlayerTag randomTagged = GetRandomActivePlayer();
        AssignTag(randomTagged, _currentTagged);
        ShowRoundStartPopup(randomTagged.PlayerIndex);
        if (debugLogs) Debug.Log($"[TagManager] Nueva ronda. Tagged=Player{randomTagged.PlayerIndex}");
        _roundActive = true;
        // Nota: NO reiniciamos CurrentStageInRound en cada ronda.
        
        // Notificar inicio de ronda
        OnRoundStarted?.Invoke();
        
        // Reproducir sonido una sola vez al reiniciar
        if (!string.IsNullOrEmpty(sfxCountdownKey))
        {
            var sm = SoundManager.instance;
            if (sm != null) sm.PlaySfxRateLimited(sfxCountdownKey, 0.2f, countdownSfxVolume);
        }
    }

    private PlayerTag GetRandomActivePlayer()
    {
        var rd = RoundData.instance;
        int maxPlayers = rd ? rd.numPlayers : 4;
        List<PlayerTag> pool = _players.FindAll(p => p != null && p.PlayerIndex > 0 && p.PlayerIndex <= maxPlayers);
        if (pool.Count == 0)
        {
            if (debugLogs) Debug.LogWarning("[TagManager] GetRandomActivePlayer sin candidatos válidos.");
            return null;
        }
        return pool[Random.Range(0, pool.Count)];
    }

    private PlayerTag DetermineInitialTaggedFromActive()
    {
        var rd = RoundData.instance;
        int maxPlayers = rd ? rd.numPlayers : 4;
        List<PlayerTag> activos = _players.FindAll(p => p != null && p.PlayerIndex <= maxPlayers && p.PlayerIndex > 0);
        if (activos.Count == 0) return null;
        if (rd != null && rd.totalPoints != null && rd.totalPoints.Length >= maxPlayers)
        {
            int bestIndex1Based = -1;
            int bestScore = int.MinValue;
            bool tie = false;
            foreach (var p in activos)
            {
                int idx0 = p.PlayerIndex - 1;
                if (idx0 < 0 || idx0 >= rd.totalPoints.Length) continue;
                int sc = rd.totalPoints[idx0];
                if (sc > bestScore)
                {
                    bestScore = sc; bestIndex1Based = p.PlayerIndex; tie = false;
                }
                else if (sc == bestScore)
                {
                    tie = true;
                }
            }
            if (bestIndex1Based > 0 && !tie)
            {
                foreach (var p in activos) if (p.PlayerIndex == bestIndex1Based) return p;
            }
        }
        return activos[Random.Range(0, activos.Count)];
    }

    private PlayerTag DetermineInitialTagged()
    {
        RoundData rd = RoundData.instance;
        if (rd == null || rd.totalPoints == null || rd.totalPoints.Length == 0)
        {
            if (debugLogs) Debug.Log("[TagManager] RoundData ausente o inválido -> random");
            return _players[Random.Range(0, _players.Count)];
        }
        // Tomar máximo y verificar si hay empate
        int max = int.MinValue;
        int maxCount = 0;
        int maxIndex = -1; // 0-based
        int limit = Mathf.Min(rd.totalPoints.Length, 4);
        for (int i = 0; i < limit; i++)
        {
            int val = rd.totalPoints[i];
            if (val > max)
            {
                max = val; maxCount = 1; maxIndex = i;
            }
            else if (val == max)
            {
                maxCount++;
            }
        }
        if (maxIndex < 0 || maxCount != 1)
        {
            if (debugLogs) Debug.Log($"[TagManager] Empate o sin líder claro (maxCount={maxCount}) -> random");
            return _players[Random.Range(0, _players.Count)];
        }
        // Buscar PlayerTag con ese índice (playerIndex1Based == maxIndex+1)
        for (int p = 0; p < _players.Count; p++)
        {
            if (_players[p].PlayerIndex == maxIndex + 1)
            {
                if (debugLogs) Debug.Log($"[TagManager] Líder claro Player{_players[p].PlayerIndex} con {max} puntos.");
                return _players[p];
            }
        }
        if (debugLogs) Debug.Log("[TagManager] No se halló jugador líder esperado -> random fallback");
        return _players[Random.Range(0, _players.Count)];
    }

    private void AssignTag(PlayerTag newTagged, PlayerTag oldTagged)
    {
        if (newTagged == null)
        {
            if (debugLogs) Debug.LogWarning("[TagManager] AssignTag recibido newTagged null");
            return;
        }
        if (oldTagged != null && oldTagged != newTagged)
        {
            oldTagged.SetTagged(false, false);
        }
        _currentTagged = newTagged;
        foreach (var p in _players)
        {
            if (!p) continue;
            if (p == newTagged) p.SetTagged(true, true); else p.SetTagged(false, false);
        }
        OnTagChanged?.Invoke(oldTagged, newTagged);
    }

    private void HandleTimeExpired()
    {
        _roundActive = false;
        if (_currentTagged == null)
        {
            if (debugLogs) Debug.LogWarning("[TagManager] Tiempo expirado pero no hay currentTagged");
            RestartRound();
            return;
        }
        // Eliminar jugador con tag
        PlayerTag eliminated = _currentTagged;
        if (debugLogs) Debug.Log($"[TagManager] Tiempo agotado -> elimina Player{eliminated.PlayerIndex}");
        RemovePlayer(eliminated);
    }

    private void RemovePlayer(PlayerTag p)
    {
        if (p == null) return;
        int eliminatedIdx0 = p.PlayerIndex - 1;
        // Registrar eliminado localmente
        _eliminationOrder0Based.Add(eliminatedIdx0);
        
        // Avanzar etapa por eliminación y notificar
        CurrentStageInRound++;
        OnStageChanged?.Invoke(CurrentStageInRound);
        if (debugLogs) Debug.Log($"[TagManager] Etapa (eliminaciones acumuladas): {CurrentStageInRound}");
        
        // Reposicionar objetos vinculados al jugador eliminado
        if (repositionersOnElimination != null)
        {
            foreach (var rep in repositionersOnElimination)
            {
                if (rep != null)
                {
                    rep.ReposicionarPorEventoJugador(eliminatedIdx0, mover: true, recolorear: true, instant: true);
                    Debug.Log("[TagManager] Reposicionando por evento jugador eliminado." + eliminatedIdx0);
                }
            }
        }
        // (Eliminada llamada a GameRoundManager.PlayerLose para desligar TurnManager)
        _players.Remove(p);
        p.EliminarJugador();
        OnPlayerEliminated?.Invoke(p);
        _roundActive = false; // detener timer hasta decidir siguiente paso
        if (_players.Count > 1)
        {
            // Detener cualquier SFX pendiente del cronómetro al abrir el panel
            var sm = SoundManager.instance; if (sm != null) sm.StopSfx();

            Time.timeScale = 0f;
            if (panelEliminacion != null)
            {
                Sprite icon = eliminatedIdx0 < eliminationIcons.Length && eliminatedIdx0 >= 0 ? eliminationIcons[eliminatedIdx0] : null;
                panelEliminacion.Show(p.PlayerIndex, icon, ContinuarTrasPanelEliminacion);
            }
            else
            {
                if (debugLogs) Debug.LogWarning("[TagManager] panelEliminacion no asignado, reiniciando ronda directo.");
                Time.timeScale = 1f;
                RestartRound();
            }
        }
        else
        {
            if (_players.Count == 1)
            {
                _pendingWinnerIndex0Based = _players[0].PlayerIndex - 1;
                // Reproducir SFX de victoria
                var smv = SoundManager.instance; if (smv != null && !string.IsNullOrEmpty(sfxVictoryKey)) smv.PlaySfx(sfxVictoryKey, 0.9f);
                // Antes: Time.timeScale = 0f;  -> Se elimina para permitir animación de victoria.
                if (debugLogs) Debug.Log("[TagManager] Un solo jugador restante: no se pausa (timeScale permanece) para reproducir animación de victoria.");
                if (panelVictoria != null)
                {
                    panelVictoria.ShowWinner(_players[0].PlayerIndex);
                }
                else
                {
                    if (debugLogs) Debug.LogWarning("[TagManager] panelVictoria no asignado. Finalizando sin animación.");
                    FinalizeVictoryImmediate();
                }
            }
            else
            {
                if (debugLogs) Debug.Log("[TagManager] No quedan jugadores tras eliminación.");
            }
        }
    }

    private void ContinuarTrasPanelEliminacion()
    {
        // Reanudar y arrancar nueva ronda
        Time.timeScale = 1f;
        RestartRound();
    }

    private void FinalizeVictoryImmediate()
    {
        if (_pendingWinnerIndex0Based >= 0)
        {
            // Preparar datos para GameRoundManager sin usar TurnManager
            var grm = GameRoundManager.instance;
            if (grm != null)
            {
                grm.FinalizeTagRound(_eliminationOrder0Based, _pendingWinnerIndex0Based);
            }
            _pendingWinnerIndex0Based = -1;
        }
    }

    public void OnVictoryAnimationFinished()
    {
        // Llamado por Animation Event vía proxy
        FinalizeVictoryImmediate();
    }

    public void NotifyTagChanged(PlayerTag oldTagged, PlayerTag newTagged)
    {
        if (debugLogs) Debug.Log($"[TagManager] Transferencia tag: {(oldTagged?"P"+oldTagged.PlayerIndex:"none")} -> P{newTagged.PlayerIndex}");
        _currentTagged = newTagged;
        OnTagChanged?.Invoke(oldTagged, newTagged);
        // Ya no avanza etapa aquí; la etapa avanza con eliminaciones.
    }

    // Nueva coroutine que reemplaza dependencia de StartTimintg
    private System.Collections.IEnumerator CoEsperarDesactivacionEIniciar()
    {
        if (debugLogs) Debug.Log("[TagManager] Esperando desactivación de gate para iniciar ronda...");
        float elapsed = 0f;
        while (objetoGateInicio != null && objetoGateInicio.activeInHierarchy && elapsed < gateTimeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime; // independiente de timeScale
            yield return null;
        }
        if (objetoGateInicio != null && objetoGateInicio.activeInHierarchy)
        {
            if (debugLogs) Debug.LogWarning($"[TagManager] Gate no desactivado tras timeout {gateTimeoutSeconds}s -> iniciando igualmente.");
        }
        else if (debugLogs)
        {
            Debug.Log("[TagManager] Gate desactivado -> inicia Tag round.");
        }
        StartNewRoundInitial();
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
