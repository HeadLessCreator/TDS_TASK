using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    public Transform truck;              // Ʈ��(����)
    public float parallaxFactor = 0.0f;  // �չ��(1.0), �޹��(0.3)
    public float backgroundWidth = 20f;  // �� ��� �̹����� ���� ��

    private Vector3 previousTruckPosition;
    public ZombieDetector detector;

    void Start()
    {
        previousTruckPosition = truck.position;
    }

    void LateUpdate()
    {
        if (detector.IsStopped) return;

        Vector3 delta = truck.position - previousTruckPosition;

        // ����� ����� ������ (Ʈ�� �̵��Ÿ� * �������)
        transform.position += new Vector3(delta.x * parallaxFactor, 0f, 0f);

        // ��� �ݺ� (���������� �����̵�)
        foreach (Transform child in transform)
        {
            if (truck.position.x - child.position.x >= backgroundWidth)
            {
                child.position += new Vector3(backgroundWidth * 3f, 0f, 0f); // ��� 3���� �̾����ִٰ� ����
            }
        }

        previousTruckPosition = truck.position;
    }
}
