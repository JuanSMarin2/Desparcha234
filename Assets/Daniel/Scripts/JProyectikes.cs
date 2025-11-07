using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class JProyectikes : MonoBehaviour
{
    [Header("UI del minijuego")]
    [Tooltip("Panel raíz o contenedor de la UI de este minijuego.")]
    public GameObject uiPanel;
    [Tooltip("Área de spawn (RectTransform dentro del Canvas) donde se mueven los proyectiles.")]
    public RectTransform spawnArea;
    [Header("Prefab del proyectil por jugador")]
    [Tooltip("Prefab por índice de jugador. Si está vacío o el índice no tiene prefab, se usa defaultProjectilePrefab.")]
    public GameObject[] projectilePrefabsByPlayer;
    [Tooltip("Prefab por defecto si no hay uno específico para el jugador.")]
    public GameObject defaultProjectilePrefab;
    [Header("Overrides visuales (opcional)")]
    [Tooltip("Si está activo, se fuerza sprite/tamaño/color en el Image del prefab instanciado.")]
    public bool overrideAppearance = false;
    [Tooltip("Sprite del proyectil (si overrideAppearance=true).")]
    public Sprite projectileSprite;
    [Tooltip("Color del proyectil (si overrideAppearance=true).")]
    public Color projectileColor = Color.white;
    [Tooltip("Tamaño del proyectil en px UI (si overrideAppearance=true).")]
    public Vector2 projectileSize = new Vector2(60, 60);

    [Header("Orientación visual")]
    [Tooltip("Marca true si el prefab está dibujado mirando a la IZQUIERDA por defecto. Si mira a la derecha, pon false.")]
    public bool prefabFacesLeft = true;

    [Header("Configuración de oleadas")]
    [Tooltip("Mínimo de proyectiles que deben atraparse para terminar el juego.")]
    public int minProjectiles = 5;
    [Tooltip("Máximo de proyectiles que deben atraparse para terminar el juego.")]
    public int maxProjectiles = 10;
    [Tooltip("Máximo de proyectiles activos simultáneamente.")]
    public int maxActiveProjectiles = 6;

    [Header("Velocidades (px/seg)")]
    [Tooltip("Velocidad mínima de los proyectiles.")]
    public float minSpeed = 120f;
    [Tooltip("Velocidad máxima de los proyectiles.")]
    public float maxSpeed = 260f;

    [Header("Distribución vertical")]
    [Tooltip("Espaciado vertical mínimo entre filas de proyectiles para evitar solapamiento.")]
    public float verticalSpacing = 70f;
    [Tooltip("Margen vertical interno del área de spawn.")]
    public float verticalPadding = 10f;
    [Tooltip("Margen horizontal interno del área de spawn.")]
    public float horizontalPadding = 10f;
    [Tooltip("Asignar aleatoriamente el lado inicial (izquierda/derecha).")]
    public bool randomizeStartSide = true;

    [Header("Eventos")]
    public UnityEvent onGameFinished;

    [Header("Integración con secuencia/turnos")]
    [Tooltip("Si es true, el GameSequenceController NO llamará a NextTurn al finalizar este minijuego.")]
    public bool skipNextTurnOnFinish = false;

    private bool gameActive = false;
    private int startedPlayerIndex = -1;

    // Estado de juego
    private int totalToCatch = 0;       // cantidad total que se deben atrapar para terminar
    private int spawnedSoFar = 0;       // cuántos se han instanciado (sumando los eliminados)
    private int caughtSoFar = 0;        // cuántos ha atrapado el jugador

    private List<float> rowYPositions = new List<float>();
    private readonly List<Projectile> active = new List<Projectile>();
    [Header("Animación de captura")]
    [Tooltip("Duración del encogimiento al atrapar (segundos)")]
    public float catchShrinkDuration = 0.25f;

    [Header("Sonidos")]
    [SerializeField, Tooltip("Clave SFX cuando aparece un proyectil en el canvas (usar siempre prefijo 'Tingo:').")]
    private string spawnSfxKey = "Tingo:ProyectilAparecer";
    [SerializeField, Tooltip("Clave SFX cuando el jugador agarra un proyectil (usar siempre prefijo 'Tingo:').")]
    private string grabSfxKey = "Tingo:ProyectilAgarrar";

    [Header("Bonus de tiempo al acertar")]
    [SerializeField, Tooltip("Segundos a otorgar por atrapar un proyectil durante el último 1/4 de tiempo.")]
    private float successBonusSeconds = 0.5f;

    [Header("Collider del proyectil")]
    [SerializeField, Tooltip("Multiplicador para el tamaño del BoxCollider2D generado (1 = igual al rect, 1.7 = 70% más grande).")]
    private float colliderSizeMultiplier = 1.7f;

    // Estructura interna para proyectiles activos
    private class Projectile
    {
        public GameObject go;
        public RectTransform rt;
        public float speed; // px/s
        public int dir;     // +1 derecha, -1 izquierda
        public float halfWidth;
        public float halfHeight;
        public float leftBound;
        public float rightBound;
        public float y;
    }

    void Awake()
    {
        // Asegúrate de que el panel arranca oculto
        if (uiPanel != null) uiPanel.SetActive(false);
    }

    // ===== API esperada por GameSequenceController =====
    [ContextMenu("PlayMiniGamen")]
    public void PlayMiniGamen()
    {
        Play();
    }

    [ContextMenu("Play")]
    public void Play()
    {
        // Activar GO y componente por si inicia primero
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!enabled) enabled = true;

        if (uiPanel != null) uiPanel.SetActive(true);

        gameActive = true;
        startedPlayerIndex = -1; // se fijará cuando TurnManager esté listo

        // Ajustar dificultad por cantidad de jugadores activos
        ApplyDifficulty();

        // Diferir inicio hasta que TurnManager esté listo para evitar prefabs por defecto inconsistentes
        StartCoroutine(InitializeGameDeferred());
    }

    public void StopGame()
    {
        // Abortado externo (timeout/eliminación). No invocar onGameFinished.
        ClearUI();
    }

    // ===== Lógica general =====
    void Update()
    {
        if (!gameActive) return;

        // Si cambió/eliminaron el jugador que inició, abortar minijuego
        if (startedPlayerIndex >= 0 && TurnManager.instance != null &&
            TurnManager.instance.GetCurrentPlayerIndex() != startedPlayerIndex)
        {
            StopGame();
            return;
        }

        // Movimiento de proyectiles
        // Usar deltaTime (no unscaled) para que el tiempo en 0 pause el movimiento
        float dt = Time.deltaTime;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Projectile p = active[i];
            if (p.go == null) { active.RemoveAt(i); continue; }

            Vector2 pos = p.rt.anchoredPosition;
            pos.x += p.dir * p.speed * dt;

            // Reaparecer al cruzar el borde opuesto
            if (p.dir > 0 && pos.x - p.halfWidth > p.rightBound)
            {
                pos.x = p.leftBound - p.halfWidth; // reaparece por la izquierda
            }
            else if (p.dir < 0 && pos.x + p.halfWidth < p.leftBound)
            {
                pos.x = p.rightBound + p.halfWidth; // reaparece por la derecha
            }

            p.rt.anchoredPosition = new Vector2(pos.x, p.y);
        }
    }

    // Llamar cuando el minijuego termina correctamente
    public void FinishSuccess()
    {
        if (!gameActive) return;
        gameActive = false;
        ClearUI();
        onGameFinished?.Invoke();
    }

    private void ClearUI()
    {
        // Limpieza de UI y estado
        // Destruir proyectiles activos
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].go != null) Destroy(active[i].go);
        }
        active.Clear();

        if (uiPanel != null) uiPanel.SetActive(false);
        gameActive = false;

        rowYPositions.Clear();
        totalToCatch = 0;
        spawnedSoFar = 0;
        caughtSoFar = 0;
    }

    void OnDisable()
    {
        ClearUI();
    }

    // ===================== LÓGICA DEL MINIJUEGO =====================
    private IEnumerator InitializeGameDeferred()
    {
        // Esperar hasta 1s en tiempo de juego (respetando pausa) a que TurnManager exista y tenga índice válido
        float maxWait = 1.0f;
        float elapsed = 0f;
        while ((TurnManager.instance == null) && elapsed < maxWait)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (TurnManager.instance != null)
        {
            while (TurnManager.instance.GetCurrentPlayerIndex() < 0 && elapsed < maxWait)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            startedPlayerIndex = TurnManager.instance.GetCurrentPlayerIndex();
        }
        else
        {
            startedPlayerIndex = -1;
        }

        InitializeGame();
    }

    private void InitializeGame()
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("JProyectikes: spawnArea no asignado.");
            return;
        }

        // Si aún no se fijó y ya hay TM, tomar índice ahora para consistencia
        if (startedPlayerIndex < 0 && TurnManager.instance != null)
        {
            startedPlayerIndex = TurnManager.instance.GetCurrentPlayerIndex();
        }

        // Validaciones de configuración
        if (minProjectiles < 1) minProjectiles = 1;
        if (maxProjectiles < minProjectiles) maxProjectiles = minProjectiles;
        if (minSpeed < 0f) minSpeed = 0f;
        if (maxSpeed < minSpeed) maxSpeed = minSpeed + 1f;
        if (verticalSpacing < 1f) verticalSpacing = 1f;
        if (horizontalPadding < 0f) horizontalPadding = 0f;
        if (verticalPadding < 0f) verticalPadding = 0f;

        // Determinar cantidad objetivo aleatoria
        totalToCatch = Random.Range(minProjectiles, maxProjectiles + 1);
        spawnedSoFar = 0;
        caughtSoFar = 0;

        // Preparar filas (Y) dentro del área
        ComputeRows();

        // Spawn inicial hasta el máximo activo o hasta completar el total
        int cap = maxActiveProjectiles > 0 ? Mathf.Min(maxActiveProjectiles, totalToCatch) : totalToCatch;
        for (int i = 0; i < cap; i++)
        {
            TrySpawnProjectile();
        }
    }

    private void ApplyDifficulty()
    {
        int players = Dificultad.GetActivePlayersCount();
        switch (players)
        {
            case 4:
                // Invertido: usar la dificultad que antes era para 2 jugadores
                minProjectiles = 20;
                maxProjectiles = 20;
                minSpeed = 320f;
                maxSpeed = 450f;
                break;
            case 3:
                minProjectiles = 10;
                maxProjectiles = 10;
                minSpeed = 300f;
                maxSpeed = 400f;
                break;
            case 2:
                // Invertido: usar la dificultad que antes era para 4 jugadores
                minProjectiles = 5;
                maxProjectiles = 5;
                minSpeed = 260f;
                maxSpeed = 400f;
                break;
            default:
                // mantener valores del inspector como fallback
                break;
        }
    }

    private void ComputeRows()
    {
        rowYPositions.Clear();

        Rect rect = spawnArea.rect;
        // Usaremos anchors al centro, por lo que (0,0) es el centro del área.
        float halfH = rect.height * 0.5f;
        float availableH = rect.height - 2f * verticalPadding;
        if (availableH <= 0f)
        {
            rowYPositions.Add(0f);
            return;
        }

        // Número de filas posibles en el alto disponible considerando spacing
        int rows = Mathf.Max(1, Mathf.FloorToInt(availableH / Mathf.Max(1f, verticalSpacing)));

        // Distribuir filas de forma equiespaciada dentro del alto útil
        float startY = -halfH + verticalPadding + verticalSpacing * 0.5f;
        for (int i = 0; i < rows; i++)
        {
            float y = startY + i * verticalSpacing;
            rowYPositions.Add(y);
        }

        // Si hay muchas filas, no necesitamos todas; mezclamos para asignar al azar
        for (int i = 0; i < rowYPositions.Count; i++)
        {
            int j = Random.Range(i, rowYPositions.Count);
            (rowYPositions[i], rowYPositions[j]) = (rowYPositions[j], rowYPositions[i]);
        }
    }

    private bool TrySpawnProjectile()
    {
        if (spawnedSoFar >= totalToCatch) return false;
        if (spawnArea == null) return false;

        // Resolver prefab según jugador
        GameObject prefab = ResolveProjectilePrefab();
        GameObject go;
        RectTransform rt;
    Image img = null;

        if (prefab != null)
        {
            go = Instantiate(prefab, spawnArea);
            rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.GetComponentInChildren<RectTransform>();
            if (rt == null)
            {
                Debug.LogWarning("JProyectikes: El prefab no tiene RectTransform. Abortando spawn.");
                Destroy(go);
                return false;
            }
            img = go.GetComponent<Image>();
            if (img == null) img = go.GetComponentInChildren<Image>();
        }
        else
        {
            // Fallback seguro si no hay prefab asignado
            go = new GameObject("Projectile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(spawnArea, false);
            rt = go.GetComponent<RectTransform>();
            // Anclar al centro para que anchoredPosition sea relativo al centro del área
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            img = go.GetComponent<Image>();
        }

        // Aplicar overrides visuales si se desea
        if (overrideAppearance)
        {
            if (rt != null) rt.sizeDelta = projectileSize;
            if (img != null)
            {
                img.sprite = projectileSprite;
                img.color = projectileColor;
                img.raycastTarget = true;
            }
        }

        // Asegurar handler de clicks con imagen + collider
        if (img == null)
        {
            img = go.AddComponent<Image>();
            img.raycastTarget = true;
        }
    // Vincular componente de click (UI) al proyectil
        var clicker = go.GetComponent<JProjectileClickable>();
        if (clicker == null) clicker = go.AddComponent<JProjectileClickable>();
        clicker.Initialize(this, go, img);
    // El collider se ajustará tras definir orientación y posición

        // Bounds horizontales (en anchoredPosition X)
        Rect rect = spawnArea.rect;
        float halfW = rect.width * 0.5f;
        float leftBound = -halfW + horizontalPadding;
        float rightBound = halfW - horizontalPadding;

        // Elegir fila Y
        float y = rowYPositions.Count > 0 ? rowYPositions[spawnedSoFar % rowYPositions.Count] : 0f;

        // Dirección inicial aleatoria
        int dir = randomizeStartSide ? (Random.value < 0.5f ? +1 : -1) : +1;
        float speed = Random.Range(minSpeed, maxSpeed);

        // Posición inicial en borde correspondiente
        // Determinar ancho/alto del proyectil desde su RectTransform (si no hay tamaño, usar override o 60x60 por defecto)
        float projWidth = rt != null ? (rt.rect.width > 0 ? rt.rect.width : (overrideAppearance ? projectileSize.x : 60f)) : (overrideAppearance ? projectileSize.x : 60f);
    float projHeight = rt != null ? (rt.rect.height > 0 ? rt.rect.height : (overrideAppearance ? projectileSize.y : 60f)) : (overrideAppearance ? projectileSize.y : 60f);
    float x = dir > 0 ? leftBound - (projWidth * 0.5f) : rightBound + (projWidth * 0.5f);

    // Ajustar orientación visual según la dirección de movimiento
    ApplyFacingFlip(img != null ? img.rectTransform : rt, dir);
        rt.anchoredPosition = new Vector2(x, y);

        // Asegurar un collider 2D ajustado al tamaño/escala finales
        EnsureCollider2D(go, rt);

        Projectile p = new Projectile
        {
            go = go,
            rt = rt,
            speed = speed,
            dir = dir,
            halfWidth = projWidth * 0.5f,
            halfHeight = projHeight * 0.5f,
            leftBound = leftBound,
            rightBound = rightBound,
            y = y
        };

        active.Add(p);
        spawnedSoFar++;

        // Sonido de aparición del proyectil (estilo JOrden/JReduce)
        PlaySfx(spawnSfxKey);
        return true;
    }

    private GameObject ResolveProjectilePrefab()
    {
        // Usar SIEMPRE el índice con el que inició el minijuego para evitar inconsistencias
        int idx = startedPlayerIndex;

        if (projectilePrefabsByPlayer != null && idx >= 0 && idx < projectilePrefabsByPlayer.Length)
        {
            var pf = projectilePrefabsByPlayer[idx];
            if (pf != null) return pf;
        }
        return defaultProjectilePrefab;
    }

    private void ApplyFacingFlip(RectTransform target, int dir)
    {
        if (target == null) return;
        // dir > 0: moviendo a la derecha; dir < 0: moviendo a la izquierda
        // Consideramos "mirar a la izquierda" como facing = -1, "mirar a la derecha" como +1
        int baseFacing = prefabFacesLeft ? -1 : +1;
        int desiredFacing = dir > 0 ? +1 : -1;
        int sign = (desiredFacing == baseFacing) ? +1 : -1;

        Vector3 s = target.localScale;
        float absX = Mathf.Abs(s.x);
        if (absX < 1e-4f) absX = 1f; // evitar escala cero accidental
        target.localScale = new Vector3(absX * sign, s.y, s.z);
    }

    public void OnProjectileClicked(GameObject go)
    {
        if (!gameActive) return;
        // Encontrar en la lista activa
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].go == go)
            {
                // Sonido de agarrar proyectil
                PlaySfx(grabSfxKey);

                // Bonus de tiempo si estamos en el último cuarto del temporizador
                if (Tempo.instance != null) Tempo.instance.TryBonusOnSuccess(successBonusSeconds, nameof(JProyectikes));

                // Quitar del pool activo e incrementar contador antes de animar
                active.RemoveAt(i);
                caughtSoFar++;

                // Deshabilitar interacción durante la animación
                var clicker = go.GetComponent<JProjectileClickable>();
                if (clicker != null) clicker.SetInteractable(false);
                var img = go.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;

                // ¿Último proyectil? entonces terminar tras animación
                if (caughtSoFar >= totalToCatch)
                {
                    StartCoroutine(AnimateCatchThenFinish(go));
                    return;
                }

                // Mantener el cupo de activos si aún quedan por spawnear
                if (spawnedSoFar < totalToCatch)
                {
                    int activeCap = maxActiveProjectiles > 0 ? maxActiveProjectiles : int.MaxValue;
                    if (active.Count < activeCap)
                    {
                        TrySpawnProjectile();
                    }
                }

                // Animar y destruir
                StartCoroutine(AnimateCatchAndDestroy(go));
                return;
            }
        }
    }

    // Añade/ajusta un BoxCollider2D para coincidir con el rectángulo visual del proyectil
    private void EnsureCollider2D(GameObject go, RectTransform rt)
    {
        if (go == null || rt == null) return;
        var col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        // Calcular tamaño local aproximado a partir del rect y la escala
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float worldW = Vector3.Distance(corners[0], corners[3]); // left-bottom to right-bottom
        float worldH = Vector3.Distance(corners[0], corners[1]); // left-bottom to left-top
        Vector3 lossy = rt.lossyScale;
        float localW = (lossy.x != 0f) ? worldW / Mathf.Abs(lossy.x) : rt.rect.width;
        float localH = (lossy.y != 0f) ? worldH / Mathf.Abs(lossy.y) : rt.rect.height;
        float k = Mathf.Max(0.01f, colliderSizeMultiplier);
        col.size = new Vector2(localW, localH) * k; // 70% más grande por defecto (1.7x)
        col.offset = Vector2.zero;
    }

    // Helper centralizado para reproducir SFX a través de SoundManager
    private void PlaySfx(string key, float volume = 1f)
    {
        var sm = SoundManager.instance;
        if (sm != null && !string.IsNullOrWhiteSpace(key))
        {
            sm.PlaySfx(key, volume);
        }
    }

    private IEnumerator AnimateCatchAndDestroy(GameObject go)
    {
        if (go == null) yield break;
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.GetComponentInChildren<RectTransform>();
        Vector3 initial = rt != null ? rt.localScale : Vector3.one;
        float t = 0f;
        float dur = Mathf.Max(0.01f, catchShrinkDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / dur);
            if (rt != null) rt.localScale = initial * k;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    private IEnumerator AnimateCatchThenFinish(GameObject go)
    {
        yield return AnimateCatchAndDestroy(go);
        FinishSuccess();
    }
}
