using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCExclamationAction : CutsceneAction
{
    [SerializeField] NPCController npc;

    public override IEnumerator Play()
    {
        yield return npc.Exclamation();
    }
}
