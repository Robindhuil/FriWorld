using UnityEngine;
using UnityEngine.UIElements;

public class StatsUI : BaseUi
{
    private VisualElement statsRoot;
    public bool IsMenuOn { get; set; }
    private Label questCount;
    private Label secretCount;
    private Label mistakeCount;
    private Label walkCount;
    private Player player;

    public StatsUI(UIDocument document, MonoBehaviour runner)
    {
        IsMenuOn = false;
        var root = document.rootVisualElement;
        
        statsRoot = root.Q<VisualElement>("StatsUI");
        questCount = statsRoot.Q<Label>("QuestCount");
        secretCount = statsRoot.Q<Label>("SecretCount");
        mistakeCount = statsRoot.Q<Label>("MistakeCount");
        walkCount = statsRoot.Q<Label>("WalkCount");
        
        player = runner.GetComponent<Player>();
        statsRoot.style.display = DisplayStyle.None;
    }

    public override void CloseWindow()
    {
        statsRoot.style.display = DisplayStyle.None;
        IsMenuOn = false;
    }

    public override void OpenWindow()
    {
        UpdateAll();
        statsRoot.style.display = DisplayStyle.Flex;
        IsMenuOn = true;
    }

    private void UpdateAll()
    {
        questCount.text = player.PlayerManagment.journal.GetCompletedQuestsCount().ToString() + "/" + player.PlayerManagment.journal.GetAllQuestsCount().ToString();
        secretCount.text = player.PlayerManagment.stats.SecretsCollected.ToString() + "/" + player.PlayerManagment.stats.TotalSecretCount.ToString();
        mistakeCount.text = player.PlayerManagment.stats.Mistakes.ToString();
        walkCount.text = player.PlayerManagment.stats.Walks.ToString() + "m";
    }
}