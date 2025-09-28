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

    [Header("Intentos restantes (opcional)")]
    [SerializeField] private TMP_Text intentosJugador1;
    [SerializeField] private TMP_Text intentosJugador2;
    [SerializeField] private TMP_Text intentosJugador3;
    [SerializeField] private TMP_Text intentosJugador4;

    [Header("Botones de jugador")]
    [SerializeField] private GameObject botonJugador1;
    [SerializeField] private GameObject botonJugador2;
    [SerializeField] private GameObject botonJugador3;
    [SerializeField] private GameObject botonJugador4;

    [Header("Advertencias")]
    [Tooltip("UI de advertencia para avisar que recoja la bola (activar/desactivar)")]
    [SerializeField] private GameObject panelAdvertenciaRecoger;

    [SerializeField] private GameObject AdvertenciaAtrapa;
  

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
        TMP_Text target = null;
        switch (playerIndexZeroBased)
        {
            case 0: target = intentosJugador1; break;
            case 1: target = intentosJugador2; break;
            case 2: target = intentosJugador3; break;
            case 3: target = intentosJugador4; break;
        }
        if (target == null) return; // opcional, no hay UI asignada
        target.text = "Tiros(" + Mathf.Max(0, intentosRestantes) + ")";
    }

    // Mostrar/ocultar advertencia de recoger la bola
    public void MostrarAdvertenciaRecoger(bool mostrar)
    {
        if (panelAdvertenciaRecoger != null)
            panelAdvertenciaRecoger.SetActive(mostrar);
    }

    private static void SetTextoPuntos(TMP_Text label, long valor)
    {
        if (label != null) label.text = valor.ToString();
    }
}
