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
    [SerializeField] private JoystickController joystick;

    [Header("Fisica")]
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

    // ====== Bonus shot (static, compartido) ======
    private static int bonusShotForPlayer = -1; // -1 = sin bonus

    // ====== Bloqueo dobles en primer ciclo ======
    private static bool firstCycleActive = true;                 // true hasta que todos tiren 1 vez
    private static System.Collections.Generic.HashSet<int> shotFirstCycle = new System.Collections.Generic.HashSet<int>();

    private static bool AllActivePlayersHaveShotOnce()
    {
        if (TurnManager.instance == null) return false;
        var active = TurnManager.instance.GetActivePlayerIndices();
        if (active == null || active.Count == 0) return false;
        for (int i = 0; i < active.Count; i++)
        {
            if (!shotFirstCycle.Contains(active[i])) return false;
        }
        return true;
    }

    private static void RegisterFirstShotIfNeeded(int playerIdx)
    {
        if (!firstCycleActive) return;
        if (!shotFirstCycle.Contains(playerIdx))
            shotFirstCycle.Add(playerIdx);

        if (AllActivePlayersHaveShotOnce())
        {
            firstCycleActive = false;
            shotFirstCycle.Clear();
        }
    }

    private static void AwardBonusToCurrentTurnIfEliminatedNotCurrent(int eliminatedPlayerIndex)
    {
        if (TurnManager.instance == null) return;
        int current = TurnManager.instance.GetCurrentPlayerIndex();
        if (current < 0) return;

        // Si aun estamos en el primer ciclo, no dar bonus
        if (firstCycleActive) return;

        // No acumular bonus
        if (bonusShotForPlayer != -1) return;

        // Solo dar bonus si el eliminado no es el que esta en turno
        if (eliminatedPlayerIndex != current)
        {
            bonusShotForPlayer = current;
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

    private static void ZeroAllMarbles()
    {
        var shooters = FindObjectsByType<MarbleShooter2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < shooters.Length; i++)
        {
            if (shooters[i] != null && shooters[i].marble != null)
            {
                shooters[i].marble.linearVelocity = Vector2.zero;
                shooters[i].marble.angularVelocity = 0f;
            }
        }
    }

    void Start()
    {
        // Reset por escena
        bonusShotForPlayer = -1;
        firstCycleActive = true;
        shotFirstCycle.Clear();

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
        // Si esta canica es la del jugador en turno y esta esperando fin de turno
        if (isWaitingToEndTurn &&
            TurnManager.instance != null &&
            TurnManager.instance.GetCurrentPlayerIndex() == playerIndex)
        {
            if (marble.linearVelocity.magnitude <= stopThreshold)
            {
                isWaitingToEndTurn = false;
                StartCoroutine(ResolveEndOfTurnAfterDelay(0.5f));
            }
        }

        ChangeDamping();
    }

    private IEnumerator ResolveEndOfTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Forzar velocidad 0 de todas las canicas al cerrar turno
        ZeroAllMarbles();

        // Descontar turnos de poderes si aplica
        marblePower?.NotifyTurnEndedIfMine();

        // Consumir bonus si pertenece a este jugador
        if (TryConsumeBonus(playerIndex))
        {
            // Mantener turno, desbloquear joystick para siguiente tiro
            joystick?.DisableBlocker();
            yield break;
        }

        // Pasar turno normal
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

        // Registrar primer tiro en el primer ciclo si aplica
        RegisterFirstShotIfNeeded(playerIndex);

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
            // Evitar doble efecto en choque entre canicas
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
        if (ExitManager.ForceReturnToMainMenu) return;

        if (collision.CompareTag("SafeZone"))
        {
            // Esta canica (playerIndex) se elimina
            AwardBonusToCurrentTurnIfEliminatedNotCurrent(playerIndex);

            GameRoundManager.instance.PlayerLose(playerIndex);
            marblePower?.ApplyPower(MarblePowerType.None);

            gameObject.SetActive(false);
            transform.position = Vector3.zero;

            // Si otorgo bonus al actual, desbloquear joystick
            if (TurnManager.instance != null && TurnManager.instance.GetCurrentPlayerIndex() >= 0)
            {
                if (bonusShotForPlayer == TurnManager.instance.GetCurrentPlayerIndex())
                    joystick?.DisableBlocker();
            }
        }
    }
}
