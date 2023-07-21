using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestSlotUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] QuestStatus status;

    RectTransform rectTransform;

    public Text NameText => nameText;
    public QuestStatus Status => status;
    public float Height => rectTransform.rect.height;

    public void SetData(Quest quest)
    {
        rectTransform = GetComponent<RectTransform>();
        nameText.text = quest.Base.Name;
        status = quest.Status;
    }
}
