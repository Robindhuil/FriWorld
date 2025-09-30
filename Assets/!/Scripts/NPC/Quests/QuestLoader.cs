using System.Collections.Generic;
using UnityEngine;

public class QuestLoader
{
    private readonly string questResourcePath;

    public QuestLoader(string resourcePath)
    {
        questResourcePath = resourcePath;
    }

    public List<Quest> LoadQuests()
    {
        var quests = new List<Quest>();

        TextAsset questFile = Resources.Load<TextAsset>(questResourcePath);

        if (questFile == null)
        {
            Debug.LogError($"[QuestLoader] Resource súbor neexistuje: {questResourcePath}");
            return quests;
        }

        string[] lines = questFile.text.Split('\n');

        foreach (var line in lines)
        {
            string[] parts = line.Trim().Split(new[] { " -> " }, System.StringSplitOptions.None);

            if (parts.Length == 3)
            {
                string[] idAndName = parts[0].Trim().Split(' ', 2);
                if (idAndName.Length == 2 && int.TryParse(idAndName[0], out int id))
                {
                    var quest = new Quest(
                        id,
                        idAndName[1].Trim(),
                        parts[1].Trim(),
                        parts[2].Trim()
                    );

                    quests.Add(quest);
                }
                else
                {
                    Debug.LogWarning($"[QuestLoader] Neplatné ID alebo názov: {line}");
                }
            }
            else
            {
                Debug.LogWarning($"[QuestLoader] Neplatný formát riadku: {line}");
            }
        }

        Debug.Log($"[QuestLoader] Načítaných {quests.Count} questov z Resources.");
        return quests;
    }
}
