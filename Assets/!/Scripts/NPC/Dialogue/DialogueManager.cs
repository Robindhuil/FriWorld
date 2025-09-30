using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueChoice
{
    public string text;
    public int next;
    public int quest;
}

[System.Serializable]
public class DialogueLine
{
    public int id;
    public string text;
    public List<DialogueChoice> choices;
    public int quest;
}

[System.Serializable]
public class DialogueLevel
{
    public int level;
    public List<DialogueLine> dialogue;
}

[System.Serializable]
public class DialogueData
{
    public List<DialogueLevel> levels;
}

public class DialogueManager
{
    private Dictionary<int, DialogueLine> dialogueLines = new();
    private int currentDialogueId = 0;
    private int currentLevel = 1;
    private DialogueData dialogueData;
    public int LastValidQuestId { get; private set; } = -1;
    public int CurrentLevel => currentLevel;
    public int CurrentDialogueId => currentDialogueId;

    /// <summary>
    /// Načíta dialóg zo súboru (JSON) z TextAsset.
    /// </summary>
    public void LoadDialogue(TextAsset dialogueFile)
    {
        if (dialogueFile == null)
        {
            Debug.LogError("[DialogueManager] Chyba: Dialogue file je null!");
            return;
        }

        Debug.Log($"[DialogueManager] Načítavam JSON: {dialogueFile.text}");

        try
        {
            dialogueData = JsonUtility.FromJson<DialogueData>(dialogueFile.text);
            if (dialogueData == null || dialogueData.levels == null)
            {
                Debug.LogError("[DialogueManager] Chyba: JSON sa neparsoval správne!");
                return;
            }
            LoadCurrentLevelDialogue();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DialogueManager] Chyba pri parsovaní JSON: {e.Message}");
        }
    }

    /// <summary>
    /// Načíta dialógové riadky pre aktuálny level.
    /// </summary>
    private void LoadCurrentLevelDialogue()
    {
        dialogueLines.Clear();
        var levelData = dialogueData.levels.Find(l => l.level == currentLevel);
        if (levelData != null)
        {
            foreach (var line in levelData.dialogue)
            {
                dialogueLines[line.id] = line;
            }
            Debug.Log($"[DialogueManager] Načítaných {dialogueLines.Count} dialógových riadkov pre level {currentLevel}");
        }
        else
        {
            Debug.LogWarning($"[DialogueManager] Chyba: Level {currentLevel} neexistuje v dialógoch!");
        }
    }

    /// <summary>
    /// Zvýši level dialógu a resetuje dialóg.
    /// Používa sa pri quest dialógoch, kde máš viacero levelov.
    /// </summary>
    public void IncreaseDialogueLevel()
    {
        currentLevel++;
        LoadCurrentLevelDialogue();
        ResetDialogue();
    }

    /// <summary>
    /// Pokračuje v dialógu na základe voľby hráča.
    /// </summary>
    public bool ContinueDialogue(int choiceIndex, out string npcLine, out string[] playerChoices)
    {
        npcLine = "";
        playerChoices = new string[0];

        if (!dialogueLines.ContainsKey(currentDialogueId))
        {
            Debug.LogError($"[DialogueManager] Chyba: Aktuálny dialóg ID {currentDialogueId} neexistuje!");
            return false;
        }

        DialogueLine currentLine = dialogueLines[currentDialogueId];

        if (currentLine.choices == null || choiceIndex < 0 || choiceIndex >= currentLine.choices.Count)
        {
            Debug.LogWarning($"[DialogueManager] Neplatný index voľby ({choiceIndex}).");
            return false;
        }

        DialogueChoice chosenChoice = currentLine.choices[choiceIndex];
        if (chosenChoice.quest != -1)
        {
            LastValidQuestId = chosenChoice.quest;
            Debug.Log($"[DialogueManager] Quest id nastavený z voľby: {LastValidQuestId}");
        }

        int nextId = chosenChoice.next;
        if (nextId <= 0 || !dialogueLines.ContainsKey(nextId))
        {
            Debug.LogWarning("[DialogueManager] Koniec dialógu. Nebol nájdený ďalší dialógový riadok.");
            return false;
        }

        currentDialogueId = nextId;
        DialogueLine nextLine = dialogueLines[currentDialogueId];
        npcLine = nextLine.text;
        playerChoices = nextLine.choices?.ConvertAll(choice => choice.text).ToArray() ?? new string[0];
        return true;
    }

    /// <summary>
    /// Resetuje dialóg na začiatok (nastaví currentDialogueId na 0).
    /// </summary>
    public void ResetDialogue()
    {
        currentDialogueId = 0;
        Debug.Log("[DialogueManager] Reset dialógu na ID 0");
    }

    /// <summary>
    /// Vyčistí celý stav dialógu.
    /// </summary>
    public void ClearDialogueState()
    {
        dialogueLines.Clear();
        currentDialogueId = 0;
        currentLevel = 1;
        Debug.Log("[DialogueManager] Vyčistený stav dialógu.");
    }

    /// <summary>
    /// Vracia aktuálny dialógový riadok, alebo null, ak neexistuje.
    /// </summary>
    public DialogueLine GetCurrentDialogue()
    {
        return dialogueLines.ContainsKey(currentDialogueId) ? dialogueLines[currentDialogueId] : null;
    }

    public bool CanIncreaseDialogueLevel()
    {
        if (dialogueData == null || dialogueData.levels == null || dialogueData.levels.Count == 0)
        {
            return false;
        }

        int maxLevel = 0;
        foreach (var level in dialogueData.levels)
        {
            if (level.level > maxLevel)
            {
                maxLevel = level.level;
            }
        }
        return currentLevel < maxLevel;
    }

}
