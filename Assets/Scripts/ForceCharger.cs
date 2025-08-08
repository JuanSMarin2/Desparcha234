using UnityEngine;

public class ForceCharger : MonoBehaviour
{
    [SerializeField] private JoystickController joystick;
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private float step = 0.1f;
    [SerializeField] private Transform forceBar;

    public float fuerza { get; private set; } = 1f;
    private bool increasing = true;
    private bool wasDragging = false;

    public System.Action<float> OnReleaseForce;
    [SerializeField] private Transform arrowObject;

    void Start()
    {
        StartCoroutine(ChargeRoutine());
    }

    private System.Collections.IEnumerator ChargeRoutine()
    {
        while (true)
        {
            if (joystick.IsDragging)
            {
                if (increasing)
                {
                    fuerza += step;
                    if (fuerza >= 5f)
                    {
                        fuerza = 5f;
                        increasing = false;
                    }
                }
                else
                {
                    fuerza -= step;
                    if (fuerza <= 1f)
                    {
                        fuerza = 1f;
                        increasing = true;
                    }
                }

                UpdateForceBar();
            }

            if (!joystick.IsDragging && wasDragging)
            {
                Debug.Log($"Fuerza final: {fuerza:F2}");

                OnReleaseForce?.Invoke(fuerza);

            }

            wasDragging = joystick.IsDragging;

            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void UpdateForceBar()
    {
        if (forceBar == null) return;

        float minScale = 0.01f;
        float maxScale = 0.3f;
        float t = Mathf.InverseLerp(1f, 5f, fuerza);

        Vector3 scale = forceBar.localScale;
        scale.y = Mathf.Lerp(minScale, maxScale, t);
        forceBar.localScale = scale;
    }
}
