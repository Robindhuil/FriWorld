using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Container for dialogue data loaded from JSON
/// </summary>
[Serializable]
public class DialogueData
{
    public string StartNodeId;              // ID of the first node to display
    public List<DialogueNode> Nodes;        // All dialogue nodes
}

/// <summary>
/// Manages node-based dialogue system with conditions and effects
/// </summary>
public class NodeDialogueManager
{
    private Dictionary<string, DialogueNode> nodes = new Dictionary<string, DialogueNode>();
    private DialogueNode currentNode;
    private string startNodeId;
    private GameState gameState;

    public DialogueNode CurrentNode => currentNode;
    public string StartNodeId => startNodeId;

    /// <summary>
    /// Loads dialogue from a JSON TextAsset
    /// </summary>
    public void LoadDialogue(TextAsset dialogueFile, GameState state)
    {
        if (dialogueFile == null)
        {
            Debug.LogError("[NodeDialogueManager] Dialogue file is null!");
            return;
        }

        if (state == null)
        {
            Debug.LogError("[NodeDialogueManager] GameState is null!");
            return;
        }

        gameState = state;
        
        try
        {
            DialogueData data = JsonUtility.FromJson<DialogueData>(dialogueFile.text);
            
            if (data == null || data.Nodes == null)
            {
                Debug.LogError("[NodeDialogueManager] Failed to parse dialogue JSON!");
                return;
            }

            // Build the node dictionary
            nodes.Clear();
            foreach (var node in data.Nodes)
            {
                if (string.IsNullOrEmpty(node.Id))
                {
                    Debug.LogWarning("[NodeDialogueManager] Found node with empty ID, skipping");
                    continue;
                }
                
                nodes[node.Id] = node;
            }

            startNodeId = data.StartNodeId;
            Debug.Log($"[NodeDialogueManager] Loaded {nodes.Count} dialogue nodes. Start node: {startNodeId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NodeDialogueManager] Error parsing JSON: {e.Message}");
        }
    }

    /// <summary>
    /// Starts the dialogue from the beginning
    /// </summary>
    public bool StartDialogue(out string npcText, out string[] playerChoices)
    {
        npcText = "";
        playerChoices = new string[0];

        if (string.IsNullOrEmpty(startNodeId))
        {
            Debug.LogError("[NodeDialogueManager] Start node ID is not set!");
            return false;
        }

        return MoveToNode(startNodeId, out npcText, out playerChoices);
    }

    /// <summary>
    /// Continues dialogue by selecting a player choice
    /// </summary>
    public bool SelectChoice(int choiceIndex, out string npcText, out string[] playerChoices)
    {
        npcText = "";
        playerChoices = new string[0];

        if (currentNode == null)
        {
            Debug.LogError("[NodeDialogueManager] No current node!");
            return false;
        }

        var validChoices = currentNode.GetValidChoices(gameState);
        
        if (choiceIndex < 0 || choiceIndex >= validChoices.Count)
        {
            Debug.LogError($"[NodeDialogueManager] Invalid choice index: {choiceIndex}");
            return false;
        }

        DialogueChoice selectedChoice = validChoices[choiceIndex];
        
        // Apply choice effects
        selectedChoice.ApplyEffects(gameState);

        // Move to next node
        if (string.IsNullOrEmpty(selectedChoice.NextNodeId))
        {
            Debug.Log("[NodeDialogueManager] Choice has no next node, ending dialogue");
            return false;
        }

        return MoveToNode(selectedChoice.NextNodeId, out npcText, out playerChoices);
    }

    /// <summary>
    /// Moves to a specific node by ID
    /// </summary>
    private bool MoveToNode(string nodeId, out string npcText, out string[] playerChoices)
    {
        npcText = "";
        playerChoices = new string[0];

        if (!nodes.ContainsKey(nodeId))
        {
            Debug.LogError($"[NodeDialogueManager] Node '{nodeId}' not found!");
            return false;
        }

        currentNode = nodes[nodeId];

        // Check if conditions are met
        if (!currentNode.AreConditionsMet(gameState))
        {
            Debug.LogWarning($"[NodeDialogueManager] Conditions not met for node '{nodeId}'");
            return false;
        }

        // Apply node effects
        currentNode.ApplyEffects(gameState);

        // Handle based on node type
        switch (currentNode.Type)
        {
            case NodeType.NPC:
                npcText = currentNode.Text;
                
                // If NPC node has choices, show them
                var validChoices = currentNode.GetValidChoices(gameState);
                if (validChoices.Count > 0)
                {
                    playerChoices = validChoices.Select(c => c.Text).ToArray();
                }
                else
                {
                    // No choices available - dialogue ends here
                    Debug.Log("[NodeDialogueManager] NPC node has no valid choices, ending dialogue");
                }
                break;

            case NodeType.Player:
                // Player nodes should always have choices
                var choices = currentNode.GetValidChoices(gameState);
                if (choices.Count > 0)
                {
                    npcText = currentNode.Text; // Can be empty or a prompt
                    playerChoices = choices.Select(c => c.Text).ToArray();
                }
                else
                {
                    Debug.LogWarning("[NodeDialogueManager] Player node has no valid choices!");
                    return false;
                }
                break;

            case NodeType.Event:
                // Event nodes automatically proceed to first valid choice
                var eventChoices = currentNode.GetValidChoices(gameState);
                Debug.Log($"[NodeDialogueManager] Event node '{nodeId}' has {eventChoices.Count} valid choices out of {currentNode.Choices?.Count ?? 0} total");
                
                if (eventChoices.Count > 0)
                {
                    var firstChoice = eventChoices[0];
                    Debug.Log($"[NodeDialogueManager] Auto-selecting first valid choice -> '{firstChoice.NextNodeId}'");
                    // Auto-select first valid choice
                    return SelectChoice(0, out npcText, out playerChoices);
                }
                else
                {
                    Debug.Log("[NodeDialogueManager] Event node triggered, ending dialogue");
                    return false;
                }
        }

        return true;
    }

    /// <summary>
    /// Resets the dialogue to the start
    /// </summary>
    public void ResetDialogue()
    {
        currentNode = null;
        Debug.Log("[NodeDialogueManager] Dialogue reset");
    }

    /// <summary>
    /// Clears all dialogue data
    /// </summary>
    public void ClearDialogue()
    {
        nodes.Clear();
        currentNode = null;
        startNodeId = null;
        Debug.Log("[NodeDialogueManager] Dialogue cleared");
    }

    /// <summary>
    /// Gets a node by ID (for debugging or special cases)
    /// </summary>
    public DialogueNode GetNode(string nodeId)
    {
        return nodes.TryGetValue(nodeId, out var node) ? node : null;
    }
}
