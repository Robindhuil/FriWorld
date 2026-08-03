using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that manages global game state variables
/// Used by the dialogue system to track quest progress, player choices, etc.
/// </summary>
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    public enum BoolStateDomain
    {
        Quest,
        Entry,
        Other
    }

    private readonly Dictionary<BoolStateDomain, Dictionary<string, bool>> boolStatesByDomain =
        new Dictionary<BoolStateDomain, Dictionary<string, bool>>
        {
            { BoolStateDomain.Quest, new Dictionary<string, bool>() },
            { BoolStateDomain.Entry, new Dictionary<string, bool>() },
            { BoolStateDomain.Other, new Dictionary<string, bool>() }
        };

    public Dictionary<string, int> intStates = new Dictionary<string, int>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("[GameState] Initialized");
    }

    #region Boolean State Management
    
    /// <summary>
    /// Gets a boolean value from the state. Returns false if key doesn't exist.
    /// </summary>
    public bool GetBool(string key)
    {
        var domain = ResolveBoolDomain(key);
        return boolStatesByDomain[domain].TryGetValue(key, out var value) && value;
    }

    public bool GetBool(string key, BoolStateDomain domain)
    {
        return boolStatesByDomain[domain].TryGetValue(key, out var value) && value;
    }

    /// <summary>
    /// Sets a boolean value in the state
    /// </summary>
    public void SetBool(string key, bool value)
    {
        var domain = ResolveBoolDomain(key);
        boolStatesByDomain[domain][key] = value;
        Debug.Log($"[GameState] SetBool: '{key}' = {value}");
    }

    public void SetBool(string key, bool value, BoolStateDomain domain)
    {
        boolStatesByDomain[domain][key] = value;
        Debug.Log($"[GameState] SetBool: '{key}' = {value}");
    }

    public Dictionary<string, bool> GetBoolStates(BoolStateDomain domain)
    {
        return boolStatesByDomain[domain];
    }

    public bool ContainsBoolKey(string key, BoolStateDomain domain)
    {
        return boolStatesByDomain[domain].ContainsKey(key);
    }

    public bool RemoveBool(string key, BoolStateDomain domain)
    {
        return boolStatesByDomain[domain].Remove(key);
    }
    
    #endregion

    #region Integer State Management
    
    /// <summary>
    /// Gets an integer value from the state. Returns 0 if key doesn't exist.
    /// </summary>
    public int GetInt(string key)
    {
        return intStates.TryGetValue(key, out var value) ? value : 0;
    }

    /// <summary>
    /// Sets an integer value in the state
    /// </summary>
    public void SetInt(string key, int value)
    {
        intStates[key] = value;
        Debug.Log($"[GameState] SetInt: '{key}' = {value}");
    }

    /// <summary>
    /// Adds to an integer value in the state
    /// </summary>
    public void AddInt(string key, int value)
    {
        int newValue = GetInt(key) + value;
        intStates[key] = newValue;
        Debug.Log($"[GameState] AddInt: '{key}' += {value} (total: {newValue})");
    }
    
    #endregion

    #region Debugging & Utilities
    
    /// <summary>
    /// Clears all state (useful for testing)
    /// </summary>
    public void ClearAllState()
    {
        foreach (var kvp in boolStatesByDomain)
        {
            kvp.Value.Clear();
        }
        intStates.Clear();
        Debug.Log("[GameState] All state cleared");
    }

    /// <summary>
    /// Prints all current state to the console (for debugging)
    /// </summary>
    public void DebugPrintAllState()
    {
        Debug.Log("=== GameState Debug ===");
        Debug.Log("Bool States:");
        foreach (var domainKvp in boolStatesByDomain)
        {
            Debug.Log($"  {domainKvp.Key}:");
            foreach (var kvp in domainKvp.Value)
            {
                Debug.Log($"    {kvp.Key}: {kvp.Value}");
            }
        }
        Debug.Log("Int States:");
        foreach (var kvp in intStates)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value}");
        }
        Debug.Log("======================");
    }

    private static BoolStateDomain ResolveBoolDomain(string key)
    {
        if (key.StartsWith("quest_"))
        {
            return BoolStateDomain.Quest;
        }
        if (key.StartsWith("entry_"))
        {
            return BoolStateDomain.Entry;
        }
        return BoolStateDomain.Other;
    }
    
    #endregion
}
