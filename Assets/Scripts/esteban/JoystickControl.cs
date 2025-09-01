using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick UI")]
    [SerializeField] private RectTransform joystickKnob;
    private RectTransform backgroundRect;
    private float joystickRadius;

    [Header("Target movement")]
    [SerializeField] private Transform targetObject;
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    [SerializeField] private float minY = -3f;
    [SerializeField] private float maxY = 3f;
    [SerializeField] private float moveRange = 3f;

    [Header("Turn wheels")]
    [SerializeField] private Transform wheel0;
    [SerializeField] private Transform wheel1;
    [SerializeField] private Transform wheel2;
    [SerializeField] private Transform wheel3;

    [Header("Visuals")]
    [SerializeField] private Image Image;
    [SerializeField] private GameObject blocker;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] objectPrefabs;   // Prefabs para cada turno
    [SerializeField] private Transform spawnPoint;         // Zona de tiro      

    [Header("Barra de Fuerza UI")]
    [SerializeField] private Image barraFuerza;            // Imagen UI (fillAmount)
    public float velocidadSubida = 1.5f;
    public float velocidadBajada = 0.5f;
    private float valorFuerza = 0f;
    private bool subiendo = true;

    [SerializeField] private Collider2D boardCollider;

    public bool IsDragging { get; private set; }
    public Vector2 inputVector { get; private set; }

    private int previousTurn;
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
    }

    private void Update()
    {
        if (multiJoystick != null && !multiJoystick.finished)
            return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        int currentTurn = TurnManager.instance.CurrentTurn();

        if (previousTurn != currentTurn)
            DisableBlocker();

        // Cambiar base según turno
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
        }

        previousTurn = currentTurn;

        UpdateTarget();
        UpdateBarraFuerza();
        SeguirSpawnPoint();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRect, eventData.position, eventData.pressEventCamera, out Vector2 pos))
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

        int currentTurn = TurnManager.instance.CurrentTurn();

        // Instanciar objeto correspondiente al turno
        if (objectPrefabs != null && objectPrefabs.Length >= currentTurn && spawnPoint != null)
        {
            GameObject prefabToSpawn = objectPrefabs[currentTurn - 1];
            if (prefabToSpawn != null)
            {
                GameObject obj = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();

                if (rb != null)
                {
                    AplicarFuerza(rb, valorFuerza);
                }

                Debug.Log("Objeto lanzado con fuerza " + valorFuerza);
            }
            else
            {
                Debug.LogWarning("Prefab no asignado para el turno " + currentTurn);
            }
        }
        else
        {
            Debug.LogWarning("No se asignaron suficientes prefabs o falta el spawnPoint.");
        }
    }

    /// <summary>
    /// Aplica la fuerza calculada según la barra y un ángulo fijo (45° por ahora)
    /// </summary>
    private void AplicarFuerza(Rigidbody2D rb, float valor)
    {
        float fuerza = Mathf.Lerp(5f, 20f, valor); // rango min-max
        float angulo = 45f * Mathf.Deg2Rad;

        Vector2 dir = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
        rb.linearVelocity = dir.normalized * fuerza;
    }

    private void UpdateTarget()
    {
        if (targetObject == null) return;

        if (inputVector.magnitude > 0.1f)
        {
            Vector3 delta = new Vector3(inputVector.x, inputVector.y, 0) * moveRange * Time.deltaTime;
            targetObject.position += delta;

            float clampedX = Mathf.Clamp(targetObject.position.x, minX, maxX);
            float clampedY = Mathf.Clamp(targetObject.position.y, minY, maxY);

            targetObject.position = new Vector3(clampedX, clampedY, targetObject.position.z);
        }
    }

    private void UpdateBarraFuerza()
    {
        if (barraFuerza == null) return;

        if (subiendo)
        {
            valorFuerza += velocidadSubida * Time.deltaTime;
            if (valorFuerza >= 1f) { valorFuerza = 1f; subiendo = false; }
        }
        else
        {
            valorFuerza -= velocidadBajada * Time.deltaTime;
            if (valorFuerza <= 0f) { valorFuerza = 0f; subiendo = true; }
        }

        barraFuerza.fillAmount = valorFuerza;
    }

    private void SeguirSpawnPoint()
    {
        if (barraFuerza != null && spawnPoint != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(spawnPoint.position + Vector3.up * 1f); // un poco arriba
            barraFuerza.transform.position = screenPos;
        }
    }

    public void DisableBlocker()
    {
        blocker.SetActive(false);
    }
}
