using GDE.GenericSelectionUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum QuestUIState { QuestSelection, Busy }

public class QuestUI : SelectionUI<TextSlot>
{
    [SerializeField] GameObject questList;
    [SerializeField] QuestSlotUI questSlotUI;

    [SerializeField] Text questName;
    [SerializeField] Text questDescription;
    [SerializeField] Text questCompleted;

    [SerializeField] Image upArrow;
    [SerializeField] Image downArrow;

    int selectedQuest = 0;

    QuestUIState state;

    const int questsInViewport = 8;

    List<QuestSlotUI> slotUiList;
    QuestList playerQuestList;
    RectTransform questListRect;
    private void Awake()
    {
        playerQuestList = QuestList.GetQuestList();
        questListRect = questList.GetComponent<RectTransform>();
    }

    private void Start()
    {
        ResetSelection();
        UpdateQuestList();

        playerQuestList.OnUpdated += UpdateQuestList;
    }

    void UpdateQuestList()
    {
        foreach (Transform child in questList.transform)
        {
            Destroy(child.gameObject);
        }

        slotUiList = new List<QuestSlotUI>();
        foreach (var quest in QuestList.GetQuestList().Quests)
        {
            var slotUIObj = Instantiate(questSlotUI, questList.transform);
            slotUIObj.SetData(quest);

            slotUiList.Add(slotUIObj);
        }

        
        foreach (Transform child in questList.transform)
        {
            var quest = child.GetComponent<QuestSlotUI>();
            //quest.SetData(QuestList.GetQuestList().Quests[0]);
        }

        UpdateQuestSelection();
    }

    public void HandleUpdate(Action onBack)
    {
        if (state == QuestUIState.QuestSelection)
        {
            int prevSelection = selectedQuest;

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                ++selectedQuest;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                --selectedQuest;
            }

            selectedQuest = Mathf.Clamp(selectedQuest, 0, playerQuestList.Quests.Count - 1);

            if (prevSelection != selectedQuest)
            {
                UpdateQuestSelection();
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                onBack?.Invoke();
            }
        }
    }
    
    void UpdateQuestSelection()
    {
        var slots = QuestList.GetQuestList().Quests;

        selectedQuest = Mathf.Clamp(selectedQuest, 0, slots.Count - 1);

        for (int i = 0; i < slotUiList.Count; i++)
        {
            if (i == selectedQuest)
            {
                slotUiList[i].NameText.color = GlobalSettings.i.HighlightedColor;
            }
            else if (slotUiList[i].Status == QuestStatus.None)
            {
                slotUiList[i].gameObject.SetActive(false);
            }
            else if (slotUiList[i].Status == QuestStatus.Started)
            {
                slotUiList[i].NameText.color = Color.black;
            }
            else if (slotUiList[i].Status == QuestStatus.Completed)
            {
                slotUiList[i].NameText.color = Color.gray;
            }
        }

        if (slots.Count > 0)
        {
            var quest = slots[selectedQuest];
            questName.text = quest.Base.Name;
            questDescription.text = quest.Base.Description;
            questCompleted.gameObject.SetActive(quest.Status == QuestStatus.Completed);
        }

        HandleScrolling();
    }

    void HandleScrolling()
    {
        if (slotUiList.Count <= questsInViewport)
        {
            return;
        }

        float scrollPos = Mathf.Clamp(selectedQuest - questsInViewport / 2, 0, selectedQuest) * slotUiList[0].Height;
        questListRect.localPosition = new Vector2(questListRect.localPosition.x, scrollPos);

        bool showUpArrow = selectedQuest > questsInViewport / 2;
        upArrow.gameObject.SetActive(showUpArrow);

        bool showDownArrow = selectedQuest + questsInViewport / 2 < slotUiList.Count;
        downArrow.gameObject.SetActive(showDownArrow);
    }

    void ResetSelection()
    {
        selectedQuest = 0;

        upArrow.gameObject.SetActive(false);
        downArrow.gameObject.SetActive(false);

        questName.text = "Quests";
        questDescription.text = "This will show the quests you will encounter";
        questCompleted.gameObject.SetActive(false);
    }
}
