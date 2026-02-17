using System;

/// <summary>
/// Represents an effect that is applied when a dialogue node or choice is activated
/// </summary>
[Serializable]
public class Effect
{
    public string Key;          
    public EffectType Type;     
    public int Value;           
    
    /// <summary>
    /// Applies this effect to the game state
    /// </summary>
    public void Apply(GameState gameState)
    {
        if (gameState == null)
        {
            UnityEngine.Debug.LogError("[Effect] GameState is null!");
            return;
        }
        
        switch (Type)
        {
            case EffectType.SetBool:
                if (!string.IsNullOrEmpty(Key) && Key.StartsWith("entry_"))
                {
                    string unlockedKey = $"{Key}_Unlocked";
                    gameState.SetBool(unlockedKey, true);
                    UnityEngine.Debug.Log($"[Effect] Set bool '{unlockedKey}' to true");
                }
                else
                {
                    gameState.SetBool(Key, Value != 0);
                    UnityEngine.Debug.Log($"[Effect] Set bool '{Key}' to {Value != 0}");
                }
                break;
                
            case EffectType.SetInt:
                gameState.SetInt(Key, Value);
                UnityEngine.Debug.Log($"[Effect] Set int '{Key}' to {Value}");
                break;
                
            case EffectType.AddInt:
                gameState.AddInt(Key, Value);
                UnityEngine.Debug.Log($"[Effect] Added {Value} to '{Key}', new value: {gameState.GetInt(Key)}");
                break;
                
            case EffectType.StartQuest:
                gameState.SetBool($"{Key}_Started", true);
                UnityEngine.Debug.Log($"[Effect] Started quest: {Key}");
                break;
                
            case EffectType.CompleteQuest:
                gameState.SetBool($"{Key}_Completed", true);
                UnityEngine.Debug.Log($"[Effect] Completed quest: {Key}");
                break;
                
            default:
                UnityEngine.Debug.LogWarning($"[Effect] Unknown effect type: {Type}");
                break;
        }
    }
}
