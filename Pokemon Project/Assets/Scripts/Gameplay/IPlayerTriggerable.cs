using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerTriggerable
{
    void OnPlayerTriggered(PlayerController player, Action OnTriggerFinished=null);

    bool TriggerRepeatedly { get; }
}
