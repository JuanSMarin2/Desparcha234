using UnityEngine;
using UnityEngine.EventSystems;

// Adjunta este script al GameObject del botón (necesita componente gráfico + Canvas con GraphicRaycaster)
// Controla el movimiento por "hold" del jugador indicado usando el sistema de Movimiento.
public class BotonMoverse : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Jugador controlado")] [Range(1,4)] [SerializeField] private int playerIndex = 1;
    [Header("Debug")] [SerializeField] private bool logEvents = false;

    private bool _pressed = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
        Movimiento.StartHoldForPlayer(playerIndex);
        if (logEvents) Debug.Log($"[BotonMoverse] DOWN player {playerIndex}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_pressed) return;
        _pressed = false;
        Movimiento.StopHoldForPlayer(playerIndex);
        if (logEvents) Debug.Log($"[BotonMoverse] UP player {playerIndex}");
    }

    // Si el dedo / cursor sale del botón sin soltar, detener para evitar quedarse moviendo
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_pressed)
        {
            _pressed = false;
            Movimiento.StopHoldForPlayer(playerIndex);
            if (logEvents) Debug.Log($"[BotonMoverse] EXIT player {playerIndex}");
        }
    }

    private void OnDisable()
    {
        if (_pressed)
        {
            _pressed = false;
            Movimiento.StopHoldForPlayer(playerIndex);
            if (logEvents) Debug.Log($"[BotonMoverse] DISABLE cleanup player {playerIndex}");
        }
    }
}
