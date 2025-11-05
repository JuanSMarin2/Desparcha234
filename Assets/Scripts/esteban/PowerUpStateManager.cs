using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PowerUpStateManager
/// - Registra qué papeletas tienen un power-up asignado durante el turno actual.
/// - Evita recoger otro power-up (p. ej. Shield o Ghost) hasta el próximo turno.
/// - Se limpia automáticamente en cambio de turno (observando TurnManager.CurrentTurn()).
/// </summary>
public class PowerUpStateManager : MonoBehaviour
{
    public static PowerUpStateManager Instance { get; private set; }

    private readonly HashSet<Transform> holders = new HashSet<Transform>();
    private int lastObservedTurn = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // opcional si cambias de escena
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (TurnManager.instance != null)
            lastObservedTurn = TurnManager.instance.CurrentTurn();
    }

    private void Update()
    {
        if (TurnManager.instance == null) return;

        int current = TurnManager.instance.CurrentTurn();
        if (lastObservedTurn != -1 && current != lastObservedTurn)
        {
            ClearAll();
        }
        lastObservedTurn = current;
    }

    public bool CanPickup(Transform papeleta)
    {
        if (papeleta == null) return false;
        return !holders.Contains(papeleta);
    }

    public void MarkHasPowerUp(Transform papeleta)
    {
        if (papeleta == null) return;
        holders.Add(papeleta);
    }

    public void Unmark(Transform papeleta)
    {
        if (papeleta == null) return;
        holders.Remove(papeleta);
    }

    public void ClearAll()
    {
        holders.Clear();
    }
}
