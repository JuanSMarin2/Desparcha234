using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData instance;

    [Header("Economia")]
    [SerializeField] private int money = 0;

    [Header("Skins por jugador")]
    [Tooltip("Skin equipada por jugador (0..3). -1 si ninguna.")]
    [SerializeField] private int[] equippedSkin = new int[4] { -1, -1, -1, -1 };

    // Para compatibilidad con tamaños variables de skins, usamos listas por jugador
    // ownedPerPlayer[p][s] = true si el jugador p posee la skin s
    [SerializeField] private List<bool>[] ownedPerPlayer = new List<bool>[4];

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            for (int p = 0; p < 4; p++)
            {
                if (ownedPerPlayer[p] == null) ownedPerPlayer[p] = new List<bool>();

                // asegurar espacio para al menos 1 skin
                EnsureSkinSlots(p, 1);

                // Skin 0 siempre comprada
                ownedPerPlayer[p][0] = true;

                // Skin 0 equipada
                equippedSkin[p] = 0;
            }
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

    public bool TrySpendMoney(int amount)
    {
        if (amount < 0) return false;
        if (money < amount) return false;
        money -= amount;
        return true;
    }

    // Asegura que la lista del jugador tenga al menos size elementos
    public void EnsureSkinSlots(int playerIndex, int size)
    {
        var list = ownedPerPlayer[playerIndex];
        while (list.Count < size) list.Add(false);
    }

    public bool IsOwned(int playerIndex, int skinNumber)
    {
        if (playerIndex < 0 || playerIndex >= 4) return false;

        EnsureSkinSlots(playerIndex, skinNumber + 1);
        return ownedPerPlayer[playerIndex][skinNumber];
    }

    public void SetOwned(int playerIndex, int skinNumber, bool owned)
    {
        if (playerIndex < 0 || playerIndex >= 4) return;
        EnsureSkinSlots(playerIndex, skinNumber + 1);
        ownedPerPlayer[playerIndex][skinNumber] = owned;
    }

    public void EquipSkin(int playerIndex, int skinNumber)
    {
        if (playerIndex < 0 || playerIndex >= 4) return;
        equippedSkin[playerIndex] = skinNumber;
        Debug.Log("Jugador " + (playerIndex + 1) + " equipa skin " + skinNumber);
    }

    public int GetEquipped(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= 4) return -1;
        return equippedSkin[playerIndex];
    }
}
