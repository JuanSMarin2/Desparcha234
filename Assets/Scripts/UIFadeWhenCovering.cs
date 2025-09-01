using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UIFadeWhenCovering : MonoBehaviour
{
    [SerializeField] private RectTransform uiRect;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask ignoreLayers = 10;

    [Header("Objetos a NO tapar")]
    [SerializeField] private Transform[] targets;        // objetos del mundo
    [SerializeField] private Renderer[] targetRenderers; // opcional: para usar bounds.center

    [Header("Ajustes")]
    [SerializeField] private float visibleAlpha = 1f;
    [SerializeField] private float hiddenAlpha = 0.2f;
    [SerializeField] private float lerpSpeed = 10f;

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

            Vector3 worldPoint = (targetRenderers != null && i < targetRenderers.Length && targetRenderers[i] != null)
                ? targetRenderers[i].bounds.center
                : t.position;

            Vector3 sp = worldCamera.WorldToScreenPoint(worldPoint);
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

            if (hits.Count > 0)
            {
                // --- CAMBIO PRINCIPAL: Filtrado por capa ---
                foreach (var hit in hits)  // Iteramos todos los hits, no solo el primero
                {
                    // Si el objeto está en una capa ignorada, lo saltamos
                    if (ignoreLayers == (ignoreLayers | (1 << hit.gameObject.layer)))
                        continue;

                    var top = hit.gameObject.transform;
                    if (top == uiRect || top.IsChildOf(uiRect))
                    {
                        shouldHide = true;
                        break;  // Salir del foreach si encontramos un hit válido
                    }
                }
                if (shouldHide) break;  // Salir del for si ya debemos ocultar
            }
        }

        float targetAlpha = shouldHide ? hiddenAlpha : visibleAlpha;
        group.alpha = Mathf.Lerp(group.alpha, targetAlpha, Time.deltaTime * lerpSpeed);
    }
}
