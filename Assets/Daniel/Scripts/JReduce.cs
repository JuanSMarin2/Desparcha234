using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class BotonReducible : MonoBehaviour
{
    [Header("Configuración general")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private RectTransform spawnParent;
    [SerializeField, Range(1, 10)] private int buttonCount = 5;

    [Header("Tamaño del botón")]
    [SerializeField] private float startSize = 70f;
    [SerializeField] private float minSize = 20f;
    [SerializeField] private float sizeReduction = 10f;

    [Header("Espaciado entre botones")]
    [SerializeField] private float minDistance = 80f;
    [SerializeField] private int maxPlacementAttempts = 40;

    private List<ButtonPressRelay> buttons = new List<ButtonPressRelay>();

    [ContextMenu("Iniciar Juego")]
    public void StartGame()
    {
        ClearExisting();

        if (buttonPrefab == null || spawnParent == null)
        {
            Debug.LogError("Asigna el prefab y el spawnParent en el inspector.");
            return;
        }

        List<Vector2> placed = new List<Vector2>();

        for (int i = 0; i < buttonCount; i++)
        {
            GameObject go = Instantiate(buttonPrefab, spawnParent);
            RectTransform rt = go.GetComponent<RectTransform>();
            TMP_Text text = go.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = "PRESIONA";

            // Posición no solapada
            Vector2 pos = GetNonOverlappingPosition(placed);
            rt.anchoredPosition = pos;
            placed.Add(pos);

            // Tamaño inicial
            rt.sizeDelta = new Vector2(startSize, startSize);

            // Configurar relay
            ButtonPressRelay relay = go.GetComponent<ButtonPressRelay>();
            if (relay == null) relay = go.AddComponent<ButtonPressRelay>();

            relay.manager = this;
            relay.rt = rt;
            relay.text = text;
            relay.currentSize = startSize;
            relay.minSize = minSize;
            relay.sizeReduction = sizeReduction;
            relay.startTextSize = text != null ? text.fontSize : 20f;

            buttons.Add(relay);
        }
    }

    private Vector2 SampleLocalPosition()
    {
        Rect r = spawnParent.rect;
        float x = Random.Range(r.xMin, r.xMax);
        float y = Random.Range(r.yMin, r.yMax);
        return new Vector2(x, y);
    }

    private Vector2 GetNonOverlappingPosition(List<Vector2> placed)
    {
        Vector2 best = SampleLocalPosition();
        float bestMinDist = 0f;

        for (int i = 0; i < maxPlacementAttempts; i++)
        {
            Vector2 candidate = SampleLocalPosition();
            float minDist = float.MaxValue;

            foreach (var p in placed)
                minDist = Mathf.Min(minDist, Vector2.Distance(candidate, p));

            if (placed.Count == 0 || minDist >= minDistance)
                return candidate;

            if (minDist > bestMinDist)
            {
                best = candidate;
                bestMinDist = minDist;
            }
        }

        return best;
    }

    public void NotifyButtonRemoved(ButtonPressRelay relay)
    {
        buttons.Remove(relay);

        if (buttons.Count == 0)
        {
            Debug.Log("🎉 ¡Ganaste! Todos los botones desaparecieron.");
            // Aquí puedes mostrar una UI de victoria, animación, etc.
        }
    }

    private void ClearExisting()
    {
        foreach (var b in buttons)
            if (b != null) Destroy(b.gameObject);
        buttons.Clear();
    }
}

public class ButtonPressRelay : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public BotonReducible manager;
    [HideInInspector] public RectTransform rt;
    [HideInInspector] public TMP_Text text;
    [HideInInspector] public float currentSize;
    [HideInInspector] public float minSize;
    [HideInInspector] public float sizeReduction;
    [HideInInspector] public float startTextSize;

    public void OnPointerClick(PointerEventData eventData)
    {
        currentSize -= sizeReduction;
        currentSize = Mathf.Max(currentSize, 0);
        rt.sizeDelta = new Vector2(currentSize, currentSize);

        // Escalar texto proporcionalmente al tamaño del botón
        if (text != null)
        {
            float t = Mathf.InverseLerp(minSize, manager != null ? manager.GetStartSize() : 70f, currentSize);
            text.fontSize = Mathf.Lerp(startTextSize * 0.4f, startTextSize, t);
        }

        if (currentSize <= minSize)
        {
            manager.NotifyButtonRemoved(this);
            Destroy(gameObject);
        }
    }
}

// --- Extensión auxiliar para obtener el tamaño inicial desde el manager ---
public static class BotonReducibleExtensions
{
    public static float GetStartSize(this BotonReducible manager)
    {
        var field = typeof(BotonReducible).GetField("startSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (float)field.GetValue(manager);
    }
}
