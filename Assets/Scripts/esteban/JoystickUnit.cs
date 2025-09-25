using UnityEngine;
using UnityEngine.EventSystems;
using System;
using TMPro;  

public class JoystickUnit : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI")]
    public RectTransform background;
    public RectTransform handle;
    public TextMeshProUGUI timerText;   // <-- referencia al UI del contador

    [SerializeField] private Vector3 initialTargetPos;

    [Header("Movimiento")]
    public Transform targetObject;
    public float moveRange = 3f;
    public float controlDuration = 3f;

    [Header("Opciones")]
    public bool startActive = true;

    public Action<JoystickUnit> OnFinished;

    [Header("Límites de movimiento del target")]
    public bool useLimits = true;
    public Vector2 minLimits = new Vector2(-5f, -5f);
    public Vector2 maxLimits = new Vector2(5f, 5f);

    public Vector2 inputVector { get; private set; }
    public bool IsFinished { get; private set; }
    public bool IsActive { get; private set; }

    float joystickRadius;
    float timer;
    bool isDragging;

    public TutorialManagerTejo tutorialManagerTejo;

    void Awake()
    {
        if (background != null)
            joystickRadius = background.rect.width * 0.5f;
    }

    void Start()
    {
        ResetUnit(startActive);

        if (targetObject != null)
            initialTargetPos = targetObject.position;
    }

    void Update()
    {
        if (!IsActive || IsFinished) return;

        // mover target
        if (inputVector.magnitude > 0.01f && targetObject != null)
        {
            Vector3 delta = new Vector3(inputVector.x, inputVector.y, 0f) * moveRange * Time.deltaTime;
            targetObject.position += delta;

            if (useLimits)
            {
                float clampedX = Mathf.Clamp(targetObject.position.x, initialTargetPos.x + minLimits.x, initialTargetPos.x + maxLimits.x);
                float clampedY = Mathf.Clamp(targetObject.position.y, initialTargetPos.y + minLimits.y, initialTargetPos.y + maxLimits.y);
                targetObject.position = new Vector3(clampedX, clampedY, targetObject.position.z);
            }
        }

        // contador de tiempo
        if (timer > 0f && !IsFinished)
        {
            timer += Time.deltaTime;

            float remaining = Mathf.Max(0, controlDuration - timer);

            // mostrar tiempo en pantalla
            if (timerText != null)
                timerText.text = remaining.ToString("F1"); // con 1 decimal

            if (timer >= controlDuration)
            {
                Finish();
            }
        }
    }

    public void ResetUnit(bool active)
    {
        IsActive = active;
        IsFinished = false;
        timer = 0f;
        isDragging = false;
        inputVector = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;

        gameObject.SetActive(active);

        if (targetObject != null)
        {
            targetObject.gameObject.SetActive(active);

            if (active)
                targetObject.position = initialTargetPos;
        }

        // resetear texto del timer
        if (timerText != null)
            timerText.text = controlDuration.ToString("F1");
    }

    public void Finish()
    {
        IsFinished = true;
        IsActive = false;
        inputVector = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;

        OnFinished?.Invoke(this);

        gameObject.SetActive(false);

        // ocultamos el texto al acabar
        if (timerText != null)
            timerText.text = "0.0";

        // Reactivar el botón de reactivación del tutorial
        if (tutorialManagerTejo != null && tutorialManagerTejo.ContinuarButton != null)
            tutorialManagerTejo.ContinuarButton.interactable = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsActive || IsFinished) return;
        OnDrag(eventData);
        isDragging = true;

        if (timer == 0f)
            timer = 0.0001f;

        // Desactivar el botón de reactivación del tutorial
        if (tutorialManagerTejo != null && tutorialManagerTejo.ContinuarButton != null)
            tutorialManagerTejo.ContinuarButton.interactable = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsActive || IsFinished) return;
        if (background == null || handle == null) return;

        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out pos))
        {
            pos = Vector2.ClampMagnitude(pos, joystickRadius);
            handle.anchoredPosition = pos;
            inputVector = pos / joystickRadius;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsActive || IsFinished) return;
        isDragging = false;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;
    }
}
