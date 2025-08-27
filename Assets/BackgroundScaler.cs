using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class BackgroundScaler : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null || sr.sprite == null) return;

        // Tama�o visible de la c�mara en unidades del mundo
        float camHeight = targetCamera.orthographicSize * 2f;
        float camWidth = camHeight * targetCamera.aspect;

        // Tama�o del sprite en unidades del mundo
        float spriteWidth = sr.sprite.bounds.size.x;
        float spriteHeight = sr.sprite.bounds.size.y;

        // Escala necesaria para abarcar toda la c�mara
        Vector3 scale = transform.localScale;
        scale.x = camWidth / spriteWidth;
        scale.y = camHeight / spriteHeight;
        transform.localScale = scale;
    }
}