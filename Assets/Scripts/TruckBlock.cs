using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckBlock : MonoBehaviour
{
    public int hp = 10;

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
        //Debug.Log($"HP LEFT: {hp}");
    }
}

