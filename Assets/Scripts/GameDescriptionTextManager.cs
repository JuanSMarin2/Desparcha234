using System.Collections;
using UnityEngine;

public class GameDescriptionTextManager : MonoBehaviour
{
    [SerializeField] private GameObject textObject;


    private void Start()
    {
        textObject.SetActive(true);
    }

    public void Continue()
    {
        textObject.SetActive(false);
    }
}
