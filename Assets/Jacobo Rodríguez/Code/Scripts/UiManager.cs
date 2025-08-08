using UnityEngine;
using TMPro;
using System;

public class UiManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Bolita bolita;        // Arrastra la Bolita desde la escena
    [SerializeField] private TMP_Text estadoLabel; // Arrastra un TextMeshProUGUI o TMP_Text
    [SerializeField] private TMP_Text cronometroLabel; // Texto del cronómetro

    private bool _cronometroActivo;
    private float _tiempo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (cronometroLabel != null) cronometroLabel.gameObject.SetActive(false);
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
            case Bolita.EstadoLanzamiento.Terminado:
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
                estadoLabel.text = "atrapa las fichas";
                break;
            case Bolita.EstadoLanzamiento.Terminado:
                estadoLabel.text = "perdiste";
                break;
            default:
                estadoLabel.text = string.Empty;
                break;
        }
    }
}
