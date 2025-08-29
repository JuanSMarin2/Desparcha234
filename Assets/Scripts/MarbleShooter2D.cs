using UnityEngine;
using System.Collections;

public class MarbleShooter2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ForceCharger forceCharger;
    [SerializeField] private Transform arrowObject;
    [SerializeField] private Rigidbody2D marble;
    [SerializeField] private int playerIndex;
    [SerializeField] private MarblePower marblePower;
    [SerializeField] private JoystickController joystick;   // NUEVO

    [Header("Física")]
    [SerializeField] private float forceMultiplier = 10f;
    [SerializeField] private float linearDamping = 3f;
    [SerializeField] private float angularDamping = 1.0f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float stopThreshold = 0.05f;

    private float startLinearDamping;
    private float startAngularDamping;
    private bool isWaitingToEndTurn = false;

    [Header("Efectos")]
    [SerializeField] private Sprite launchSprite;
    [SerializeField] private Sprite impactSprite;
    [SerializeField] private float impactMinVelocity = 3f;

    // ====== BONUS SHOT (estático, compartido) ======
    private static int bonusShotForPlayer = -1; // -1 = sin bonus

    private static void AwardBonusToCurrentTurnIfEliminatedNotCurrent(int eliminatedPlayerIndex)
    {
        if (TurnManager.instance == null) return;
        int current = TurnManager.instance.GetCurrentPlayerIndex();
        if (current < 0) return;

        // Solo dar bonus si el eliminado NO es el que está en turno
        if (eliminatedPlayerIndex != current)
        {
            bonusShotForPlayer = current;
            // Debug.Log($"[Bonus] Se concedió bonus al Jugador {current+1} por eliminación de Jugador {eliminatedPlayerIndex+1}");
        }
    }

    private static bool TryConsumeBonus(int playerIdx)
    {
        if (bonusShotForPlayer == playerIdx)
        {
            bonusShotForPlayer = -1;
            return true;
        }
        return false;
    }

    void Start()
    {
        startLinearDamping = linearDamping;
        startAngularDamping = angularDamping;
        forceCharger.OnReleaseForce += TryShoot;
    }

    void FixedUpdate()
    {
        if (marble.linearVelocity.magnitude > maxSpeed)
            marble.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        // Si esta canica es la del jugador en turno y está esperando fin de turno…
        if (isWaitingToEndTurn &&
            TurnManager.instance != null &&
            TurnManager.instance.GetCurrentPlayerIndex() == playerIndex)
        {
            if (marble.linearVelocity.magnitude <= stopThreshold)
            {
                isWaitingToEndTurn = false;
                // Esperar 0.5s y decidir (bonus o siguiente turno)
                StartCoroutine(ResolveEndOfTurnAfterDelay(0.5f));
            }
        }

        ChangeDamping();
    }

    private IEnumerator ResolveEndOfTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Turno acaba");

        // Avisar al sistema de poderes que terminó MI turno (descuenta turnos de power-ups)
        marblePower?.NotifyTurnEndedIfMine();

        // ¿Tengo bonus para repetir lanzamiento?
        if (TryConsumeBonus(playerIndex))
        {
            // Mantener el turno NO NextTurn()
            // Desbloquear joystick para que pueda volver a disparar
            joystick?.DisableBlocker();

        
            yield break;
        }

        // Si no hay bonus, ahora sí pasamos el turno
        TurnManager.instance?.NextTurn();
    }

    private void ChangeDamping()
    {
        if (playerIndex + 1 != TurnManager.instance.CurrentTurn())
        {
            linearDamping = 1f;
            angularDamping = 0.5f;
        }
        else
        {
            linearDamping = startLinearDamping;
            angularDamping = startAngularDamping;
        }
    }

    private void TryShoot(float fuerza)
    {
        if (playerIndex != TurnManager.instance.GetCurrentPlayerIndex()) return;

        ShootMarble(fuerza);
        isWaitingToEndTurn = true;

        if (EffectPool.Instance != null && launchSprite != null)
            EffectPool.Instance.Spawn(transform.position, launchSprite, 0.3f, 1f);
    }

    private void ShootMarble(float fuerza)
    {
        if (marble == null || arrowObject == null) return;

        Vector2 direction = arrowObject.up.normalized;

        marble.linearVelocity = Vector2.zero;
        marble.angularVelocity = 0f;
        marble.linearDamping = linearDamping;
        marble.angularDamping = angularDamping;

        float effectiveMultiplier = marblePower
            ? marblePower.GetLaunchMultiplier(forceMultiplier)
            : forceMultiplier;

        marble.AddForce(direction * fuerza * effectiveMultiplier, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (marble.linearVelocity.magnitude < impactMinVelocity) return;

        var otherShooter = collision.collider.GetComponent<MarbleShooter2D>();
        if (otherShooter != null)
        {
            if (this.gameObject.GetInstanceID() < otherShooter.gameObject.GetInstanceID())
            {
                if (EffectPool.Instance != null && impactSprite != null)
                    EffectPool.Instance.Spawn(transform.position, impactSprite, 0.3f, 1f);
            }
        }
        else
        {
            if (EffectPool.Instance != null && impactSprite != null)
                EffectPool.Instance.Spawn(transform.position, impactSprite, 0.3f, 1f);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("SafeZone"))
        {
            // Esta canica (playerIndex) se elimina
            AwardBonusToCurrentTurnIfEliminatedNotCurrent(playerIndex);

            GameRoundManager.instance.PlayerLose(playerIndex);
            marblePower?.ApplyPower(MarblePowerType.None);

            gameObject.SetActive(false);
            transform.position = Vector3.zero;

            // Si al eliminarse esto otorga bonus al actual, de una vez desbloqueamos el joystick por si es el mismo que está jugando
            if (TurnManager.instance != null && TurnManager.instance.GetCurrentPlayerIndex() >= 0)
            {
                if (bonusShotForPlayer == TurnManager.instance.GetCurrentPlayerIndex())
                    joystick?.DisableBlocker();
            }
        }
    }
}
