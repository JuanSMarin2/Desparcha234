using UnityEngine;

public class PowerUpController : MonoBehaviour
{
    private bool wasCounted = false;
    private bool isBeingDestroyed = false;

    private void OnEnable()
    {
        if (!wasCounted)
        {
            PowerUpRegistry.ActiveCount++;
            wasCounted = true;
            Debug.Log("PowerUp Apareci� - Total: " + PowerUpRegistry.ActiveCount);
        }
    }

    private void OnDisable()
    {
        // Solo restar si no est� siendo destruido y ya fue contado
        if (!isBeingDestroyed && wasCounted)
        {
            PowerUpRegistry.ActiveCount--;
            wasCounted = false;
            Debug.Log("PowerUp Desactivado - Total: " + PowerUpRegistry.ActiveCount);
        }
    }

    private void OnDestroy()
    {
        isBeingDestroyed = true;
        if (wasCounted)
        {
            PowerUpRegistry.ActiveCount--;
            wasCounted = false;
            Debug.Log("PowerUp Destruido - Total: " + PowerUpRegistry.ActiveCount);
        }
    }

    // M�todo para manejar recolecci�n manualmente
    public void HandleCollection()
    {
        if (wasCounted)
        {
            PowerUpRegistry.ActiveCount--;
            wasCounted = false;
            Debug.Log("PowerUp Recogido - Total: " + PowerUpRegistry.ActiveCount);
        }
    }
}