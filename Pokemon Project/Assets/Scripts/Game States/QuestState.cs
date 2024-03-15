using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestState : State<GameController>
{
    [SerializeField] QuestUI questUI;

    public static QuestState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    QuestList questList;
    private void Start()
    {
        questList = QuestList.GetQuestList();
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        questUI.gameObject.SetActive(true);
        questUI.OnBack += OnBack;
    }

    public override void Execute()
    {
        questUI.HandleUpdate(OnBack);
    }

    public override void Exit()
    {
        questUI.gameObject.SetActive(false);
        questUI.OnBack -= OnBack;
    }

    public void OnBack()
    {
        gc.StateMachine.Pop();
    }
}
