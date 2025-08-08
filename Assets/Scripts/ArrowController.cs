using System.Collections.Generic;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [SerializeField] private Transform arrowObject; // El objeto de la flecha
    [SerializeField] private List<Transform> marbleCenters; // Lista de las 4 canicas
    [SerializeField] private JoystickController joystick;

    void Update()
    {
        int currentPlayerIndex = TurnManager.instance.GetCurrentPlayerIndex();

        if (currentPlayerIndex >= 0 && currentPlayerIndex < marbleCenters.Count)
        {
            // Posiciona la flecha en el centro de la canica activa
            arrowObject.position = marbleCenters[currentPlayerIndex].position;

            // Mostrar/ocultar según si se está usando el joystick
            arrowObject.gameObject.SetActive(joystick.IsDragging);
        }
        else
        {
            // En caso de error o sin jugadores
            arrowObject.gameObject.SetActive(false);
        }
    }
}
