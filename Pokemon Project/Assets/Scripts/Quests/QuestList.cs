using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestList : MonoBehaviour, ISavable
{
    List<Quest> quests = new List<Quest>();

    public List<Quest> Quests => quests;

    public event Action OnUpdated;

    public void AddQuest(Quest quest)
    {
        bool ok = true;
        foreach (var q in quests)
        {
            if (q.Base.Name == quest.Base.Name)
            {
                ok = false;
            }
        }

        if (ok)
        {
            quests.Add(quest);
        }
        else
        {
            var q = quests.FirstOrDefault(q => q.Base.Name == quest.Base.Name);
            q.ForceComplete();
        }

        OnUpdated?.Invoke();
    }

    public bool IsStarted(string questName)
    {
        var questStatus = quests.FirstOrDefault(q => q.Base.Name == questName)?.Status;
        return questStatus == QuestStatus.Started || questStatus == QuestStatus.Completed;
    }

    public bool IsCompleted(string questName)
    {
        var questStatus = quests.FirstOrDefault(q => q.Base.Name == questName)?.Status;
        return questStatus == QuestStatus.Completed;
    }

    public static QuestList GetQuestList()
    {
        return FindObjectOfType<PlayerController>().GetComponent<QuestList>();
    }

    public object CaptureState()
    {
        return quests.Select(q => q.GetSaveData()).ToList();
    }

    public void RestoreState(object state)
    {
        var saveData = state as List<QuestSaveData>;
        if (saveData != null)
        {
            quests = saveData.Select(q => new Quest(q)).ToList();
            OnUpdated?.Invoke();
        }
    }
}
