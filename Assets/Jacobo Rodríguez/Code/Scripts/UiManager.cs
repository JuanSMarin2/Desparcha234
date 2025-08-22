using UnityEngine;
using TMPro;
using System;

public class UiManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Bolita bolita;        // Arrastra la Bolita desde la escena
    [SerializeField] private TMP_Text estadoLabel; // Arrastra un TextMeshProUGUI o TMP_Text
    [SerializeField] private TMP_Text cronometroLabel; // Texto del cronómetro

    [Header("Puntajes (Jugadores 1-4)")]
    [SerializeField] private TMP_Text puntosJugador1;
    [SerializeField] private TMP_Text puntosJugador2;
    [SerializeField] private TMP_Text puntosJugador3;
    [SerializeField] private TMP_Text puntosJugador4;

    private bool _cronometroActivo;
    private float _tiempo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (cronometroLabel != null) cronometroLabel.gameObject.SetActive(false);

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
    }

    private static int GetSafe(int[] arr, int idx)
    {
        if (arr == null) return 0;
        return (idx >= 0 && idx < arr.Length) ? arr[idx] : 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (_cronometroActivo)
        {
            _tiempo += Time.deltaTime;
            ActualizarCronometroVisual();
        }
    }

    private void OnEnable()
    {
        if (bolita != null)
        {
            bolita.OnEstadoCambio += OnEstadoCambio;
            ActualizarTexto(bolita.Estado);
            SincronizarCronometroConEstado(bolita.Estado);
        }
    }

    private void OnDisable()
    {
        if (bolita != null)
            bolita.OnEstadoCambio -= OnEstadoCambio;
    }

    private void OnEstadoCambio(Bolita.EstadoLanzamiento estado)
    {
        ActualizarTexto(estado);
        SincronizarCronometroConEstado(estado);
    }

    private void SincronizarCronometroConEstado(Bolita.EstadoLanzamiento estado)
    {
        switch (estado)
        {
            case Bolita.EstadoLanzamiento.PendienteDeLanzar:
                _cronometroActivo = false;
                _tiempo = 0f;
                if (cronometroLabel != null) cronometroLabel.gameObject.SetActive(false);
                break;
            case Bolita.EstadoLanzamiento.EnElAire:
                _tiempo = 0f; // reinicia al despegar
                _cronometroActivo = true;
                if (cronometroLabel != null) cronometroLabel.gameObject.SetActive(true);
                ActualizarCronometroVisual();
                break;
            case Bolita.EstadoLanzamiento.Fallado:
                _cronometroActivo = false; // se detiene pero queda visible el tiempo final
                if (cronometroLabel != null) cronometroLabel.gameObject.SetActive(true);
                break;
        }
    }

    private void ActualizarCronometroVisual()
    {
        if (cronometroLabel == null) return;
        TimeSpan t = TimeSpan.FromSeconds(_tiempo);
        cronometroLabel.text = string.Format("{0:00}:{1:00}.{2:00}", t.Minutes, t.Seconds, t.Milliseconds / 10);
    }

    public void ActualizarTexto(Bolita.EstadoLanzamiento estado)
    {
        if (estadoLabel == null)
        {
            Debug.LogWarning("UiManager: Falta asignar el campo 'estadoLabel'.");
            return;
        }

        switch (estado)
        {
            case Bolita.EstadoLanzamiento.PendienteDeLanzar:
                estadoLabel.text = "Lanza la bola";
                break;
            case Bolita.EstadoLanzamiento.EnElAire:
                estadoLabel.text = "Atrapa las fichas";
                break;
            case Bolita.EstadoLanzamiento.TocadaPorJugador:
                estadoLabel.text = "Decidiendo etapa"; // estado intermedio
                break;
            case Bolita.EstadoLanzamiento.Fallado:
                estadoLabel.text = "Perdiste";
                break;
            default:
                estadoLabel.text = string.Empty;
                break;
        }
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

    private static void SetTextoPuntos(TMP_Text label, long valor)
    {
        if (label != null) label.text = valor.ToString();
    }
}
