using UnityEngine;

// Pega este script en el mismo GameObject que reproduce la animación (el que tiene el Animator)
// para poder recibir el Animation Event y reenviar el cierre al Progression.
public class VictoryAnimationEventProxy : MonoBehaviour
{
    [Tooltip("Opcional: arrastra aquí el objeto que tiene Progression. Si se deja vacío se buscará automáticamente en escena.")]
    public Progression progression;

    // Llama a este método desde el Animation Event (último frame de la animación de victoria)
    public void OnAnimacionFinalizada()
    {
        var prog = progression != null ? progression : FindAnyObjectByType<Progression>();
        if (prog != null)
        {
            prog.FinalizarPorAnimacion();
        }
        else
        {
            Debug.LogWarning("[VictoryAnimationEventProxy] Progression no encontrado; no se pudo finalizar por Animation Event.");
        }
    }
}
