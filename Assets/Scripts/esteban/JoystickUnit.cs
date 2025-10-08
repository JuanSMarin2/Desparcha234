using UnityEngine;
using UnityEngine.EventSystems;
using System;
using TMPro;

public class JoystickUnit : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI")]
    public RectTransform background;
    public RectTransform handle;
    public TextMeshProUGUI timerText;

    [SerializeField] private Vector3 initialTargetPos;

    [Header("Movimiento")]
    public Transform targetObject;
    public float moveRange = 3f;
    public float controlDuration = 3f;

    [Header("Opciones")]
    public bool startActive = true;

    public Action<JoystickUnit> OnFinished;

    [Header("Limites de movimiento del target")]
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

    [Header("Gizmos de editor")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color limitsColor = new Color(0f, 0.75f, 1f, 0.75f);
    [SerializeField] private Color limitsWireColor = new Color(0f, 0.5f, 1f, 1f);
    [SerializeField] private Color initialCrossColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float initialCrossSize = 0.3f;

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

        if (timer > 0f && !IsFinished)
        {
            timer += Time.deltaTime;

            float remaining = Mathf.Max(0, controlDuration - timer);

            if (timerText != null)
                timerText.text = remaining.ToString("F1");

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

        if (timerText != null)
            timerText.text = "0.0";

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

    void OnValidate()
    {
        if (useLimits)
        {
            if (maxLimits.x < minLimits.x) maxLimits.x = minLimits.x;
            if (maxLimits.y < minLimits.y) maxLimits.y = minLimits.y;
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 basePos = initialTargetPos;
        if (targetObject != null && initialTargetPos == default)
            basePos = targetObject.position;

        if (useLimits)
        {
            Vector3 minWorld = new Vector3(basePos.x + minLimits.x, basePos.y + minLimits.y, basePos.z);
            Vector3 maxWorld = new Vector3(basePos.x + maxLimits.x, basePos.y + maxLimits.y, basePos.z);

            Vector3 center = (minWorld + maxWorld) * 0.5f;
            Vector3 size = new Vector3(Mathf.Abs(maxWorld.x - minWorld.x), Mathf.Abs(maxWorld.y - minWorld.y), 0.01f);

            Color prev = Gizmos.color;

            Gizmos.color = limitsColor;
            Gizmos.DrawCube(center, size);

            Gizmos.color = limitsWireColor;
            Gizmos.DrawWireCube(center, size);

            Gizmos.color = prev;
        }

        {
            Color prev = Gizmos.color;
            Gizmos.color = initialCrossColor;

            float s = Mathf.Max(0.01f, initialCrossSize);
            Gizmos.DrawLine(basePos + new Vector3(-s, 0f, 0f), basePos + new Vector3(s, 0f, 0f));
            Gizmos.DrawLine(basePos + new Vector3(0f, -s, 0f), basePos + new Vector3(0f, s, 0f));

            Gizmos.color = prev;
        }
    }
}
