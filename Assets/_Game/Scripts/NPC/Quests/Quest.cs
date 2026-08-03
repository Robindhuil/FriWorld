[System.Serializable]
public class Quest
{
    public string Id { get; private set; }
    public string QuestName { get; private set; }
    public string QuestObjective { get; private set; }
    public string QuestInfo { get; private set; }

    public bool SwitchGiverModeOnCompletion { get; private set; }

    public QuestStatus Status { get; set; }
    public Npc fromNpc { get; set; }

    /// <summary>
    /// Inicializuje novú úlohu s danými parametrami a nastaví jej stav na neaktívny.
    /// Parameter switchGiverModeOnCompletion môže byť voliteľný – predvolene false.
    /// </summary>
    public Quest(string id, string questName, string questObjective, string questInfo, bool switchGiverModeOnCompletion = false)
    {
        this.Id = id;
        this.QuestName = questName;
        this.QuestObjective = questObjective;
        this.QuestInfo = questInfo;
        this.SwitchGiverModeOnCompletion = switchGiverModeOnCompletion;
        SetInactive();
        fromNpc = null;
    }

    /// <summary>
    /// Nastaví stav úlohy na aktívny.
    /// </summary>
    public void SetActive()
    {
        Status = QuestStatus.Active;
    }

    /// <summary>
    /// Nastaví stav úlohy na neaktívny.
    /// </summary>
    public void SetInactive()
    {
        Status = QuestStatus.Inactive;
    }

    /// <summary>
    /// Nastaví stav úlohy na dokončený.
    /// </summary>
    public void SetCompleted()
    {
        Status = QuestStatus.Completed;
    }
}
