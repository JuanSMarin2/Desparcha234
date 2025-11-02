using UnityEngine;

public class MarbleExplosion : MonoBehaviour
{
    private float timer = 0f;
    private const float deactivateTime = 1f;

    void OnEnable()
    {
        SoundManager.instance.PlaySfx("Canicas:explosion");
        timer = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= deactivateTime)
        {
            gameObject.SetActive(false);
        }
    }
}
