using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Quest
{
    public QuestBase Base { get; private set; }
    public QuestStatus Status { get; private set; }

    public Quest(QuestBase questBase)
    {
        Base = questBase;
    }

    public Quest(QuestSaveData saveData)
    {
        Base = QuestDB.GetObjectByName(saveData.name);
        Status = saveData.status;
    }

    public QuestSaveData GetSaveData()
    {
        var saveData = new QuestSaveData()
        {
            name = Base.name,
            status = Status
        };
        return saveData;
    }

    public IEnumerator StartQuest()
    {
        Status = QuestStatus.Started;

        yield return DialogManager.Instance.ShowDialog(Base.StartDialogue);

        var questList = QuestList.GetQuestList();
        questList.AddQuest(this);
    }
    
    public IEnumerator CompleteQuest(Transform playerTransform)
    {
        Status = QuestStatus.Completed;

        yield return DialogManager.Instance.ShowDialog(Base.CompletedDialogue);

        var inventory = Inventory.GetInventory();
        if (Base.RequiredItem.Count >= 1)
        {
            foreach (var item in Base.RequiredItem)
            {
                if (Base.RemoveItem)
                {
                    inventory.RemoveItem(item);
                }
            }
        }

        if (Base.RewardItem != null)
        {
            inventory.AddItem(Base.RewardItem);

            var player = playerTransform.GetComponent<PlayerController>();
            yield return DialogManager.Instance.ShowDialogText($"You received {Base.RewardItem.Name}");
        }

        var questList = QuestList.GetQuestList();
        questList.AddQuest(this);
        UnityEngine.Debug.Log($"{Base.Name} : completed");
    }

    public void ForceComplete()
    {
        Status = QuestStatus.Completed;
    }

    public bool CanBeCompleted()
    {
        var inventory = Inventory.GetInventory();
        if (Base.RequiredItem.Count >= 1)
        {
            foreach (var item in Base.RequiredItem)
            {
                if (!inventory.HasItem(item))
                {
                    UnityEngine.Debug.Log(item.Name);
                    return false;
                }
            }
        }

        return true;
    }
}

[System.Serializable]
public class QuestSaveData
{
    public string name;
    public QuestStatus status;
}

public enum QuestStatus { None, Started, Completed }
