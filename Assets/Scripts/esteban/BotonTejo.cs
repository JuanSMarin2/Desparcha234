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

    [Header("Flechas de ángulo")]
    public GameObject flechaHorizontalObj;
    public GameObject flechaVerticalObj;

    private FlechaTejo flechaHorizontal;
    private FlechaTejo flechaVertical;

    private bool enModoVertical = false;
    private int previusTurn;

    void Start()
    {
        if (blocker != null)
            blocker.SetActive(false);

        // Obtenemos scripts de rotación
        flechaHorizontal = flechaHorizontalObj.GetComponent<FlechaTejo>();
        flechaVertical = flechaVerticalObj.GetComponent<FlechaTejo>();

        // Al inicio, solo mostramos la flecha horizontal
        flechaHorizontalObj.SetActive(true);
        flechaVerticalObj.SetActive(false);
    }

    void Update()
    {
        int currentTurn = TurnManager.instance.CurrentTurn();

        if (previusTurn != currentTurn)
            DisableBlocker();

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
        }

        previusTurn = currentTurn;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enModoVertical)
        {
            // Guardamos ángulo horizontal y cambiamos a vertical
            float anguloH = flechaHorizontal.GetAngle();
            Debug.Log("Ángulo horizontal seleccionado: " + anguloH);

            flechaHorizontalObj.SetActive(false);
            flechaVerticalObj.SetActive(true);

            enModoVertical = true;
        }
        else
        {
            // Guardamos ángulo vertical
            float anguloV = flechaVertical.GetAngle();
            Debug.Log("Ángulo vertical seleccionado: " + anguloV);

            if (blocker != null)
                blocker.SetActive(true);

            // Aquí después lanzaremos el tejo usando anguloH y anguloV
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // No necesitamos nada especial aquí por ahora
    }

    public void DisableBlocker()
    {
        if (blocker != null)
            blocker.SetActive(false);
    }
}
