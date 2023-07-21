using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wallet : MonoBehaviour
{
    [SerializeField] float money;

    public void AddMoney(float amount)
    {
        money += amount;
    }

    public void TakeMoney(float amount)
    {
        money -= amount;
    }

    public float Money => money;
}
