using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerUpPower : MonoBehaviour
{
    [Header("Poder a aplicar")]
    [SerializeField] private MarblePowerType powerType = MarblePowerType.MorePower;

    [Header("Opcional")]
    [SerializeField] private bool destroyOnPick = false; // si prefieres destruir en vez de desactivar

    private void Reset()
    {
        // Para que detecte el contacto sin bloquear
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // La canica debe tener MarblePower
        MarblePower mp = other.GetComponent<MarblePower>();
        if (mp == null) return;

        // Aplica el poder
        mp.ApplyPower(powerType);

        // Desaparece el pickup
        if (destroyOnPick) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
