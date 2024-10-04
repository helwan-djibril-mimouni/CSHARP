using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Create new rare item")]
public class RareItem : ItemBase
{
    public override bool Use(Pokemon pokemon)
    {
        return false;
    }
}
