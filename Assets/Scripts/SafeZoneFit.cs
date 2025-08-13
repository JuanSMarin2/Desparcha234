using UnityEngine;

[ExecuteAlways]
public class SafeZoneFit : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [Header("Márgenes (unidades de mundo)")]
    [SerializeField] private float left = 0.5f;
    [SerializeField] private float right = 0.5f;
    [SerializeField] private float top = 0.5f;
    [SerializeField] private float bottom = 0.5f;

    [Header("Opcional")]
    [SerializeField] private SpriteRenderer sprite;      // si tu zona es un sprite rectangular
    [SerializeField] private BoxCollider2D box;          // si quieres que el collider coincida

    void Reset()
    {
        cam = Camera.main;
        sprite = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();
        if (sprite) sprite.drawMode = SpriteDrawMode.Sliced; // para poder usar sprite.size
    }

    void LateUpdate()
    {
        if (!cam || !cam.orthographic) return;

        // Tamaño visible de la cámara en mundo
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        // Tamaño de la zona segura (restando márgenes)
        float zoneW = Mathf.Max(0f, width - (left + right));
        float zoneH = Mathf.Max(0f, height - (top + bottom));

        // Centro de la cámara (en el plano 2D)
        Vector3 camCenter = cam.transform.position;
        camCenter.z = transform.position.z;

        // El centro de la zona se desplaza si los márgenes no son simétricos
        float cx = camCenter.x + (right - left) * 0.5f;
        float cy = camCenter.y + (bottom - top) * 0.5f;  // bottom > top => sube

        // Posicionar
        transform.position = new Vector3(cx, cy, transform.position.z);

        // Aplicar tamaño
        if (sprite && sprite.drawMode != SpriteDrawMode.Simple)
        {
            sprite.size = new Vector2(zoneW, zoneH);
        }
        else
        {
            // Escala directa (tu mesh/child debe medir 1x1 para que sea exacto)
            transform.localScale = new Vector3(zoneW, zoneH, 1f);
        }

        // Ajustar collider si existe
        if (box) box.size = new Vector2(zoneW, zoneH);
    }
}
