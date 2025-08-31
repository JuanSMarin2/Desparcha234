using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class JoystickUnit : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI")]
    public RectTransform background;  // RectTransform del fondo (este GameObject normalmente)
    public RectTransform handle;      // RectTransform del "knob" hijo

    [Header("Movimiento")]
    public Transform targetObject;    // El objeto (ficha) que moverá este joystick
    public float moveRange = 3f;      // Velocidad / rango
    public float controlDuration = 3f;// segundos que puede usarse

    [Header("Opciones")]
    public bool startActive = true;   // activado al Start si true

    public Action<JoystickUnit> OnFinished; // evento al terminar

    public Vector2 inputVector { get; private set; }
    public bool IsFinished { get; private set; }
    public bool IsActive { get; private set; }

    float joystickRadius;
    float timer;
    bool isDragging;

    void Awake()
    {
        if (background != null)
            joystickRadius = background.rect.width * 0.5f;
    }

    void Start()
    {
        ResetUnit(startActive);
    }

    void Update()
    {
        if (!IsActive || IsFinished) return;

        // mover el target según el input acumulado cada frame
        if (inputVector.magnitude > 0.01f && targetObject != null)
        {
            Vector3 delta = new Vector3(inputVector.x, inputVector.y, 0f) * moveRange * Time.deltaTime;
            targetObject.position += delta;
        }

        // si está usando el joystick (arrastrando o con input) contamos tiempo
        if (isDragging || inputVector.magnitude > 0.01f)
        {
            timer += Time.deltaTime;
            if (timer >= controlDuration)
                Finish();
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

        // activamos/desactivamos el objeto del joystick (visual + receptor de eventos)
        gameObject.SetActive(active);
    }

    public void Finish()
    {
        IsFinished = true;
        IsActive = false;
        inputVector = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;

        // avisamos antes de desactivar
        OnFinished?.Invoke(this);

        // ocultamos el joystick (opcional, el manager puede hacerlo también)
        gameObject.SetActive(false);
    }

    // --- eventos UI ---
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsActive || IsFinished) return;
        OnDrag(eventData);
        isDragging = true;
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
