using UnityEngine;

public class Progression : MonoBehaviour
{
    bool touchedBall = false;
    public int stage = 0;
    public int neededJacks = 0;
    private const int MaxStage = 5;

    public int jacksCounter = 0;
    public long currentScore = 0;

    private UiManager _ui;

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
        if (completoEtapa)
        {
            if (stage < MaxStage)
            {
                stage++; // Avanza a la siguiente etapa
                ActualizarNeededJacks();
                Debug.Log($"Etapa superada. Nueva etapa: {stage} (neededJacks = {neededJacks})");
                // Preparar conteo para la siguiente etapa
                jacksCounter = 0;
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
        BarraFuerza barra = FindAnyObjectByType<BarraFuerza>();
        if (barra != null)
        {
            barra.Reiniciar();
            Debug.Log("Barra de fuerza reiniciada.");
        }
        TurnManager.instance.NextTurn();
        JackSpawner spawner = Object.FindFirstObjectByType<JackSpawner>();
        spawner?.SpawnJacks(); // Reiniciar jacks para el nuevo turno
        currentScore = 0; // Reiniciar puntaje actual
        Debug.Log("Turno terminado. Puntaje actual reiniciado. El nuevo turno es para el jugador " + TurnManager.instance.CurrentTurn());
    }
}
