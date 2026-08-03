# New Node-Based Dialogue System

## Overview
The new dialogue system is based on a flexible node structure with conditions and effects. This allows for complex branching dialogues that react to the player's quest progress and choices.

## Core Components

### 1. **GameState** (Singleton)
Manages all game state variables used by the dialogue system.
- **Location**: `GameState.cs`
- **Usage**: Attach to a GameObject in your scene (create an empty GameObject called "GameState")
- **Methods**:
  - `GetBool(string key)` - Get a boolean value
  - `SetBool(string key, bool value)` - Set a boolean value
  - `GetInt(string key)` - Get an integer value
  - `SetInt(string key, int value)` - Set an integer value
  - `AddInt(string key, int value)` - Add to an integer value

### 2. **DialogueNode**
Represents a single point in the dialogue tree.
- **Fields**:
  - `Id` - Unique identifier (string)
  - `Text` - The dialogue text to display
  - `Type` - NodeType (NPC, Player, or Event)
  - `Choices` - List of available player choices
  - `Conditions` - Must be met to access this node
  - `Effects` - Applied when the node is displayed

### 3. **DialogueChoice**
A player choice option.
- **Fields**:
  - `Text` - Display text for the choice
  - `NextNodeId` - ID of the next node
  - `Conditions` - Must be met for choice to appear
  - `Effects` - Applied when choice is selected

### 4. **Condition**
A requirement that must be met.
- **Fields**:
  - `Key` - Variable name
  - `Type` - ConditionType (see below)
  - `IntValue` - Value for integer comparisons

### 5. **Effect**
An action that modifies game state.
- **Fields**:
  - `Key` - Variable name or quest ID
  - `Type` - EffectType (see below)
  - `Value` - Value to set/add

## Enums Reference

### NodeType
- `0` = **NPC** - NPC is speaking
- `1` = **Player** - Player choice/response
- `2` = **Event** - Triggers events without dialogue (auto-progresses)

### ConditionType
- `0` = **Bool** - Check if bool is true
- `1` = **IntEqual** - Check if int equals value
- `2` = **IntGreater** - Check if int is greater than value
- `3` = **IntLess** - Check if int is less than value
- `4` = **FlippedBool** - Check if bool is false

### EffectType
- `0` = **SetBool** - Set a boolean value
- `1` = **SetInt** - Set an integer value
- `2` = **AddInt** - Add to an integer value
- `3` = **StartQuest** - Start a quest (sets "{Key}_Started" bool to true, where Key is the quest ID like "quest_0_start")
- `4` = **CompleteQuest** - Complete a quest (sets "{Key}_Completed" bool to true, where Key is the quest ID)

## JSON Format Example

```json
{
  "StartNodeId": "greeting",
  "Nodes": [
    {
      "Id": "greeting",
      "Text": "Hello traveler!",
      "Type": 0,
      "Choices": [
        {
          "Text": "I brought the documents.",
          "NextNodeId": "quest_complete",
          "Conditions": [
            {
              "Key": "quest_1_programming_languages_Completed",
              "Type": 0,
              "IntValue": 0
            }
          ],
          "Effects": []
        }
      ],
      "Conditions": [],
      "Effects": []
    }
  ]
}
```

## How to Use

### 1. Setup GameState
```csharp
// Create an empty GameObject in your scene
// Add the GameState component to it
// It will automatically become a singleton
```

### 2. Load Dialogue
```csharp
NodeDialogueManager dialogueManager = new NodeDialogueManager();
dialogueManager.LoadDialogue(dialogueTextAsset, GameState.Instance);
```

### 3. Start Dialogue
```csharp
if (dialogueManager.StartDialogue(out string npcText, out string[] choices))
{
    // Display npcText and choices in your UI
}
```

### 4. Select Choice
```csharp
if (dialogueManager.SelectChoice(choiceIndex, out string npcText, out string[] choices))
{
    // Display next npcText and choices
}
else
{
    // Dialogue ended
}
```

## Common Patterns

### Quest Flow
1. NPC offers quest → Effect: StartQuest
2. Player completes quest → GameState.SetBool("quest_1_programming_languages_Completed", true)
3. Return to NPC → Choice with Condition checking quest_1_programming_languages_Completed
4. Complete dialogue → Effect: CompleteQuest + AddInt for gold reward

### Conditional Choices
Show different dialogue options based on player progress:
```json
{
  "Text": "I have the item you need.",
  "Conditions": [
    {
      "Key": "HasSpecialItem",
      "Type": 0
    }
  ]
}
```

### Branching Dialogue
Use different NextNodeId values to create branches:
- Choice A → "node_friendly"
- Choice B → "node_hostile"

## Migration from Old System
Your old `DialogueManager.cs` used levels and line IDs. The new system:
- Replace **levels** with **conditional nodes** (check quest progress via Conditions)
- Replace **line IDs** with **string node IDs** (more readable)
- **Quest tracking** is now handled by GameState instead of DialogueManager
- **Choices** can now have their own conditions and effects

## Notes
- Node IDs are case-sensitive
- Empty NextNodeId ends the dialogue
- Event nodes (Type: 2) auto-progress through first valid choice
- All conditions in a list must be true (AND logic)
- Effects are applied in order
