using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    public Transform truck;              // 트럭(기준)
    public float parallaxFactor = 0.0f;  // 앞배경(1.0), 뒷배경(0.3)
    public float backgroundWidth = 20f;  // 한 배경 이미지의 가로 폭

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

        // 배경의 상대적 움직임 (트럭 이동거리 * 시차계수)
        transform.position += new Vector3(delta.x * parallaxFactor, 0f, 0f);

        // 배경 반복 (오른쪽으로 순간이동)
        foreach (Transform child in transform)
        {
            if (truck.position.x - child.position.x >= backgroundWidth)
            {
                child.position += new Vector3(backgroundWidth * 3f, 0f, 0f); // 배경 3개가 이어져있다고 가정
            }
        }

        previousTruckPosition = truck.position;
    }
}
