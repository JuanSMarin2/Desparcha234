using UnityEngine;

[DefaultExecutionOrder(-50)]
public class AudioKeysValidator : MonoBehaviour
{
    [Header("SFX keys required in SceneAudioLibrary")]
    [SerializeField] private string[] requiredSfxKeys = new string[]
    {
        "catapis:warning",
        "catapis:woosh",
        "catapis:tick",
        "catapis:error",
        "catapis:atrapada",
        "lleva:Eliminado",
        "lleva:cronometro",
        "lleva:victoria",
        "lleva:Start",
        "lleva:TagTransfer"
    };

    [SerializeField] private bool logOnce = true;
    private bool _logged;

    void Start()
    {
        Validate();
    }

    [ContextMenu("Validate Now")]
    public void Validate()
    {
        if (logOnce && _logged) return;
        var sm = SoundManager.instance;
        if (sm == null)
        {
            Debug.LogWarning("[AudioKeysValidator] SoundManager.instance no encontrado en escena.");
            return;
        }
        int missing = 0;
        foreach (var k in requiredSfxKeys)
        {
            if (string.IsNullOrWhiteSpace(k)) continue;
            bool ok = sm.TryPlaySfx(k, 0f); // volumen 0 => solo verifica existencia
            if (!ok)
            {
                Debug.LogWarning($"[AudioKeysValidator] Falta registrar SFX key: '{k}' en alguna SceneAudioLibrary de la escena.");
                missing++;
            }
        }
        if (missing == 0)
            Debug.Log("[AudioKeysValidator] Todas las SFX keys requeridas están registradas.");
        _logged = true;
    }
}
