using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Minijuego de ritmo/memoria: una matriz muestra un patrón y el jugador debe repetirlo en otra matriz.
// Integra con la secuencia: PlayMiniGamen/Play, StopGame y onGameFinished.
public class RecordarInsame : MonoBehaviour
{
    [Header("Eventos")]
    public UnityEvent onGameFinished;
    [Tooltip("Si es true, el controlador no llamará a NextTurn al terminar este minijuego.")]
    public bool skipNextTurnOnFinish = false;

    [Header("Bonus de tiempo al acertar")]
    [SerializeField, Tooltip("Segundos a otorgar por acierto durante el último 1/4 de tiempo.")]
    private float successBonusSeconds = 0.5f;

    [Header("Contenedores de matrices")]
    [SerializeField, Tooltip("Matriz que reproduce el patrón (solo visual)")] private RectTransform patternGridParent;
    [SerializeField, Tooltip("Matriz para la entrada del jugador (clics)")] private RectTransform inputGridParent;
    [SerializeField, Tooltip("Si se true, usaremos un único grid para mostrar el patrón y permitir la entrada del jugador. Si está activo, asigne 'singleGridParent'.")]
    private bool useSingleGrid = true;
    [SerializeField, Tooltip("Padre único para el grid cuando useSingleGrid = true")] private RectTransform singleGridParent;

    [Header("Prefabs por color (apagado/encendido)")]
    [SerializeField, Tooltip("Prefabs APAGADO por color (indexa colores)")] private GameObject[] offPrefabsByColor;
    [SerializeField, Tooltip("Prefabs ENCENDIDO por color (indexa colores)")] private GameObject[] onPrefabsByColor;
    [SerializeField, Tooltip("Prefab APAGADO por defecto si no hay por color")] private GameObject defaultOffPrefab;
    [SerializeField, Tooltip("Prefab ENCENDIDO por defecto si no hay por color")] private GameObject defaultOnPrefab;

    [System.Serializable]
    public class ColorPrefabSet
    {
        [Tooltip("Prefabs APAGADO por color para este jugador")] public GameObject[] offPrefabsByColor;
        [Tooltip("Prefabs ENCENDIDO por color para este jugador")] public GameObject[] onPrefabsByColor;
    }

    [Header("Prefabs por jugador (colores)")]
    [SerializeField, Tooltip("Array por índice de jugador (0-based). Cada entrada define sus prefabs por color (on/off) para ese jugador.")]
    private ColorPrefabSet[] colorSetsByPlayer;

    [Header("Diseño de celda")]
    [SerializeField, Tooltip("Tamaño de celda (UI px)")] private Vector2 cellSize = new Vector2(90, 90);
    [SerializeField, Tooltip("Espaciado entre celdas (UI px)")] private Vector2 cellSpacing = new Vector2(12, 12);
    [SerializeField, Tooltip("Si no hay set por jugador, usa un solo color por jugador mapeando los arrays globales por índice de jugador.")]
    private bool useSingleGlobalColorPerPlayer = true;

    [Header("Patrón y tiempos")]
    [SerializeField, Tooltip("Duración de encendido por paso (segundos)")] private float stepOnTime = 0.5f;
    [SerializeField, Tooltip("Pausa entre pasos (segundos)")] private float stepPauseTime = 0.15f;
    [SerializeField, Tooltip("Delay antes de mostrar el patrón (segundos)")] private float showDelay = 0.5f;
    [SerializeField, Tooltip("Reproducir automáticamente el patrón de nuevo al fallar")]
    private bool replayOnFail = true;
    [Header("Feedback de error")]
    [SerializeField, Tooltip("Color para indicar error (por defecto #EC4949)")]
    private Color errorColor = new Color(0.925f, 0.286f, 0.286f);
    [SerializeField, Tooltip("Duración del destello de error en segundos")]
    private float errorFlashSeconds = 1.0f;

    [Header("Sonidos")]
    [SerializeField, Tooltip("Prefijo fijo para las teclas. Debe ser 'Tingo:' según convención.")]
    private string notePrefix = "Tingo:";
    [SerializeField, Tooltip("Si es true, también suena la nota cuando el jugador presiona la celda.")]
    private bool playNoteOnPlayerPress = true;
    [Header("Final visual")]
    [SerializeField, Tooltip("Duración del parpadeo cuando suenan todas las notas al final.")]
    private float finalChordVisualTime = 0.5f;

    // Estado de grid y patrón
    private List<Cell> patternCells = new();
    private List<Cell> inputCells = new();
    private List<int> pattern = new();
    private int inputProgress = 0;
    private bool isShowing = false;
    private bool running = false;
    private bool stopping = false;
    private int startedPlayerIndex = -1;

    private struct Cell
    {
        public int colorIndex;
        public GameObject offGO;
        public GameObject onGO;
        public RectTransform rt;
        public Button button; // solo en input
    }

    // ===== API estándar =====
    [ContextMenu("PlayMiniGamen")]
    public void PlayMiniGamen()
    {
        Play();
    }

    [ContextMenu("Play")]
    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(BeginWhenReady());
    }

    public void StopGame()
    {
        stopping = true;
        running = false;
        StopAllCoroutines();
        CleanupGrids();
        isShowing = false;
        pattern.Clear();
        inputProgress = 0;
    }

    private void OnDisable()
    {
        StopGame();
    }

    private IEnumerator BeginWhenReady()
    {
        // Espera a TurnManager (hasta 1s) para ajustar dificultad por jugadores
        float waited = 0f;
        while ((TurnManager.instance == null || TurnManager.instance.GetCurrentPlayerIndex() < 0) && waited < 1f)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        stopping = false;
        running = true;
    startedPlayerIndex = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        BuildGridsByDifficulty();
        yield return new WaitForSeconds(showDelay);

        yield return StartCoroutine(ShowPattern());
    }

    private void BuildGridsByDifficulty()
    {
    CleanupGrids();
    int players = Dificultad.GetActivePlayersCount();
    int rows = 1, cols = 3; // base
    // Invertir 4 <-> 2; 3 queda igual (neutral)
    if (players == 4) { rows = 3; cols = 3; }
    else if (players == 3) { rows = 2; cols = 3; }
    else if (players == 2) { rows = 1; cols = 3; }

        // Construir celdas
        if (useSingleGrid && singleGridParent != null)
        {
            // Crear un único grid interactivo que primero se usará para mostrar el patrón (interacción deshabilitada durante la reproducción)
            var single = BuildGrid(singleGridParent, rows, cols, interactive: true);
            patternCells = single;
            inputCells = single;
        }
        else
        {
            patternCells = BuildGrid(patternGridParent, rows, cols, interactive: false);
            inputCells = BuildGrid(inputGridParent, rows, cols, interactive: true);
        }

        // Generar patrón (longitud basada en jugadores)
        // Reglas:
        // 1) No más de 3 apariciones totales por nota (distribución)
        // 2) No más de 2 repeticiones consecutivas de la misma nota (evitar triples seguidos)
    // Invertir longitud: lo que era de 4 ahora aplica a 2, y viceversa; 3 se mantiene
    int patternLen = players >= 4 ? 5 : (players == 3 ? 4 : 3);
        patternLen = Mathf.Clamp(patternLen, 1, rows * cols * 3); // límite teórico considerando 3 repeticiones por celda
        pattern.Clear();

        int totalCells = rows * cols;
        const int maxPerNote = 3;
        var counts = new Dictionary<int, int>(totalCells);

        for (int i = 0; i < patternLen; i++)
        {
            int attempts = 0;
            int candidate = 0;
            bool found = false;
            while (attempts < 100)
            {
                candidate = Random.Range(0, totalCells);
                int cnt = counts.ContainsKey(candidate) ? counts[candidate] : 0;
                bool makesTriple = (i >= 2 && pattern[i - 1] == candidate && pattern[i - 2] == candidate);
                if (cnt < maxPerNote && !makesTriple)
                {
                    found = true;
                    break;
                }
                attempts++;
            }
            if (!found)
            {
                // Fallback: elegir la celda menos utilizada hasta ahora
                int bestIdx = 0;
                int bestCount = int.MaxValue;
                for (int c = 0; c < totalCells; c++)
                {
                    int cnt = counts.ContainsKey(c) ? counts[c] : 0;
                    bool makesTriple = (i >= 2 && pattern[i - 1] == c && pattern[i - 2] == c);
                    if (cnt < bestCount && cnt < maxPerNote && !makesTriple)
                    {
                        bestCount = cnt;
                        bestIdx = c;
                    }
                }
                candidate = bestIdx;
            }
            pattern.Add(candidate);
            counts[candidate] = (counts.ContainsKey(candidate) ? counts[candidate] : 0) + 1;
        }

        inputProgress = 0;
    }

    private List<Cell> BuildGrid(RectTransform parent, int rows, int cols, bool interactive)
    {
        var list = new List<Cell>();
        if (parent == null)
        {
            Debug.LogError("RecordarInsame: asigna los Contenedores de matrices (patternGridParent e inputGridParent) o singleGridParent si useSingleGrid está activo.");
            return list;
        }

        Rect area = parent.rect;
        // Calcular origen para centrar la malla
        float totalW = cols * cellSize.x + (cols - 1) * cellSpacing.x;
        float totalH = rows * cellSize.y + (rows - 1) * cellSpacing.y;
        Vector2 origin = new Vector2(-totalW * 0.5f + cellSize.x * 0.5f, -totalH * 0.5f + cellSize.y * 0.5f);

        // Determinar set de colores activo (por jugador si existe)
        var activeSet = GetActiveColorSet();
        int colorCountFromSet = 0;
        if (activeSet != null)
        {
            colorCountFromSet = Mathf.Max(activeSet.onPrefabsByColor != null ? activeSet.onPrefabsByColor.Length : 0,
                                          activeSet.offPrefabsByColor != null ? activeSet.offPrefabsByColor.Length : 0);
        }
        int colorCountFromGlobals = Mathf.Max(onPrefabsByColor != null ? onPrefabsByColor.Length : 0,
                                              offPrefabsByColor != null ? offPrefabsByColor.Length : 0);
        int effectiveColorCount = (activeSet != null) ? colorCountFromSet : colorCountFromGlobals;
        if (effectiveColorCount <= 0) effectiveColorCount = 1;

        bool singleColorMode = (activeSet == null) && useSingleGlobalColorPerPlayer;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int flat = r * cols + c;
                int colorIdx = singleColorMode ? 0 : (flat % effectiveColorCount);
                // Resolver índice de color final y prefabs
                int pIdx = startedPlayerIndex >= 0 ? startedPlayerIndex : (TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : 0);
                int colFromPlayer = effectiveColorCount > 0 ? (pIdx % effectiveColorCount) : 0;
                int resolvedColorIdx = singleColorMode ? colFromPlayer : colorIdx;
                GameObject offPrefab = ResolveOffPrefab(resolvedColorIdx, activeSet);
                GameObject onPrefab = ResolveOnPrefab(resolvedColorIdx, activeSet);

                // Crear contenedor vacío por celda
                var cellGO = new GameObject($"Cell_{r}_{c}", typeof(RectTransform));
                var cellRT = (RectTransform)cellGO.transform;
                cellRT.SetParent(parent, false);
                // Anclaje centrado para posicionamiento estable
                cellRT.anchorMin = new Vector2(0.5f, 0.5f);
                cellRT.anchorMax = new Vector2(0.5f, 0.5f);
                cellRT.pivot = new Vector2(0.5f, 0.5f);
                cellRT.localScale = Vector3.one;
                cellRT.localRotation = Quaternion.identity;
                cellRT.sizeDelta = cellSize;
                cellRT.anchoredPosition = origin + new Vector2(c * (cellSize.x + cellSpacing.x), r * (cellSize.y + cellSpacing.y));

                // Instanciar APAGADO
                GameObject offGO = null, onGO = null;
                if (offPrefab != null)
                {
                    offGO = Instantiate(offPrefab, cellRT);
                    var rt = offGO.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = Vector2.zero;
                        rt.sizeDelta = cellSize;
                        rt.localScale = Vector3.one;
                        rt.localRotation = Quaternion.identity;
                    }
                }
                if (onPrefab != null)
                {
                    onGO = Instantiate(onPrefab, cellRT);
                    var rt = onGO.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = Vector2.zero;
                        rt.sizeDelta = cellSize;
                        rt.localScale = Vector3.one;
                        rt.localRotation = Quaternion.identity;
                    }
                }
                if (onGO != null) onGO.SetActive(false);
                if (offGO != null) offGO.SetActive(true);

                Button btn = null;
                if (interactive)
                {
                    // Añadir un botón a la celda (no al prefab) para capturar clics de toda el área
                    var button = cellGO.AddComponent<Button>();
                    btn = button;
                    int indexCopy = flat;
                    button.onClick.AddListener(() => OnCellPressed(indexCopy));
                }

                list.Add(new Cell
                {
                    colorIndex = colorIdx,
                    offGO = offGO,
                    onGO = onGO,
                    rt = cellRT,
                    button = btn
                });
            }
        }
        return list;
    }

    private ColorPrefabSet GetActiveColorSet()
    {
        if (colorSetsByPlayer == null || colorSetsByPlayer.Length == 0) return null;
        int idx = startedPlayerIndex;
        if (idx < 0) idx = TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (idx >= 0 && idx < colorSetsByPlayer.Length)
        {
            var set = colorSetsByPlayer[idx];
            // Validar que al menos tenga algún contenido
            bool any = (set != null) && (
                (set.offPrefabsByColor != null && set.offPrefabsByColor.Length > 0) ||
                (set.onPrefabsByColor != null && set.onPrefabsByColor.Length > 0)
            );
            if (any) return set;
        }
        return null;
    }

    private GameObject ResolveOffPrefab(int colorIndex, ColorPrefabSet set)
    {
        if (set != null && set.offPrefabsByColor != null && colorIndex >= 0 && colorIndex < set.offPrefabsByColor.Length && set.offPrefabsByColor[colorIndex] != null)
            return set.offPrefabsByColor[colorIndex];
        if (offPrefabsByColor != null && colorIndex >= 0 && colorIndex < offPrefabsByColor.Length && offPrefabsByColor[colorIndex] != null)
            return offPrefabsByColor[colorIndex];
        return defaultOffPrefab;
    }

    private GameObject ResolveOnPrefab(int colorIndex, ColorPrefabSet set)
    {
        if (set != null && set.onPrefabsByColor != null && colorIndex >= 0 && colorIndex < set.onPrefabsByColor.Length && set.onPrefabsByColor[colorIndex] != null)
            return set.onPrefabsByColor[colorIndex];
        if (onPrefabsByColor != null && colorIndex >= 0 && colorIndex < onPrefabsByColor.Length && onPrefabsByColor[colorIndex] != null)
            return onPrefabsByColor[colorIndex];
        return defaultOnPrefab;
    }

    private IEnumerator ShowPattern()
    {
        if (!running || stopping) yield break;
        isShowing = true;
        // Desactivar interacción mientras se muestra
        SetInputInteractable(false);

        // Asegurar que el grid del jugador esté totalmente apagado antes de reproducir el patrón
        ResetInputVisuals();

        // Encender por pasos en el grid de PATRÓN
        for (int i = 0; i < pattern.Count; i++)
        {
            int idx = pattern[i];
            // Reproducir la nota asociada a esta celda del patrón
            PlayNoteForCellIndex(idx);
            yield return StartCoroutine(BlinkCell(patternCells, idx, stepOnTime));
            yield return new WaitForSeconds(stepPauseTime);
        }

        isShowing = false;
        inputProgress = 0;
        SetInputInteractable(true);
    }

    private IEnumerator BlinkCell(List<Cell> cells, int index, float onTime)
    {
        if (index < 0 || index >= cells.Count) yield break;
        var cell = cells[index];
        if (cell.onGO != null) cell.onGO.SetActive(true);
        if (cell.offGO != null) cell.offGO.SetActive(false);
        float t = 0f;
        while (t < onTime)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (cell.onGO != null) cell.onGO.SetActive(false);
        if (cell.offGO != null) cell.offGO.SetActive(true);
    }

    private void SetInputInteractable(bool interact)
    {
        foreach (var c in inputCells)
        {
            if (c.button != null) c.button.interactable = interact;
        }
    }

    private void OnCellPressed(int index)
    {
        if (!running || stopping || isShowing) return;

        // Tocar la nota asociada a la celda presionada (si está habilitado)
        if (playNoteOnPlayerPress) PlayNoteForCellIndex(index);

        // Verificar contra el patrón
        if (index == pattern[inputProgress])
        {
            // Éxito: pequeño blink en la celda tocada
            StartCoroutine(BlinkCell(inputCells, index, Mathf.Min(0.2f, stepOnTime * 0.5f)));
            // Bonus de tiempo si estamos en el último cuarto del temporizador
            if (Tempo.instance != null) Tempo.instance.TryBonusOnSuccess(successBonusSeconds, nameof(RecordarInsame));
            inputProgress++;
            if (inputProgress >= pattern.Count)
            {
                // Éxito
                running = false;
                StartCoroutine(FinishAfterPatternSequence());
            }
        }
        else
        {
            // Fallo: destello rojo en la celda presionada y gestionar replay/reinicio
            StartCoroutine(HandleFailWithFlash(index));
        }
    }

    private IEnumerator HandleFailWithFlash(int index)
    {
        SetInputInteractable(false);
        // Asegurar que no haya otros parpadeos activos que ensucien el feedback
        // Nota: no usamos StopAllCoroutines aquí para no interrumpir esta corrutina
        yield return StartCoroutine(FlashErrorCell(index, errorFlashSeconds));

        if (replayOnFail)
        {
            inputProgress = 0;
            ResetInputVisuals();
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(ShowPattern());
        }
        else
        {
            ResetInputVisuals();
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(BeginWhenReady());
        }
    }

    private IEnumerator FlashErrorCell(int index, float seconds)
    {
        if (index < 0 || index >= inputCells.Count) yield break;
        var cell = inputCells[index];
        // Recolectar imágenes de ambos estados para asegurar el tinte
        var images = new List<Image>();
        if (cell.offGO != null) images.AddRange(cell.offGO.GetComponentsInChildren<Image>(true));
        if (cell.onGO != null) images.AddRange(cell.onGO.GetComponentsInChildren<Image>(true));
        var originals = new List<Color>(images.Count);
        for (int i = 0; i < images.Count; i++)
        {
            originals.Add(images[i].color);
            images[i].color = errorColor;
        }
        // Asegurar que algo sea visible: preferimos mostrar OFF tintado
        if (cell.onGO != null) cell.onGO.SetActive(false);
        if (cell.offGO != null) cell.offGO.SetActive(true);

        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            yield return null;
        }
        for (int i = 0; i < images.Count; i++)
        {
            if (images[i] != null) images[i].color = originals[i];
        }
    }

    private IEnumerator ReplayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(ShowPattern());
    }

    private IEnumerator FinishAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CleanupGrids();
        onGameFinished?.Invoke();
    }

    // Secuencia final: esperar 0.5s, tocar todas las notas, esperar 0.5s y luego finalizar
    private IEnumerator FinishAfterPatternSequence()
    {
        // Pausa breve para que se "sienta" el cierre del patrón
        yield return new WaitForSeconds(0.5f);
        // Tocar acorde con todas las teclas previas
        PlayAllNotesSimultaneously();
        // Dejar que se vea/escuche el acorde
        yield return new WaitForSeconds(0.5f);
        CleanupGrids();
        onGameFinished?.Invoke();
    }

    private void CleanupGrids()
    {
        // Si usamos un único padre, evitar limpiar dos veces
        if (useSingleGrid && singleGridParent != null)
        {
            CleanupChildren(singleGridParent);
        }
        else
        {
            CleanupChildren(patternGridParent);
            CleanupChildren(inputGridParent);
        }
        patternCells.Clear();
        inputCells.Clear();
    }

    private void CleanupChildren(RectTransform parent)
    {
        if (parent == null) return;
        var toDestroy = new List<GameObject>();
        for (int i = 0; i < parent.childCount; i++)
        {
            var ch = parent.GetChild(i);
            if (ch != null) toDestroy.Add(ch.gameObject);
        }
        foreach (var go in toDestroy)
        {
            if (go != null) Destroy(go);
        }
    }

    // Forzar que el grid de entrada quede completamente en estado APAGADO
    private void ResetInputVisuals()
    {
        for (int i = 0; i < inputCells.Count; i++)
        {
            var c = inputCells[i];
            if (c.onGO != null) c.onGO.SetActive(false);
            if (c.offGO != null) c.offGO.SetActive(true);
        }
    }

    // ===================== Sonido de notas =====================
    private void PlayNoteForCellIndex(int cellIndex)
    {
        if (cellIndex < 0) return;
        int noteNumber = cellIndex + 1; // Mapear celda 0->1, 1->2, ...
        string key = string.Concat(notePrefix, noteNumber.ToString()); // "Tingo:1", "Tingo:2", ...
        var sm = SoundManager.instance;
        if (sm != null) sm.PlaySfx(key);
    }

    private void PlayAllNotesSimultaneously()
    {
        var sm = SoundManager.instance;
        if (sm == null || pattern == null || pattern.Count == 0) return;
        // Tocar cada nota única del patrón a la vez
        var seen = new HashSet<int>();
        for (int i = 0; i < pattern.Count; i++)
        {
            int idx = pattern[i];
            if (!seen.Add(idx)) continue; // evitar duplicados
            int noteNumber = idx + 1;
            string key = string.Concat(notePrefix, noteNumber.ToString());
            sm.PlaySfx(key);
            // Activación visual simultánea: si ambos grids son la misma lista, solo ejecutar una vez
            bool same = ReferenceEquals(patternCells, inputCells);
            StartCoroutine(BlinkCell(patternCells, idx, finalChordVisualTime));
            if (!same)
            {
                StartCoroutine(BlinkCell(inputCells, idx, finalChordVisualTime));
            }
        }
    }
}

