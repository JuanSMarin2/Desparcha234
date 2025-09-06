using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData instance;

    [Header("Economia")]
    [SerializeField] private int money = 0;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int Money => money;

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        money += amount;
        Debug.Log("GameData money actualizado. Suma: " + amount + " Total: " + money);
    }
}
