using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Cutscene : MonoBehaviour, IPlayerTriggerable
{
    [SerializeReference]
    [SerializeField] List<CutsceneAction> actions;
    [SerializeField] bool triggerRepeatedly;
    [SerializeField] bool triggerInside;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public bool TriggerInside => triggerInside;

    public IEnumerator Play()
    {
        GameController.Instance.StateMachine.Push(CutsceneState.i);

        foreach (var action in actions)
        {
            if (action.WaitForCompletion)
            {
                yield return action.Play();
            }
            else
            {
                StartCoroutine(action.Play());
            }
        }

        GameController.Instance.StateMachine.Pop();
    }

    public void AddAction(CutsceneAction action)
    {
#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(this, "Add action to cutscene.");
#endif

        action.Name = action.GetType().ToString();
        actions.Add(action);
    }

    public void OnPlayerTriggered(PlayerController player, Action onTriggerFinished = null)
    {
        player.Character.Animator.IsMoving = false;

        StartCoroutine(Play());
    }
}
