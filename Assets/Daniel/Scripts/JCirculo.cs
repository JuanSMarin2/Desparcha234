using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;

public class CircleGameManagerUI : MonoBehaviour
{
    [Header("Prefabs y zona de aparición")]
    public GameObject circlePrefab;
    public RectTransform spawnArea;

    [Header("Configuración de aparición")]
    [Range(1, 10)] public int numberOfCircles = 5;
    [Tooltip("Distancia mínima entre círculos (en unidades UI).")]
    public float minDistance = 150f;
    [Tooltip("Número máximo de intentos para colocar un círculo sin solapamiento.")]
    public int maxPlacementAttempts = 30;

    private List<Vector2> usedPositions = new List<Vector2>();
    private List<CircleTarget> allCircles = new List<CircleTarget>();
    private List<CircleTarget> failedCircles = new List<CircleTarget>();

    private int currentIndex = 0;
    private bool gameActive = false;

    [Header("Events")]
    public UnityEvent onGameFinished;

    // 🔹 Llamar a esta función para iniciar el juego manualmente
    public void StartGame()
    {
        if (circlePrefab == null || spawnArea == null)
        {
            Debug.LogError("❌ Asigna el prefab y el área de aparición en el inspector.");
            return;
        }

        // Reinicia si ya había un juego
        foreach (var c in allCircles)
            if (c != null) Destroy(c.gameObject);

        allCircles.Clear();
        failedCircles.Clear();
        usedPositions.Clear();
        currentIndex = 0;

        SpawnCircles();
        ActivateNextCircle();
        gameActive = true;
    }

    void SpawnCircles()
    {
        Rect rect = spawnArea.rect;

        for (int i = 0; i < numberOfCircles; i++)
        {
            Vector2 pos = GetNonOverlappingPosition();
            GameObject circleObj = Instantiate(circlePrefab, spawnArea);
            RectTransform rt = circleObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;

            CircleTarget circle = circleObj.GetComponent<CircleTarget>();
            circle.Initialize(this, i);
            allCircles.Add(circle);
        }
    }

    Vector2 GetNonOverlappingPosition()
    {
        Rect rect = spawnArea.rect;
        Vector2 best = Vector2.zero;
        float bestMinDist = float.MaxValue;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            float x = Random.Range(rect.xMin, rect.xMax);
            float y = Random.Range(rect.yMin, rect.yMax);
            Vector2 candidate = new Vector2(x, y);

            float minDist = float.MaxValue;
            foreach (var p in usedPositions)
            {
                float d = Vector2.Distance(candidate, p);
                if (d < minDist) minDist = d;
            }

            if (minDist >= minDistance || usedPositions.Count == 0)
            {
                usedPositions.Add(candidate);
                return candidate;
            }

            if (minDist > bestMinDist)
            {
                best = candidate;
                bestMinDist = minDist;
            }
        }

        usedPositions.Add(best);
        return best;
    }

    public void OnCircleResult(CircleTarget circle, bool success)
    {
        if (!success)
            failedCircles.Add(circle);

        currentIndex++;

        if (currentIndex < allCircles.Count)
        {
            ActivateNextCircle();
        }
        else
        {
            if (failedCircles.Count > 0)
            {
                Debug.Log("🔁 Repitiendo los círculos fallados...");
                Invoke(nameof(RestartFailedCircles), 0.8f);
            }
            else
            {
                Debug.Log("🎉 ¡Juego completado!");
                gameActive = false;
                Invoke(nameof(ClearAllCircles), 0.6f); // 🔹 limpia los círculos al terminar
                onGameFinished?.Invoke();
            }
        }
    }

    void ActivateNextCircle()
    {
        foreach (var c in allCircles)
            c.SetActiveState(false);

        if (currentIndex < allCircles.Count)
            allCircles[currentIndex].SetActiveState(true);
    }

    void RestartFailedCircles()
{
    // 🔹 Destruye todos los círculos que no fallaron (los correctos)
    foreach (var c in allCircles)
    {
        if (c != null && !failedCircles.Contains(c))
            Destroy(c.gameObject);
    }

    // 🔹 Ahora solo trabajamos con los círculos fallados
    allCircles = new List<CircleTarget>(failedCircles);
    failedCircles.Clear();
    currentIndex = 0;

    // 🔹 Reinicia los fallados para un nuevo intento
    foreach (var c in allCircles)
        c.ResetForRetry();

    ActivateNextCircle();
}

    // 🔹 Nueva función: elimina todos los círculos al completar el juego
    void ClearAllCircles()
    {
        foreach (var c in allCircles)
        {
            if (c != null)
                Destroy(c.gameObject);
        }
        allCircles.Clear();
        Debug.Log("🧹 Todos los círculos eliminados. Fin del juego.");
    }
}
