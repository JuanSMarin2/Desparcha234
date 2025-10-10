using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class JoystickControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick UI")]
    [SerializeField] private RectTransform joystickKnob;
    private RectTransform backgroundRect;
    private float joystickRadius;

    [Header("Activar al aparecer el joystick")]
    [SerializeField] private GameObject objectToActivateWhenEnabled; // asigna en el Inspector
    [SerializeField] private bool deactivateOnDisable = true; // si quieres que se apague cuando el joystick se oculte

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
    [SerializeField] private Image Image;       // El componente de la UI que muestras
    [SerializeField] private Sprite[] playerSprites; // Sprites para cada jugador
    [SerializeField] private GameObject blocker;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] objectPrefabs;   // Prefabs por turno
    [SerializeField] private Transform spawnPoint;         // Zona de tiro      

    [Header("Barra de Fuerza UI")]
    [SerializeField] private Image barraFuerza;            // Imagen UI (fillAmount)
    public float velocidadSubida = 1.5f;
    public float velocidadBajada = 0.5f;
    private float valorFuerza = 0f;
    private bool subiendo = true;

    [SerializeField] private BarraDeFuerza barraDeFuerza; // referencia al script de la barra

    // --- en tu clase JoystickControl ---
    [Header("Barra de Fuerza Offset")]
    [SerializeField] private Vector3 barraOffset = new Vector3(1f, 0f, 0f);
    // mueve la barra a la derecha por defecto

    [Header("Panels por jugador (opcional)")]
    [Tooltip("Paneles (0..3) que se activan según el jugador en turno. Asignar 4 GameObjects.")]
    [SerializeField] private GameObject[] playerTurnPanels;
    [Tooltip("Text dentro de cada panel para cambiar su color (opcional) y mostrar tiros.")]
    [SerializeField] private Text[] playerPanelTexts;
    [Tooltip("Color para el texto de cada panel (opcional).")]
    [SerializeField] private Color[] playerPanelTextColors;

    private bool lanzando = false; // evita dobles tiros mientras corre la corrutina
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

        // Validación útil: asegúrate de que el array tenga al menos la cantidad esperada
        if (playerTurnPanels != null && playerTurnPanels.Length < 4)
            Debug.LogWarning($"JoystickControl: playerTurnPanels length = {playerTurnPanels.Length}. Esperado 4. Revisa asignaciones en el inspector.");

        if (playerPanelTexts != null && playerPanelTexts.Length < 4)
            Debug.LogWarning($"JoystickControl: playerPanelTexts length = {playerPanelTexts.Length}. Esperado 4 si usas textos por jugador.");

        // Aseguramos estado inicial de panels por jugador
        UpdatePlayerPanels();
        RefreshTirosPanel();
    }

    private void Update()
    {
        // No permitir usar el joystick principal hasta que los mini-joysticks hayan terminado
        if (multiJoystick != null && !multiJoystick.finished)
            return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        int currentTurn = TurnManager.instance.CurrentTurn();

        if (previousTurn != currentTurn)
        {
            DisableBlocker();
            // actualizar UI cuando cambia el turno
            UpdatePlayerPanels();
            RefreshTirosPanel();
        }

        // Cambiar base según turno
        switch (currentTurn)
        {
            case 1:
                transform.position = wheel0.position;
                if (playerSprites != null && playerSprites.Length > 0) Image.sprite = playerSprites[0]; // Sprite del jugador 1
                break;
            case 2:
                transform.position = wheel1.position;
                if (playerSprites != null && playerSprites.Length > 1) Image.sprite = playerSprites[1]; // Sprite del jugador 2
                break;
            case 3:
                transform.position = wheel2.position;
                if (playerSprites != null && playerSprites.Length > 2) Image.sprite = playerSprites[2]; // Sprite del jugador 3
                break;
            case 4:
                transform.position = wheel3.position;
                if (playerSprites != null && playerSprites.Length > 3) Image.sprite = playerSprites[3]; // Sprite del jugador 4
                break;
        }

        previousTurn = currentTurn;

        UpdateTarget();
        UpdateBarraFuerza();
        SeguirSpawnPoint();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Solo permitir mover el centro si NO se está lanzando
        if (!lanzando)
        {
            // --- REAPARECER CENTRO SI FUE DESTRUIDO ---
            CentroController centro = FindObjectOfType<CentroController>();
            if (centro == null && multiJoystick != null && multiJoystick.centroPrefab != null)
            {
                GameObject centroObj = Instantiate(multiJoystick.centroPrefab,
                    multiJoystick.centroSpawnPoint != null ? multiJoystick.centroSpawnPoint.position : Vector3.zero,
                    Quaternion.identity);
                centro = centroObj.GetComponent<CentroController>();
            }
            if (centro != null)
            {
                SpriteRenderer sr = centro.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = true;
                Collider2D col = centro.GetComponent<Collider2D>();
                if (col != null) col.enabled = true;
                centro.MoverCentro();
            }
        }

        // Continúa con la lógica original
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
        if (lanzando) return; // protege de dobles taps mientras vuela el tejo

        IsDragging = false;
        joystickKnob.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;

        int currentTurn = TurnManager.instance.CurrentTurn();

        if (objectPrefabs != null && objectPrefabs.Length >= currentTurn && spawnPoint != null)
        {
            GameObject prefabToSpawn = objectPrefabs[currentTurn - 1];
            if (prefabToSpawn != null)
            {
                GameObject obj = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);

                // asignar dueño
                Tejo tejoScript = obj.GetComponent<Tejo>();
                if (tejoScript != null)
                {
                    tejoScript.jugadorID = currentTurn;
                }

                // registrar tiro en el GameManager (control centralizado de 3/3)
                GameManagerTejo.instance.RegistrarTejoLanzado();

                // usar el valor real de la barra visible y lanzar con "parábola falsa"
                float fuerza = (barraDeFuerza != null) ? barraDeFuerza.GetValorFuerza() : valorFuerza;
                StartCoroutine(MoverObjeto(obj, fuerza, tejoScript));
            }
        }
    }

    // Corrutina del “falso parabólico”: interpola posición + cambia escala + rehabilita colisión al final
    private IEnumerator MoverObjeto(GameObject obj, float valor, Tejo tejoScript)
    {
        lanzando = true;

        // ocultar la barra de fuerza mientras dura el lanzamiento
        if (barraFuerza != null)
            barraFuerza.gameObject.SetActive(false);

        Transform tejo = obj.transform;
        Collider2D col = obj.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Vector3 start = spawnPoint.position;
        float distanciaMax = 10f;
        float distancia = Mathf.Lerp(0.001f, distanciaMax, valor);
        Vector3 end = start + new Vector3(0f, 1f, 0f) * distancia;

        float duracion = 1.2f;
        float t = 0f;

        Vector3 escalaInicial = tejo.localScale;
        Vector3 escalaFinal = escalaInicial * 0.5f;

        while (t < 1f)
        {
            t += Time.deltaTime / duracion;

            // parábola simple: 4 * (t - t^2) para "arco" visual
            float altura = 4f * (t - t * t);
            tejo.position = new Vector3(start.x, Mathf.Lerp(start.y, end.y, t) + altura, start.z);

            tejo.localScale = Vector3.Lerp(escalaInicial, escalaFinal, t);

            yield return null;
        }

        if (col != null) col.enabled = true;

        

        // Espera 2 pasos de física para que se procesen colisiones/puntos antes de mover el centro
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Ahora sí, permitimos al Tejo reportar “me detuvo”
        if (tejoScript != null)
            tejoScript.HabilitarReporteAlDetenerse();

        lanzando = false;

        // reactivar la barra de fuerza sólo si el joystick sigue activo en la jerarquía
        if (barraFuerza != null && gameObject.activeInHierarchy)
            barraFuerza.gameObject.SetActive(true);
    }

    /// <summary>
    /// Actualiza el panel de tiros (lo llama GameManagerTejo y también OnEnable)
    /// Ahora escribe el texto en el panel del jugador que tiene el turno (si está configurado).
    /// </summary>
    public void RefreshTirosPanel()
    {
        if (GameManagerTejo.instance == null) return;

        int remaining = GameManagerTejo.instance.ShotsRemaining();

        int playerIdx = -1;
        if (TurnManager.instance != null)
            playerIdx = TurnManager.instance.GetCurrentPlayerIndex(); // 0-based

        // Si hay textos por panel configurados, escribir en el correspondiente
        if (playerPanelTexts != null && playerIdx >= 0 && playerIdx < playerPanelTexts.Length && playerPanelTexts[playerIdx] != null)
        {
            playerPanelTexts[playerIdx].text = $"Tiros: {remaining}";
            // aseguramos que el panel del jugador esté activo
            if (playerTurnPanels != null && playerIdx < playerTurnPanels.Length && playerTurnPanels[playerIdx] != null)
                playerTurnPanels[playerIdx].SetActive(true);
        }
        else
        {
            if (playerIdx < 0)
                Debug.LogWarning("JoystickControl.RefreshTirosPanel: TurnManager no disponible o no hay jugador en turno.");
        }
    }

    /// <summary>
    /// Activa el panel correspondiente al jugador en turno y desactiva los demás.
    /// También aplica color al Text del panel si se configuró.
    /// </summary>
    private void UpdatePlayerPanels()
    {
        if (playerTurnPanels == null || playerTurnPanels.Length == 0) return;

        int playerIdx = -1;
        if (TurnManager.instance != null)
            playerIdx = TurnManager.instance.GetCurrentPlayerIndex(); // 0-based

        if (playerIdx < 0)
        {
            // no hay jugador válido; desactivar todos
            DeactivateAllPlayerPanels();
            return;
        }

        for (int i = 0; i < playerTurnPanels.Length; i++)
        {
            GameObject panel = playerTurnPanels[i];
            if (panel == null) continue;

            // No depender del estado del joystick: si es el índice del jugador en turno, activar.
            bool shouldBeActive = (playerIdx == i);
            panel.SetActive(shouldBeActive);

            // aplicar color al texto del panel si existe
            if (playerPanelTexts != null && i < playerPanelTexts.Length && playerPanelTexts[i] != null)
            {
                Color colorToApply = Color.white;
                if (playerPanelTextColors != null && i < playerPanelTextColors.Length)
                    colorToApply = playerPanelTextColors[i];

                // forzamos alfa a 1 para evitar invisibilidad accidental
                colorToApply.a = 1f;
                playerPanelTexts[i].color = colorToApply;
            }

            // Si el panel no es el activo, opcionalmente limpiar su texto de tiros
            // if (!(playerIdx == i) && playerPanelTexts != null && i < playerPanelTexts.Length && playerPanelTexts[i] != null)
            //     playerPanelTexts[i].text = string.Empty;
        }
    }

    private void DeactivateAllPlayerPanels()
    {
        if (playerTurnPanels == null) return;
        foreach (var p in playerTurnPanels)
            if (p != null) p.SetActive(false);
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
            // Usa el offset configurable desde el Inspector
            Vector3 screenPos = Camera.main.WorldToScreenPoint(spawnPoint.position + barraOffset);
            barraFuerza.transform.position = screenPos;
        }
    }

    public void DisableBlocker()
    {
        if (blocker != null) blocker.SetActive(false);
    }

    void OnEnable()
    {
        RefreshTirosPanel();
        UpdatePlayerPanels();

        if (barraFuerza != null)
            barraFuerza.gameObject.SetActive(true);

        if (objectToActivateWhenEnabled != null)
            objectToActivateWhenEnabled.SetActive(true);
    }

    void OnDisable()
    {
        // desactivar panels por jugador cuando se desactiva el joystick
        DeactivateAllPlayerPanels();

        if (barraFuerza != null)
            barraFuerza.gameObject.SetActive(false);

        if (objectToActivateWhenEnabled != null && deactivateOnDisable)
            objectToActivateWhenEnabled.SetActive(false);
    }
}
