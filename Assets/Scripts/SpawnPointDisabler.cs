using UnityEngine;

public class SpawnPointDisabler : MonoBehaviour
{
    [SerializeField] private GameObject childToDisable; // referencia al hijo a desactivar

    private void Awake()
    {
        // Si no asignas manualmente, toma el primer hijo
        if (childToDisable == null && transform.childCount > 0)
        {
            childToDisable = transform.GetChild(0).gameObject;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
  
        // No desactivamos inmediatamente, esperamos al final del frame
        Invoke(nameof(DisableChild), 0.1f);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
  
        if (childToDisable != null)
            childToDisable.SetActive(true);
    }

    private void DisableChild()
    {
        if (childToDisable != null)
        {
            childToDisable.SetActive(false);
       
        }
    }
}
