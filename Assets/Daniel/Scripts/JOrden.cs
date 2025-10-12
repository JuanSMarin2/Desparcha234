using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class JOrden: MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private RectTransform spawnParent;
    [SerializeField, Range(1, 5)] private int buttonCount = 3;
    [SerializeField] private bool restartOnFail = true;
    [SerializeField] private RectTransform spawnZone;

    [Header("Separación")]
    [SerializeField, Tooltip("Distancia mínima entre botones (en unidades UI).")]
    private float minDistance = 80f;

    private readonly List<ButtonClickRelay> buttons = new();
    private int nextExpected = 1;

    // ====== INICIAR SECUENCIA ======
    [ContextMenu("Start Sequence")]
    public void StartSequence()
    {
        ClearExisting();

        if (!buttonPrefab || !spawnParent)
        {
            Debug.LogError("BotonJugadpr: Falta asignar el prefab o el parent.");
            return;
        }

        buttonCount = Mathf.Clamp(buttonCount, 1, 5);
        List<Vector2> placedPositions = new();

        for (int i = 0; i < buttonCount; i++)
        {
            // Crear botón
            GameObject go = Instantiate(buttonPrefab, spawnParent);
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
                Debug.Log("🎉 ¡Victoria! Secuencia completada.");
                ClearExisting();
            }
        }
        else
        {
            Debug.Log("❌ Orden incorrecto.");
            if (restartOnFail) StartSequence();
        }
    }

    private void ClearExisting()
    {
        foreach (var b in buttons)
            if (b != null) Destroy(b.gameObject);
        buttons.Clear();
    }

    private void OnValidate()
    {
        buttonCount = Mathf.Clamp(buttonCount, 1, 5);
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
