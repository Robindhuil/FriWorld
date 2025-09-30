[System.Serializable]
public class Quest
{
    public int id;
    public string questName;
    public string questObjective;
    public string questInfo;

    public bool switchGiverModeOnCompletion;

    public QuestStatus Status { get; set; }
    public Npc fromNpc { get; set; }

    /// <summary>
    /// Inicializuje novú úlohu s danými parametrami a nastaví jej stav na neaktívny.
    /// Parameter switchGiverModeOnCompletion môže byť voliteľný – predvolene false.
    /// </summary>
    public Quest(int id, string questName, string questObjective, string questInfo, bool switchGiverModeOnCompletion = false)
    {
        this.id = id;
        this.questName = questName;
        this.questObjective = questObjective;
        this.questInfo = questInfo;
        this.switchGiverModeOnCompletion = switchGiverModeOnCompletion;
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
