using UnityEngine;

// Adjunta este script al GameObject que reproduce la animación de victoria (el que tiene el Animator)
// y desde el último frame (Animation Event) llama al método OnVictoryAnimationFinished para notificar al TagManager.
public class VictoryTagAnimationEventProxy : MonoBehaviour
{
    [Tooltip("Opcional: referencia manual al TagManager. Si se deja vacío se usa TagManager.Instance")] 
    public TagManager tagManager;

    // Llamar este método desde un Animation Event al final de la animación de victoria.
    public void OnVictoryAnimationFinished()
    {
        var mgr = tagManager != null ? tagManager : TagManager.Instance;
        if (mgr != null)
        {
            mgr.OnVictoryAnimationFinished();
        }
        else
        {
            Debug.LogWarning("[VictoryTagAnimationEventProxy] TagManager no encontrado al finalizar animación de victoria.");
        }
    }

    // (Opcional) Otro evento intermedio por si quieres SFX o efectos.
    public void OnVictoryAnimationMidPoint()
    {
        // Placeholder para SFX / efectos intermedios. Puedes rellenar luego.
        // Debug.Log("[VictoryTagAnimationEventProxy] Midpoint animación de victoria");
    }
}
