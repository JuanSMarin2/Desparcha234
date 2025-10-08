using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class MarbleShooter2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ForceCharger forceCharger;
    [SerializeField] private Transform arrowObject;
    [SerializeField] private Rigidbody2D marble;
    [SerializeField] private int playerIndex;
    [SerializeField] private MarblePower marblePower;
    [SerializeField] private JoystickController joystick;
    [SerializeField] private GameObject marbleExplosion;
    [SerializeField] private WinnerPanel winnerPanel;
    [SerializeField] private GameObject winnerPanelRoot;
    private bool isEnding;
 



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
    [SerializeField] private float impactMinVelocity = 3f;

    [Header("Spin")]
    [SerializeField] private float spinRefSpeed = 10f;
    [SerializeField] private float maxSpinAngular = 720f;
    [SerializeField] private float spinLerpSpeed = 12f;
    [SerializeField] private bool spinUseVelocitySign = true;

    private static int bonusShotForPlayer = -1;

    private static bool firstCycleActive = true;
    private static System.Collections.Generic.HashSet<int> shotFirstCycle = new System.Collections.Generic.HashSet<int>();


  
    private static bool AllActivePlayersHaveShotOnce()
    {
        if (TurnManager.instance == null) return false;
        var active = TurnManager.instance.GetActivePlayerIndices();
        if (active == null || active.Count == 0) return false;
        for (int i = 0; i < active.Count; i++)
            if (!shotFirstCycle.Contains(active[i])) return false;
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
        if (firstCycleActive) return;
        if (bonusShotForPlayer != -1) return;
        if (eliminatedPlayerIndex != current) bonusShotForPlayer = current;
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
        bonusShotForPlayer = -1;
        firstCycleActive = true;
        shotFirstCycle.Clear();

        startLinearDamping = linearDamping;
        startAngularDamping = angularDamping;
        forceCharger.OnReleaseForce += TryShoot;
        isEnding = false;
    }

    void FixedUpdate()
    {
        if (marble.linearVelocity.magnitude > maxSpeed)
            marble.linearVelocity = Vector2.zero;

        UpdateSpin();
    }

    void Update()
    {
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

    private void UpdateSpin()
    {
        if (marble == null) return;

        float speed = marble.linearVelocity.magnitude;
        float t = (spinRefSpeed <= 0f) ? 0f : Mathf.Clamp01(speed / spinRefSpeed);
        float target = Mathf.Lerp(0f, maxSpinAngular, t);

        if (spinUseVelocitySign && speed > 0.01f)
        {
            float sign = Mathf.Sign(marble.linearVelocity.x);
            if (Mathf.Approximately(sign, 0f)) sign = 1f;
            target *= sign;
        }

        float newAng = Mathf.MoveTowards(marble.angularVelocity, target, spinLerpSpeed * 100f * Time.fixedDeltaTime);
        marble.angularVelocity = newAng;
    }

    private IEnumerator ResolveEndOfTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ZeroAllMarbles();
        marblePower?.NotifyTurnEndedIfMine();

        if (TryConsumeBonus(playerIndex))
        {
            joystick?.DisableBlocker();
            yield break;
        }

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

        RegisterFirstShotIfNeeded(playerIndex);

        if (EffectPool.Instance != null)
            EffectPool.Instance.SpawnTrigger(transform.position, "Launch", 0.3f, 1f);
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
                if (EffectPool.Instance != null)
                    EffectPool.Instance.SpawnTrigger(transform.position, "Impact", 0.3f, 1f);
        }
        else
        {
            if (EffectPool.Instance != null)
                EffectPool.Instance.SpawnTrigger(transform.position, "Impact", 0.3f, 1f);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (ExitManager.ForceReturnToMainMenu) return;
        if (!collision.CompareTag("SafeZone")) return;

        AwardBonusToCurrentTurnIfEliminatedNotCurrent(playerIndex);
        marblePower?.ApplyPower(MarblePowerType.None);

        if (!isEnding && gameObject.activeInHierarchy)
            StartCoroutine(HandleEliminationRoutine());
    }

    private IEnumerator HandleEliminationRoutine()
    {
        // Cachea la posición YA MISMO; evita leer transform luego si el GO es destruido
        Vector3 cachedPos = transform.position;

        // Deshabilita interacción y visual de la canica
        var col = GetComponent<Collider2D>(); if (col) col.enabled = false;
        var sr = GetComponentInChildren<SpriteRenderer>(); if (sr) sr.enabled = false;
        if (marble != null) { marble.linearVelocity = Vector2.zero; marble.angularVelocity = 0f; }

        // Efecto de explosión (si existe)
        if (marbleExplosion != null)
        {
            Transform eT = marbleExplosion.transform;
            if (eT != null) eT.position = cachedPos;
            marbleExplosion.SetActive(true);
        }

        bool willEndRound = false;
        int winnerIndex = -1;

        if (TurnManager.instance != null)
        {
            var active = TurnManager.instance.GetActivePlayerIndices();
            if (active != null)
            {
                var after = new System.Collections.Generic.List<int>(active);
                after.Remove(playerIndex);
                if (after.Count == 1)
                {
                    willEndRound = true;
                    winnerIndex = after[0];
                }
            }
        }

        if (willEndRound && winnerIndex >= 0)
        {
            // Verifica que los refs sigan vivos antes de usarlos
            if (winnerPanel != null && winnerPanelRoot != null)
            {
                // Todavía estamos activos?
                if (!this || !gameObject) yield break;

                // Prepara y activa el panel
                winnerPanel.Prepare(winnerIndex);
                winnerPanelRoot.SetActive(true);

                // Espera de exhibición
                float wait = 2f;
                float t = 0f;
                while (t < wait)
                {
                    // Si en medio de la espera destruyen este GO/escena, salir
                    if (!this || !gameObject) yield break;
                    t += Time.deltaTime;
                    yield return null;
                }
            }
        }

        if (!isEnding) EndRound();
    }

    private void EndRound()
    {
        isEnding = true;

        // Intenta apagar el efecto si sigue vivo
        if (marbleExplosion != null)
        {
            // null-check extra por si fue destruido en el cambio de escena
            var go = marbleExplosion.gameObject;
            if (go != null) go.SetActive(false);
        }

        GameRoundManager.instance.PlayerLose(playerIndex);
        // No toques más nada aquí; la escena puede cambiar inmediatamente
    }


}

