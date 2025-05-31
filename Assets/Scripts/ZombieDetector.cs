using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ZombieDetector : MonoBehaviour
{
    [SerializeField] private int stopZombieCount = 1;
    public HashSet<GameObject> zombiesInRange = new HashSet<GameObject>();
    private bool isStopped;

    // UnityEvent 정의
    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    // Inspector에서 리스너 연결 가능
    public BoolEvent OnStopStateChanged = new BoolEvent();

    public bool IsStopped => isStopped;

    void Update()
    {
        bool stoppedNow = zombiesInRange.Count >= stopZombieCount;
        if (stoppedNow != isStopped)
        {
            isStopped = stoppedNow;
            OnStopStateChanged.Invoke(isStopped);
        }
        zombiesInRange.RemoveWhere(z => z == null || !z.activeInHierarchy);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Zombie")) zombiesInRange.Add(col.gameObject);
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Zombie")) zombiesInRange.Remove(col.gameObject);
    }
}
