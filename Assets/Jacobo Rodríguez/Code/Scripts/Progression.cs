using UnityEngine;

public class Progression : MonoBehaviour
{
    bool touchedBall = false;
    public int stage = 0;
    public int neededJacks = 0;

    public int jacksCounter = 0;
    public long currentScore = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NotificarJackTocado(Jack jack)
    {
        if (jack != null)
        {
            jacksCounter++;
            currentScore += jack.Puntos;
            Debug.Log($"Jack recolectado (+{jack.Puntos}). Total jacks: {jacksCounter}, Puntos: {currentScore}");
        }
    }

    public void NotificarBolitaTocada()
    {
        touchedBall = true;
        Debug.Log("Bolita tocada");
    }
}
