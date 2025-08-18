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
    private bool listoParaFuerza = false;
    private bool cargandoFuerza = false;

    private float anguloHorizontal;
    private float anguloVertical;
    private float fuerza;

    [Header("Barra de fuerza")]
    public GameObject barraFuerzaObj;     // el fondo de la barra
    public RectTransform barraFuerzaFill; // el relleno que escala
    private float valorFuerza = 0f;
    private float velocidadOscilacion = 2f; // controla la velocidad de subida/bajada

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

        if (barraFuerzaObj != null)
            barraFuerzaObj.SetActive(false);
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

        // Oscilación de la barra de fuerza
        if (cargandoFuerza && barraFuerzaFill != null)
        {
            // PingPong normal, pero más tiempo en la parte baja
            valorFuerza = (Mathf.Sin(Time.time * velocidadOscilacion) + 1f) / 2f;
            valorFuerza = Mathf.Pow(valorFuerza, 2f); // hace que pase más tiempo abajo

            barraFuerzaFill.localScale = new Vector3(1f, valorFuerza, 1f);
        }

        previusTurn = currentTurn;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enModoVertical)
        {
            // Primer click: guardamos horizontal
            anguloHorizontal = flechaHorizontal.GetAngle();
            Debug.Log("Ángulo Horizontal seleccionado: " + anguloHorizontal);

            flechaHorizontalObj.SetActive(false);
            flechaVerticalObj.SetActive(true);

            enModoVertical = true;
        }
        else if (!listoParaFuerza)
        {
            // Segundo click: guardamos vertical
            anguloVertical = flechaVertical.GetAngle();
            Debug.Log("Ángulo Vertical seleccionado: " + anguloVertical);

            flechaVerticalObj.SetActive(false);

            // Activamos barra de fuerza
            if (barraFuerzaObj != null)
                barraFuerzaObj.SetActive(true);

            cargandoFuerza = true;
            listoParaFuerza = true;
        }
        else
        {
            // Tercer click: confirmamos fuerza
            fuerza = valorFuerza;
            Debug.Log($"Lanzar con Ángulos H:{anguloHorizontal} V:{anguloVertical} y Fuerza:{fuerza}");

            cargandoFuerza = false;

            if (barraFuerzaObj != null)
                barraFuerzaObj.SetActive(false);

            // aquí más adelante llamaremos al método para lanzar el tejo
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

        // Reset al estado inicial en cada turno nuevo
        enModoVertical = false;
        listoParaFuerza = false;
        cargandoFuerza = false;

        flechaHorizontalObj.SetActive(true);
        flechaVerticalObj.SetActive(false);

        if (barraFuerzaObj != null)
            barraFuerzaObj.SetActive(false);
    }
}
