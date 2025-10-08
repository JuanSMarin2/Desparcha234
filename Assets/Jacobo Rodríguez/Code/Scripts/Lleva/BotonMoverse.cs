using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

// Adjunta este script al GameObject del botón (necesita componente gráfico + Canvas con GraphicRaycaster)
// Controla el movimiento por "hold" del jugador indicado usando el sistema de Movimiento.
public class BotonMoverse : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Jugador controlado")] [Range(1,4)] [SerializeField] private int playerIndex = 1;
    [Header("Debug")] [SerializeField] private bool logEvents = false;

    [Header("Ocultar si tapa jugadores")] 
    [Tooltip("Hacer el botón semitransparente si su área proyectada cubre a un jugador activo")] [SerializeField] private bool autoTransparenteSiCubre = true;
    [Tooltip("Alpha normal del botón")] [SerializeField] private float alphaNormal = 1f;
    [Tooltip("Alpha cuando cubre un jugador")] [SerializeField] private float alphaCubriendo = 0.35f;
    [Tooltip("Chequear cada X segundos para reducir costo")] [SerializeField] private float checkInterval = 0.1f;
    [Tooltip("Radio (en píxeles) alrededor del jugador para considerar que está cubierto")] [SerializeField] private float playerScreenRadius = 40f;
    [Tooltip("Cámara UI (si Canvas Screen Space Camera). Si null y Canvas Overlay se usa null")] [SerializeField] private Camera uiCamera;
    [Tooltip("Cámara de mundo para proyectar jugadores. Si null -> Camera.main")] [SerializeField] private Camera worldCamera;
    [Tooltip("Si está presionado, mantener alphaNormal aunque cubra")] [SerializeField] private bool ignorarMientrasPresionado = true;

    private bool _pressed = false;
    private CanvasGroup _cg;
    private Image _image;
    private RectTransform _rt;
    private float _nextCheckTime = 0f;
    private readonly List<PlayerTag> _playersCache = new List<PlayerTag>();
    private int _lastPlayerCount = -1;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        _image = GetComponent<Image>();
        if (worldCamera == null) worldCamera = Camera.main;
        RebuildPlayersCache();
        ApplyAlpha(alphaNormal);
    }

    void Update()
    {
        if (autoTransparenteSiCubre && Time.unscaledTime >= _nextCheckTime)
        {
            _nextCheckTime = Time.unscaledTime + checkInterval;
            CheckOverlapPlayers();
        }
    }

    private void RebuildPlayersCache()
    {
        _playersCache.Clear();
#if UNITY_2023_1_OR_NEWER
        var found = FindObjectsByType<PlayerTag>(FindObjectsSortMode.None);
#else
        var found = FindObjectsOfType<PlayerTag>();
#endif
        _playersCache.AddRange(found);
        _lastPlayerCount = _playersCache.Count;
    }

    private void CheckOverlapPlayers()
    {
        if (_rt == null) return;
        if (RoundData.instance != null && _playersCache.Count != _lastPlayerCount) // simple invalidación
        {
            RebuildPlayersCache();
        }
        // Obtener rectángulo de pantalla del botón
        Vector3[] corners = new Vector3[4];
        _rt.GetWorldCorners(corners);
        // Convertir a pantalla
        for (int i = 0; i < 4; i++) corners[i] = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
        float minX = corners[0].x, maxX = corners[0].x, minY = corners[0].y, maxY = corners[0].y;
        for (int i = 1; i < 4; i++)
        {
            var c = corners[i];
            if (c.x < minX) minX = c.x; if (c.x > maxX) maxX = c.x; if (c.y < minY) minY = c.y; if (c.y > maxY) maxY = c.y;
        }
        bool cubre = false;
        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        foreach (var p in _playersCache)
        {
            if (p == null) continue;
            // Limitar a jugadores activos según RoundData
            var rd = RoundData.instance;
            if (rd != null && p.PlayerIndex > rd.numPlayers) continue;
            Vector3 sp = cam.WorldToScreenPoint(p.transform.position);
            if (sp.z < 0f) continue; // detrás de la cámara
            // Distancia a rect (expandido por radius)
            float rxMin = minX - playerScreenRadius;
            float rxMax = maxX + playerScreenRadius;
            float ryMin = minY - playerScreenRadius;
            float ryMax = maxY + playerScreenRadius;
            if (sp.x >= rxMin && sp.x <= rxMax && sp.y >= ryMin && sp.y <= ryMax)
            {
                cubre = true; break;
            }
        }
        float targetAlpha = (!cubre || (ignorarMientrasPresionado && _pressed)) ? alphaNormal : alphaCubriendo;
        ApplyAlpha(targetAlpha);
    }

    private void ApplyAlpha(float a)
    {
        if (_cg != null)
        {
            _cg.alpha = a;
        }
        else if (_image != null)
        {
            var c = _image.color; c.a = a; _image.color = c;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
        Movimiento.StartHoldForPlayer(playerIndex);
        if (logEvents) Debug.Log($"[BotonMoverse] DOWN player {playerIndex}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_pressed) return;
        _pressed = false;
        Movimiento.StopHoldForPlayer(playerIndex);
        if (logEvents) Debug.Log($"[BotonMoverse] UP player {playerIndex}");
    }

    // Si el dedo / cursor sale del botón sin soltar, detener para evitar quedarse moviendo
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_pressed)
        {
            _pressed = false;
            Movimiento.StopHoldForPlayer(playerIndex);
            if (logEvents) Debug.Log($"[BotonMoverse] EXIT player {playerIndex}");
        }
    }

    private void OnDisable()
    {
        if (_pressed)
        {
            _pressed = false;
            Movimiento.StopHoldForPlayer(playerIndex);
            if (logEvents) Debug.Log($"[BotonMoverse] DISABLE cleanup player {playerIndex}");
        }
    }
}
