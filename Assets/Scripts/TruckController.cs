using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;           // 이동 속도
    private bool isStopped;

    [Header("Dependencies")]
    [SerializeField] private ZombieDetector detector;         // 연결된 Detector

    [Header("Wheels")]
    [SerializeField] private List<Transform> wheels;          // 앞/뒤 바퀴 Transform
    [SerializeField] private float rotationFactor = 360f;     // 속도→휠 회전비

    void OnEnable()
    {
        detector.OnStopStateChanged.AddListener(OnStoppedChanged);
    }

    void OnDisable()
    {
        detector.OnStopStateChanged.RemoveListener(OnStoppedChanged);
    }

    void Update()
    {
        if (!isStopped)
            MoveAndRotate();
    }

    private void MoveAndRotate()
    {
        // 1) 이동
        float delta = moveSpeed * Time.deltaTime;
        transform.position += Vector3.right * delta;

        // 2) 휠 회전
        float rot = -delta * rotationFactor;
        foreach (var w in wheels)
            w.Rotate(Vector3.forward, rot);
    }

    private void OnStoppedChanged(bool stopped)
    {
        isStopped = stopped;
    }
}
