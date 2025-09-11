using System.Collections;
using UnityEngine;

public class GameDescriptionTextManager : MonoBehaviour
{
    [SerializeField] private GameObject textObject;
    [SerializeField] private byte delay;
    private void Start()
    {
        StartCoroutine(TimeToBanish());
    }

    private IEnumerator TimeToBanish()
    {


        yield return new WaitForSeconds(delay);
        textObject.SetActive(false);
    }
}
