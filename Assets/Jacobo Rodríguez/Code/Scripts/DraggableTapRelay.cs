using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Relay para habilitar arrastre en una Image y reenviar los clics a Bolita
public class DraggableTapRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private RectTransform _rt;
    private Canvas _canvas;
    private Bolita _owner;

    public void Init(Bolita owner)
    {
        _owner = owner;
        _rt = GetComponent<RectTransform>();
        if (_rt == null) _rt = gameObject.AddComponent<RectTransform>();
        // Tratar de encontrar un Canvas padre para convertir deltas correctamente
        _canvas = GetComponentInParent<Canvas>();
        var img = GetComponent<Image>();
        if (img != null) img.raycastTarget = true;
    }

    public void OnBeginDrag(PointerEventData eventData) { /* no-op */ }

    public void OnDrag(PointerEventData eventData)
    {
        if (_rt == null) return;
        Vector2 delta = eventData.delta;
        float scale = 1f;
        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay && _canvas.scaleFactor != 0f)
            scale = _canvas.scaleFactor;
        _rt.anchoredPosition += delta / Mathf.Max(0.0001f, scale);
    }

    public void OnEndDrag(PointerEventData eventData) { /* no-op */ }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_owner != null)
            _owner.TriggerTapFromRelay();
    }
}
