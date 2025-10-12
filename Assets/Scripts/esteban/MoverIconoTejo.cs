using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MoverIconoTejo : MonoBehaviour
{
    public enum TriggerMode { OnTurnAdvanced, OnPreLaunch, Manual }

    [Header("Iconos (orden jugador 1..4)")]
    [Tooltip("RectTransforms de cada icono (0 = jugador 1)")]
    public RectTransform[] iconRects;

    [Header("Movimiento")]
    public float moveDistance = 40f;
    public float moveSpeed = 6f;
    public float holdTime = 0.25f;

    [Header("Activación")]
    public TriggerMode trigger = TriggerMode.OnTurnAdvanced;
    private bool systemActive = true;

    [Header("Sprites por situación (opcional)")]
    [Tooltip("Cada SituationSet tiene un key y sprites por jugador (0..3).")]
    public SituationSet[] situations;

    [System.Serializable]
    public class SituationSet
    {
        public string key; // ej: "prelaunch", "caught", "groundfail"
        public Sprite[] spritesByPlayer = new Sprite[4];
    }

    private Vector3[] originalPositions;
    private bool isMoving;
    private int lastMovedPlayer = -1;
    private Coroutine moveCo;

    void Awake()
    {
        if (iconRects != null)
        {
            originalPositions = new Vector3[iconRects.Length];
            for (int i = 0; i < iconRects.Length; i++)
                if (iconRects[i] != null) originalPositions[i] = iconRects[i].localPosition;
        }
    }

    void Start()
    {
        // Lanzar rutina que espera a que termine el tutorial y luego mueve los iconos de los jugadores que no tienen el turno
        StartCoroutine(CoWaitForTutorialThenMove());
    }

    void OnEnable()
    {
        if (trigger == TriggerMode.OnTurnAdvanced || trigger == TriggerMode.OnPreLaunch)
            Progression.OnTurnAdvanced += HandleOnTurnAdvanced;
    }

    void OnDisable()
    {
        Progression.OnTurnAdvanced -= HandleOnTurnAdvanced;
    }

    private void HandleOnTurnAdvanced(int playerIndex)
    {
        if (!systemActive) return;
        if (playerIndex == lastMovedPlayer) return;
        lastMovedPlayer = playerIndex;
        // En modo OnPreLaunch se reutiliza este handler; otros sistemas pueden llamar TriggerMoveForCurrentPlayer cuando convenga.
        StartMoveForPlayer(playerIndex);
    }

    public void SetSystemActive(bool active) => systemActive = active;

    public void TriggerMoveForCurrentPlayer()
    {
        int idx = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (idx >= 0) StartMoveForPlayer(idx);
    }

    public void TriggerMoveForPlayer(int playerIndexZeroBased) => StartMoveForPlayer(playerIndexZeroBased);

    private void StartMoveForPlayer(int index)
    {
        if (iconRects == null || index < 0 || index >= iconRects.Length) return;
        if (isMoving) return;
        moveCo = StartCoroutine(MoveIconCoroutine(index));
    }

    private IEnumerator MoveIconCoroutine(int index)
    {
        isMoving = true;
        var rt = iconRects[index];
        if (rt == null) { isMoving = false; yield break; }

        Vector3 startPos = (originalPositions != null && index < originalPositions.Length) ? originalPositions[index] : rt.localPosition;
        float direction = (index == 0 || index == 3) ? 1f : -1f; // misma heurística que IconMover
        Vector3 targetPos = startPos + Vector3.up * moveDistance * direction;

        while (Vector3.Distance(rt.localPosition, targetPos) > 0.1f)
        {
            rt.localPosition = Vector3.Lerp(rt.localPosition, targetPos, Time.deltaTime * moveSpeed);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(holdTime);

        while (Vector3.Distance(rt.localPosition, startPos) > 0.1f)
        {
            rt.localPosition = Vector3.Lerp(rt.localPosition, startPos, Time.deltaTime * moveSpeed);
            yield return null;
        }

        isMoving = false;
    }

    // Espera a que el tutorial termine (si existe) y luego mueve los iconos de los jugadores que no tienen el turno.
    private IEnumerator CoWaitForTutorialThenMove()
    {
        // Esperar a que TurnManager esté listo
        float timeout = 1f;
        while ((TurnManager.instance == null) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        // Esperar tutorial si existe
        var tut = FindObjectOfType<TutorialManagerTejo>();
        if (tut != null)
        {
            // Si el blocker está asignado y activo, esperar a que se desactive (usuario avanzó/skip)
            var blockerField = tut.GetType().GetField("blocker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            GameObject blocker = null;
            if (blockerField != null) blocker = blockerField.GetValue(tut) as GameObject;

            if (blocker != null)
            {
                while (blocker.activeInHierarchy)
                {
                    yield return null;
                }
            }
            else
            {
                // fallback: esperar PlayerPrefs flag si es que se setea
                while (PlayerPrefs.GetInt("TutorialMostrado", 0) == 0)
                {
                    yield return null;
                }
            }
        }

        // Pequeña pausa para asegurar que la UI haya terminado de actualizarse
        yield return new WaitForSecondsRealtime(0.1f);

        // Ejecutar movimiento para los jugadores que NO tienen el turno (secuencial)
        int current = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (iconRects == null || iconRects.Length == 0) yield break;

        for (int i = 0; i < iconRects.Length; i++) 
        { 
            if (i == current) continue; // Ejecutar y esperar la animación de ese icono
            yield return StartCoroutine(MoveIconCoroutine(i)); // Retardo escalonado
            yield return new WaitForSecondsRealtime(0.06f);
        }
    }
}