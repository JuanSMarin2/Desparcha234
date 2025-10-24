using UnityEngine;
using TMPro;
using System.Collections;

public class ScorePopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text tmpText;

    [Header("Animación")]
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float startScale = 0.85f;
    [SerializeField] private float endScale = 1.25f;
    [SerializeField] private float moveUpDistance = 1.0f;
    [SerializeField] private Vector2 jitterXY = new Vector2(0.15f, 0.1f);
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool unscaledTime = true;

    [Header("Sorting (WorldSpace)")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 200;

    [Header("Orientación por jugador")]
    [Tooltip("Si está activo, rota 180° el popup para los jugadores 2 y 4 (1-based) para facilitar lectura")]
    [SerializeField] private bool rotateForPlayers2And4 = true;

    private ScorePopupManager _manager;
    private Color _baseColor = Color.white;
    private Quaternion _baseRotation;

    void Awake()
    {
        if (tmpText == null) tmpText = GetComponentInChildren<TMP_Text>(true);
        // Intentar aplicar sorting si es MeshRenderer (TextMeshPro 3D)
        var mr = GetComponentInChildren<MeshRenderer>(true);
        if (mr != null && !string.IsNullOrEmpty(sortingLayerName))
        {
            mr.sortingLayerName = sortingLayerName;
            mr.sortingOrder = sortingOrder;
        }
        _baseRotation = transform.rotation;
        gameObject.SetActive(false);
    }

    public void Init(ScorePopupManager manager)
    {
        _manager = manager;
    }

    public void Show(Vector3 worldPos, int points, Color? colorOverride = null)
    {
        if (tmpText == null) return;
        transform.position = worldPos + new Vector3(Random.Range(-jitterXY.x, jitterXY.x), Random.Range(-jitterXY.y, jitterXY.y), 0f);
        tmpText.text = points >= 0 ? $"+{points}" : points.ToString();

        _baseColor = colorOverride ?? tmpText.color;
        var c = _baseColor; c.a = 1f; tmpText.color = c;
        transform.localScale = Vector3.one * startScale;

        // Decidir orientación según turno actual (jugadores 2 y 4 => 180°)
        if (rotateForPlayers2And4)
        {
            int idx0 = -1;
            if (TurnManager.instance != null)
            {
                idx0 = TurnManager.instance.GetCurrentPlayerIndex(); // 0-based
            }
            int idx1 = (idx0 >= 0) ? idx0 + 1 : -1;
            bool flip = (idx1 == 2) || (idx1 == 4);
            transform.rotation = flip ? _baseRotation * Quaternion.Euler(0f, 0f, 180f) : _baseRotation;
        }
        else
        {
            transform.rotation = _baseRotation;
        }

        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(CoAnim());
    }

    private IEnumerator CoAnim()
    {
        float t = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * moveUpDistance;
        float dt() => unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        while (t < duration)
        {
            t += dt();
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));
            float e = ease != null ? Mathf.Clamp01(ease.Evaluate(k)) : k;

            transform.position = Vector3.LerpUnclamped(startPos, endPos, e);
            float s = Mathf.LerpUnclamped(startScale, endScale, e);
            transform.localScale = new Vector3(s, s, 1f);

            var col = _baseColor; col.a = 1f - e; tmpText.color = col;
            yield return null;
        }
        // Finalizar
        if (_manager != null) _manager.Recycle(this); else gameObject.SetActive(false);
    }
}
