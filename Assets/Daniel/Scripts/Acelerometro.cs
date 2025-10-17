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
    [Tooltip("Tiempo mínimo entre detecciones para evitar rebotes (s)")]
    [Range(0f, 0.6f)] public float cooldown = 0.15f;
    [Tooltip("Suavizado del filtrado de gravedad (0-1)")]
    [Range(0f, 1f)] public float smoothFactor = 0.12f;
    [Header("Umbrales (Landscape)")]
    [Tooltip("Magnitud mínima (en ejes de pantalla) para considerar movimiento válido")]
    [Range(0.02f, 1f)] public float directionThreshold = 0.22f;
    [Tooltip("Magnitud para aceptar de forma inmediata (saltando cualquier espera)")]
    [Range(0.02f, 1f)] public float instantThreshold = 0.35f;
    [Tooltip("Zona neutra para exigir volver a reposo entre pasos")]
    [Range(0.0f, 0.5f)] public float neutralDeadzone = 0.08f;
    [Tooltip("Requerir volver a zona neutra entre pasos de la secuencia")]
    public bool requireNeutralBetweenSteps = true;
    
    [Header("Fusión de sensores")]
    [Tooltip("Combinar giroscopio (gravity) con acelerómetro para una inclinación más estable")]
    public bool useGyroFusion = true;
    [Range(0f, 1f), Tooltip("Peso del giroscopio en la fusión (1 = solo gyro, 0 = solo acelerómetro)")]
    public float gyroWeight = 0.7f;
    
    [Header("Suavizado y histeresis del gesto")]
    [Tooltip("Suavizado adicional sobre el vector fusionado (0 = sin suavizar, 1 = sin respuesta)")]
    [Range(0f, 1f)] public float gestureSmoothing = 0.2f;
    [Tooltip("Relación para soltar/habilitar siguiente paso (release = threshold * hysteresisRatio). 0.6–0.8 recomendado")]
    [Range(0.2f, 0.95f)] public float hysteresisRatio = 0.7f;
    
    [Header("Orientación y plataforma")]
    [Tooltip("Mapear la aceleración a ejes de pantalla según la orientación (recomendado para Landscape)")]
    public bool mapToScreenOrientation = true;
    [Tooltip("Recalibrar automáticamente al cambiar a Landscape Left/Right")]
    public bool recalibrateOnOrientationChange = true;
    
    [Header("UI Panel")]
    [Tooltip("Panel (o Canvas) que contiene la UI del juego. Será activado al iniciar Play() y desactivado al terminar.")]
    public GameObject uiPanel;

    private Vector3 gravity;              // Low-pass en espacio orientado a pantalla
    private Vector3 linearAcc;            // Alta frecuencia (aceleración - base)
    private Vector3 calibratedGravity;    // Base en espacio orientado
    private bool calibrated = false;
    private DeviceOrientation _lastDeviceOrientation = DeviceOrientation.Unknown;

    private string currentState = "idle";
    private float cooldownTimer = 0f;
    private bool waitingNeutral = false;
    private Vector2 _fusedXYSmoothed = Vector2.zero;

    private string[] possibleDirs = { "up", "down", "left", "right" };
    private List<string> sequence = new List<string>();
    private int currentIndex = 0;
    private bool gameActive = false;
    private int startedPlayerIndex = -1;

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
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }
        gravity = GetOrientedAcceleration(Input.acceleration);
        _lastDeviceOrientation = Input.deviceOrientation;
        Calibrate();
    }

    // API pública unificada para iniciar el minijuego
    [ContextMenu("PlayMiniGamen")]
    public void PlayMiniGamen()
    {
        Play();
    }

    // 🔹 Llamar desde botón u otro script para iniciar el juego
    public void Play()
    {
        // Asegurar que este objeto y componente estén activos (por si la secuencia inicia con este minijuego)
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        if (!enabled)
            enabled = true;

        // Asegurar que la cadena de padres hasta el Canvas esté activa para que el panel sea visible
        EnsurePanelChainActive();

        // Activar panel e imágenes
        if (uiPanel != null)
            uiPanel.SetActive(true);

        if (sequenceImages != null)
        {
            foreach (var img in sequenceImages)
            {
                if (img != null)
                {
                    img.gameObject.SetActive(true);
                    img.enabled = true;
                    var c = img.color; c.a = 1f; img.color = c;
                }
            }
        }

        GenerateSequence();
        ShowSequence();
        ShowState("idle");

        // Refrescar layout tras mostrar la secuencia
        if (uiPanel != null)
        {
            var rt = uiPanel.GetComponent<RectTransform>();
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            Canvas.ForceUpdateCanvases();
        }

        // Guardar jugador que comenzó este minijuego (para detectar eliminación/cambio)
        startedPlayerIndex = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
    }

    private void EnsurePanelChainActive()
    {
        if (uiPanel == null) return;
        Transform t = uiPanel.transform;
        // Subir hasta la raíz y activar cada padre
        var chain = new System.Collections.Generic.List<Transform>();
        while (t != null)
        {
            chain.Add(t);
            t = t.parent;
        }

        for (int i = chain.Count - 1; i >= 0; i--)
        {
            var tr = chain[i];
            if (tr == null) continue;
            tr.gameObject.SetActive(true);
            var canvas = tr.GetComponent<Canvas>();
            if (canvas != null && !canvas.enabled) canvas.enabled = true;
            var cg = tr.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        if (mainImage != null) mainImage.enabled = true;
        if (statusText != null) statusText.gameObject.SetActive(true);

        // Forzar reconstrucción de layout por si venía colapsado
        var rt = uiPanel.GetComponent<RectTransform>();
        if (rt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        Canvas.ForceUpdateCanvases();
    }

    void Update()
    {
        if (!gameActive) return;

        // Asegurar que el panel siga activo durante el juego (por si otro script lo desactiva al inicio)
        if (uiPanel != null && (!uiPanel.activeSelf || !uiPanel.activeInHierarchy))
        {
            EnsurePanelChainActive();
            uiPanel.SetActive(true);
        }

        // Asegurar visibilidad de las imágenes de secuencia
        if (sequenceImages != null)
        {
            for (int i = 0; i < sequenceImages.Length; i++)
            {
                var img = sequenceImages[i];
                if (img == null) continue;
                if (!img.gameObject.activeSelf || !img.enabled)
                {
                    img.gameObject.SetActive(true);
                    img.enabled = true;
                    var c = img.color; c.a = 1f; img.color = c;
                }
            }
        }

        // Si el jugador que comenzó ya no es el actual (eliminación o cambio externo), abortar y limpiar
        if (startedPlayerIndex >= 0 && TurnManager.instance != null &&
            TurnManager.instance.GetCurrentPlayerIndex() != startedPlayerIndex)
        {
            StopGame();
            return;
        }

        // Recalibración si cambia la orientación del dispositivo a Landscape
        var devOri = Input.deviceOrientation;
        if (recalibrateOnOrientationChange && IsLandscape(devOri) && devOri != _lastDeviceOrientation)
        {
            _lastDeviceOrientation = devOri;
            Calibrate();
        }

        // 1) Actualizar sensores, orientar a pantalla y calcular componentes
        Vector2 fusedXY = UpdateSensorsAndGetFusedXY();
        // 2) Suavizado adicional del gesto para evitar brusquedad
        _fusedXYSmoothed = Vector2.Lerp(_fusedXYSmoothed, fusedXY, Mathf.Clamp01(1f - gestureSmoothing));

        // Tiempos
        float dt = Time.deltaTime;
        cooldownTimer -= dt;
        if (cooldownTimer < 0f) cooldownTimer = 0f;

        // Exigir regreso a neutro entre pasos, si está activado
        if (requireNeutralBetweenSteps && waitingNeutral)
        {
            float release = Mathf.Min(directionThreshold, instantThreshold) * hysteresisRatio;
            float magNeutral = Mathf.Max(Mathf.Abs(_fusedXYSmoothed.x), Mathf.Abs(_fusedXYSmoothed.y));
            if (magNeutral < Mathf.Max(neutralDeadzone, release))
            {
                waitingNeutral = false;
            }
            else
            {
                return; // aún no vuelve a neutro
            }
        }

        // Detección por umbral de magnitud y eje dominante
        float mag;
        string candidate = DetectDirection(new Vector3(_fusedXYSmoothed.x, _fusedXYSmoothed.y, 0f), out mag);
        if (cooldownTimer > 0f) return;
        if (candidate == "idle") return;

        if (mag >= instantThreshold || mag >= directionThreshold)
        {
            if (currentState != candidate)
                ShowState(candidate);
            cooldownTimer = cooldown;
            CheckDirection(candidate);
            if (requireNeutralBetweenSteps)
                waitingNeutral = true;
        }
    }

    // Actualiza lectura de sensores y devuelve el vector XY fusionado en ejes de pantalla
    private Vector2 UpdateSensorsAndGetFusedXY()
    {
        Vector3 rawAcc = Input.acceleration;
        Vector3 oriented = GetOrientedAcceleration(rawAcc);
        gravity = Vector3.Lerp(gravity, oriented, Mathf.Clamp01(smoothFactor));
        Vector3 baseGrav = calibrated ? calibratedGravity : gravity;
        linearAcc = oriented - baseGrav;

        // Gravedad del giroscopio orientada (si disponible)
        Vector3 gyroGravOriented = oriented;
        if (useGyroFusion && SystemInfo.supportsGyroscope)
        {
            gyroGravOriented = GetOrientedAcceleration(Input.gyro.gravity);
        }

        Vector2 accXY = new Vector2(linearAcc.x, linearAcc.y);
        Vector2 gyroXY = new Vector2(gyroGravOriented.x, gyroGravOriented.y);
        return useGyroFusion ? (gyroWeight * gyroXY + (1f - gyroWeight) * accXY) : accXY;
    }

    private string DetectDirection(Vector3 acc, out float magnitude)
    {
        float x = acc.x;
        float y = acc.y;
        float threshold = directionThreshold > 0f ? directionThreshold : (0.3f / Mathf.Max(0.01f, sensitivity));

        if (Mathf.Abs(x) < threshold && Mathf.Abs(y) < threshold)
        {
            magnitude = 0f;
            return "idle";
        }

        float ax = Mathf.Abs(x);
        float ay = Mathf.Abs(y);
        magnitude = Mathf.Max(ax, ay);
        if (ax > ay)
            return x > 0f ? "right" : "left";
        else
            return y > 0f ? "up" : "down";
    }

    public void Calibrate()
    {
        if (SystemInfo.supportsGyroscope)
            calibratedGravity = GetOrientedAcceleration(Input.gyro.gravity);
        else
            calibratedGravity = GetOrientedAcceleration(Input.acceleration);
        calibrated = true;
        Debug.Log("Acelerómetro calibrado.");
    }

    private bool IsLandscape(DeviceOrientation ori)
    {
        return ori == DeviceOrientation.LandscapeLeft || ori == DeviceOrientation.LandscapeRight;
    }

    // Mapea la aceleración cruda del dispositivo al sistema de ejes de la pantalla
    // para que "derecha" siempre sea +X de pantalla y "arriba" sea +Y, en Landscape.
    private Vector3 GetOrientedAcceleration(Vector3 acc)
    {
        if (!mapToScreenOrientation)
            return acc;

        // Preferir Screen.orientation cuando es Landscape explícito; de lo contrario usar deviceOrientation
        var so = Screen.orientation;
        var od = Input.deviceOrientation;

        // Si Screen.orientation no es explícito, usar deviceOrientation
    bool useDevice = (so == ScreenOrientation.AutoRotation);

        if (!useDevice)
        {
            switch (so)
            {
                case ScreenOrientation.LandscapeLeft:
                    // Eje X pantalla (derecha) = +Y del dispositivo; Eje Y pantalla (arriba) = -X
                    return new Vector3(acc.y, -acc.x, acc.z);
                case ScreenOrientation.LandscapeRight:
                    // Eje X pantalla = -Y; Eje Y pantalla = +X
                    return new Vector3(-acc.y, acc.x, acc.z);
                case ScreenOrientation.Portrait:
                    return acc;
                case ScreenOrientation.PortraitUpsideDown:
                    return new Vector3(-acc.x, -acc.y, acc.z);
                default:
                    return acc;
            }
        }
        else
        {
            switch (od)
            {
                case DeviceOrientation.LandscapeLeft:
                    return new Vector3(acc.y, -acc.x, acc.z);
                case DeviceOrientation.LandscapeRight:
                    return new Vector3(-acc.y, acc.x, acc.z);
                case DeviceOrientation.Portrait:
                    return acc;
                case DeviceOrientation.PortraitUpsideDown:
                    return new Vector3(-acc.x, -acc.y, acc.z);
                default:
                    return acc;
            }
        }
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

            if (currentIndex < sequence.Count)
            {
                sequenceImages[currentIndex].rectTransform.localScale = Vector3.one * 1.2f;
                statusText.text = "¡Bien! Siguiente...";
            }
            else
            {
                statusText.text = "🎉 ¡Completado!";
                gameActive = false;
                // 🔹 Limpieza estándar al finalizar
                ClearUI();
                onGameFinished?.Invoke();
            }
        }
        else
        {
            statusText.text = "❌ Fallaste. Inténtalo otra vez.";
        }
    }

    // Limpia y oculta la UI del minijuego
    private void ClearUI()
    {
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

        // Reset de estado interno
        sequence.Clear();
        currentIndex = 0;
        cooldownTimer = 0f;
        gameActive = false;
    }

    // Permite abortar/terminar el minijuego desde controladores externos (timeout, eliminación, etc.)
    public void StopGame()
    {
        ClearUI();
        // No invocar onGameFinished aquí para evitar dobles señales desde el controlador de secuencia
    }

    private void OnDisable()
    {
        // Asegurar limpieza si este componente se desactiva
        ClearUI();
    }
}
