using UnityEngine;

public class MultiJoystickControl : MonoBehaviour
{
    [Header("Joysticks")]
    [SerializeField] private JoystickControl joystick1;
    [SerializeField] private JoystickControl joystick2;
    [SerializeField] private JoystickControl joystick3;
    [SerializeField] private JoystickControl joystick4;

    [Header("Objetos a mover")]
    [SerializeField] private Transform target1;
    [SerializeField] private Transform target2;
    [SerializeField] private Transform target3;
    [SerializeField] private Transform target4;

    [Header("Configuración de movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float controlDuration = 3f; // Tiempo de control en segundos

    private float timer1 = 0f;
    private float timer2 = 0f;
    private float timer3 = 0f;
    private float timer4 = 0f;

    public bool finished = false; // Señal para JoystickControl

    private int blockedJoystick; // el joystick que no se puede usar en esta ronda

    private void Start()
    {
        
    }

    private void Update()
    {
        // Determinar qué joystick queda bloqueado según el turno actual
        blockedJoystick = TurnManager.instance.CurrentTurn();
        

        // Desactivar el joystick en la UI
        switch (blockedJoystick)
        {
            case 1: joystick1.gameObject.SetActive(false); target1.gameObject.SetActive(false); break;
            case 2: joystick2.gameObject.SetActive(false); target2.gameObject.SetActive(false); break;
            case 3: joystick3.gameObject.SetActive(false); target3.gameObject.SetActive(false); break;
            case 4: joystick4.gameObject.SetActive(false); target4.gameObject.SetActive(false); break;
        }

        if (finished) return;

        // --- Movimiento joystick 1 ---
        if (blockedJoystick != 1 && joystick1.inputVector.magnitude > 0.1f && timer1 < controlDuration)
        {
            Vector3 move = new Vector3(joystick1.inputVector.x, joystick1.inputVector.y, 0) * moveSpeed * Time.deltaTime;
            target1.position += move;
            timer1 += Time.deltaTime;
        }

        // --- Movimiento joystick 2 ---
        if (blockedJoystick != 2 && joystick2.inputVector.magnitude > 0.1f && timer2 < controlDuration)
        {
            Vector3 move = new Vector3(joystick2.inputVector.x, joystick2.inputVector.y, 0) * moveSpeed * Time.deltaTime;
            target2.position += move;
            timer2 += Time.deltaTime;
        }

        // --- Movimiento joystick 3 ---
        if (blockedJoystick != 3 && joystick3.inputVector.magnitude > 0.1f && timer3 < controlDuration)
        {
            Vector3 move = new Vector3(joystick3.inputVector.x, joystick3.inputVector.y, 0) * moveSpeed * Time.deltaTime;
            target3.position += move;
            timer3 += Time.deltaTime;
        }

        // --- Movimiento joystick 4 ---
        if (blockedJoystick != 4 && joystick4.inputVector.magnitude > 0.1f && timer4 < controlDuration)
        {
            Vector3 move = new Vector3(joystick4.inputVector.x, joystick4.inputVector.y, 0) * moveSpeed * Time.deltaTime;
            target4.position += move;
            timer4 += Time.deltaTime;
        }

        // Verificar si los 3 joysticks activos ya terminaron
        if (HasFinishedAllActiveJoysticks())
        {
            finished = true;
            Debug.Log("MultiJoystickControl terminado, ahora puede ejecutarse JoystickControl");
        }
    }

    private bool HasFinishedAllActiveJoysticks()
    {
        return
            (blockedJoystick == 1 || timer1 >= controlDuration) &&
            (blockedJoystick == 2 || timer2 >= controlDuration) &&
            (blockedJoystick == 3 || timer3 >= controlDuration) &&
            (blockedJoystick == 4 || timer4 >= controlDuration);
    }
}
