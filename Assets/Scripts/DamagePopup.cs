using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// ���� �������� ���� ��, �Ӹ� ���� �������� �ؽ�Ʈ �˾�.
/// - ���� ��� ���� ��¦ ��������, ������ �ð�(lifeTime) �ڿ� �ڵ� �ı�.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class DamagePopup : MonoBehaviour
{
    [Tooltip("�˾��� ȭ�鿡 �� �ִ� �� �ð� (��)")]
    public float lifeTime = 1f;

    [Tooltip("���� �������� �ӵ� (����/��)")]
    public float floatSpeed = 1f;

    private TextMeshPro textMesh;

    void Awake()
    {
        // TextMeshPro ������Ʈ ����
        textMesh = GetComponent<TextMeshPro>();

        // lifeTime(��: 1��) �ڿ� �ڵ����� �� ���ӿ�����Ʈ�� �ı�
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// ���� ���� ������ ���� ������ �� ȣ��
    /// </summary>
    public void SetText(string damageText)
    {
        if (textMesh != null)
            textMesh.text = damageText;
    }

    void Update()
    {
        // �� �����Ӹ��� Y������ floatSpeed��ŭ ���� ������
        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);
    }
}
