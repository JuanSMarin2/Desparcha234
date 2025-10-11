using UnityEngine;
using UnityEngine.UI;

public class Semaforo : MonoBehaviour
{
	[Header("Timer reference")]
	[SerializeField] private StartTimintg timer;

	[Header("Sound")]
	[SerializeField] public AudioClip semaforoSound;
	[SerializeField] private AudioSource globalASource;

	[Header("Sprites for seconds 0..3")]
 	[SerializeField] private Sprite image0;
	[SerializeField] private Sprite image1;
	[SerializeField] private Sprite image2;
	[SerializeField] private Sprite image3;

	[Header("Target Image (will be on the same GameObject)")]
	[SerializeField] private Image targetImage;

	void Start()
	{
		globalASource.PlayOneShot(semaforoSound);
		// If no explicit targetImage set, try to get one from this GameObject
		if (targetImage == null)
			targetImage = GetComponent<Image>();

		// If timer not set, attempt to find one in scene
		if (timer == null)
			timer = Object.FindFirstObjectByType<StartTimintg>();
		var sm = SoundManager.instance;
		if (sm != null)
		{
			sm.PlaySfx("semaforo:Start",0.8f);
		}
		
	}

	void Update()
	{
		if (timer == null || targetImage == null) return;

		// Determine which second we're at: 0,1,2,3
		float remaining = timer.RemainingSeconds;

		// Calculate elapsed seconds since start: elapsed = total - remaining
		int elapsed = Mathf.FloorToInt(timer.TotalSeconds - remaining);

		// Clamp elapsed to 0..3 for index
		if (elapsed < 0) elapsed = 0;

		// Determine last index based on available sprites (hardcoded 3 here)
		int lastIndex = 3;

		// Ensure elapsed does not exceed lastIndex for sprite selection
		int spriteIndex = Mathf.Clamp(elapsed, 0, lastIndex);

		// Swap the sprite based on spriteIndex
		switch (spriteIndex)
		{
			case 0: if (image0 != null) targetImage.sprite = image0; break;
			case 1: if (image1 != null) targetImage.sprite = image1; break;
			case 2: if (image2 != null) targetImage.sprite = image2; break;
			case 3: if (image3 != null) targetImage.sprite = image3; break;
		}

		// If we've reached the last sprite, start the one-time 1s wait then disable
		if (elapsed >= lastIndex && !hasStartedDisable)
		{
			hasStartedDisable = true;
			StartCoroutine(DisableAfterOneSecond());
		}
	}

	private bool hasStartedDisable = false;

	private System.Collections.IEnumerator DisableAfterOneSecond()
	{
		yield return new WaitForSeconds(1f);
		gameObject.SetActive(false);
	}

}
