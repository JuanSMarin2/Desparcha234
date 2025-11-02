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
        powerUpController = GetComponent<PowerUpController>();
        if (powerUpController == null)
            powerUpController = gameObject.AddComponent<PowerUpController>();
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

        // Aplica poder
        mp.ApplyPower(powerType);

        // Feedback con playerIndex (0..3)
        PowerUpTextFeedback.instance?.TextFeedback(powerType, mp.PlayerIndex);

        // Notifica y desaparece
        powerUpController?.HandleCollection();



        if (destroyOnPick) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
