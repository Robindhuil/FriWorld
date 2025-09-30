using UnityEngine;

public class QuestItem : Questable
{
    /// <summary>
    /// Hlavná interakčná metóda, ktorá overuje stav úlohy a prípadne ju dokončí.
    /// </summary>
    protected override void Interact()
    {
        base.Interact();
        if (CanBeInteracted)
        {
            Debug.Log($"[QuestItem] Interacted with quest object (ID: {questId}). Object will be destroyed.");
            base.CompleteQuest(Journal);
            Destroy(gameObject);
        }
    }
}
