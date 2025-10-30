using UnityEngine;
using TMPro;
using System;

public class UiManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Bolita bolita;        // Arrastra la Bolita desde la escena
    

    [Header("Puntajes (Jugadores 1-4)")]
    [SerializeField] private TMP_Text puntosJugador1;
    [SerializeField] private TMP_Text puntosJugador2;
    [SerializeField] private TMP_Text puntosJugador3;
    [SerializeField] private TMP_Text puntosJugador4;

    [Header("Ronda (etapa global)")]
    [Tooltip("Texto único que muestra la ronda actual (Stage)")]
    [SerializeField] private TMP_Text rondaTexto;

    [Header("Botones de jugador")]
    [SerializeField] private GameObject botonJugador1;
    [SerializeField] private GameObject botonJugador2;
    [SerializeField] private GameObject botonJugador3;
    [SerializeField] private GameObject botonJugador4;

    [Header("Advertencias")]
    [Tooltip("UI de advertencia para avisar que recoja la bola (activar/desactivar)")]
    [SerializeField] private GameObject panelAdvertenciaRecoger;

    [SerializeField] private GameObject AdvertenciaAtrapa;
    // Nuevo: texto configurable para la advertencia principal
    [SerializeField] private TMP_Text advertenciaAtrapaLabel; // Asignar el TMP_Text del objeto AdvertenciaAtrapa (opcional)
    [SerializeField] private string textoAdvertenciaNatural = "Toca las fichas!!!";
    [SerializeField] private string textoAdvertenciaNearGround = "Atrapa la bola!!!";
  

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    
        // Inicializar puntajes desde RoundData (persistentes)
        if (RoundData.instance != null)
        {
            SetTextoPuntos(puntosJugador1, GetSafe(RoundData.instance.currentPoints, 0));
            SetTextoPuntos(puntosJugador2, GetSafe(RoundData.instance.currentPoints, 1));
            SetTextoPuntos(puntosJugador3, GetSafe(RoundData.instance.currentPoints, 2));
            SetTextoPuntos(puntosJugador4, GetSafe(RoundData.instance.currentPoints, 3));
        }
        else
        {
            // Fallback si no existe RoundData aún
            SetTextoPuntos(puntosJugador1, 0);
            SetTextoPuntos(puntosJugador2, 0);
            SetTextoPuntos(puntosJugador3, 0);
            SetTextoPuntos(puntosJugador4, 0);
        }

        // Asegurar que el panel de advertencia inicie oculto
        if (panelAdvertenciaRecoger != null) panelAdvertenciaRecoger.SetActive(false);

        // Inicializar texto base de la advertencia principal
        SetAdvertenciaAtrapaText(textoAdvertenciaNatural);

        if (bolita == null) bolita = FindAnyObjectByType<Bolita>();
        if (bolita != null)
        {
            bolita.OnEstadoCambio += OnBolitaEstadoCambio;
            // Forzar estado inicial
            OnBolitaEstadoCambio(bolita.Estado);
        }
    }

    private void OnDestroy()
    {
        if (bolita != null) bolita.OnEstadoCambio -= OnBolitaEstadoCambio;
    }

    private void OnBolitaEstadoCambio(Bolita.EstadoLanzamiento estado)
    {
        if (rondaTexto == null) return;
        // Ocultar mientras esté en el aire, mostrar en otros estados
        bool visible = estado != Bolita.EstadoLanzamiento.EnElAire;
        if (rondaTexto.gameObject.activeSelf != visible)
            rondaTexto.gameObject.SetActive(visible);
    }

    private static int GetSafe(int[] arr, int idx)
    {
        if (arr == null) return 0;
        return (idx >= 0 && idx < arr.Length) ? arr[idx] : 0;
    }

    // Update is called once per frame
  
    public void MostrarBotonJugadorActivo(int turnoActual)
    {
        // Ocultar todos primero
        OcultarTodosBotonesJugadores();

        // Mostrar solo el del jugador actual (1-4)
        switch (turnoActual)
        {
            case 1:
                if (botonJugador1 != null) botonJugador1.SetActive(true);
                break;
            case 2:
                if (botonJugador2 != null) botonJugador2.SetActive(true);
                break;
            case 3:
                if (botonJugador3 != null) botonJugador3.SetActive(true);
                break;
            case 4:
                if (botonJugador4 != null) botonJugador4.SetActive(true);
                break;
            default:
                // No hacer nada si el número es inválido
                break;
        }
    }
    public void OcultarTodosBotonesJugadores()
    {
        if (botonJugador1 != null) botonJugador1.SetActive(false);
        if (botonJugador2 != null) botonJugador2.SetActive(false);
        if (botonJugador3 != null) botonJugador3.SetActive(false);
        if (botonJugador4 != null) botonJugador4.SetActive(false);
    }
    
    // Nuevo: mostrar/"highlight" de todos los botones de jugadores (útil para fin de juego)
    public void MostrarTodosBotonesJugadores()
    {
        if (botonJugador1 != null) botonJugador1.SetActive(true);
        if (botonJugador2 != null) botonJugador2.SetActive(true);
        if (botonJugador3 != null) botonJugador3.SetActive(true);
        if (botonJugador4 != null) botonJugador4.SetActive(true);
    }

    // Método público para actualizar los 4 textos de puntaje.
    // Pasa el arreglo de puntos (por ejemplo RoundData.instance.currentPoints)
    public void ActualizarPuntos(int playernum,long puntos)
    {
        switch (playernum)
        {
            case 1: SetTextoPuntos(puntosJugador1, puntos); break;
            case 2: SetTextoPuntos(puntosJugador2, puntos); break;
            case 3: SetTextoPuntos(puntosJugador3, puntos); break;
            case 4: SetTextoPuntos(puntosJugador4, puntos); break;
            default:
                Debug.LogWarning("UiManager: Número de jugador no válido: " + playernum);
                return;
        }
    }
    public void MostrarTextoAtrapa()
    {
        if (AdvertenciaAtrapa != null)
        {
            // Asegurar que el texto esté en su estado natural al mostrarse manualmente
            SetAdvertenciaAtrapaText(textoAdvertenciaNatural);
            AdvertenciaAtrapa.SetActive(true);
        Debug.Log("Advertencia Atrapa Activada");
        }     
    }

    public void OcultarTextoAtrapa()
    {
        if (AdvertenciaAtrapa != null)
        {
            AdvertenciaAtrapa.SetActive(false);
        Debug.Log("Advertencia Atrapa Desactivada");
        }     
    }
    // Nuevo: actualizar intentos para un jugador (índice 0-based)
    public void ActualizarIntentosJugador(int playerIndexZeroBased, int intentosRestantes)
    {
        // Obsoleto: ya no se muestran intentos por jugador; se conserva para evitar errores de referencias.
    }

    public void ActualizarRonda(int ronda)
    {
        if (rondaTexto != null)
        {
            rondaTexto.text = "Ronda " + Mathf.Max(1, ronda) + "/3";
        }
    }

    // Mostrar/ocultar advertencia de recoger la bola
    public void MostrarAdvertenciaRecoger(bool mostrar)
    {
        if (panelAdvertenciaRecoger != null)
            panelAdvertenciaRecoger.SetActive(mostrar);

        // Al avisar near-ground, cambiar el texto principal a "Atrapa la bola!!!" y asegurarnos de que esté visible
        if (mostrar)
        {
            SetAdvertenciaAtrapaText(textoAdvertenciaNearGround);
            if (AdvertenciaAtrapa != null && !AdvertenciaAtrapa.activeSelf)
                AdvertenciaAtrapa.SetActive(true);
        }
    }

    // Nuevo: método público para restaurar el texto natural al finalizar el turno
    public void OnFinDeTurno_ResetAdvertenciaAtrapa()
    {
        SetAdvertenciaAtrapaText(textoAdvertenciaNatural);
        // Ocultar el panel de recoger por si quedó activo
        if (panelAdvertenciaRecoger != null) panelAdvertenciaRecoger.SetActive(false);
    }

    private void SetAdvertenciaAtrapaText(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return;

        // Usar referencia directa si existe; si no, intentar buscar en el objeto AdvertenciaAtrapa
        if (advertenciaAtrapaLabel == null && AdvertenciaAtrapa != null)
            advertenciaAtrapaLabel = AdvertenciaAtrapa.GetComponentInChildren<TMP_Text>(true);

        if (advertenciaAtrapaLabel != null)
        {
            advertenciaAtrapaLabel.text = texto;
        }
    }

    private static void SetTextoPuntos(TMP_Text label, long valor)
    {
        if (label != null) label.text = valor.ToString();
    }
}
