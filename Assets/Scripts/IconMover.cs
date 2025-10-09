using UnityEngine;
using System.Collections;

public class IconMover : MonoBehaviour
{
    [SerializeField] private Transform[] playerIcons; // Íconos de cada jugador (0-3)
    [SerializeField] private float moveDistance = 50f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float holdTime = 0.3f;

    private Vector3[] originalPositions;
    private bool isMoving;
    private bool systemActive; // <- Solo se moverá después de la señal inicial
    private int lastMovedPlayer = -1; // Para evitar repetir el mismo turno
    private bool isFinishing;

    void Start()
    {
        isFinishing = false;
        // Guardar posiciones iniciales
        originalPositions = new Vector3[playerIcons.Length];
        for (int i = 0; i < playerIcons.Length; i++)
        {
            originalPositions[i] = playerIcons[i].localPosition;
        }
    }

    /// <summary>
    /// Señal que habilita el movimiento por turnos.
    /// Llama a esto UNA sola vez cuando el juego esté listo.
    /// </summary>
    public void ActivateSystem()
    {
        systemActive = true;
        Debug.Log("IconMover activado: ahora seguirá los turnos.");
    }

    void Update()
    {
      
        if (!systemActive || isMoving || isFinishing) return;

        int current = TurnManager.instance.GetCurrentPlayerIndex();
        if (current != lastMovedPlayer && current >= 0 && current < playerIcons.Length)
        {
            lastMovedPlayer = current;
            StartCoroutine(MoveIcon(current));
        }
    }

    private IEnumerator MoveIcon(int index)
    {
        isMoving = true;

        // Determinar dirección (jugadores 1 y 4 arriba, 2 y 3 abajo)
        float direction = (index == 0 || index == 3) ? 1f : -1f;
        Vector3 startPos = originalPositions[index];
        Vector3 targetPos = startPos + Vector3.up * moveDistance * direction;

        // Subir o bajar
        while (Vector3.Distance(playerIcons[index].localPosition, targetPos) > 0.1f)
        {
            playerIcons[index].localPosition = Vector3.Lerp(
                playerIcons[index].localPosition, targetPos, Time.deltaTime * moveSpeed);
            yield return null;
        }

        yield return new WaitForSeconds(holdTime);

        // Volver a la posición inicial
        while (Vector3.Distance(playerIcons[index].localPosition, startPos) > 0.1f)
        {
            playerIcons[index].localPosition = Vector3.Lerp(
                playerIcons[index].localPosition, startPos, Time.deltaTime * moveSpeed);
            yield return null;
        }

        playerIcons[index].localPosition = startPos;
        isMoving = false;
    }

    public void IsFinishing()
    {
        isFinishing = true;
    }
}
