using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel de inicio: muestra el orden de los jugadores y un texto explicativo.
// Al pulsar el botón, arranca la secuencia (minijuegos + temporizador).
[DisallowMultipleComponent]
public class PreGameOrderPanel : MonoBehaviour
{
    [Header("Panel y UI")]
    [SerializeField, Tooltip("Panel raíz que se activa/desactiva para mostrar el orden de jugadores.")]
    private GameObject panel;
    [SerializeField, Tooltip("Texto explicativo del juego (TMP_Text).")]
    private TMP_Text descriptionText;
    [SerializeField, Tooltip("Botón para iniciar el juego.")]
    private Button startButton;

    [Header("Slots de orden (1º, 2º, 3º, 4º)")]
    [SerializeField, Tooltip("Cuatro imágenes en orden de posición: 1º, 2º, 3º, 4º.")]
    private Image[] orderSlots = new Image[4];

    [Header("Fuentes de sprite por índice de jugador")]
    [SerializeField, Tooltip("Imágenes en la escena que ya tienen asignados los sprites por índice (0-based). Se leerá su sprite (y opcionalmente su color) sin mover esos objetos.")]
    private Image[] playerSpriteSourcesByIndex;
    [SerializeField, Tooltip("Fallback opcional si no hay fuente: sprites por índice (0-based).")]
    private Sprite[] playerSpritesByIndex;
    [SerializeField, Tooltip("Copiar también el color de la imagen fuente cuando esté disponible.")]
    private bool copySourceColor = true;

    [Header("Secuencia y temporizador")]
    [SerializeField, Tooltip("Controlador de la secuencia que inicia los minijuegos y el Tempo.")]
    private GameSequenceController sequenceController;

    [Header("Ajustes")]
    [SerializeField, Tooltip("Mostrar el panel automáticamente al arrancar.")]
    private bool showOnAwake = true;
    [TextArea]
    [SerializeField, Tooltip("Texto por defecto si no se define en el inspector.")]
    private string defaultDescription =
        "Bienvenido. El orden de los jugadores es el siguiente.\n" +
        "Cada turno, el jugador actual intenta superar un minijuego antes de que el tiempo llegue a '¡¡Tango!!'.\n" +
        "Cuando estés listo, presiona Iniciar para comenzar.";

    private void Awake()
    {
        if (descriptionText != null && string.IsNullOrWhiteSpace(descriptionText.text))
        {
            descriptionText.text = defaultDescription;
        }
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            startButton.onClick.AddListener(OnStartClicked);
        }
    }

    private void OnEnable()
    {
        if (showOnAwake)
        {
            Show();
        }
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
        }
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
        // Asegurar que el temporizador no corra hasta que se presione Iniciar
        if (sequenceController != null && sequenceController.turnTimer != null)
        {
            sequenceController.turnTimer.StopTimer();
        }
        StopAllCoroutines();
        StartCoroutine(SetupWhenReady());
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    private IEnumerator SetupWhenReady()
    {
        // Espera breve a que TurnManager esté listo para leer el orden
        float waited = 0f;
        const float maxWait = 1.0f;
        while ((TurnManager.instance == null) && waited < maxWait)
        {
            waited += Time.deltaTime;
            yield return null;
        }

    int[] order = ResolveInitialOrderIndices();
    order = RotateOrderToCurrentStarter(order);
    ApplyOrderToSlots(order);
    }

    private void ApplyOrderToSlots(int[] order)
    {
        if (orderSlots == null || orderSlots.Length == 0) return;

        for (int i = 0; i < orderSlots.Length; i++)
        {
            var slot = orderSlots[i];
            if (slot == null) continue;

            int playerIdx = (order != null && i < order.Length) ? order[i] : -1;

            // Reset visual por seguridad
            slot.enabled = false;
            slot.sprite = null;
            slot.color = Color.white;

            // 1) Preferir fuentes de sprite existentes (Image) en la escena
            if (playerIdx >= 0 && playerSpriteSourcesByIndex != null && playerIdx < playerSpriteSourcesByIndex.Length)
            {
                var src = playerSpriteSourcesByIndex[playerIdx];
                if (src != null && src.sprite != null)
                {
                    slot.enabled = true;
                    slot.sprite = src.sprite;
                    if (copySourceColor) slot.color = src.color;
                    continue;
                }
            }

            // 2) Fallback a arreglo de sprites serializado
            if (playerIdx >= 0 && playerSpritesByIndex != null && playerIdx < playerSpritesByIndex.Length)
            {
                slot.enabled = true;
                slot.sprite = playerSpritesByIndex[playerIdx];
            }
        }
    }

    private void OnStartClicked()
    {
        StopAllCoroutines();
        StartCoroutine(BeginAfterFreeze());
    }

    // Intenta obtener el orden inicial de jugadores desde TurnManager
    // Si no existe API explícita, usa un orden por defecto [0,1,2,3] limitado por sprites.
    private int[] ResolveInitialOrderIndices()
    {
        // 1) Intentar método público conocido
        var tm = TurnManager.instance;
        if (tm != null)
        {
            // Intentar varios nombres comunes vía reflexión
            var methods = new string[]
            {
                "GetPlayerOrderIndices",
                "GetPlayersOrder",
                "GetActivePlayerIndices",
                "GetInitialOrder"
            };
            foreach (var mName in methods)
            {
                var m = tm.GetType().GetMethod(mName, BindingFlags.Public | BindingFlags.Instance);
                if (m != null)
                {
                    var result = m.Invoke(tm, null);
                    if (result is int[] arr && arr.Length > 0) return ClampToFour(arr);
                    if (result is List<int> list && list.Count > 0) return ClampToFour(list.ToArray());
                }
            }
        }

        // 2) Fallback a orden secuencial fijo (4 posiciones)
        int[] def = new int[4];
        for (int i = 0; i < 4; i++) def[i] = i;
        return def;
    }

    private int[] ClampToFour(int[] src)
    {
        int count = Mathf.Min(4, src.Length);
        int[] dst = new int[count];
        for (int i = 0; i < count; i++) dst[i] = Mathf.Max(-1, src[i]);
        return dst;
    }

    // Coloca al jugador que iniciará el juego en la primera posición del panel
    private int[] RotateOrderToCurrentStarter(int[] order)
    {
        if (order == null || order.Length == 0) return order;
        int starter = -1;
        if (TurnManager.instance != null)
        {
            // Si TurnManager expone el jugador inicial explícito, úsalo; si no, usa GetCurrentPlayerIndex
            var tm = TurnManager.instance;
            int idx = tm.GetCurrentPlayerIndex();
            if (idx >= 0) starter = idx;
        }
        if (starter < 0) return order;

        int pos = System.Array.IndexOf(order, starter);
        if (pos <= 0) return order; // ya es primero o no encontrado

        int n = Mathf.Min(4, order.Length);
        int[] rotated = new int[n];
        int k = 0;
        for (int i = 0; i < n; i++)
        {
            int srcIdx = (pos + i) % n;
            rotated[k++] = order[srcIdx];
        }
        return rotated;
    }

    [Header("Inicio con pausa")]
    [SerializeField, Tooltip("Segundos de pausa (tiempo real) tras pulsar Iniciar antes de comenzar la secuencia.")]
    private float startFreezeSeconds = 2f;

    private IEnumerator BeginAfterFreeze()
    {
        // Ocultar panel y congelar el tiempo del juego
        Hide();
        float prevScale = Time.timeScale;
        Time.timeScale = 0f;
        // Espera en tiempo real para que no dependa de timeScale
        float waited = 0f;
        float dur = Mathf.Max(0f, startFreezeSeconds);
        while (waited < dur)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        // Asegurar reanudación del tiempo a escala normal
        Time.timeScale = 1f;

        if (sequenceController != null)
        {
            // Iniciar temporizador solo cuando se presiona Iniciar
            if (sequenceController.turnTimer != null)
            {
                sequenceController.turnTimer.StopTimer();
                sequenceController.turnTimer.StartTimer();
            }
            sequenceController.PlaySequence();
        }
        else
        {
            Debug.LogWarning("PreGameOrderPanel: No hay GameSequenceController asignado.");
        }
    }
}
