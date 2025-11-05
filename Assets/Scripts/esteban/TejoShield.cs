using UnityEngine;

/// <summary>
/// TejoShield
/// - Coloca este script en un GameObject con Collider2D marcado como Trigger.
/// - Es un consumible: cuando una Papeleta entra, aplica el "shield" (desactiva el trigger de su PolygonCollider2D)
///   y se desactiva a sí mismo. El feedback visual y la restauración al final del turno
///   los gestiona TejoShieldManager (persistente), sin tocar TurnManager.
/// </summary>
public class TejoShield : MonoBehaviour
{
    [Header("Detección de Papeletas")]
    [Tooltip("Tags que se consideran como papeletas válidas (0..n).")]
    [SerializeField] private string[] papeletaTags = new string[] { "Papeleta", "Papeleta1", "Papeleta2", "Papeleta3", "Papeleta4" };

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPapeleta(other)) return;

        // Si la papeleta ya tiene un power-up este turno, no permite recoger otro
        if (PowerUpStateManager.Instance != null && !PowerUpStateManager.Instance.CanPickup(other.transform))
        {
            return;
        }

        // Encontrar el PolygonCollider2D de la papeleta (en el mismo objeto o en hijos)
        PolygonCollider2D poly = other.GetComponent<PolygonCollider2D>();
        if (poly == null) poly = other.GetComponentInChildren<PolygonCollider2D>();
        if (poly == null)
        {
            Debug.LogWarning($"[TejoShield] La papeleta no tiene PolygonCollider2D: {other.name}");
            return;
        }

        // Aplicar el shield a través del manager (toglea isTrigger=false y maneja feedback)
        if (TejoShieldManager.Instance != null)
        {
            TejoShieldManager.Instance.ApplyShield(other.transform, poly);
            // Registrar que esta papeleta ya tiene power-up
            if (PowerUpStateManager.Instance != null)
                PowerUpStateManager.Instance.MarkHasPowerUp(other.transform);

            // Consumible: nos registramos para ser reactivados al cambiar de turno y nos desactivamos ahora
            TejoShieldManager.Instance.RegisterConsumableForReset(gameObject);
            gameObject.SetActive(false);
        }
        else
        {
            // Fallback mínimo: desactivar el trigger de la papeleta para cumplir con el requisito principal
            poly.isTrigger = false;
            gameObject.SetActive(false);
        }
    }

    private bool IsPapeleta(Collider2D col)
    {
        if (col == null) return false;
        string t = col.tag;
        if (string.IsNullOrEmpty(t)) return false;
        if (papeletaTags == null || papeletaTags.Length == 0) return t.StartsWith("Papeleta");
        for (int i = 0; i < papeletaTags.Length; i++)
        {
            if (t == papeletaTags[i]) return true;
        }
        return false;
    }
}
