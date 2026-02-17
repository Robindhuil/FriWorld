using System;

/// <summary>
/// Represents a condition that must be met for a dialogue node or choice to be available
/// </summary>
[Serializable]
public class Condition
{
    public string Key;             
    public ConditionType Type;      
    public int IntValue;            
    
    /// <summary>
    /// Evaluates this condition against the current game state
    /// </summary>
    public bool Evaluate(GameState gameState)
    {
        if (gameState == null) return false;
        
        switch (Type)
        {
            case ConditionType.Bool:
                return gameState.GetBool(Key);

            case ConditionType.FlippedBool:
                return !gameState.GetBool(Key);
                
            case ConditionType.IntEqual:
                return gameState.GetInt(Key) == IntValue;
                
            case ConditionType.IntGreater:
                return gameState.GetInt(Key) > IntValue;
                
            case ConditionType.IntLess:
                return gameState.GetInt(Key) < IntValue;
                
            default:
                UnityEngine.Debug.LogWarning($"[Condition] Unknown condition type: {Type}");
                return false;
        }
    }
}
