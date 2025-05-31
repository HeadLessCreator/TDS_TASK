using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;         // Ʈ���� Transform�� �Ҵ�
    public Vector3 offset = new Vector3(2f, 0f, -10f); // ī�޶� ��ġ ������
    public float smoothSpeed = 5f;   // ���󰡴� �ε巯��

    void LateUpdate()
    {
        if (target != null)
        {
            // ��ǥ ��ġ ��� (Ʈ�� + offset)
            Vector3 desiredPosition = target.position + offset;

            // �ε巴�� �̵� (Lerp)
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // ī�޶� ��ġ ������Ʈ
            transform.position = smoothedPosition;
        }
    }
}
