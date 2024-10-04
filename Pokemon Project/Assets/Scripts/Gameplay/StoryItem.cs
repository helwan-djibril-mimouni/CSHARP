using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryItem : MonoBehaviour, IPlayerTriggerable
{
    [SerializeField] Dialog dialog;
    public bool blocksMovement = false;

    public void OnPlayerTriggered(PlayerController player, Action onTriggerFinished=null)
    {
        player.Character.Animator.IsMoving = false;

        StartCoroutine(DialogManager.Instance.ShowDialog(dialog, onFinished: onTriggerFinished));
    }

    public bool TriggerRepeatedly => false;
    public bool TriggerInside => false;
}
