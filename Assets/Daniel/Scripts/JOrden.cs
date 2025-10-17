using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Collections;

public class JOrden: MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField, Tooltip("Prefabs por jugador según índice (0-based). Si está asignado y no es nulo en la posición del jugador actual, se usará este en lugar del prefab por defecto.")]
    private GameObject[] buttonPrefabsByPlayer;
    [SerializeField] private RectTransform spawnParent;
    [SerializeField, Range(1, 5)] private int buttonCount = 3;
    [SerializeField] private bool restartOnFail = true;
    [SerializeField] private RectTransform spawnZone;

    [Header("Separación")]
    [SerializeField, Tooltip("Distancia mínima entre botones (en unidades UI).")]
    private float minDistance = 80f;

    private readonly List<ButtonClickRelay> buttons = new();
    private int nextExpected = 1;
    [Header("Animación de guía")]
    [SerializeField, Tooltip("Activar pulso lento en el botón esperado.")]
    private bool pulseExpected = true;
    [SerializeField, Tooltip("Velocidad del pulso (ciclos/seg).")]
    private float pulseSpeed = 1.0f;
    [SerializeField, Tooltip("Escala mínima del pulso.")]
    private float pulseScaleMin = 0.9f;
    [SerializeField, Tooltip("Escala máxima del pulso.")]
    private float pulseScaleMax = 1.1f;
    private float pulsePhase = 0f;
    [Header("Events")]
    public UnityEvent onGameFinished;

    // ====== API pública para iniciar el minijuego ======
    [ContextMenu("PlayMiniGamen")]
    public void PlayMiniGamen()
    {
        StartSequence();
    }

    [ContextMenu("Play")]
    public void Play()
    {
        PlayMiniGamen();
    }

    // ====== INICIAR SECUENCIA ======
    [ContextMenu("Start Sequence")]
    public void StartSequence()
    {
        ClearExisting();

        if (!spawnParent)
        {
            Debug.LogError("JOrden: Falta asignar el parent para los botones (spawnParent).");
            return;
        }

        // Asegurar que TurnManager tenga un índice válido antes de resolver el prefab por jugador
        StartCoroutine(StartSequenceDeferred());
    }

    private IEnumerator StartSequenceDeferred()
    {
        float waited = 0f;
        while ((TurnManager.instance == null || TurnManager.instance.GetCurrentPlayerIndex() < 0) && waited < 1f)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        GameObject prefabToUse = ResolveButtonPrefab();

        if (!prefabToUse)
        {
            Debug.LogError("JOrden: No hay prefab válido (por jugador o por defecto).");
            yield break;
        }

        buttonCount = Mathf.Clamp(buttonCount, 1, 5);
        List<Vector2> placedPositions = new();

        for (int i = 0; i < buttonCount; i++)
        {
            // Crear botón
            GameObject go = Instantiate(prefabToUse, spawnParent);
            RectTransform rt = go.GetComponent<RectTransform>();
            Vector2 pos = GetNonOverlappingPosition(rt, placedPositions);
            rt.anchoredPosition = pos;
            placedPositions.Add(pos);

            // Texto del número
            TMP_Text label = go.GetComponentInChildren<TMP_Text>();
            if (label) label.text = (i + 1).ToString();

            // Configurar relay
            ButtonClickRelay relay = go.GetComponent<ButtonClickRelay>() ?? go.AddComponent<ButtonClickRelay>();
            relay.orderIndex = i + 1;
            relay.manager = this;
            buttons.Add(relay);
        }

        nextExpected = 1;
    }

    // ====== SELECCIÓN DE PREFAB POR JUGADOR ======
    private GameObject ResolveButtonPrefab()
    {
        int playerIdx = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (playerIdx >= 0 && buttonPrefabsByPlayer != null && playerIdx < buttonPrefabsByPlayer.Length)
        {
            var p = buttonPrefabsByPlayer[playerIdx];
            if (p != null) return p;
        }
        return buttonPrefab;
    }

    // ====== GENERADOR DE POSICIONES ======
    private Vector2 GetNonOverlappingPosition(RectTransform buttonRect, List<Vector2> placed)
    {
        Rect area = spawnZone ? spawnZone.rect : spawnParent.rect;
        float halfW = buttonRect.rect.width * 0.5f;
        float halfH = buttonRect.rect.height * 0.5f;

        Vector2 best = Vector2.zero;
        float bestMinDist = float.MinValue;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            float x = Random.Range(area.xMin + halfW, area.xMax - halfW);
            float y = Random.Range(area.yMin + halfH, area.yMax - halfH);
            Vector2 candidate = new(x, y);

            float minDist = float.MaxValue;
            foreach (var p in placed)
                minDist = Mathf.Min(minDist, Vector2.Distance(candidate, p));

            // Si cumple distancia mínima, úsalo
            if (minDist >= minDistance)
                return candidate;

            // Guardar mejor candidato si no cumple
            if (minDist > bestMinDist)
            {
                bestMinDist = minDist;
                best = candidate;
            }
        }

        // Si no se encuentra ninguno perfecto, usar el mejor
        return best;
    }

    // ====== EVENTOS ======
    public void HandleButtonPressed(ButtonClickRelay pressed)
    {
        if (pressed.orderIndex == nextExpected)
        {
            nextExpected++;
            if (nextExpected > buttonCount)
            {
                ClearExisting();
                onGameFinished?.Invoke();
            }
            pulsePhase = 0f; // reiniciar animación para el siguiente esperado
        }
        else
        {
            if (restartOnFail) PlayMiniGamen();
        }
    }

    private void ClearExisting()
    {
        foreach (var b in buttons)
            if (b != null) Destroy(b.gameObject);
        buttons.Clear();

        // Limpieza robusta: por si quedaron hijos sin registrar
        if (spawnParent != null)
        {
            var relays = spawnParent.GetComponentsInChildren<ButtonClickRelay>(true);
            foreach (var r in relays)
            {
                if (r != null) Destroy(r.gameObject);
            }
        }
    }

    private void OnValidate()
    {
        buttonCount = Mathf.Clamp(buttonCount, 1, 5);
    }

    // Finalizar/abortar minijuego por eventos externos (p.ej., fin de turno/timeout)
    public void StopGame()
    {
        ClearExisting();
        // No invocar onGameFinished aquí para evitar duplicidad con el controlador de secuencia
    }

    private void OnDisable()
    {
        // Asegurar limpieza si el componente se desactiva externamente
        ClearExisting();
    }

    private void Update()
    {
        if (!pulseExpected) return;
        if (nextExpected < 1 || nextExpected > buttons.Count) return;
        var relay = buttons[nextExpected - 1];
        if (relay == null || relay.gameObject == null) return;
        var rt = relay.GetComponent<RectTransform>();
        if (rt == null) rt = relay.GetComponentInChildren<RectTransform>();
        if (rt == null) return;

        pulsePhase += Time.deltaTime * Mathf.Max(0f, pulseSpeed) * Mathf.PI * 2f;
        float s = Mathf.Lerp(pulseScaleMin, pulseScaleMax, (Mathf.Sin(pulsePhase) + 1f) * 0.5f);
        rt.localScale = new Vector3(s, s, 1f);
    }
}


// ======================================================
// Componente Relay para detectar clics en los botones
// ======================================================
public class ButtonClickRelay : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public int orderIndex;
    [HideInInspector] public JOrden manager;

    public void OnPointerClick(PointerEventData eventData)
    {
        gameObject.SetActive(false);
        manager?.HandleButtonPressed(this);
    }
}
