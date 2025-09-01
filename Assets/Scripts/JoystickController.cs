using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform joystickKnob;
    [SerializeField] private GameObject arrowObject;       
    [SerializeField] private Transform targetObject;

    [SerializeField] private Transform wheel0;
    [SerializeField] private Transform wheel1;
    [SerializeField] private Transform wheel2;
    [SerializeField] private Transform wheel3;

    [SerializeField] private Image Image;

    [SerializeField] private GameObject blocker;

    private RectTransform backgroundRect;
    private float joystickRadius;
    public bool IsDragging { get; private set; }

    public Vector2 inputVector { get; private set; }

    private int previusTurn;

    void Awake()
    {
        backgroundRect = GetComponent<RectTransform>();
    }

    void Start()
    {
        joystickRadius = backgroundRect.rect.width / 2;
        inputVector = Vector2.zero;

        if (arrowObject != null)
            arrowObject.SetActive(false);

        if(blocker != null)
            blocker.SetActive(false);
    }

    private void Update()
    {
        int currentTurn = TurnManager.instance.CurrentTurn();

        if(previusTurn != currentTurn)
        {
            DisableBlocker();
            arrowObject.transform.position = Vector3.zero;
        }
          



        switch (currentTurn)
        {
            case 1:
                transform.position = wheel0.position;
                Image.color = Color.red;
                wheel0.gameObject.SetActive(false);
                wheel1.gameObject.SetActive(true);
                wheel2.gameObject.SetActive(true);
                wheel3.gameObject.SetActive(true);
                break;

            case 2:
                transform.position = wheel1.position;
                Image.color = Color.blue;
                wheel0.gameObject.SetActive(true);
                wheel1.gameObject.SetActive(false);
                wheel2.gameObject.SetActive(true);
                wheel3.gameObject.SetActive(true);
                break;

            case 3:
                transform.position = wheel2.position;
                Image.color = Color.yellow;
                wheel0.gameObject.SetActive(true);
                wheel1.gameObject.SetActive(true);
                wheel2.gameObject.SetActive(false);
                wheel3.gameObject.SetActive(true);
                break;

            case 4:
                transform.position = wheel3.position;
                Image.color = Color.green;
                wheel0.gameObject.SetActive(true);
                wheel1.gameObject.SetActive(true);
                wheel2.gameObject.SetActive(true);
                wheel3.gameObject.SetActive(false);
                break;

            default:
                Debug.Log("Error turno no válido: " + currentTurn);
                break;
        }




        previusTurn = currentTurn;

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

            UpdateArrow();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsDragging = false;
        blocker.SetActive(true);

        joystickKnob.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;

        if (arrowObject != null)
            arrowObject.SetActive(false);
    }

    private void UpdateArrow()
    {
        if (arrowObject == null || targetObject == null) return;

        if (inputVector.magnitude > 0.1f)
        {
            arrowObject.SetActive(true);

            Vector2 oppositeDirection = inputVector.normalized;
            float angle = Mathf.Atan2(oppositeDirection.y, oppositeDirection.x) * Mathf.Rad2Deg;
            arrowObject.transform.rotation = Quaternion.Euler(0, 0, angle + 90f);

     
            arrowObject.transform.position = targetObject.position;
        }
        else
        {
            arrowObject.SetActive(false);
        }
    }

    public void DisableBlocker()
    {
        blocker.SetActive(false);
    }
}
