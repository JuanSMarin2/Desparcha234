using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerUpPower : MonoBehaviour
{
    [Header("Poder a aplicar")]
    [SerializeField] private MarblePowerType powerType = MarblePowerType.MorePower;

    [Header("Opcional")]
    [SerializeField] private bool destroyOnPick = false;

    private PowerUpController powerUpController;

    private void Start()
    {
        // Asegurarnos de tener el controlador
        powerUpController = GetComponent<PowerUpController>();
        if (powerUpController == null)
        {
            powerUpController = gameObject.AddComponent<PowerUpController>();
        }
    }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        MarblePower mp = other.GetComponent<MarblePower>();
        if (mp == null) return;

        // Aplica el poder
        mp.ApplyPower(powerType);
        PowerUpTextFeedback.instance.TextFeedback(powerType);

        Debug.Log($"[PowerUpPower] Power-up {powerType} recogido por {other.name}");

        // Notifica al controlador primero
        if (powerUpController != null)
        {
            powerUpController.HandleCollection();
        }

        // Luego desaparece el pickup
        if (destroyOnPick)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}