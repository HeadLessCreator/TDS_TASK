using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>  
/// - 일정 시간 간격마다 공격 시도  
/// - 공격 범위 내에서 가장 가까운 좀비를 찾아서 총알 발사  
/// </summary>
public class HeroController : MonoBehaviour
{
    [Header("공격 설정")]
    [Tooltip("초당 공격 횟수 (1이면 1초에 한 번, 2면 0.5초마다 한 번)")]
    public float attackSpeed = 1f;

    [Tooltip("1발당 입힐 데미지")]
    public int damageAmount = 5;

    [Tooltip("히어로 기준 공격 범위 반경")]
    public float attackRange = 3f;

    [Tooltip("좀비 탐지용 레이어 마스크 배열")]
    public LayerMask[] zombieLayerMasks;

    [Header("위치 참조")]
    [Tooltip("총알 발사 기준 지점")]
    public Transform attackPoint;

    [Header("총알(Bullet) 관련")]
    [Tooltip("발사할 총알 prefab")]
    public GameObject bulletPrefab;

    [Tooltip("인스펙터에서 true로 설정하면 공격을 멈춥니다")]
    public bool debugAttackStop;

    private float attackInterval;
    private int combinedZombieMask;

    void Start()
    {
        if (attackSpeed <= 0f) attackSpeed = 1f;
        attackInterval = 1f / attackSpeed;

        // 1) 배열에 들어온 LayerMask들을 OR 연산으로 합쳐서 하나의 int 마스크로 만든다.
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
            // debugAttackStop이 true면 Attack() 호출하지 않음
            if (!debugAttackStop)
                Attack();

            yield return new WaitForSeconds(attackInterval);
        }
    }

    void Attack()
    {
        // 합쳐진 combinedZombieMask를 사용해 한 번에 모든 좀비 레이어를 검색
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, combinedZombieMask);
        if (hits.Length == 0) return;

        // 그중 가장 가까운 좀비 찾아서 총알 발사
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
