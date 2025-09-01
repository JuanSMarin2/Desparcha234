using UnityEngine;
using System.Linq;

public class MultiJoystickControl : MonoBehaviour
{
    [Header("Joystick units (ordenados 0..3)")]
    public JoystickUnit[] units;

    [Header("Joystick principal (punto de lanzamiento)")]
    public GameObject mainJoystickControlObject;

    [Header("Bloqueador del joystick")]
    public GameObject bloqueador; // arrástralo desde el inspector

    public bool finished { get; private set; }
    private bool initialized = false;

    void Start()
    {
        finished = false;
        if (mainJoystickControlObject != null)
            mainJoystickControlObject.SetActive(false);
    }

    // --- utilidad: llamar al comenzar nueva ronda/turno ---
    public void PrepareForNextRound()
    {
        // Destruir todos los proyectiles de la ronda anterior
        GameObject[] projectiles = GameObject.FindGameObjectsWithTag("Tejo");

        foreach (GameObject proj in projectiles)
        {
            Destroy(proj);
        }

        finished = false;

        int blockedIndex = Mathf.Clamp(TurnManager.instance.CurrentTurn() - 1, 0, units.Length - 1);

        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] == null) continue;
            bool shouldBeActive = (i != blockedIndex);
            units[i].ResetUnit(shouldBeActive);
        }

        if (mainJoystickControlObject != null)
            mainJoystickControlObject.SetActive(false);

        //  Aquí apagas el bloqueador para que no moleste en la nueva ronda
        if (bloqueador != null)
            bloqueador.SetActive(false);
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
        Debug.Log($"JoystickUnit {unit.name} terminó. Revisando si todos acabaron...");

        if (AllActiveFinished())
        {
            finished = true;
            Debug.Log(" MultiJoystickManager: todos los joysticks activos terminaron.");

            if (mainJoystickControlObject != null)
            {
                mainJoystickControlObject.SetActive(true);
                Debug.Log("¿Activo?: " + mainJoystickControlObject.activeSelf);
                Debug.Log(" Activado Joystick principal (zona de tiro).");
            }
        }
        else
        {
            Debug.Log(" Aún faltan joysticks por terminar.");
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
}
