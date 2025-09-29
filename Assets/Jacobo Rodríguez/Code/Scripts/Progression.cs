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

    // Nuevo: evento global para notificar que el turno avanzó.
    // Envía el índice del jugador actual (0-based).
    public static event Action<int> OnTurnAdvanced;

    private int _lastSpawnFrame = -1; // guard para evitar spawns duplicados en mismo frame
    private string _lastSpawnOrigin = ""; // info debug
    private string _pendingSpawnOrigin = "PendingThrow"; // origen que se usará para el próximo spawn cuando la bola quede pendiente

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
            _playerCurrentScores = new long[n]; // iniciar en 0; no escribir en RoundData.currentPoints
        }

        int idx0 = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : 0;
        _attemptScore = 0;
        _baseScore = 0;
        currentScore = 0;
        if (_playerCurrentScores != null && idx0 >= 0 && idx0 < _playerCurrentScores.Length)
            _playerCurrentScores[idx0] = currentScore;

        _ui?.ActualizarPuntos(TurnManager.instance != null ? TurnManager.instance.CurrentTurn() : 1, currentScore);
        ActualizarUiIntentos(true);

        // Notificar turno actual SOLO una vez
        if (TurnManager.instance != null)
        {
            int idx = TurnManager.instance.GetCurrentPlayerIndex();
            if (idx >= 0) OnTurnAdvanced?.Invoke(idx);
        }
        
        // Activar pausa pre-lanzamiento antes de spawnear
        if (!_preLaunchPaused)
        {
            ActivarPausaPreLanzamiento();
        }
        // Evitar que Time.timeScale quede pausado si se usa lógica distinta: dejamos el timeScale en 1 (el bloqueo es con flag)
        Time.timeScale = 1f;
        // Spawnear jacks iniciales en estado transparente: sólo si aún no se han spawneado
        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        SpawnearJacksTransparentes("Start"); // spawn inicial bajo bloqueo
    }

   

    public void NotificarJackTocado(Jack jack)
    {
        if (jack != null)
        {
            jacksCounter++;
            _attemptScore += jack.Puntos;
            currentScore = _baseScore + _attemptScore;
            if (TurnManager.instance != null && _playerCurrentScores != null)
            {
                int idx0 = TurnManager.instance.GetCurrentPlayerIndex();
                if (idx0 >= 0 && idx0 < _playerCurrentScores.Length) _playerCurrentScores[idx0] = currentScore;
            }
        }
        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        if (_ui != null && TurnManager.instance != null)
        {
            _ui.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
        }  
    }

    public void OnBallLaunched()
    {
        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        _spawner?.EnableJacks();
    }

   

    // Alias con ortografía correcta por si se usa desde otros lugares
    public void OnBallPendingThrow()
    {
        if (_bolita.Estado == Bolita.EstadoLanzamiento.PendienteDeLanzar) //Validacion de estado
        {
            // 1) Pausar primero para bloquear cualquier input hasta confirmar
            ActivarPausaPreLanzamiento();

            // 2) Spawnear jacks transparentes bajo la pausa (sin ventana de input abierta)
            var usedOrigin = _pendingSpawnOrigin;
            SpawnearJacksTransparentes(usedOrigin);

            // 3) Resetear origen por defecto para el siguiente ciclo
            _pendingSpawnOrigin = "PendingThrow";

            Debug.Log($"[Progression] OnBallPendingThrow -> Pause then Spawn (originUsed={usedOrigin})");
        }
    }

    public void SpawnearJacksTransparentes(string origin = "Unknown")
    {
        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        // Guard: evitar doble spawn en mismo frame (p.ej. Reiniciar + llamada redundante)
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
        // Time.timeScale = 0f; //// No se pausa porque caga las animaciones.
        Debug.Log("[Progression] Juego pausado esperando botonListo");
    }

    public void BotonListoConfirmar()
    {
        if (!_preLaunchPaused) return;
        _preLaunchPaused = false;
        if (botonListo != null) botonListo.SetActive(false);
        // Antes de desbloquear los shakes, limpiar estado y aplicar cooldown anti-lanzamiento accidental
        if (_barra == null) _barra = FindAnyObjectByType<BarraFuerza>();
        _barra?.PostResumeReset(0.3f);
        BarraFuerza.SetGlobalShakeBlocked(false);
        Time.timeScale = 1f;
        Debug.Log("[Progression] Reanudado tras botonListo (cooldown shakes aplicado)");
    }

    private void ConsolidarIntento()
    {
        if (_attemptScore <= 0) return;
        _baseScore += _attemptScore;
        _attemptScore = 0;
        currentScore = _baseScore;
        if (TurnManager.instance != null && _playerCurrentScores != null)
        {
            int idx0 = TurnManager.instance.GetCurrentPlayerIndex();
            if (idx0 >= 0 && idx0 < _playerCurrentScores.Length) _playerCurrentScores[idx0] = currentScore;
        }
        _ui?.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
    }

    public void TerminarTurno()
    {
        _barra?.Reiniciar();
        // Ya no destruimos aquí: ReiniciarBola llama a OnballePendingThrow que spawnea nuevos jacks.

        if (TurnManager.instance != null)
        {
            // Guardar índice previo para detectar wrap-around de ronda completa
            int idxPrev = TurnManager.instance.GetCurrentPlayerIndex();

            int idx0 = idxPrev;
            if (_attemptsLeft != null && idx0 >= 0 && idx0 < _attemptsLeft.Length)
            {
                _attemptsLeft[idx0] = Mathf.Max(0, _attemptsLeft[idx0] - 1);
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
                } while (safety > 0 && _attemptsLeft[TurnManager.instance.GetCurrentPlayerIndex()] <= 0);
            }
            else
            {
                TurnManager.instance.NextTurn();
            }

            // Detectar fin de vuelta completa (wrap a índice menor o igual)
            int idxActual = TurnManager.instance.GetCurrentPlayerIndex();
            if (idxActual <= idxPrev)
            {
                int prevStage = stage;
                stage = Mathf.Min(stage + 1, MaxStage);
                if (stage != prevStage)
                {
                    Debug.Log($"[Progression] Avance de etapa: {prevStage} -> {stage} (fin de vuelta)");
                }
            }

            // Notificar que el turno avanzó (índice 0-based del jugador actual)
            if (idxActual >= 0) OnTurnAdvanced?.Invoke(idxActual);

            _bolita?.ActualizarSpritePorTurno();
        }

        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        if (TurnManager.instance != null)
        {
            int idxNuevo = TurnManager.instance.GetCurrentPlayerIndex();
            _attemptScore = 0;
            _baseScore = (_playerCurrentScores != null && idxNuevo >= 0 && idxNuevo < _playerCurrentScores.Length) ? _playerCurrentScores[idxNuevo] : 0;
            currentScore = _baseScore;
            _ui?.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
            ActualizarUiIntentos();
        }

        // Forzar reposicionamiento incluso en objetos inactivos que tengan ReposicionarPorTurno
        ForceApplyRepositionAll();

        // Reiniciar explícitamente la bola UNA sola vez aquí
        _bolita?.ReiniciarBola(); // Esto disparará OnBallPendingThrow con guard de frame
    }


    // Nuevo: devuelve todos los ganadores (1-based) en caso de empate
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
                ganadores.Add(i + 1); // 1-based
        }
        Debug.Log($"[Progression] Ganadores calculados (1-based): [{string.Join(",", ganadores)}] con scoreMax={max}");
        return ganadores.ToArray();
    }

    private void PrepararFinDeJuegoVisual()
    {
        // Bloquear inputs por shake
        BarraFuerza.SetGlobalShakeBlocked(true);

        // Ocultar botón "Listo"
        if (botonListo != null) botonListo.SetActive(false);

        // Ocultar barra de fuerza por completo
        if (_barra == null) _barra = FindAnyObjectByType<BarraFuerza>();
        _barra?.OcultarUIBarra();

        // Limpiar todos los jacks (disable)
        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        _spawner?.DisableAll();

        // Ocultar la bolita para evitar interacción/visual residual
        if (_bolita == null) _bolita = FindAnyObjectByType<Bolita>();
        if (_bolita != null) _bolita.gameObject.SetActive(false);

        // Mostrar/"highlight" de todos los botones de jugador
        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        _ui?.MostrarTodosBotonesJugadores();

        Debug.Log("[Progression] Preparación visual de fin de juego aplicada (jacks deshabilitados, bolita oculta, botones resaltados, listo/barra ocultos).");
    }

    private void MostrarAnimacionGanadorYOtorgarCierre()
    {
        if (_endGameAnimationTriggered)
        {
            Debug.Log("[Progression] Animación de fin ya fue disparada; ignorando duplicado.");
            return;
        }
        _endGameAnimationTriggered = true;

        // Preparar fin de juego a nivel visual (ocultar UI y limpiar jacks)
        PrepararFinDeJuegoVisual();

        // Consolidar intento en curso por seguridad antes de mostrar ganador
        if (_attemptScore > 0)
        {
            _baseScore += _attemptScore;
            _attemptScore = 0;
            currentScore = _baseScore;
            if (TurnManager.instance != null && _playerCurrentScores != null)
            {
                int idx = TurnManager.instance.GetCurrentPlayerIndex();
                if (idx >= 0 && idx < _playerCurrentScores.Length)
                    _playerCurrentScores[idx] = currentScore;
            }
        }

        var ganadores = CalcularGanadoresIndex1Based();
        if (_animacionFinal == null) _animacionFinal = FindAnyObjectByType<Animacionfinal>();

        if (_animacionFinal != null)
        {
            Debug.Log($"[Progression] Mostrando animación de ganador(es) [{string.Join(",", ganadores)}]. Esperando Animation Event para finalizar.");
            _animacionFinal.AnimacionFinal(ganadores);
            // No finalizamos aquí; se llamará a FinalizarPorAnimacion() desde un Animation Event.
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
        // Preparar fin de juego visual por si no fue preparado aún (fallback)
        PrepararFinDeJuegoVisual();

        // Asegurar que el intento en curso quede consolidado para el jugador actual
        if (_attemptScore > 0)
        {
            _baseScore += _attemptScore;
            _attemptScore = 0;
            currentScore = _baseScore;
            if (TurnManager.instance != null && _playerCurrentScores != null)
            {
                int idx = TurnManager.instance.GetCurrentPlayerIndex();
                if (idx >= 0 && idx < _playerCurrentScores.Length)
                    _playerCurrentScores[idx] = currentScore;
            }
        }

        // Intentar finalizar la ronda con los puntajes acumulados
        var grm = FindAnyObjectByType<GameRoundManager>();
        if (grm != null && _playerCurrentScores != null && _playerCurrentScores.Length > 0)
        {
            Debug.Log("[Progression] Finalizando mini-juego con puntajes acumulados.");
            grm.FinalizeRoundFromScores(_playerCurrentScores);
        }
        else
        {
            // Fallback: recargar la escena actual si no hay gestor o puntajes
            Debug.LogWarning("[Progression] No se pudo usar GameRoundManager. Recargando escena actual como fallback.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void ForceApplyRepositionAll()
    {
        // Buscar también deshabilitados
        var all = Resources.FindObjectsOfTypeAll<ReposicionarPorTurno>();
        if (all == null || all.Length == 0) return;
        int idx = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        foreach (var r in all)
        {
            if (r == null) continue;
            // Evitar tocar prefabs (sin escena) o assets
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
        ConsolidarIntento();
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
        // Origen del siguiente spawn: fallo por tocar suelo
        _pendingSpawnOrigin = "GroundFail";
        _attemptScore = 0;
        currentScore = _baseScore;
        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        if (_ui != null && TurnManager.instance != null)
        {
            _ui.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
        }
        if (TurnManager.instance != null && _playerCurrentScores != null)
        {
            int idx0 = TurnManager.instance.GetCurrentPlayerIndex();
            if (idx0 >= 0 && idx0 < _playerCurrentScores.Length) _playerCurrentScores[idx0] = currentScore;
        }
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
}
