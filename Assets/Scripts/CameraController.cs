using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;         // 트럭의 Transform을 할당
    public Vector3 offset = new Vector3(2f, 0f, -10f); // 카메라 위치 오프셋
    public float smoothSpeed = 5f;   // 따라가는 부드러움

    void LateUpdate()
    {
        if (target != null)
        {
            // 목표 위치 계산 (트럭 + offset)
            Vector3 desiredPosition = target.position + offset;

            // 부드럽게 이동 (Lerp)
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // 카메라 위치 업데이트
            transform.position = smoothedPosition;
        }
    }
}
