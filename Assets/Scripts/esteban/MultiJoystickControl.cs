using UnityEngine;
using System.Linq;

public class MultiJoystickControl : MonoBehaviour
{
    [Header("Joystick units (ordenados 0..3)")]
    public JoystickUnit[] units; // asigna en el inspector (cuatro)

    [Header("Joystick principal (punto de lanzamiento)")]
    public GameObject mainJoystickControlObject; // el GameObject con JoystickControl (se activará al final)

    public bool finished { get; private set; }
    private bool initialized = false; // <- bandera para correr solo una vez

    void Start()
    {
        finished = false;

        // desactivar joystick principal al inicio
        if (mainJoystickControlObject != null)
            mainJoystickControlObject.SetActive(false);
    }

    void Update()
    {
        // Esperar hasta que TurnManager esté listo
        if (!initialized && TurnManager.instance != null && TurnManager.instance.CurrentTurn() > 0)
        {
            initialized = true;

            // determinamos qué joystick queda bloqueado según el turno (CurrentTurn devuelve 1..N)
            int blockedIndex = Mathf.Clamp(TurnManager.instance.CurrentTurn() - 1, 0, units.Length - 1);

            // subscribir y resetear unidades
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] == null) continue;

                units[i].OnFinished += HandleUnitFinished;

                // si es el bloqueado lo dejamos inactivo, los demás activos
                bool shouldBeActive = (i != blockedIndex);
                units[i].ResetUnit(shouldBeActive);
            }
        }
    }

    void HandleUnitFinished(JoystickUnit unit)
    {
        // comprobamos si todas las unidades activas terminaron
        if (AllActiveFinished())
        {
            finished = true;
            Debug.Log("MultiJoystickManager: todos los joysticks activos terminaron.");

            // activamos joystick principal (punto de lanzamiento)
            if (mainJoystickControlObject != null)
                mainJoystickControlObject.SetActive(true);
        }
    }

    bool AllActiveFinished()
    {
        // todas las unidades que estaban activadas (IsActive==true) deben tener IsFinished==true
        foreach (var u in units)
        {
            if (u == null) continue;
            if (u.IsActive && !u.IsFinished) return false;
        }
        return true;
    }

    // --- utilidad: llamar al comenzar nueva ronda/turno ---
    public void PrepareForNextRound()
    {
        finished = false;

        // recalcular bloqueado y resetear
        int blockedIndex = Mathf.Clamp(TurnManager.instance.CurrentTurn() - 1, 0, units.Length - 1);

        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] == null) continue;
            bool shouldBeActive = (i != blockedIndex);
            units[i].ResetUnit(shouldBeActive);
        }

        if (mainJoystickControlObject != null)
            mainJoystickControlObject.SetActive(false);
    }
}
