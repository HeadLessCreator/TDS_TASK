using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BulletController : MonoBehaviour
{
    [Tooltip("총알 속도")]
    public float speed = 10f;

    [Tooltip("총알이 입힐 데미지")]
    public int damageAmount = 10;

    [Header("다수의 좀비 레이어(인스펙터에 배열로 추가)")]
    [Tooltip("인스펙터에서 'ZombieRow0', 'ZombieRow1', 'ZombieRow2' 등을 모두 추가")]
    public LayerMask[] zombieLayerMasks;

    [Tooltip("총알이 자동 파괴되기 전 최대 생존 시간(초)")]
    public float maxLifeTime = 5f;

    private Rigidbody2D rb;
    private Vector2 moveDirection; //날아갈 방향

    // 런타임에 계산할, OR 연산된 단일 비트마스크
    private int combinedZombieMask;

    void Awake()
    {
        // Collider2D를 Trigger로 설정해 둔 상태여야 함
        // Rigidbody2D의 Gravity Scale은 0으로 세팅되어 있어야 중력 영향을 받지 않고 직선 비행 가능
        rb = GetComponent<Rigidbody2D>();

        // 배열에 들어온 LayerMask들을 OR 연산으로 합쳐서 하나의 int 마스크로 만든다.
        combinedZombieMask = 0;
        foreach (LayerMask lm in zombieLayerMasks)
        {
            combinedZombieMask |= lm.value;
        }
    }

    /// <summary>
    /// 총알이 생성되고 나서 가장 먼저 호출되어야 하는 초기화 메서드
    /// </summary>
    public void Initialize(Vector2 targetPosition, int damage)
    {
        // 움직일 방향: 타겟 위치 - 발사 위치
        Vector2 startPos = transform.position;
        Vector2 direction = (targetPosition - startPos).normalized;
        moveDirection = direction;

        damageAmount = damage;

        // Rigidbody2D velocity 설정
        rb.velocity = moveDirection * speed;

        // 일정 시간 후 자동 파괴 예약
        Destroy(gameObject, maxLifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌한 오브젝트의 레이어가, OR 연산된 combinedZombieMask 비트마스크 안에 있는지 확인
        //    (1 << other.gameObject.layer) 은 other 오브젝트 레이어의 비트 위치
        //    & combinedZombieMask != 0 이면 해당 레이어가 포함되어 있다는 의미
        if (((1 << other.gameObject.layer) & combinedZombieMask) != 0)
        {            
            ZombieController zombie = other.GetComponent<ZombieController>();
            if (zombie != null)
            {
                zombie.TakeDamage(damageAmount);
            }

            // 충돌 시 즉시 총알 파괴
            Destroy(gameObject);
        }
    }
}
