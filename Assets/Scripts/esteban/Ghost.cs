using UnityEngine;

/// <summary>
/// Ghost power-up
/// - En cada turno aparece en una posición aleatoria (candidatePositions).
/// - Al ser recogido por una Papeleta, se desactiva (consumible) y el FADE DE ALPHA se aplica a la PAPELETA:
///   1 -> 0.5 en la recogida, 0.5 -> 0 al lanzamiento, y 0/0.5 -> 1 al cambiar de turno.
/// - Si una Papeleta ya tiene un power-up (vía PowerUpStateManager), no puede recoger Ghost.
/// </summary>
public class Ghost : MonoBehaviour
{
    [Header("Posibles ubicaciones")]
    [SerializeField] private Transform[] candidatePositions;
    [SerializeField] private bool matchRotation = false;
    [SerializeField] private bool preserveZ = true;
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    [Header("Detección de Papeletas")]
    [SerializeField] private string[] papeletaTags = new string[] { "Papeleta", "Papeleta1", "Papeleta2", "Papeleta3", "Papeleta4" };

    private int lastIndex = -1;
    private Collider2D col2d;

    private void Awake()
    {
        col2d = GetComponent<Collider2D>();
        if (col2d == null) col2d = GetComponentInChildren<Collider2D>();
    }

    private void Start()
    {
        // Colocación inicial cuando está activo en escena
        MoveToRandom(differentFromIndex: -1);
        SetColliderEnabled(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPapeleta(other)) return;

        // Bloquear si ya tiene power-up
        if (PowerUpStateManager.Instance != null && !PowerUpStateManager.Instance.CanPickup(other.transform))
            return;

        // Aplicar efecto Ghost a la PAPELETA (fades los gestiona el manager)
        if (GhostManager.Instance != null)
        {
            GhostManager.Instance.ApplyGhost(other.transform);
            GhostManager.Instance.RegisterConsumable(this);

            // SFX de power-up
            if (SoundManager.instance != null)
                SoundManager.instance.PlaySfx("Tejo:PowerUp");
            else
            {
                var sm = FindAnyObjectByType<SoundManager>();
                if (sm != null) sm.PlaySfx("Tejo:PowerUp");
            }
        }
        else
        {
            // Fallback: al menos registra el poder en el estado global
            if (PowerUpStateManager.Instance != null)
                PowerUpStateManager.Instance.MarkHasPowerUp(other.transform);

            // SFX de power-up
            if (SoundManager.instance != null)
                SoundManager.instance.PlaySfx("Tejo:PowerUp");
            else
            {
                var sm = FindAnyObjectByType<SoundManager>();
                if (sm != null) sm.PlaySfx("Tejo:PowerUp");
            }
        }

        // Consumible: desactivar el pickup por completo (lógica seguirá en el manager)
        gameObject.SetActive(false);
    }

    // Llamado por el GhostManager al cambiar de turno para reactivar y reubicar
    public void ReactivateForNewTurn()
    {
        // Reposicionar (e.g. distinto índice si es posible)
        MoveToRandom(differentFromIndex: lastIndex);
        SetColliderEnabled(true);
        gameObject.SetActive(true);
    }

    private void MoveToRandom(int differentFromIndex)
    {
        if (candidatePositions == null || candidatePositions.Length == 0) return;
        int n = candidatePositions.Length;
        int newIndex = 0;
        if (n > 1)
        {
            int guard = 0;
            do
            {
                newIndex = Random.Range(0, n);
                guard++;
                if (guard > 50) break;
            } while (newIndex == differentFromIndex || candidatePositions[newIndex] == null);
        }
        else newIndex = 0;

        var target = candidatePositions[newIndex];
        if (target == null) return;

        Vector3 pos = target.position + worldOffset;
        if (preserveZ) pos.z = transform.position.z;
        transform.position = pos;
        if (matchRotation)
            transform.rotation = target.rotation;

        lastIndex = newIndex;
    }

    private bool IsPapeleta(Collider2D col)
    {
        if (col == null) return false;
        string t = col.tag;
        if (string.IsNullOrEmpty(t)) return false;
        if (papeletaTags == null || papeletaTags.Length == 0) return t.StartsWith("Papeleta");
        for (int i = 0; i < papeletaTags.Length; i++)
            if (t == papeletaTags[i]) return true;
        return false;
    }

    private void SetColliderEnabled(bool enabled)
    {
        if (col2d != null) col2d.enabled = enabled;
    }

    // Eliminado: fades ahora se aplican sobre la papeleta y los maneja GhostManager
}
