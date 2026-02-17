using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a single node in the dialogue tree
/// </summary>
[Serializable]
public class DialogueNode
{
    public string Id;                       // Unique identifier for this node
    public string Text;                     // Dialogue text to display
    public NodeType Type;                   // Type of node (NPC, Player, Event)
    public List<DialogueChoice> Choices;    // Available player choices (if Type is Player)
    public List<Condition> Conditions;      // Conditions that must be met to access this node
    public List<Effect> Effects;            // Effects to apply when this node is displayed
    
    /// <summary>
    /// Checks if all conditions for this node are met
    /// </summary>
    public bool AreConditionsMet(GameState gameState)
    {
        if (Conditions == null || Conditions.Count == 0)
            return true;
            
        return Conditions.All(condition => condition.Evaluate(gameState));
    }
    
    /// <summary>
    /// Applies all effects of this node
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
    
    /// <summary>
    /// Gets all valid choices (those whose conditions are met)
    /// </summary>
    public List<DialogueChoice> GetValidChoices(GameState gameState)
    {
        if (Choices == null || Choices.Count == 0)
            return new List<DialogueChoice>();
            
        return Choices.Where(choice => choice.AreConditionsMet(gameState)).ToList();
    }
}
