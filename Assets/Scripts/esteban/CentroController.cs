using UnityEngine;

public class CentroController : MonoBehaviour
{
    [Header("Límites del tablero")]
    public float minX = -5f;
    public float maxX = 5f;
    public float minY = -3f;
    public float maxY = 3f;

    [Header("Gizmos (visualización en editor)")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color areaColor = new Color(0f, 0.5f, 1f, 0.1f);
    [SerializeField] private Color borderColor = new Color(0f, 0.8f, 1f, 0.8f);
    [SerializeField] private Color crossColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float crossSize = 0.25f;

    // Llamar este método manualmente (por ejemplo, desde otro script o un botón en el inspector)
    public void MoverCentro()
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);

        transform.position = new Vector3(randomX, randomY, transform.position.z);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Dibuja área de movimiento
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, transform.position.z);
        Vector3 size = new Vector3(Mathf.Abs(maxX - minX), Mathf.Abs(maxY - minY), 0.01f);

        Color prev = Gizmos.color;

        // Área semitransparente
        Gizmos.color = areaColor;
        Gizmos.DrawCube(center, size);

        // Contorno
        Gizmos.color = borderColor;
        Gizmos.DrawWireCube(center, size);

        // Cruz en la posición actual
        Gizmos.color = crossColor;
        float s = crossSize;
        Gizmos.DrawLine(transform.position + new Vector3(-s, 0, 0), transform.position + new Vector3(s, 0, 0));
        Gizmos.DrawLine(transform.position + new Vector3(0, -s, 0), transform.position + new Vector3(0, s, 0));

        Gizmos.color = prev;
    }

    private void OnValidate()
    {
        // Mantener coherencia si se modifican los límites
        if (maxX < minX) maxX = minX;
        if (maxY < minY) maxY = minY;
    }
}
