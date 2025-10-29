using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Graphic (Image/RawImage/TMP)

[RequireComponent(typeof(MarblePower))]
public class PowerUpFeedback : MonoBehaviour
{
    [Header("Follow (no parenting)")]
    [SerializeField] private Transform target;            // canica
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [SerializeField] private bool lockZ = true;
    [SerializeField] private float fixedZ = 0f;

    [Header("Refs")]
    [SerializeField] private MarblePower marblePower;     // auto si null
    [SerializeField] private Animator animator;           // estados Base/Ghost/Power/Shield
    [SerializeField] private CanvasGroup canvasFX;        // (opcional) UI
    [SerializeField] private SpriteRenderer spriteFX;     // (opcional) mundo
    [SerializeField] private Graphic graphicFX;           // (opcional) UI Image/TMP/etc

    [Header("Animator Triggers")]
    [SerializeField] private string baseTrigger = "Base";
    [SerializeField] private string ghostTrigger = "Ghost";
    [SerializeField] private string powerTrigger = "Power";
    [SerializeField] private string shieldTrigger = "Shield";

    [Header("Fades")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private float ensureAlphaDuration = 0.12f;
    [SerializeField] private bool forceResetAlphaOnGain = true;

    private MarblePowerType lastType = MarblePowerType.None;
    private Coroutine fadeCo;
    private Coroutine switchCo;

    void Reset()
    {
        marblePower = GetComponent<MarblePower>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!canvasFX) canvasFX = GetComponentInChildren<CanvasGroup>();
        if (!spriteFX) spriteFX = GetComponentInChildren<SpriteRenderer>();
        if (!graphicFX) graphicFX = GetComponentInChildren<Graphic>();
    }

    void Awake()
    {
        if (!marblePower) marblePower = GetComponent<MarblePower>();
        ApplyAlpha(0f);                    // oculto al inicio
        lastType = MarblePowerType.None;
    }

    void Update()
    {
        var current = marblePower ? marblePower.CurrentType : MarblePowerType.None;
        if (current != lastType)
        {
            if (switchCo != null) StopCoroutine(switchCo);
            switchCo = StartCoroutine(HandleTransition(lastType, current));
            lastType = current;
        }
    }

    void LateUpdate()
    {
        if (!target) return;
        var pos = target.position + worldOffset;
        if (lockZ) pos.z = fixedZ;
        transform.position = pos; // sin heredar rot/escala
    }

    private IEnumerator HandleTransition(MarblePowerType from, MarblePowerType to)
    {
        // NONE -> POWER_X : TRIGGER PRIMERO, luego fade-in
        if (from == MarblePowerType.None && to != MarblePowerType.None)
        {
            gameObject.SetActive(true);

            // 1) activar estado visual primero
            FirePowerTrigger(to);

            // 2) asegurar alpha 0 y hacer fade-in
            if (forceResetAlphaOnGain) ApplyAlpha(0f);
            if (fadeCo != null) StopCoroutine(fadeCo);
            yield return (fadeCo = StartCoroutine(FadeTo(1f, fadeInDuration)));
            yield break;
        }

        // POWER_A -> POWER_B : cambiar trigger primero, luego asegurar alpha 1 (rápido)
        if (from != MarblePowerType.None && to != MarblePowerType.None)
        {
            FirePowerTrigger(to);
            if (fadeCo != null) StopCoroutine(fadeCo);
            yield return (fadeCo = StartCoroutine(FadeTo(1f, ensureAlphaDuration)));
            yield break;
        }

        // POWER -> NONE : fade-out y AL FINAL volver a Base
        if (from != MarblePowerType.None && to == MarblePowerType.None)
        {
            if (fadeCo != null) StopCoroutine(fadeCo);
            yield return (fadeCo = StartCoroutine(FadeTo(0f, fadeOutDuration)));
            FireBase(); // base no tiene sprite, se hace al final
            yield break;
        }

        // NONE -> NONE : asegurar invisible/base
        if (to == MarblePowerType.None)
        {
            if (fadeCo != null) StopCoroutine(fadeCo);
            yield return (fadeCo = StartCoroutine(FadeTo(0f, fadeOutDuration)));
            FireBase();
        }
    }

    // -------- Animator helpers --------
    private void FireBase()
    {
        if (!animator) return;
        animator.ResetTrigger(baseTrigger);
        animator.SetTrigger(baseTrigger);
    }

    private void FirePowerTrigger(MarblePowerType t)
    {
        if (!animator) return;
        animator.ResetTrigger(ghostTrigger);
        animator.ResetTrigger(powerTrigger);
        animator.ResetTrigger(shieldTrigger);

        switch (t)
        {
            case MarblePowerType.Ghost: animator.SetTrigger(ghostTrigger); break;
            case MarblePowerType.MorePower: animator.SetTrigger(powerTrigger); break;
            case MarblePowerType.Unmovable: animator.SetTrigger(shieldTrigger); break;
        }
    }

    // -------- Fade helpers --------
    private IEnumerator FadeTo(float target, float duration)
    {
        float a0 = GetCurrentAlpha();
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            ApplyAlpha(Mathf.Lerp(a0, target, k));
            yield return null;
        }
        ApplyAlpha(target);
    }

    private float GetCurrentAlpha()
    {
        if (canvasFX) return canvasFX.alpha;
        if (spriteFX) return spriteFX.color.a;
        if (graphicFX) return graphicFX.color.a;
        return 1f;
    }

    private void ApplyAlpha(float a)
    {
        if (canvasFX) canvasFX.alpha = a;

        if (spriteFX)
        {
            var c = spriteFX.color; c.a = a; spriteFX.color = c;
        }

        if (graphicFX)
        {
            var c = graphicFX.color; c.a = a; graphicFX.color = c;
        }
    }

    // -------- API pública --------
    public void SetFollowTarget(Transform t) => target = t;

    public void ForceRefresh()
    {
        var current = marblePower ? marblePower.CurrentType : MarblePowerType.None;
        lastType = MarblePowerType.None;
        if (switchCo != null) StopCoroutine(switchCo);
        switchCo = StartCoroutine(HandleTransition(MarblePowerType.None, current));
        lastType = current;
    }
}
