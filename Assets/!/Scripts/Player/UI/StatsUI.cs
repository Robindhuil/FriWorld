using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsUI : BaseUi
{
    private Canvas statsCanvas;
    public bool IsMenuOn { get; set; }
    TextMeshProUGUI questCount;
    TextMeshProUGUI secretCount;
    TextMeshProUGUI mistakeCount;
    TextMeshProUGUI walkCount;
    private Player player;
    public StatsUI(Canvas canvas, TextMeshProUGUI questCount, TextMeshProUGUI secretCount, TextMeshProUGUI mistakeCount, TextMeshProUGUI walkCount, MonoBehaviour runner)
    {
        IsMenuOn = false;
        statsCanvas = canvas;
        statsCanvas.gameObject.SetActive(false);
        this.questCount = questCount;
        this.secretCount = secretCount;
        this.mistakeCount = mistakeCount;
        this.walkCount = walkCount;
        player = runner.GetComponent<Player>();
    }

    public override void CloseWindow()
    {
        statsCanvas.gameObject.SetActive(false);
        IsMenuOn = false;
    }

    public override void OpenWindow()
    {
        UpdateAll();
        statsCanvas.gameObject.SetActive(true);
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