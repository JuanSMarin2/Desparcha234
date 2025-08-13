using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BotonTejo : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image imageBoton;
    [SerializeField] private GameObject blocker;

    [SerializeField] private Transform wheel0;
    [SerializeField] private Transform wheel1;
    [SerializeField] private Transform wheel2;
    [SerializeField] private Transform wheel3;

    private int previusTurn;

    void Start()
    {
        if (blocker != null)
            blocker.SetActive(false);
    }

    void Update()
    {
        int currentTurn = TurnManager.instance.CurrentTurn();

        if (previusTurn != currentTurn)
            DisableBlocker();

        // Cambiar posición y color según turno
        switch (currentTurn)
        {
            case 1:
                transform.position = wheel0.position;
                imageBoton.color = Color.red;
                break;
            case 2:
                transform.position = wheel1.position;
                imageBoton.color = Color.blue;
                break;
            case 3:
                transform.position = wheel2.position;
                imageBoton.color = Color.yellow;
                break;
            case 4:
                transform.position = wheel3.position;
                imageBoton.color = Color.green;
                break;
            default:
                Debug.LogWarning("Turno no válido: " + currentTurn);
                break;
        }

        previusTurn = currentTurn;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Botón presionado");
        // Aquí va la lógica para lanzar el tejo o iniciar la acción
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("Botón soltado");
        if (blocker != null)
            blocker.SetActive(true);
    }

    public void DisableBlocker()
    {
        if (blocker != null)
            blocker.SetActive(false);
    }
}
