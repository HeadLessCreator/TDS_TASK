using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>  
/// - ���� �ð� ���ݸ��� ���� �õ�  
/// - ���� ���� ������ ���� ����� ���� ã�Ƽ� �Ѿ� �߻�  
/// </summary>
public class HeroController : MonoBehaviour
{
    [Header("���� ����")]
    [Tooltip("�ʴ� ���� Ƚ�� (1�̸� 1�ʿ� �� ��, 2�� 0.5�ʸ��� �� ��)")]
    public float attackSpeed = 1f;

    [Tooltip("1�ߴ� ���� ������")]
    public int damageAmount = 5;

    [Tooltip("����� ���� ���� ���� �ݰ�")]
    public float attackRange = 3f;

    [Tooltip("���� Ž���� ���̾� ����ũ �迭")]
    public LayerMask[] zombieLayerMasks;

    [Header("��ġ ����")]
    [Tooltip("�Ѿ� �߻� ���� ����")]
    public Transform attackPoint;

    [Header("�Ѿ�(Bullet) ����")]
    [Tooltip("�߻��� �Ѿ� prefab")]
    public GameObject bulletPrefab;

    [Tooltip("�ν����Ϳ��� true�� �����ϸ� ������ ����ϴ�")]
    public bool debugAttackStop;

    private float attackInterval;
    private int combinedZombieMask;

    void Start()
    {
        if (attackSpeed <= 0f) attackSpeed = 1f;
        attackInterval = 1f / attackSpeed;

        // 1) �迭�� ���� LayerMask���� OR �������� ���ļ� �ϳ��� int ����ũ�� �����.
        combinedZombieMask = 0;
        foreach (LayerMask lm in zombieLayerMasks)
        {
            combinedZombieMask |= lm.value;
        }

        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        while (true)
        {
            // debugAttackStop�� true�� Attack() ȣ������ ����
            if (!debugAttackStop)
                Attack();

            yield return new WaitForSeconds(attackInterval);
        }
    }

    void Attack()
    {
        // ������ combinedZombieMask�� ����� �� ���� ��� ���� ���̾ �˻�
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, combinedZombieMask);
        if (hits.Length == 0) return;

        // ���� ���� ����� ���� ã�Ƽ� �Ѿ� �߻�
        Collider2D nearest = hits[0];
        float minDist = Vector2.Distance(attackPoint.position, nearest.transform.position);

        for (int i = 1; i < hits.Length; i++)
        {
            float dist = Vector2.Distance(attackPoint.position, hits[i].transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = hits[i];
            }
        }

        SpawnBullet(nearest.transform);
    }

    void SpawnBullet(Transform target)
    {
        if (bulletPrefab == null || attackPoint == null || target == null) return;

        GameObject bulletObj = Instantiate(bulletPrefab, attackPoint.position, Quaternion.identity);
        BulletController bc = bulletObj.GetComponent<BulletController>();
        if (bc != null)
        {
            bc.Initialize(target.position, damageAmount);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
