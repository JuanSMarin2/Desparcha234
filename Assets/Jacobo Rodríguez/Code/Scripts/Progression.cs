using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class Progression : MonoBehaviour
{
    bool touchedBall = false;
    public int stage = 0;
    public int neededJacks = 0;
    private const int MaxStage = 3;

    public int jacksCounter = 0;
    public long currentScore = 0; // base + intento
    private long _baseScore = 0;   // consolidado del jugador
    private long _attemptScore = 0; // puntaje del intento actual
    private long[] _playerCurrentScores; // puntaje actual por jugador (para ranking)

    [Header("Configuración Etapas / Intentos")]
    [SerializeField] private bool respawnJacksEachStage = false;
    [SerializeField] private int attemptsPerPlayer = 2;
    private int[] _attemptsLeft;

    [Header("Referencias")]
    private UiManager _ui;
    private BarraFuerza _barra;
    private JackSpawner _spawner;
    private Bolita _bolita;

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
        ActualizarNeededJacks();

        int n = RoundData.instance != null ? Mathf.Max(0, RoundData.instance.numPlayers) : 0;
        if (n > 0)
        {
            _attemptsLeft = new int[n];
            for (int i = 0; i < n; i++) _attemptsLeft[i] = Mathf.Max(1, attemptsPerPlayer);
            _playerCurrentScores = new long[n]; // iniciar en 0; no escribir en RoundData.currentPoints
        }

        int idx0 = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : 0;
        _attemptScore = 0;
        _baseScore = (idx0 >= 0 && _playerCurrentScores != null && idx0 < _playerCurrentScores.Length) ? _playerCurrentScores[idx0] : 0;
        currentScore = _baseScore;
        if (_playerCurrentScores != null && idx0 >= 0 && idx0 < _playerCurrentScores.Length)
            _playerCurrentScores[idx0] = currentScore;

        _ui?.ActualizarPuntos(TurnManager.instance != null ? TurnManager.instance.CurrentTurn() : 1, currentScore);
        ActualizarUiIntentos(true);
    }

    private void ActualizarNeededJacks()
    {
        jacksCounter = 0;
        neededJacks = Mathf.Clamp(stage, 1, MaxStage);
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
        _spawner?.SpawnJacks();
    }

    public void NotificarBolitaTocada()
    {
        touchedBall = true;
        ConsolidarIntento();
        TerminarTurno();
        touchedBall = false;
    }
    public void OnballePendingThrow()
    {
        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        _spawner.DisableAll();
        Debug.Log("Disabling jacks because ball is pending throw");
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
        _bolita?.ReiniciarBola();
        _spawner?.DisableAll();

        if (TurnManager.instance != null)
        {
            int idx0 = TurnManager.instance.GetCurrentPlayerIndex();
            if (_attemptsLeft != null && idx0 >= 0 && idx0 < _attemptsLeft.Length)
            {
                _attemptsLeft[idx0] = Mathf.Max(0, _attemptsLeft[idx0] - 1);
                ActualizarUiIntentos();

                int totalRestantes = 0;
                for (int i = 0; i < _attemptsLeft.Length; i++) totalRestantes += _attemptsLeft[i];
                if (totalRestantes <= 0)
                {
                    FinalizarMinijuegoPorPuntaje();
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
        }

        stage = 1;
        ActualizarNeededJacks();

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
    }

    private void FinalizarMinijuegoPorPuntaje()
    {
        int n = _playerCurrentScores != null ? _playerCurrentScores.Length : (RoundData.instance != null ? RoundData.instance.numPlayers : 0);
        if (n <= 0) return;

        // Pasar scores directamente al GameRoundManager para que asigne puntos con empates
        var grm = GameRoundManager.instance != null ? GameRoundManager.instance : FindAnyObjectByType<GameRoundManager>();
        if (grm != null)
        {
            long[] scores = new long[n];
            for (int i = 0; i < n; i++) scores[i] = _playerCurrentScores[i];
            grm.FinalizeRoundFromScores(scores);
        }
    }

    public void Avanzaretapa()
    {
        stage++;
        if (stage > MaxStage) stage = MaxStage;
        ActualizarNeededJacks();
        _barra?.Reiniciar();
        if (_spawner != null)
        {
            if (respawnJacksEachStage) _spawner.SpawnJacks(); else _spawner.EnableAll();
        }
    }

    public void PerderPorTocarSuelo()
    {
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
