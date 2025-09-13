using System.Collections;
using UnityEngine;

public class GameDescriptionTextManager : MonoBehaviour
{
    [SerializeField] private GameObject textObject;


    public void Continue()
    {
        textObject.SetActive(false);
    }
}
