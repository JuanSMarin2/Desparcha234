using TMPro;
using UnityEngine;

public class Progression : MonoBehaviour
{
    bool touchedBall = false;
    public int stage = 0;
    public int neededJacks = 0;
    private const int MaxStage = 3;
    
    public int jacksCounter = 0;
    public long currentScore = 0; // Puntaje mostrado (base + intento)
    private long _baseScore = 0;   // Puntaje consolidado del jugador (persistente)
    private long _attemptScore = 0; // Puntaje del intento actual (se descarta si toca suelo)

    [Header("Configuración Etapas")] 
    [SerializeField] private bool respawnJacksEachStage = false; // true = respawnear; false = re‑habilitar existentes

    // Cached refs
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Inicializa el sistema de 5 etapas: etapa 1 requiere 1 jack, ... etapa 5 requiere 5 jacks
        if (stage < 1) stage = 1;
        if (stage > MaxStage) stage = MaxStage;
        ActualizarNeededJacks();

        // Sincronizar el score base con el acumulado del jugador actual en RoundData
        if (RoundData.instance != null && TurnManager.instance != null)
        {
            int idx0 = TurnManager.instance.GetCurrentPlayerIndex();
            if (idx0 >= 0 && idx0 < RoundData.instance.currentPoints.Length)
            {
                _baseScore = RoundData.instance.currentPoints[idx0];
            }
        }
        _attemptScore = 0;
        currentScore = _baseScore;
        _ui?.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
    }

    private void ActualizarNeededJacks()
    {
        // En este diseño, los neededJacks son iguales al número de etapa (clamp 1..5)
        jacksCounter = 0; // Reiniciar conteo de jacks al cambiar etapa
        neededJacks = Mathf.Clamp(stage, 1, MaxStage);
    }

    public void NotificarJackTocado(Jack jack)
    {
        if (jack != null)
        {
            jacksCounter++;
            // Sumar puntos SOLO al intento; se consolidan al finalizar intento si no toca suelo
            _attemptScore += jack.Puntos;
            currentScore = _baseScore + _attemptScore;
            Debug.Log($"Jack recolectado (+{jack.Puntos}). Total jacks: {jacksCounter}, Puntaje intento: {_attemptScore}, Puntaje mostrado: {currentScore}");
        }

        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        if (_ui != null && TurnManager.instance != null)
        {
            _ui.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
        }
        // Ya no se deshabilitan los jacks al alcanzar la cuota.
    }

    // Puede ser llamado cuando la bolita se lanza por primera vez o en cada etapa
    public void OnBallLaunched()
    {
        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        _spawner?.SpawnJacks();
    }

    public void NotificarBolitaTocada()
    {
        touchedBall = true;
        Debug.Log("Bolita tocada");

        // Cierre de intento por toque de bolita: consolidar puntaje del intento
        ConsolidarIntento();

        // Al tocar la bolita (fin del lanzamiento), validar progreso de etapa
        bool completoEtapa = jacksCounter == neededJacks;
        Debug.Log($"Validando etapa {stage}: jacks recolectados {jacksCounter}/{neededJacks} - Completo: {completoEtapa}");
        if (completoEtapa)
        {
            if (stage < MaxStage)
            {
                Avanzaretapa();
                Debug.Log($"Etapa {stage} completada. Avanzando a la siguiente etapa.");
            }
            else
            {
                // Pasó la última etapa -> terminar turno
                Debug.Log("Se completó la última etapa. Terminar turno.");
                jacksCounter = 0;
                TerminarTurno();
            }
        }
        else
        {
            // No consiguió los jacks necesarios -> terminar turno (pero el puntaje del intento ya fue consolidado)
            Debug.Log($"No se alcanzaron los jacks necesarios ({jacksCounter}/{neededJacks}). Terminar turno.");
            jacksCounter = 0;
            TerminarTurno();
        }

        touchedBall = false;
    }

    private void ConsolidarIntento()
    {
        if (_attemptScore <= 0) return;
        _baseScore += _attemptScore;
        _attemptScore = 0;
        currentScore = _baseScore;
        // Persistir en RoundData
        if (RoundData.instance != null && TurnManager.instance != null)
        {
            int idx0 = TurnManager.instance.GetCurrentPlayerIndex();
            if (idx0 >= 0 && idx0 < RoundData.instance.currentPoints.Length)
            {
                long nuevo = _baseScore;
                int nuevoInt = nuevo > int.MaxValue ? int.MaxValue : (nuevo < int.MinValue ? int.MinValue : (int)nuevo);
                RoundData.instance.currentPoints[idx0] = nuevoInt;
            }
        }
        // Refrescar UI
        _ui?.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
        Debug.Log($"Puntaje consolidado del jugador: {currentScore}");
    }

    public void TerminarTurno()
    {
        if (_barra != null)
        {
            _barra.Reiniciar();
            Debug.Log("Barra de fuerza reiniciada.");
        }
        if (_bolita != null)
        {
            _bolita.ReiniciarBola();
            Debug.Log("Bolita reiniciada.");
        }
        TurnManager.instance.NextTurn();

        // Resetear etapa para el siguiente jugador
        stage = 1;
        ActualizarNeededJacks();

        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        if (_spawner != null)
        {
            _spawner.SpawnJacks();
        }

        // Al cambiar de jugador, sincronizar y mostrar su puntaje acumulado (base)
        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        if (TurnManager.instance != null)
        {
            int idx0 = TurnManager.instance.GetCurrentPlayerIndex();
            _attemptScore = 0;
            _baseScore = 0;
            if (RoundData.instance != null && idx0 >= 0 && idx0 < RoundData.instance.currentPoints.Length)
            {
                _baseScore = RoundData.instance.currentPoints[idx0];
            }
            currentScore = _baseScore;
            _ui?.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
        }
        Debug.Log("Turno terminado. Reiniciando a etapa 1 para jugador " + TurnManager.instance.CurrentTurn());
    }

    public void Avanzaretapa()
    {
        stage++;
        if (stage > MaxStage) stage = MaxStage;
        ActualizarNeededJacks(); // ya pone jacksCounter = 0
        Debug.Log($"Avanzando a la etapa {stage}. Jacks necesarios: {neededJacks}");

        // Reiniciar barra (internamente reinicia bolita también)
        if (_barra == null) _barra = FindAnyObjectByType<BarraFuerza>();
        _barra?.Reiniciar();

        // Manejo de jacks
        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        if (_spawner != null)
        {
            if (respawnJacksEachStage)
                _spawner.SpawnJacks();
            else
                _spawner.EnableAll();
        }
        else
        {
            Debug.LogWarning("No se encontró JackSpawner para preparar la nueva etapa.");
        }
        // Ya no llamamos bolita.ReiniciarTurno() aquí porque Reiniciar() de la barra ya lo hace.
    }

    public void PerderPorTocarSuelo()
    {
        // Falló por tocar el suelo: descartar puntaje del intento y mantener el acumulado base
        _attemptScore = 0;
        currentScore = _baseScore; // mostrar solo lo consolidado previo
        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        if (_ui != null && TurnManager.instance != null)
        {
            _ui.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
        }
        Debug.Log("Falló por tocar el suelo. Puntaje del intento = 0. Se mantiene el acumulado base y se pasa al siguiente turno.");
        TerminarTurno();
    }
}
