using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a player choice in dialogue
/// </summary>
[Serializable]
public class DialogueChoice
{
    public string Text;                     
    public string NextNodeId;              
    public List<Condition> Conditions;      
    public List<Effect> Effects;            
    
    /// <summary>
    /// Checks if all conditions for this choice are met
    /// </summary>
    public bool AreConditionsMet(GameState gameState)
    {
        if (Conditions == null || Conditions.Count == 0)
            return true;
            
        return Conditions.All(condition => condition.Evaluate(gameState));
    }
    
    /// <summary>
    /// Applies all effects of this choice
    /// </summary>
    public void ApplyEffects(GameState gameState)
    {
        if (Effects == null || Effects.Count == 0)
            return;
            
        foreach (var effect in Effects)
        {
            effect.Apply(gameState);
        }
    }
}
