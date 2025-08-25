using UnityEngine;

public class BorderFeedback : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 360f; 
    [SerializeField] private MarblePower[] marbles;      

    void Update()
    {
        if (TurnManager.instance == null || marbles == null || marbles.Length == 0) return;

        int currentIndex = TurnManager.instance.GetCurrentPlayerIndex();

        if (currentIndex < 0 || currentIndex >= marbles.Length) return;

      
        transform.position = marbles[currentIndex].transform.position;

    
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }
}
