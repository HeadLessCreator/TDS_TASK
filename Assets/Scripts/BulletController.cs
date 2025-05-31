using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BulletController : MonoBehaviour
{
    [Tooltip("�Ѿ� �ӵ�")]
    public float speed = 10f;

    [Tooltip("�Ѿ��� ���� ������")]
    public int damageAmount = 10;

    [Header("�ټ��� ���� ���̾�(�ν����Ϳ� �迭�� �߰�)")]
    [Tooltip("�ν����Ϳ��� 'ZombieRow0', 'ZombieRow1', 'ZombieRow2' ���� ��� �߰�")]
    public LayerMask[] zombieLayerMasks;

    [Tooltip("�Ѿ��� �ڵ� �ı��Ǳ� �� �ִ� ���� �ð�(��)")]
    public float maxLifeTime = 5f;

    private Rigidbody2D rb;
    private Vector2 moveDirection; //���ư� ����

    // ��Ÿ�ӿ� �����, OR ����� ���� ��Ʈ����ũ
    private int combinedZombieMask;

    void Awake()
    {
        // Collider2D�� Trigger�� ������ �� ���¿��� ��
        // Rigidbody2D�� Gravity Scale�� 0���� ���õǾ� �־�� �߷� ������ ���� �ʰ� ���� ���� ����
        rb = GetComponent<Rigidbody2D>();

        // �迭�� ���� LayerMask���� OR �������� ���ļ� �ϳ��� int ����ũ�� �����.
        combinedZombieMask = 0;
        foreach (LayerMask lm in zombieLayerMasks)
        {
            combinedZombieMask |= lm.value;
        }
    }

    /// <summary>
    /// �Ѿ��� �����ǰ� ���� ���� ���� ȣ��Ǿ�� �ϴ� �ʱ�ȭ �޼���
    /// </summary>
    public void Initialize(Vector2 targetPosition, int damage)
    {
        // ������ ����: Ÿ�� ��ġ - �߻� ��ġ
        Vector2 startPos = transform.position;
        Vector2 direction = (targetPosition - startPos).normalized;
        moveDirection = direction;

        damageAmount = damage;

        // Rigidbody2D velocity ����
        rb.velocity = moveDirection * speed;

        // ���� �ð� �� �ڵ� �ı� ����
        Destroy(gameObject, maxLifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // �浹�� ������Ʈ�� ���̾, OR ����� combinedZombieMask ��Ʈ����ũ �ȿ� �ִ��� Ȯ��
        //    (1 << other.gameObject.layer) �� other ������Ʈ ���̾��� ��Ʈ ��ġ
        //    & combinedZombieMask != 0 �̸� �ش� ���̾ ���ԵǾ� �ִٴ� �ǹ�
        if (((1 << other.gameObject.layer) & combinedZombieMask) != 0)
        {            
            ZombieController zombie = other.GetComponent<ZombieController>();
            if (zombie != null)
            {
                zombie.TakeDamage(damageAmount);
            }

            // �浹 �� ��� �Ѿ� �ı�
            Destroy(gameObject);
        }
    }
}
