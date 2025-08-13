using UnityEngine;

public class MarbleShooter2D : MonoBehaviour
{
    [SerializeField] private ForceCharger forceCharger;
    [SerializeField] private Transform arrowObject;
    [SerializeField] private Rigidbody2D marble;
    [SerializeField] private float forceMultiplier = 10f;
    [SerializeField] private int playerIndex;

    [SerializeField] private float linearDamping = 3f;
    [SerializeField] private float angularDamping = 1.0f;

 

    private float startLinearDamping;
    private float startAngularDamping;

    private bool isWaitingToEndTurn = false;
    private float stopThreshold = 0.05f;

    [SerializeField] private float speed;

   [ SerializeField] private float maxSpeed = 30f; // velocidad máxima en unidades/segundo

    void FixedUpdate()
    {
      
        // Limitar velocidad máxima
        if (marble.linearVelocity.magnitude > maxSpeed)
        {
            speed = marble.linearVelocity.magnitude;
            marble.linearVelocity = Vector2.zero;
        }
    }

    void Start()
    {
        startLinearDamping = linearDamping;
        startAngularDamping = angularDamping;
        forceCharger.OnReleaseForce += TryShoot;

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
                TurnManager.instance.NextTurn();
            }
        }

        ChangeDamping();
    }

    private void ChangeDamping()
    {
        if(playerIndex +1 != TurnManager.instance.CurrentTurn())
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
    }

    private void ShootMarble(float fuerza)
    {
        if (marble == null || arrowObject == null) return;

        Vector2 direction = arrowObject.up.normalized;

        marble.linearVelocity = Vector2.zero;
        marble.angularVelocity = 0f;

        marble.linearDamping = linearDamping;
        marble.angularDamping = angularDamping;

        marble.AddForce(direction * fuerza * forceMultiplier, ForceMode2D.Impulse);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("SafeZone")) 
        {
            GameRoundManager.instance.PlayerLose(playerIndex);
            this.gameObject.SetActive(false);
            this.gameObject.transform.position = Vector3.zero;

        }
    }
}
  