using UnityEngine;

public class GameSetupManager : MonoBehaviour
{
    [Header("Configuración de Objetos por Número de Jugadores")]
   

    [Space(10)]
    [Header("Objetos que solo se activan si el numero de jugadores es 3 o 4. ")]
    [Tooltip("Objetos que solo se activan si el numero de jugadores es 3 o 4. ") ]
    [SerializeField] private GameObject[] objectsFor3Players;

    [Space(10)]
    [Header("Objetos que solo se activan si el numero de jugadores es 4. ")]
    [Tooltip("Objetos que solo se activan si el numero de jugadores es 4. ")]
    [SerializeField] private GameObject[] objectsFor4Players;

    void Start()
    {

        int numPlayers = RoundData.instance.numPlayers;

        switch (numPlayers)
        {
            case 2:

                SetObjectsActive(objectsFor3Players, false);
                SetObjectsActive(objectsFor4Players, false);
                break;
            case 3:


                SetObjectsActive(objectsFor3Players, true);
                SetObjectsActive(objectsFor4Players, false);
                break;
            case 4:
                SetObjectsActive(objectsFor3Players, true);
                SetObjectsActive(objectsFor4Players, true);
                break;
            default:
                Debug.LogError("Número de jugadores no válido: " + numPlayers);
                break;
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool isActive)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }
        }
    }
}