using UnityEngine;

public class ForceCharger : MonoBehaviour
{
    [SerializeField] private JoystickController joystick;
    [SerializeField] private float updateInterval = 0.1f; // cada cuánto subir/bajar
    [SerializeField] private float step = 0.1f;           // cuánto sube/baja por tick
    [SerializeField] private Transform forceBar;
    [SerializeField] private Transform arrowObject;

    public float fuerza { get; private set; } = 1f;
    private bool increasing = true;
    private bool wasDragging = false;

    public System.Action<float> OnReleaseForce;

    private float tickTimer = 0f;

    void Update()
    {
        // usamos tiempo NO escalado para ser idénticos en móvil/editor
        float dt = Time.unscaledDeltaTime;

        if (joystick.IsDragging)
        {
            tickTimer += dt;

            // procesamos todos los ticks acumulados (por si hubo frames lentos)
            while (tickTimer >= updateInterval)
            {
                tickTimer -= updateInterval;

                if (increasing)
                {
                    fuerza += step;
                    if (fuerza >= 5f) { fuerza = 5f; increasing = false; }
                }
                else
                {
                    fuerza -= step;
                    if (fuerza <= 1f) { fuerza = 1f; increasing = true; }
                }

                UpdateForceBar();
            }
        }

        // al soltar, disparamos el evento una única vez
        if (!joystick.IsDragging && wasDragging)
        {
            Debug.Log($"Fuerza final: {fuerza:F2}");
            OnReleaseForce?.Invoke(fuerza);
            tickTimer = 0f; // opcional: resetea el acumulador
        }

        wasDragging = joystick.IsDragging;
    }

    private void UpdateForceBar()
    {
        if (!forceBar) return;

        float minScale = 0.01f;
        float maxScale = 0.17f;
        float t = Mathf.InverseLerp(1f, 5f, fuerza);

        var s = forceBar.localScale;
        s.y = Mathf.Lerp(minScale, maxScale, t);
        forceBar.localScale = s;
    }
}
