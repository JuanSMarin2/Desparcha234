using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class TagManager : MonoBehaviour
{
    public static TagManager Instance { get; private set; }

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

    [Header("Debug")] [SerializeField] private bool debugLogs = true;

    private List<PlayerTag> _players = new List<PlayerTag>();
    private PlayerTag _currentTagged;
    private float _timeRemaining;
    private bool _roundActive = false;
    private int _pendingWinnerIndex0Based = -1; // almacenado hasta animación final
    private List<int> _eliminationOrder0Based = new List<int>(); // orden cronológico de eliminados (0-based)

    public System.Action<PlayerTag> OnPlayerEliminated; // callback externo (futuro panel)
    public System.Action<PlayerTag, PlayerTag> OnTagChanged; // old,new

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CollectPlayers();
        StartNewRoundInitial();
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

    private void StartNewRoundInitial()
    {
        if (_players.Count == 0)
        {
            if (debugLogs) Debug.LogWarning("[TagManager] No hay jugadores.");
            return;
        }
        PlayerTag chosen = DetermineInitialTagged();
        AssignTag(chosen, null);
        _timeRemaining = roundDuration;
        _roundActive = true;
        if (debugLogs) Debug.Log($"[TagManager] Ronda inicial comenzada. Tagged=Player{chosen.PlayerIndex}");
    }

    public void RestartRound()
    {
        if (_players.Count <= 1)
        {
            // Fin del juego: un solo jugador queda
            if (debugLogs) Debug.Log("[TagManager] Juego terminado. Ganador pendiente de panel.");
            _roundActive = false;
            return;
        }
        _timeRemaining = roundDuration;
        PlayerTag randomTagged = _players[Random.Range(0, _players.Count)];
        AssignTag(randomTagged, _currentTagged);
        if (debugLogs) Debug.Log($"[TagManager] Nueva ronda. Tagged=Player{randomTagged.PlayerIndex}");
        _roundActive = true;
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
                Time.timeScale = 0f;
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
    }
}
