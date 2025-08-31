using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform joystickKnob;
    [SerializeField] private Transform targetObject; // El objeto que se moverá con el joystick
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    [SerializeField] private float minY = -3f;
    [SerializeField] private float maxY = 3f;   
    [SerializeField] private float moveRange = 3f;     // Rango máximo de movimiento en el mundo

    [SerializeField] private Transform wheel0;
    [SerializeField] private Transform wheel1;
    [SerializeField] private Transform wheel2;
    [SerializeField] private Transform wheel3;

    [SerializeField] private Image Image;
    [SerializeField] private GameObject blocker;

    [SerializeField] private Collider2D boardCollider;

    private RectTransform backgroundRect;
    private float joystickRadius;
    public bool IsDragging { get; private set; }
    public Vector2 inputVector { get; private set; }

    private int previusTurn;

    private MultiJoystickControl multiJoystick;

    void Awake()
    {
        backgroundRect = GetComponent<RectTransform>();
    }

    void Start()
    {
        multiJoystick = FindObjectOfType<MultiJoystickControl>();

        joystickRadius = backgroundRect.rect.width / 2;
        inputVector = Vector2.zero;

        if (blocker != null)
            blocker.SetActive(false);

        //  Desactivar el joystick al inicio hasta que termine MultiJoystickControl
        if (multiJoystick != null)
            gameObject.SetActive(false);
    }

    private void Update()
    {
        if (multiJoystick != null && !multiJoystick.finished)
            return; //  no hacemos nada mientras multiJoystick esté en curso

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true); //  activamos el joystick cuando ya terminó MultiJoystickControl
        }

        int currentTurn = TurnManager.instance.CurrentTurn();

        if (previusTurn != currentTurn)
            DisableBlocker();

        // Cambiar la posición base del joystick dependiendo del turno
        switch (currentTurn)
        {
            case 1:
                transform.position = wheel0.position;
                Image.color = Color.red;
                break;
            case 2:
                transform.position = wheel1.position;
                Image.color = Color.blue;
                break;
            case 3:
                transform.position = wheel2.position;
                Image.color = Color.yellow;
                break;
            case 4:
                transform.position = wheel3.position;
                Image.color = Color.green;
                break;
            default:
                Debug.Log("Error turno no valido: " + currentTurn);
                break;
        }

        previusTurn = currentTurn;

        // Actualizar movimiento del target
        UpdateTarget();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRect, eventData.position, eventData.pressEventCamera, out pos))
        {
            IsDragging = true;

            pos.x = (pos.x / backgroundRect.rect.width);
            pos.y = (pos.y / backgroundRect.rect.height);

            inputVector = new Vector2(pos.x * 2, pos.y * 2);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

            joystickKnob.anchoredPosition = new Vector2(inputVector.x * joystickRadius, inputVector.y * joystickRadius);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsDragging = false;
        blocker.SetActive(true);

        joystickKnob.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;
    }

    private void UpdateTarget()
    {
        if (targetObject == null) return;

        if (inputVector.magnitude > 0.1f)
        {
            // Movimiento incremental
            Vector3 delta = new Vector3(inputVector.x, inputVector.y, 0) * moveRange * Time.deltaTime;
            targetObject.position += delta;

            // Limitar dentro del área
            float clampedX = Mathf.Clamp(targetObject.position.x, minX, maxX);
            float clampedY = Mathf.Clamp(targetObject.position.y, minY, maxY);

            targetObject.position = new Vector3(clampedX, clampedY, targetObject.position.z);
        }
    }

    public void DisableBlocker()
    {
        blocker.SetActive(false);
    }
}
