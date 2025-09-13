using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]

public class RandomBackgroundSelector : MonoBehaviour
{
    [SerializeField] private Sprite[] backgrounds; // Sprites de fondo

    void Start()
    {
        var imagenfondo = GetComponent<Image>();
        if (imagenfondo == null || backgrounds == null || backgrounds.Length == 0) return;

        int indiceAleatorio = Random.Range(0, backgrounds.Length);
        imagenfondo.sprite = backgrounds[indiceAleatorio]; // asignar sprite
        Debug.Log($"[RandomBackgroundSelector] Fondo aleatorio asignado: {backgrounds[indiceAleatorio].name}");
    }
}