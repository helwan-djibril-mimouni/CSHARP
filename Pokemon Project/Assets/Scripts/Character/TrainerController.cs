using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class TrainerController : MonoBehaviour, Interactable, ISavable
{
    [SerializeField] string name;
    [SerializeField] Sprite sprite;
    [SerializeField] Dialog dialog;
    [SerializeField] Dialog dialogAfterBattle;
    [SerializeField] QuestBase questToStart;
    [SerializeField] QuestBase questToComplete;
    [SerializeField] GameObject exclamation;
    [SerializeField] GameObject fov;

    [SerializeField] AudioClip trainerAppearsClip;

    bool battleLost = false;
    Quest activeQuest;

    Character character;

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        SetFOVRotation(character.Animator.DefaultDirection);
    }

    private void Update()
    {
        character.HandleUpdate();
    }

    public IEnumerator Interact(Transform initiator)
    {
        character.LookTowards(initiator.position);

        if (questToComplete != null)
        {
            var quest = new Quest(questToComplete);
            yield return quest.CompleteQuest(initiator);
            questToComplete = null;

            UnityEngine.Debug.Log($"{quest.Base.Name} : completed");
        }
        else if (questToStart != null)
        {
            activeQuest = new Quest(questToStart);
            yield return activeQuest.StartQuest();
            questToStart = null;

            if (activeQuest.CanBeCompleted())
            {
                yield return activeQuest.CompleteQuest(initiator);
                activeQuest = null;
            }
        }
        else if (activeQuest != null)
        {
            if (activeQuest.CanBeCompleted())
            {
                yield return activeQuest.CompleteQuest(initiator);
                activeQuest = null;
            }
            else
            {
                yield return DialogManager.Instance.ShowDialog(activeQuest.Base.InProgressDialogue);
            }
        }

        if (!battleLost)
        {
            AudioManager.i.PlayMusic(trainerAppearsClip);

            yield return DialogManager.Instance.ShowDialog(dialog);
            GameController.Instance.StartTrainerBattle(this);
        }
        else
        {
            yield return DialogManager.Instance.ShowDialog(dialogAfterBattle);
        }
    }

    public IEnumerator TriggerTrainerBattle(PlayerController player)
    {
        GameController.Instance.StateMachine.Push(CutsceneState.i);

        AudioManager.i.PlayMusic(trainerAppearsClip);

        exclamation.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        exclamation.SetActive(false);

        var diff = player.transform.position - transform.position;
        var moveVec = diff - diff.normalized;
        moveVec = new Vector2(Mathf.Round(moveVec.x), Mathf.Round(moveVec.y));

        yield return character.Move(moveVec);

        yield return DialogManager.Instance.ShowDialog(dialog);

        GameController.Instance.StateMachine.Pop();
        GameController.Instance.StartTrainerBattle(this);
    }

    public void BattleLost()
    {
        battleLost = true;
        fov.gameObject.SetActive(false);
    }

    public void SetFOVRotation(FacingDirection dir)
    {
        float angle = 0f;

        if (dir == FacingDirection.Right)
        {
            angle = 90f;
        }
        else if (dir == FacingDirection.Up)
        {
            angle = 180f;
        }
        else if (dir == FacingDirection.Left)
        {
            angle = 270f;
        }

        fov.transform.eulerAngles = new Vector3(0f, 0f, angle);
    }

    public object CaptureState()
    {
        var saveData = new TrainerSaveData();
        saveData.activeQuest = activeQuest?.GetSaveData();

        if (questToStart != null)
        {
            saveData.questToStart = (new Quest(questToStart)).GetSaveData();
        }

        if (questToComplete != null)
        {
            saveData.questToComplete = (new Quest(questToComplete)).GetSaveData();
        }

        saveData.battleLost = battleLost;

        return saveData;
    }

    public void RestoreState(object state)
    {
        var saveData = state as TrainerSaveData;
        if (saveData != null)
        {
            activeQuest = (saveData.activeQuest != null) ? new Quest(saveData.activeQuest) : null;

            questToStart = (saveData.questToStart != null) ? new Quest(saveData.questToStart).Base : null;
            questToComplete = (saveData.questToComplete != null) ? new Quest(saveData.questToComplete).Base : null;
        }

        battleLost = saveData.battleLost;

        if (battleLost)
        {
            fov.gameObject.SetActive(false);
        }
    }

    public string Name
    {
        get => name;
    }

    public Sprite Sprite
    {
        get => sprite;
    }
}

[System.Serializable]
public class TrainerSaveData
{
    public QuestSaveData activeQuest;
    public QuestSaveData questToStart;
    public QuestSaveData questToComplete;
    public bool battleLost;
}
