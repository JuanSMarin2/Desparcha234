using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Simple click handler for UI Images (no Button). Relays clicks back to JProyectikes.
/// Optionally controls Image.raycastTarget for enabling/disabling interaction.
/// </summary>
public class JProjectileClickable : MonoBehaviour, IPointerClickHandler
{
    private JProyectikes owner;
    private GameObject target;
    private Image image;
    private bool interactable = true;

    public void Initialize(JProyectikes owner, GameObject target, Image img)
    {
        this.owner = owner;
        this.target = target != null ? target : gameObject;
        this.image = img != null ? img : GetComponent<Image>();
        if (this.image != null) this.image.raycastTarget = true;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        if (image != null) image.raycastTarget = value;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable || owner == null) return;
        owner.OnProjectileClicked(target != null ? target : gameObject);
    }
}
