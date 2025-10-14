using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;

public class AccelerometerGame : MonoBehaviour
{
    [Header("Imagen principal (actual)")]
    public Image mainImage;

    [Header("Panel de secuencia (5 imágenes)")]
    public Image[] sequenceImages;

    [Header("Sprites según dirección")]
    public Sprite idleSprite;
    public Sprite upSprite;
    public Sprite downSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Texto de estado (TextMeshPro)")]
    public TMP_Text statusText;

    [Header("Parámetros de detección")]
    [Range(0.2f, 3f)] public float sensitivity = 0.7f;
    public float holdTime = 0.1f;
    public float cooldown = 0.3f;
    public float smoothFactor = 0.1f;
    
    [Header("UI Panel")]
    [Tooltip("Panel (o Canvas) que contiene la UI del juego. Será activado al iniciar Play() y desactivado al terminar.")]
    public GameObject uiPanel;

    private Vector3 gravity;
    private Vector3 linearAcc;
    private Vector3 calibratedGravity;
    private bool calibrated = false;

    private string currentState = "idle";
    private string pendingState = null;
    private float pendingTime = 0f;
    private float cooldownTimer = 0f;
    private float delayTimer = 0f;

    private string[] possibleDirs = { "up", "down", "left", "right" };
    private List<string> sequence = new List<string>();
    private int currentIndex = 0;
    private bool gameActive = false;

    [Header("Events")]
    public UnityEvent onGameFinished;

    void Awake()
    {
        // 🔹 Al iniciar Unity (antes del Play), desactivar todo
        if (uiPanel != null)
            uiPanel.SetActive(false);

        if (sequenceImages != null)
        {
            foreach (var img in sequenceImages)
            {
                if (img != null)
                    img.gameObject.SetActive(false);
            }
        }
    }

    void Start()
    {
        gravity = Input.acceleration;
        Calibrate();
    }

    // 🔹 Llamar desde botón u otro script para iniciar el juego
    public void Play()
    {
        // Activar panel e imágenes
        if (uiPanel != null)
            uiPanel.SetActive(true);

        if (sequenceImages != null)
        {
            foreach (var img in sequenceImages)
            {
                if (img != null)
                    img.gameObject.SetActive(true);
            }
        }

        GenerateSequence();
        ShowSequence();
        ShowState("idle");
    }

    void Update()
    {
        if (!gameActive) return;

        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
            return;
        }

        Vector3 rawAcc = Input.acceleration;
        gravity = Vector3.Lerp(gravity, rawAcc, smoothFactor);
        Vector3 baseGrav = calibrated ? calibratedGravity : gravity;
        linearAcc = rawAcc - baseGrav;

        string candidate = DetectDirection(linearAcc);
        float dt = Time.deltaTime;
        cooldownTimer -= dt;
        if (cooldownTimer > 0f) return;

        if (candidate != pendingState)
        {
            pendingState = candidate;
            pendingTime = 0f;
        }
        else
        {
            pendingTime += dt;
            if (pendingTime >= holdTime)
            {
                if (currentState != candidate)
                {
                    ShowState(candidate);
                    cooldownTimer = cooldown;
                    CheckDirection(candidate);
                }
                pendingState = null;
                pendingTime = 0f;
            }
        }
    }

    private string DetectDirection(Vector3 acc)
    {
        float x = acc.x;
        float y = acc.y;
        float threshold = 0.3f / sensitivity;

        if (Mathf.Abs(x) < threshold && Mathf.Abs(y) < threshold)
            return "idle";

        if (Mathf.Abs(x) > Mathf.Abs(y))
        {
            if (x > threshold) return "right";
            else if (x < -threshold) return "left";
        }
        else
        {
            if (y > threshold) return "up";
            else if (y < -threshold) return "down";
        }

        return "idle";
    }

    public void Calibrate()
    {
        calibratedGravity = Input.acceleration;
        calibrated = true;
        Debug.Log("Acelerómetro calibrado.");
    }

    private void ShowState(string state)
    {
        currentState = state;

        switch (state)
        {
            case "up": mainImage.sprite = upSprite; break;
            case "down": mainImage.sprite = downSprite; break;
            case "left": mainImage.sprite = leftSprite; break;
            case "right": mainImage.sprite = rightSprite; break;
            default: mainImage.sprite = idleSprite; break;
        }
    }

    private void GenerateSequence()
    {
        sequence.Clear();
        for (int i = 0; i < 5; i++)
        {
            string dir = possibleDirs[Random.Range(0, possibleDirs.Length)];
            sequence.Add(dir);
        }
        currentIndex = 0;
        gameActive = true;
        statusText.text = "Mueve el dispositivo según la secuencia.";
    }

    private void ShowSequence()
    {
        for (int i = 0; i < sequenceImages.Length; i++)
        {
            if (i < sequence.Count)
                sequenceImages[i].sprite = GetSprite(sequence[i]);
            else
                sequenceImages[i].sprite = idleSprite;

            sequenceImages[i].rectTransform.localScale = Vector3.one * 0.8f;
        }

        if (sequenceImages.Length > 0)
            sequenceImages[0].rectTransform.localScale = Vector3.one * 1.2f;
    }

    private Sprite GetSprite(string dir)
    {
        switch (dir)
        {
            case "up": return upSprite;
            case "down": return downSprite;
            case "left": return leftSprite;
            case "right": return rightSprite;
            default: return idleSprite;
        }
    }

    private void CheckDirection(string input)
    {
        if (input == "idle") return;

        if (input == sequence[currentIndex])
        {
            // ✅ Correcto
            sequenceImages[currentIndex].rectTransform.localScale = Vector3.one;
            currentIndex++;
            delayTimer = 1f;

            if (currentIndex < sequence.Count)
            {
                sequenceImages[currentIndex].rectTransform.localScale = Vector3.one * 1.2f;
                statusText.text = "¡Bien! Siguiente...";
            }
            else
            {
                statusText.text = "🎉 ¡Completado!";
                gameActive = false;

                // 🔹 Al ganar, desactivar panel e imágenes
                if (uiPanel != null)
                    uiPanel.SetActive(false);

                if (sequenceImages != null)
                {
                    foreach (var img in sequenceImages)
                    {
                        if (img != null)
                            img.gameObject.SetActive(false);
                    }
                }
                onGameFinished?.Invoke();
            }
        }
        else
        {
            statusText.text = "❌ Fallaste. Inténtalo otra vez.";
        }
    }
}
