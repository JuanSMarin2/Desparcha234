using TMPro;
using UnityEngine;

public class Progression : MonoBehaviour
{
    bool touchedBall = false;
    public int stage = 0;
    public int neededJacks = 0;
    private const int MaxStage = 3;
    
    public int jacksCounter = 0;
    public long currentScore = 0;

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
            currentScore += jack.Puntos;
            Debug.Log($"Jack recolectado (+{jack.Puntos}). Total jacks: {jacksCounter}, Puntos: {currentScore}");
        }

        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        if (_ui != null)
        {
            _ui.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
        }

        // Si ya alcanzó los jacks necesarios para la etapa actual, deshabilitar todos los jacks
        if (jacksCounter == neededJacks)
        {
            JackSpawner spawner = Object.FindFirstObjectByType<JackSpawner>();
            if (spawner != null)
            {
                spawner.DisableAll();
                Debug.Log("Se alcanzó la cantidad necesaria de jacks. Todos los jacks han sido deshabilitados.");
            }
        }
    }

    public void NotificarBolitaTocada()
    {
        touchedBall = true;
        Debug.Log("Bolita tocada");

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
                // Pasó la última etapa (5) -> terminar turno
                Debug.Log("Se completó la última etapa. Terminar turno.");
                jacksCounter = 0;
                TerminarTurno();
            }
        }
        else
        {
            // No consiguió los jacks necesarios -> terminar turno
            Debug.Log($"No se alcanzaron los jacks necesarios ({jacksCounter}/{neededJacks}). Terminar turno.");
            jacksCounter = 0;
            TerminarTurno();
        }

        touchedBall = false;
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
        currentScore = 0;
        jacksCounter = 0;

        if (_spawner == null) _spawner = FindAnyObjectByType<JackSpawner>();
        if (_spawner != null)
        {
            _spawner.SpawnJacks();
        }

        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        if (_ui != null)
        {
            _ui.ActualizarPuntos(TurnManager.instance.CurrentTurn(), 0);
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
        // El jugador falló por tocar el suelo: no se cuentan puntos finales
        jacksCounter = 0;
        currentScore = 0;
        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        if (_ui != null)
        {
            _ui.ActualizarPuntos(TurnManager.instance.CurrentTurn(), currentScore);
        }
        Debug.Log("Falló por tocar el suelo. Se reinicia puntaje y se pasa al siguiente turno.");
        TerminarTurno();
    }
}
