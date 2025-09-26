using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Image statusImage;  
    [SerializeField] private Sprite caidoImagen;
    [SerializeField] private Sprite ganaImagen;

    private bool llegoMeta = false;
    private bool estaCaido = false;

    void Start()
    {
        if (statusImage != null)
            statusImage.gameObject.SetActive(false); // siempre empieza desactivado
    }

    // Llamar cuando el jugador se caiga
    public void SetCaido()
    {
        estaCaido = true;
        llegoMeta = false;
        if (statusImage != null && caidoImagen != null)
        {
            statusImage.sprite = caidoImagen;
            statusImage.gameObject.SetActive(true);
        }
    }

    // Llamar cuando el jugador gane
    public void SetGana()
    {
        llegoMeta = true;
        estaCaido = false;
        if (statusImage != null && ganaImagen != null)
        {
            statusImage.sprite = ganaImagen;
            statusImage.gameObject.SetActive(true);
        }
    }

    // Reset / desactivar en cualquier otro caso
    public void ResetEstado()
    {
        estaCaido = false;
        llegoMeta = false;
        if (statusImage != null)
            statusImage.gameObject.SetActive(false);
    }
}
