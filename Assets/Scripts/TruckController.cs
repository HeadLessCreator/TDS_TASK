using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;           // �̵� �ӵ�
    private bool isStopped;

    [Header("Dependencies")]
    [SerializeField] private ZombieDetector detector;         // ����� Detector

    [Header("Wheels")]
    [SerializeField] private List<Transform> wheels;          // ��/�� ���� Transform
    [SerializeField] private float rotationFactor = 360f;     // �ӵ����� ȸ����

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
        // 1) �̵�
        float delta = moveSpeed * Time.deltaTime;
        transform.position += Vector3.right * delta;

        // 2) �� ȸ��
        float rot = -delta * rotationFactor;
        foreach (var w in wheels)
            w.Rotate(Vector3.forward, rot);
    }

    private void OnStoppedChanged(bool stopped)
    {
        isStopped = stopped;
    }
}
