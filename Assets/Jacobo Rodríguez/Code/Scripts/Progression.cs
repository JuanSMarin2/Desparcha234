using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.SceneManagement;

public class Progression : MonoBehaviour
{
    public int stage = 0;
    private const int MaxStage = 3;

    bool touchedBall = false;
    public int jacksCounter = 0;
    public long currentScore = 0; // base + intento
    private long _baseScore = 0;   // consolidado del jugador
    private long _attemptScore = 0; // puntaje del intento actual
    private long[] _playerCurrentScores; // puntaje actual por jugador (para ranking)

    [Header("Configuración Etapas / Intentos")]
    [SerializeField] private int attemptsPerPlayer = 2;
    private int[] _attemptsLeft;

    [Header("Referencias")]
    private UiManager _ui;
    private BarraFuerza _barra;
    private JackSpawner _spawner;
    private Bolita _bolita;

    [Header("UI Lanzamiento")]
    [SerializeField] private GameObject botonListo; // asignar en inspector
    private bool _preLaunchPaused = false;

    [Header("Animación Fin de Partida")]
    [SerializeField] private Animacionfinal _animacionFinal; // Referencia en escena para animación de ganador
    private bool _endGameAnimationTriggered = false;

    public static event Action<int> OnTurnAdvanced;

    private int _lastSpawnFrame = -1; // guard para evitar spawns duplicados en mismo frame
    private string _lastSpawnOrigin = ""; // info debug
    private string _pendingSpawnOrigin = "PendingThrow"; // origen que se usará para el próximo spawn cuando la bola quede pendiente

    #region HelpersPuntaje
    private int CurrentIdx()
    {
        return TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
    }

    private void EnsureUiRef()
    {
        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
    }

    private void SetPlayerScore(int idx, long value)
    {
        if (_playerCurrentScores == null) return;
        if (idx < 0 || idx >= _playerCurrentScores.Length) return;
        _playerCurrentScores[idx] = value;
    }

    private long GetPlayerScore(int idx)
    {
        if (_playerCurrentScores == null) return 0;
        if (idx < 0 || idx >= _playerCurrentScores.Length) return 0;
        return _playerCurrentScores[idx];
    }

    private void RecalculateCurrentScoreOnly()
    {
        currentScore = _baseScore + _attemptScore;
    }

    private void SyncCurrentPlayerScoreAndUi()
    {
        int idx = CurrentIdx();
        if (idx >= 0) SetPlayerScore(idx, currentScore);
        EnsureUiRef();
        if (_ui != null && TurnManager.instance != null)
        {
            _ui.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
        }
    }

    private void ResetAttemptScore()
    {
        _attemptScore = 0;
        RecalculateCurrentScoreOnly();
        SyncCurrentPlayerScoreAndUi();
    }

    private void ConsolidarIntentoSimplificado()
    {
        if (_attemptScore <= 0) return;
        _baseScore += _attemptScore;
        _attemptScore = 0;
        RecalculateCurrentScoreOnly();
        SyncCurrentPlayerScoreAndUi();
    }
    #endregion

    void Awake()
    {
        _barra = FindAnyObjectByType<BarraFuerza>();
        _spawner = FindAnyObjectByType<JackSpawner>();
        _bolita = FindAnyObjectByType<Bolita>();
        _ui = FindAnyObjectByType<UiManager>();
    }

    void Start()
    {
        if (stage < 1) stage = 1;
        if (stage > MaxStage) stage = MaxStage;

        int n = RoundData.instance != null ? Mathf.Max(0, RoundData.instance.numPlayers) : 0;
        if (n > 0)
        {
            _attemptsLeft = new int[n];
            for (int i = 0; i < n; i++) _attemptsLeft[i] = Mathf.Max(1, attemptsPerPlayer);
            _playerCurrentScores = new long[n];
        }

        int idx0 = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : 0;
        _attemptScore = 0;
        _baseScore = 0;
        currentScore = 0;
        if (_playerCurrentScores != null && idx0 >= 0 && idx0 < _playerCurrentScores.Length)
            _playerCurrentScores[idx0] = currentScore;

        _ui?.ActualizarPuntos(TurnManager.instance != null ? TurnManager.instance.CurrentTurn() : 1, currentScore);
        ActualizarUiIntentos(true);

        if (TurnManager.instance != null)
        {
            int idx = TurnManager.instance.GetCurrentPlayerIndex();
            if (idx >= 0) OnTurnAdvanced?.Invoke(idx);
        }

        if (!_preLaunchPaused)
        {
            ActivarPausaPreLanzamiento();
        }
        Time.timeScale = 1f;
        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        SpawnearJacksTransparentes("Start");
    }

    public void NotificarJackTocado(Jack jack)
    {
        if (jack == null) return;
        jacksCounter++;
        _attemptScore += jack.Puntos;
        RecalculateCurrentScoreOnly();
        SyncCurrentPlayerScoreAndUi();
    }

    public void OnBallLaunched()
    {
        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        _spawner?.EnableJacks();
    }

    public void OnBallPendingThrow()
    {
        if (_bolita.Estado == Bolita.EstadoLanzamiento.PendienteDeLanzar)
        {
            ActivarPausaPreLanzamiento();
            var usedOrigin = _pendingSpawnOrigin;
            SpawnearJacksTransparentes(usedOrigin);
            _pendingSpawnOrigin = "PendingThrow";
            Debug.Log($"[Progression] OnBallPendingThrow -> Pause then Spawn (originUsed={usedOrigin})");
        }
    }

    public void SpawnearJacksTransparentes(string origin = "Unknown")
    {
        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        if (Time.frameCount == _lastSpawnFrame)
        {
            Debug.Log($"[Progression][SpawnGuard] Ignorado spawn duplicado mismo frame. Origen nuevo={origin} previo={_lastSpawnOrigin}");
            return;
        }
        _lastSpawnFrame = Time.frameCount;
        _lastSpawnOrigin = origin;
        _spawner?.SpawnJacks();
        Debug.Log($"[Progression] SpawnJacks origin={origin} frame={_lastSpawnFrame}");
    }

    private void ActivarPausaPreLanzamiento()
    {
        if (botonListo != null) botonListo.SetActive(true);
        _preLaunchPaused = true;
        global::BarraFuerza.SetGlobalShakeBlocked(true);
        Debug.Log("[Progression] Juego pausado esperando botonListo");
    }

    public void BotonListoConfirmar()
    {
        if (!_preLaunchPaused) return;
        _preLaunchPaused = false;
        if (botonListo != null) botonListo.SetActive(false);
        if (_barra == null) _barra = FindAnyObjectByType<BarraFuerza>();
        _barra?.PostResumeReset(0.3f);
        BarraFuerza.SetGlobalShakeBlocked(false);
        Time.timeScale = 1f;
        Debug.Log("[Progression] Reanudado tras botonListo (cooldown shakes aplicado)");
    }

    private void ConsolidarIntento()
    {
        ConsolidarIntentoSimplificado();
    }

    public void TerminarTurno()
    {
        _barra?.Reiniciar();
        _ui?.OcultarTextoAtrapa();

        if (TurnManager.instance != null)
        {
            int idxPrev = CurrentIdx();

            HandleAttemptsAndAdvanceTurn();
            if (_endGameAnimationTriggered) return;

            int idxActual = CurrentIdx();
            if (idxActual <= idxPrev && idxActual != -1)
            {
                int prevStage = stage;
                stage = Mathf.Min(stage + 1, MaxStage);
                if (stage != prevStage)
                {
                    Debug.Log($"[Progression] Avance de etapa: {prevStage} -> {stage} (fin de vuelta)");
                }
            }

            if (idxActual >= 0) OnTurnAdvanced?.Invoke(idxActual);
            _bolita?.ActualizarSpritePorTurno();
        }

        EnsureUiRef();
        if (TurnManager.instance != null)
        {
            int idxNuevo = CurrentIdx();
            _attemptScore = 0;
            _baseScore = GetPlayerScore(idxNuevo);
            RecalculateCurrentScoreOnly();
            SyncCurrentPlayerScoreAndUi();
            ActualizarUiIntentos();
        }

        ForceApplyRepositionAll();
        _bolita?.ReiniciarBola();
    }

    private void HandleAttemptsAndAdvanceTurn()
    {
        int idx = CurrentIdx();
        if (_attemptsLeft == null)
        {
            TurnManager.instance.NextTurn();
            return;
        }
        if (idx < 0 || idx >= _attemptsLeft.Length)
        {
            TurnManager.instance.NextTurn();
            return;
        }
        _attemptsLeft[idx] = Mathf.Max(0, _attemptsLeft[idx] - 1);
        ActualizarUiIntentos();

        int totalRestantes = 0;
        for (int i = 0; i < _attemptsLeft.Length; i++) totalRestantes += _attemptsLeft[i];
        if (totalRestantes <= 0)
        {
            MostrarAnimacionGanadorYOtorgarCierre();
            return;
        }

        int safety = _attemptsLeft.Length;
        do
        {
            TurnManager.instance.NextTurn();
            safety--;
        } while (safety > 0 && _attemptsLeft[CurrentIdx()] <= 0);
    }

    private void MostrarAnimacionGanadorYOtorgarCierre()
    {
        if (_endGameAnimationTriggered)
        {
            Debug.Log("[Progression] Animación de fin ya fue disparada; ignorando duplicado.");
            return;
        }
        _endGameAnimationTriggered = true;

        PrepararFinDeJuegoVisual();
        ConsolidarIntentoSimplificado();

        var ganadores = CalcularGanadoresIndex1Based();
        if (_animacionFinal == null) _animacionFinal = FindAnyObjectByType<Animacionfinal>();
        if (_animacionFinal != null)
        {
            Debug.Log($"[Progression] Mostrando animación de ganador(es) [{string.Join(",", ganadores)}]. Esperando Animation Event para finalizar.");
            _animacionFinal.AnimacionFinal(ganadores);
        }
        else
        {
            Debug.LogWarning("[Progression] Animacionfinal no encontrada. Finalizando directamente por puntaje.");
            FinalizarMiniJuegoPorPuntaje();
        }
    }

    public void FinalizarPorAnimacion()
    {
        Debug.Log("[Progression] Animation Event recibido: finalizando mini-juego por puntaje.");
        FinalizarMiniJuegoPorPuntaje();
    }

    private void FinalizarMiniJuegoPorPuntaje()
    {
        PrepararFinDeJuegoVisual();
        ConsolidarIntentoSimplificado();

        var grm = FindAnyObjectByType<GameRoundManager>();
        if (grm != null && _playerCurrentScores != null && _playerCurrentScores.Length > 0)
        {
            Debug.Log("[Progression] Finalizando mini-juego con puntajes acumulados.");
            grm.FinalizeRoundFromScores(_playerCurrentScores);
        }
        else
        {
            Debug.LogWarning("[Progression] No se pudo usar GameRoundManager. Recargando escena actual como fallback.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void ForceApplyRepositionAll()
    {
        var all = Resources.FindObjectsOfTypeAll<ReposicionarPorTurno>();
        if (all == null || all.Length == 0) return;
        int idx = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        foreach (var r in all)
        {
            if (r == null) continue;
            if (r.gameObject.scene.IsValid())
            {
                r.ApplyForPlayerIndexInstant(idx);
            }
        }
        Debug.Log($"[Progression] ForceApplyRepositionAll aplicado a {all.Length} componentes (incluye inactivos). Jugador idx={idx}");
    }

    public void NotificarBolitaTocada()
    {
        _pendingSpawnOrigin = "Pickup";
        touchedBall = true;
        ConsolidarIntentoSimplificado();
        var smSingleton = SoundManager.instance;
        if (smSingleton != null)
        {
            smSingleton.PlaySfx("catapis:bolitatocada");
        }
        TerminarTurno();
        touchedBall = false;
    }

    public void PerderPorTocarSuelo()
    {
        _pendingSpawnOrigin = "GroundFail";
        _attemptScore = 0;
        RecalculateCurrentScoreOnly();
        SyncCurrentPlayerScoreAndUi();
        TerminarTurno();
    }

    private void ActualizarUiIntentos()
    {
        if (_attemptsLeft == null || TurnManager.instance == null) return;
        int idx = TurnManager.instance.GetCurrentPlayerIndex();
        if (idx < 0 || idx >= _attemptsLeft.Length) return;
        _ui?.ActualizarIntentosJugador(idx, _attemptsLeft[idx]);
    }

    private void ActualizarUiIntentos(bool inicializarTodos)
    {
        if (!inicializarTodos) { ActualizarUiIntentos(); return; }
        if (_attemptsLeft == null || _ui == null) return;
        for (int i = 0; i < _attemptsLeft.Length && i < 4; i++)
        {
            _ui.ActualizarIntentosJugador(i, _attemptsLeft[i]);
        }
    }

    private int[] CalcularGanadoresIndex1Based()
    {
        if (_playerCurrentScores == null || _playerCurrentScores.Length == 0)
            return new[] { 1 };

        long max = _playerCurrentScores[0];
        for (int i = 1; i < _playerCurrentScores.Length; i++)
        {
            if (_playerCurrentScores[i] > max)
                max = _playerCurrentScores[i];
        }

        List<int> ganadores = new List<int>();
        for (int i = 0; i < _playerCurrentScores.Length; i++)
        {
            if (_playerCurrentScores[i] == max)
                ganadores.Add(i + 1);
        }
        Debug.Log($"[Progression] Ganadores calculados (1-based): [{string.Join(",", ganadores)}] con scoreMax={max}");
        return ganadores.ToArray();
    }

    private void PrepararFinDeJuegoVisual()
    {
        BarraFuerza.SetGlobalShakeBlocked(true);

        if (botonListo != null) botonListo.SetActive(false);

        if (_barra == null) _barra = FindAnyObjectByType<BarraFuerza>();
        _barra?.OcultarUIBarra();

        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        _spawner?.DisableAll();

        if (_bolita == null) _bolita = FindAnyObjectByType<Bolita>();
        if (_bolita != null) _bolita.gameObject.SetActive(false);

        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        _ui?.MostrarTodosBotonesJugadores();

        Debug.Log("[Progression] Preparación visual de fin de juego aplicada (jacks deshabilitados, bolita oculta, botones resaltados, listo/barra ocultos).");
    }
}
