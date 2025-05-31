using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum ZombieState
{
    Run,
    Attack,
    Die
}

public class ZombieController : MonoBehaviour
{
    //헷갈리지 말고 Truck 태그는 목표 위치 가져와서 이동하는데 사용
    //나머지 개별 박스에는 Hero 태그 붙여놓고 활용

    [Header("기본 설정")]
    public ZombieState currentState = ZombieState.Run;
    public int spawnerID; //0,1,2 -> 같은 스포너에서 나온 좀비끼리는 ID 비교로 구분
    public float runSpeed = 2f;
    public float jumpForce = 5f;
    public float forwardForce = 5f; // 점프 후 앞으로 밀어줄 힘
    public int hp = 3;
    public int attackDamage = 1;
    [Tooltip("앞 좀비와 너무 가까워질 때, 앞 좀비가 피해 가지 않도록 일정 거리 유지")]
    public float minDistanceToFrontZombie = 0.02f;
    [Tooltip("뒤 좀비 치밀림 전달 시 최소 거리")]
    public float minDistanceToBackZombie = 0.05f;

    [Header("검사용 Transform & Sizes")]
    public Transform topCheck;
    public Transform behindCheck;
    public Transform bottomCheck;
    public Transform frontCheck;

    public Vector2 checkBoxSize = new Vector2(0.25f, 0.25f); // 너비/높이 조절 가능
    public Vector2 frontCheckBoxSize = new Vector2(0.05f, 0.5f); // 너비/높이 조절 가능

    [Header("트럭(Hero) 감지용")]
    private Transform truckTarget; // 트럭 위치를 참조 (태그 "Hero"로 지정된 오브젝트)

    [Header("밀기용 힘")]
    public Vector3 pushForce = new Vector3(0.1f, 0f, 0f);

    // 내부 컴포넌트 참조
    private Animator animator;
    private CapsuleCollider2D col;
    private Rigidbody2D rb;

    // 점프 & 밀림 상태를 위한 플래그
    [SerializeField] private bool isJumping = false; // 현재 점프 애니메이션 중인지
    [SerializeField] private bool hasAppliedForwardForce = false; // 점프 최고점에서 앞으로 밀어준 적이 있는지
    [SerializeField] private bool isPushed = false; // 밀리는 코루틴이 동작 중인지

    // Overlap 검사에서 찾아낸 인접 좀비(앞, 뒤, 아래, 위) 캐싱용
    private ZombieController frontZombie = null;
    private ZombieController backZombie = null;
    private ZombieController belowZombie = null;
    private ZombieController topZombie = null;

    // 공격 시 타격 대상 (트럭 블록)
    private GameObject blockTarget; // Hero(TruckBlock) 참조

    void Start()
    {
        var truckObj = GameObject.FindWithTag("Hero"); animator = GetComponent<Animator>();
        if (truckObj != null)
            truckTarget = truckObj.transform;

        animator = GetComponent<Animator>();
        col = GetComponent<CapsuleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        switch (currentState)
        {
            case ZombieState.Run:
                Run();
                break;
            case ZombieState.Attack:
                Attack();
                break;
            case ZombieState.Die:
                Die();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (isJumping)
        {
            if (rb.velocity.y < 0f && !hasAppliedForwardForce)
            {
                // 점프 최고점 도달
                if (!IsTooCloseToFrontZombie()) // 앞에 아무도 없다면
                {
                    rb.AddForce(Vector2.left * forwardForce, ForceMode2D.Impulse); // 왼쪽으로 이동 (트럭 방향)
                }

                hasAppliedForwardForce = true;
            }

            if (isGrounded() || HasZombieBelow())
            {
                isJumping = false;
            }
        }
    }

    //범용 overlap 검사 함수
    private Collider2D OverlapCheck(Transform checkPoint, Vector2 boxSize, int layerMask, Color debugColor)
    {
        // 오버랩 박스 그리기 (디버그용)
        DebugDrawBox(checkPoint.position, boxSize, debugColor);

        return Physics2D.OverlapBox(checkPoint.position, boxSize, 0f, layerMask);
    }

    //좀비 바로 앞 검사
    bool IsTooCloseToFrontZombie()
    {
        // "Zombie" 레이어만 검출
        Collider2D hit = OverlapCheck(frontCheck, frontCheckBoxSize, LayerMask.GetMask("Zombie"), Color.cyan);

        frontZombie = null;
        if (hit != null)
        {
            ZombieController other = hit.GetComponent<ZombieController>();
            if (other != null && other.spawnerID == this.spawnerID && other != gameObject)
            {
                frontZombie = other;
                return true;
            }
        }

        return false;
    }

    //트럭(Hero) 바로 앞 검사
    bool IsTruckInFront()
    {
        Collider2D hit = OverlapCheck(frontCheck, frontCheckBoxSize, LayerMask.GetMask("Hero"), Color.red);

        if (hit != null)
        {
            GameObject hitObj = hit.gameObject;

            // 좀비는 제외
            if (hitObj.CompareTag("Zombie") && hitObj != gameObject)
            {
                blockTarget = null;
                return false;
            }

            // 트럭이면 true
            if (hitObj.CompareTag("Hero"))
            {
                blockTarget = hitObj;
                return true;
            }
        }

        // 기타는 무시
        blockTarget = null;
        return false;
    }

    // 좀비 바로 뒤 검사
    bool hasZombieBehind()
    {
        Collider2D hit = OverlapCheck(behindCheck, checkBoxSize, LayerMask.GetMask("Zombie"), Color.magenta);

        // 디버그 시각화
        DebugDrawBox(behindCheck.position, checkBoxSize, Color.magenta);

        backZombie = null;

        if (hit != null)
        {
            ZombieController other = hit.GetComponent<ZombieController>();
            if (other != null && other.spawnerID == this.spawnerID && other != gameObject)
            {
                backZombie = other;
                return true;
            }
        }

        return false;
    }

    //머리 위 좀비 검사
    public bool HasZombieOnTop()
    {
        Collider2D hit = OverlapCheck(topCheck, checkBoxSize, LayerMask.GetMask("Zombie"), Color.cyan);

        topZombie = null;

        if (hit != null)
        {
            ZombieController other = hit.GetComponent<ZombieController>();
            if (other != null && other.spawnerID == this.spawnerID && other != gameObject)
            {
                topZombie = other;
                return true;
            }
        }

        return false;
    }

    //발 밑 좀비 검사
    public bool HasZombieBelow()
    {
        Collider2D hit = OverlapCheck(bottomCheck, checkBoxSize, LayerMask.GetMask("Zombie"), Color.green);

        belowZombie = null;
        if (hit != null)
        {
            ZombieController other = hit.GetComponent<ZombieController>();
            if (other != null && other.spawnerID == this.spawnerID)
            {
                belowZombie = other;
                return true;
            }
        }

        return false;
    }

    //땅 위에 착지 검사
    public bool isGrounded()
    {
        Collider2D hit = OverlapCheck(bottomCheck, checkBoxSize, LayerMask.GetMask("Ground"), Color.red);
        return hit != null;
    }

    void Run() //기본 상태임
    {
        animator.SetBool("IsAttacking", false);

        // 만약 점프 중이거나 밀리는 중이면 Run 로직 수행하지 않음
        if (isJumping || isPushed) return;

        // 트럭이 앞에 있으면 바로 공격 상태 전환
        if (IsTruckInFront())
        {
            currentState = ZombieState.Attack;
            return;
        }

        //앞 좀비가 너무 가까이 붙었으면 Jump 로직 시도
        //(단, 뒤좀비가 없고, 앞 좀비 머리 위에도 좀비가 없어야 함)
        if (IsTooCloseToFrontZombie() && !hasZombieBehind() && !frontZombie.HasZombieOnTop())
        {
            StartJump();
            return;            
        }

        //앞 좀비가 붙어 있으면 멈추기
        if (IsTooCloseToFrontZombie())
        {
            rb.velocity = new Vector2(0f, rb.velocity.y); // 이동 멈춤
            return;
        }

        //그 외 왼쪽(트럭 방향)으로 달리기
        rb.velocity = new Vector2(-runSpeed, rb.velocity.y); // 왼쪽(트럭 방향) 이동
    }

    void Attack()
    {
        animator.SetBool("IsAttacking", true); //애니메이션 이벤트 OnAttack

        // 트럭 감지가 안 되면 다시 Run 상태로 전환
        if (!IsTruckInFront())
        {
            currentState = ZombieState.Run;
            return;
        }

        // 공격 중 아래 좀비 검사
        if (HasZombieBelow())
        {
            // 공격 중이라면 밀어냄
            if (belowZombie.currentState == ZombieState.Attack)
            {
                belowZombie.PushFromFront(pushForce);
            }
        }
    }

    void StartJump()
    {
        if (isJumping) return;

        isJumping = true;
        hasAppliedForwardForce = false;

        rb.velocity = Vector2.zero; // 속도 초기화
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    void Die()
    {
        Destroy(gameObject);
    }

    public void OnAttack()
    {
        if(blockTarget != null)
        {
            TruckBlock block = blockTarget.GetComponent<TruckBlock>();
            if (block != null)
            {
                block.TakeDamage(attackDamage);
            }
        }
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0)
        {
            currentState = ZombieState.Die;
        }
    }

    public void PushFromFront(Vector3 force)
    {
        if (isPushed) return;
        StartCoroutine(SmoothPush(force));
    }

    IEnumerator SmoothPush(Vector3 force)
    {
        isPushed = true;

        Vector3 start = transform.position;
        Vector3 end = start + force;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = end;

        yield return new WaitForSeconds(0.05f); // 밀림 후 잠깐 기다림

        Collider2D hit = Physics2D.OverlapBox(behindCheck.position, checkBoxSize, 0f, LayerMask.GetMask("Zombie"));
        if (hit != null)
        {
            var next = hit.GetComponent<ZombieController>();
            // 같은 spawnerID 이고 아직 밀리는 중이 아니면 재귀 호출
            if (next != null && next.spawnerID == this.spawnerID && !next.isPushed)
            {
                // 뒷줄엔 좀 약하게 전달 또는 그대로 전달
                next.PushFromFront(force * 0.9f);
                //next.PushFromFront(force);
            }
        }

        // ↓ 여기서 아래로 추가 힘 주기 ↓
        rb.AddForce(Vector2.down * forwardForce * 0.5f, ForceMode2D.Impulse);

        // 밀림 끝
        yield return new WaitForSeconds(0.05f);
        isPushed = false;
    }

    void DebugDrawBox(Vector2 center, Vector2 size, Color color)
    {
        Vector2 halfSize = size * 0.5f;
        Vector3 topLeft = center + new Vector2(-halfSize.x, halfSize.y);
        Vector3 topRight = center + new Vector2(halfSize.x, halfSize.y);
        Vector3 bottomLeft = center + new Vector2(-halfSize.x, -halfSize.y);
        Vector3 bottomRight = center + new Vector2(halfSize.x, -halfSize.y);

        Debug.DrawLine(topLeft, topRight, color);     // Top
        Debug.DrawLine(topRight, bottomRight, color); // Right
        Debug.DrawLine(bottomRight, bottomLeft, color); // Bottom
        Debug.DrawLine(bottomLeft, topLeft, color);   // Left
    }

}

