using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainerFOV : MonoBehaviour, IPlayerTriggerable
{
    public void OnPlayerTriggered(PlayerController player, Action OnTriggerFinished = null)
    {
        player.Character.Animator.IsMoving = false;
        GameController.Instance.OnEnterTrainersView(GetComponentInParent<TrainerController>());
    }

    public bool TriggerRepeatedly => false;
    public bool TriggerInside => false;
}
