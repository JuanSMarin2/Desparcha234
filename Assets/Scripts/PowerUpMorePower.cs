// PowerUpObject.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerUpObject : MonoBehaviour
{
    [SerializeField] private MarblePowerType powerType = MarblePowerType.MorePower;
    [SerializeField] private bool destroyOnPick = false;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable() { PowerUpRegistry.ActiveCount++; }
    void OnDisable() { PowerUpRegistry.ActiveCount = Mathf.Max(0, PowerUpRegistry.ActiveCount - 1); }
    void OnDestroy() { OnDisable(); }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[PowerUpObject] Trigger con: {other.name}");

        var mp = other.GetComponent<MarblePower>();
        if (!mp)
        {
            Debug.LogWarning("[PowerUpObject] El otro no tiene MarblePower. ¿Tiene Rigidbody2D alguno de los dos? ¿Layers/Collision Matrix?");
            return;
        }

        if (PowerUpTextFeedback.instance == null)
        {
            Debug.LogError("[PowerUpObject] NO hay PowerUpTextFeedback.instance en escena.");
        }
        else
        {
            Debug.Log("[PowerUpObject] Llamando a TextFeedback...");
            PowerUpTextFeedback.instance.TextFeedback(powerType, mp.PlayerIndex);
        }

        mp.ApplyPower(powerType);

        if (destroyOnPick) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

}
