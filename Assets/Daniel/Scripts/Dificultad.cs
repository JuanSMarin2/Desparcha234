using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Utilidad central para leer la cantidad de jugadores activos y ajustar dificultad.
public static class Dificultad
{
    // Intenta deducir la cantidad de jugadores activos desde TurnManager usando nombres comunes.
    // Si no se puede determinar, retorna 4 como valor por defecto (configuración base).
    public static int GetActivePlayersCount()
    {
        var tm = TurnManager.instance;
        if (tm == null) return 4;

        // 1) Intentar métodos que devuelvan índices activos u orden
        string[] methodCandidates = new[]
        {
            "GetActivePlayerIndices", // int[] o List<int>
            "GetPlayerOrderIndices",
            "GetPlayersOrder",
            "GetInitialOrder"
        };
        foreach (var mName in methodCandidates)
        {
            var m = tm.GetType().GetMethod(mName, BindingFlags.Public | BindingFlags.Instance);
            if (m != null)
            {
                var result = m.Invoke(tm, null);
                if (result is int[] arr) return Mathf.Max(1, arr.Length);
                if (result is List<int> list) return Mathf.Max(1, list.Count);
            }
        }

        // 2) Intentar propiedades/campos comunes
        string[] propCandidates = new[]
        {
            "ActivePlayers", "Players", "RemainingPlayers", "AlivePlayers",
            "activePlayers", "players", "remainingPlayers", "alivePlayers"
        };
        foreach (var pName in propCandidates)
        {
            var p = tm.GetType().GetProperty(pName, BindingFlags.Public | BindingFlags.Instance);
            if (p != null)
            {
                var val = p.GetValue(tm);
                int count = TryCount(val);
                if (count > 0) return count;
            }
            var f = tm.GetType().GetField(pName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (f != null)
            {
                var val = f.GetValue(tm);
                int count = TryCount(val);
                if (count > 0) return count;
            }
        }

        // 3) Último recurso: si hay propiedad/método PlayerCount
        var propCount = tm.GetType().GetProperty("PlayerCount", BindingFlags.Public | BindingFlags.Instance);
        if (propCount != null)
        {
            var v = propCount.GetValue(tm);
            if (v is int c && c > 0) return c;
        }
        var methodCount = tm.GetType().GetMethod("GetPlayerCount", BindingFlags.Public | BindingFlags.Instance);
        if (methodCount != null)
        {
            var v = methodCount.Invoke(tm, null);
            if (v is int c && c > 0) return c;
        }

        return 4; // por defecto
    }

    private static int TryCount(object val)
    {
        if (val == null) return 0;
        switch (val)
        {
            case System.Array arr:
                return arr.Length;
            case IList<int> listInt:
                return listInt.Count;
            case IList<object> listObj:
                return listObj.Count;
        }
        return 0;
    }
}
