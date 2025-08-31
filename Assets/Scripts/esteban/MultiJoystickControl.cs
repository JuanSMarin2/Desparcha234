using UnityEngine;
using System.Linq;

public class MultiJoystickControl : MonoBehaviour
{
    [Header("Joystick units (ordenados 0..3)")]
    public JoystickUnit[] units; // asigna en el inspector (cuatro)

    [Header("Joystick principal (punto de lanzamiento)")]
    public GameObject mainJoystickControlObject; // el GameObject con JoystickControl (se activar� al final)

    public bool finished { get; private set; }

    void Start()
    {
        finished = false;

        // determinamos qu� joystick queda bloqueado seg�n el turno (CurrentTurn devuelve 1..N)
        int blockedIndex = Mathf.Clamp(TurnManager.instance.CurrentTurn() - 1, 0, units.Length - 1);

        // subscribir y resetear unidades
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] == null) continue;

            units[i].OnFinished += HandleUnitFinished;

            // si es el bloqueado lo dejamos inactivo, los dem�s activos
            bool shouldBeActive = (i != blockedIndex);
            units[i].ResetUnit(shouldBeActive);
        }

        // desactivar joystick principal hasta que terminen las unidades
        if (mainJoystickControlObject != null)
            mainJoystickControlObject.SetActive(false);
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
            // Si la unidad est� activa y no termin� -> a�n no
            if (u.IsActive && !u.IsFinished) return false;
        }
        return true;
    }

    // --- utilidad: llamar al comenzar nueva ronda/turno ---
    public void PrepareForNextRound()
    {
        finished = false;
        // recalcular bloqueado y resetear: (usa Start() logic o personal�zalo)
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
