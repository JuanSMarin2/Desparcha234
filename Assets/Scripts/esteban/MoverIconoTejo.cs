using UnityEngine;
using System.Collections;

public class MoverIconoTejo : MonoBehaviour
{
    [Header("Referencias de Paneles (solo informativas)")]
    public GameObject panelTutorial;
    public GameObject panelTurno;

    [Header("Iconos (orden jugador 1..4)")]
    [Tooltip("RectTransforms de cada icono (0 = jugador 1)")]
    public RectTransform[] iconRects;

    [Header("Movimiento Vertical")]
    [Tooltip("Distancia vertical del movimiento (en unidades locales)")]
    public float moveDistance = 160f;

    [Tooltip("Velocidad de movimiento (más alto = más rápido)")]
    public float moveSpeed = 1.5f;

    [Tooltip("Tiempo que permanece arriba antes de volver a su posición original")]
    public float holdTime = 0.25f;

    [Header("Iconos tristes")]
    public GameObject[] iconosTristes; // Solo se usa el registro, no posiciones manuales

    private Vector3[] originalPositions;

    void Awake()
    {
        if (iconRects != null)
        {
            originalPositions = new Vector3[iconRects.Length];
            for (int i = 0; i < iconRects.Length; i++)
                if (iconRects[i] != null)
                    originalPositions[i] = iconRects[i].localPosition;
        }
    }

    void OnEnable()
    {
        TutorialManagerTejo.OnPanelCerrado += OnPanelCerrado;
    }

    void OnDisable()
    {
        TutorialManagerTejo.OnPanelCerrado -= OnPanelCerrado;
    }

    private void OnPanelCerrado(int panelID)
    {
        Debug.Log($"[MoverIconoTejo] Panel cerrado con ID {panelID}. Ejecutando animaciones...");

        switch (panelID)
        {
            // paneles que muestran el jugador actual
            case 8:
            case 9:
            case 10:
            case 11:
                {
                    int jugadorTurno = TurnManager.instance != null ? TurnManager.instance.CurrentTurn() : 1;
                    MoverIconoPorJugador(jugadorTurno - 1);
                    break;
                }

            // paneles que muestran los demás jugadores
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 12:
                {
                    MostrarIconosRestantes();
                    break;
                }

            default:
                Debug.Log($"[MoverIconoTejo] Panel {panelID} no mapeado explícitamente. Sin acción.");
                break;
        }
    }

    // === MÉTODOS DE ANIMACIÓN ===

    public void MostrarIconosRestantes()
    {
        int current = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (iconRects == null || iconRects.Length == 0) return;

        for (int i = 0; i < iconRects.Length; i++)
        {
            if (i == current) continue;
            if (iconRects[i] != null)
                StartCoroutine(MoverIconoAnimado(i, true));
        }
    }

    public void OcultarIconosRestantes()
    {
        int current = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (iconRects == null || iconRects.Length == 0) return;

        for (int i = 0; i < iconRects.Length; i++)
        {
            if (i == current) continue;
            if (iconRects[i] != null)
                StartCoroutine(MoverIconoAnimado(i, false));
        }
    }

    public void MoverIconoPorJugador(int index)
    {
        if (iconRects == null || index < 0 || index >= iconRects.Length) return;
        StartCoroutine(MoverIconoAnimado(index, true));
    }

    private IEnumerator MoverIconoAnimado(int index, bool mostrar)
    {
        var rt = iconRects[index];
        if (rt == null) yield break;

        Vector3 startPos = originalPositions[index];
        float direction = (index == 0 || index == 3) ? 1f : -1f;
        Vector3 targetPos = startPos + Vector3.up * moveDistance * direction;

        if (mostrar)
        {
            yield return MoveBetween(rt, startPos, targetPos);
            yield return new WaitForSecondsRealtime(holdTime);
            yield return MoveBetween(rt, targetPos, startPos);
        }
        else
        {
            Vector3 downPos = startPos - Vector3.up * moveDistance * direction;
            yield return MoveBetween(rt, startPos, downPos);
            yield return new WaitForSecondsRealtime(holdTime);
            yield return MoveBetween(rt, downPos, startPos);
        }

        rt.localPosition = startPos;
    }

    private IEnumerator MoveBetween(RectTransform rt, Vector3 from, Vector3 to)
    {
        float duration = 1f / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            rt.localPosition = Vector3.Lerp(from, to, smoothT);
            yield return null;
        }

        rt.localPosition = to;
    }

    // === NUEVO MÉTODO UNIFICADO: mostrar y mover ícono triste usando el mismo sistema ===
    public void MostrarYMoverIconoTriste(int jugadorID)
    {
        if (iconosTristes == null || jugadorID < 0 || jugadorID >= iconosTristes.Length)
        {
            Debug.LogWarning($"[MoverIconoTejo] Índice inválido ({jugadorID}) o iconosTristes no configurados.");
            return;
        }

        GameObject icono = iconosTristes[jugadorID];
        if (icono == null)
        {
            Debug.LogWarning($"[MoverIconoTejo] El icono triste del jugador {jugadorID + 1} no está asignado.");
            return;
        }

        // Activar el icono triste
        icono.SetActive(true);

        // Mover usando el mismo sistema de movimiento vertical
        StartCoroutine(MoverIconoTriste(icono.transform, jugadorID));
    }

    private IEnumerator MoverIconoTriste(Transform icono, int jugadorID)
    {
        Vector3 startPos = icono.localPosition;
        float direction = (jugadorID == 0 || jugadorID == 3) ? 1f : -1f;
        Vector3 targetPos = startPos + Vector3.up * moveDistance * direction;

        // Subir
        yield return MoveBetweenTransform(icono, startPos, targetPos);
        yield return new WaitForSecondsRealtime(holdTime);

        // Bajar
        yield return MoveBetweenTransform(icono, targetPos, startPos);

        // Al final lo mantenemos activo (ya que puede permanecer triste)
        // Si quieres que desaparezca luego, puedes activar la siguiente línea:
        // icono.gameObject.SetActive(false);
    }

    private IEnumerator MoveBetweenTransform(Transform tr, Vector3 from, Vector3 to)
    {
        float duration = 1f / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            tr.localPosition = Vector3.Lerp(from, to, smoothT);
            yield return null;
        }

        tr.localPosition = to;
    }
}
