using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UIFadeWhenCovering : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RectTransform uiRect;       // El panel UI que se va a volver transparente
    [SerializeField] private Canvas canvas;              // El canvas donde vive
    [SerializeField] private GraphicRaycaster raycaster; // Para detectar UI debajo del puntero
    [SerializeField] private Camera worldCamera;         // Cámara ortográfica 2D

    [Header("Objetos que NO debe tapar")]
    [SerializeField] private Transform[] targets;        // Objetos del mundo 2D (ej: personajes, ítems)

    [Header("Ajustes de transparencia")]
    [SerializeField] private float visibleAlpha = 1f;    // Opacidad normal
    [SerializeField] private float hiddenAlpha = 0.2f;   // Opacidad cuando tapa algo
    [SerializeField] private float lerpSpeed = 10f;      // Velocidad de transición

    [Header("Capas a ignorar (por ejemplo, UI_Joystick)")]
    [SerializeField] private LayerMask ignoreLayers;

    private CanvasGroup group;
    private PointerEventData ped;
    private readonly List<RaycastResult> hits = new();

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!raycaster) raycaster = canvas?.GetComponent<GraphicRaycaster>();
        if (!worldCamera) worldCamera = Camera.main;
        if (!uiRect) uiRect = GetComponent<RectTransform>();

        ped = new PointerEventData(EventSystem.current);
    }

    void LateUpdate()
    {
        if (!uiRect || !canvas || !raycaster || targets == null) return;

        bool shouldHide = false;

        for (int i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            if (!t || !t.gameObject.activeInHierarchy)
                continue;

            Vector3 sp = worldCamera.WorldToScreenPoint(t.position);
            if (sp.z <= 0f) continue;

            bool insideRect = RectTransformUtility.RectangleContainsScreenPoint(
                uiRect,
                sp,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera
            );

            if (!insideRect) continue;

            ped.position = sp;
            hits.Clear();
            raycaster.Raycast(ped, hits);

            foreach (var hit in hits)
            {
                // Opción 2 — Ignorar la capa del joystick
                if ((ignoreLayers.value & (1 << hit.gameObject.layer)) != 0)
                    continue;

      

                // Si el hit pertenece a este UI o sus hijos, significa que está tapando
                if (hit.gameObject.transform == uiRect || hit.gameObject.transform.IsChildOf(uiRect))
                {
                    shouldHide = true;
                    break;
                }
            }

            if (shouldHide)
                break;
        }

        float targetAlpha = shouldHide ? hiddenAlpha : visibleAlpha;
        group.alpha = Mathf.Lerp(group.alpha, targetAlpha, Time.deltaTime * lerpSpeed);
    }
}
