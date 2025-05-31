using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum ZombieState
{
    Run,
    Jump,
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
    public int MaxHP = 10;
    public int CurrentHP;
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

    [Header("데미지 팝업 설정")]
    [Tooltip("데미지 팝업으로 사용할 Prefab")]
    public GameObject damagePopupPrefab;

    [Tooltip("팝업을 띄울 오프셋 (좀비 위치 + this offset)")]
    public Vector3 popupOffset = new Vector3(0f, 1f, 0f);

    // 내부 컴포넌트
    private Animator animator;
    private CapsuleCollider2D col;
    private Rigidbody2D rb;

    // 점프 & 밀림 상태를 위한 플래그
    [SerializeField] private bool hasAppliedForwardForce = false; // 점프 최고점에서 앞으로 밀어준 적이 있는지
    [SerializeField] private bool isPushed = false; // 밀리는 코루틴이 동작 중인지

    // Overlap 검사에서 찾아낸 인접 좀비(앞, 뒤, 아래, 위) 캐시
    private ZombieController frontZombie = null;
    private ZombieController backZombie = null;
    private ZombieController belowZombie = null;
    private ZombieController topZombie = null;

    // 공격 시 타격 대상 (트럭 블록)
    private GameObject blockTarget; // Hero(TruckBlock) 참조

    //높이 제한 - 영웅 높이
    [SerializeField] private GameObject heightLimit;

    void Awake()
    {
        animator = GetComponent<Animator>();
        col = GetComponent<CapsuleCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        CurrentHP = MaxHP;
    }

    private void OnEnable()
    {
        // 풀에서 꺼낼 때마다 실행 초기화 코드
        CurrentHP = MaxHP;
        currentState = ZombieState.Run;
        hasAppliedForwardForce = false;
        isPushed = false;
        heightLimit = GameObject.FindWithTag("HeightLimit");
        rb.velocity = Vector2.zero;                 // 물리 속도 초기화
        animator.SetBool("IsAttacking", false);     // 애니메이터 초기화
    }

    private void OnDisable()
    {
        // 풀에 돌아갈 때(Disable) 할 정리 작업이 있으면 여기에 추가
    }

    void Update()
    {
        // 현재 상태에 따라 “상태 전환용 검사”만 수행
        switch (currentState)
        {
            case ZombieState.Run:
                Run_StateUpdate();
                break;

            case ZombieState.Jump:
                Jump_StateUpdate();
                break;

            case ZombieState.Attack:
                Attack_StateUpdate();
                break;

            case ZombieState.Die:
                // Die 상태는 감지만 해두면, 실제 파괴는 한 번만 실행
                Die_StateUpdate();
                break;
        }
    }

    private void FixedUpdate()
    {
        // 현재 상태에 따라 “물리 연산(이동/점프/밀림 등)”만 수행
        switch (currentState)
        {
            case ZombieState.Run:
                Run_PhysicsUpdate();
                break;

            case ZombieState.Jump:
                Jump_PhysicsUpdate();
                break;

            case ZombieState.Attack:
                Attack_PhysicsUpdate();
                break;

            case ZombieState.Die:
                // Die 상태는 물리 연산 없음
                break;
        }
    }

    /// <summary>
    /// Run 상태에서 매 프레임마다 “상태 전환 여부”를 검사
    /// - 영웅 높이보다 올라가 있으면 점프 시도 안함
    /// </summary>
    void Run_StateUpdate()
    {
        animator.SetBool("IsAttacking", false);

        // 밀림 처리 중이라면 검사 중단
        if (isPushed)
            return;

        // 1. 트럭이 바로 앞에 있으면 Attack으로 전환
        if (IsTruckInFront())
        {
            currentState = ZombieState.Attack;
            return;
        }

        // 2. 앞 좀비가 너무 가까우면
        if (IsTooCloseToFrontZombie())
        {
            // 2-1. 뒤 좀비가 없고, 앞 좀비 위에도 좀비가 없으면 → Jump 상태로 전환
            if (!HasZombieBehind() && !frontZombie.HasZombieOnTop())
            {
                EnterJumpState();
                return;
            }
            // 2-2. 그 외(뒤 좀비 있거나 앞 좀비 위에 좀비 있으면) → 달리기 멈춤
            return;
        }

        // 3. 그 외 달리기(속도 설정은 Run_PhysicsUpdate에서 처리)
    }

    /// <summary>
    /// Run 상태에서 FixedUpdate 물리 연산 처리
    /// </summary>
    void Run_PhysicsUpdate()
    {
        // 1. 밀림 중이면 달리기/멈춤 관련 물리 연산 하지 않음
        if (isPushed)
            return;

        // 2. 앞 좀비가 너무 가까우면 달리기 멈춤
        if (IsTooCloseToFrontZombie())
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        // 3. 달리기: 왼쪽 트럭 방향으로 RunSpeed 만큼 일정 속도 유지
        rb.velocity = new Vector2(-runSpeed, rb.velocity.y);
    }

    /// <summary>
    /// Run -> Jump 상태로 진입할 때 호출
    /// </summary>
    void EnterJumpState()
    {
        // Animator에 “IsAttacking” false(공격 중이 아님) 설정
        animator.SetBool("IsAttacking", false);

        // Jump 상태로 전환
        currentState = ZombieState.Jump;

        // 점프 관련 플래그 초기화
        hasAppliedForwardForce = false;

        // 수직 속도 초기화 후 위로 Impulse 추가
        rb.velocity = Vector2.zero;
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    /// <summary>
    /// Jump 상태에서 매 프레임마다 상태 전환 여부 검사
    /// - 영웅 높이보다 올라가 있으면 점프 시도 자체를 막음
    /// </summary>
    void Jump_StateUpdate()
    {
        //밀림 처리 중이라면(특정 상황에서 공중에서도 밀릴 수 있으므로) 탈출
        if (isPushed)
            return;

        //영웅 오브젝트에 붙어있는 heightlimit 오브젝트의 높이를 가져와서, 
        //만약 좀비 위치 Y가 heightlimit 이상이라면 더 이상 '점프' 시도하지 않는다.
        if (heightLimit != null && transform.position.y >= heightLimit.transform.position.y)
        {
            // 높은 곳에 도달했으므로, 앞으로 점프 로직은 건너뛰고 Run_PhysicsUpdate()쪽으로 넘어가서 달리기만 유지
            return;
        }

        // 1. 점프 최고점(속도 y가 0에서 음수로 바뀔 때)에서 한 번만 앞으로 밀어줌
        if (rb.velocity.y < 0f && !hasAppliedForwardForce)
        {
            // 앞 좀비가 없다면 → 앞으로 밀어줘야 함
            if (!IsTooCloseToFrontZombie())
            {
                rb.AddForce(Vector2.left * forwardForce, ForceMode2D.Impulse);
            }
            hasAppliedForwardForce = true;
        }

        // 2. 착지 판단: 땅에 닿았거나 아래에 좀비가 있는 경우 → Run 상태로 전환
        if (IsGrounded() || HasZombieBelow())
        {
            currentState = ZombieState.Run;
        }
    }

    /// <summary>
    /// Jump 상태에서 FixedUpdate 시 물리 연산 처리
    /// </summary>
    void Jump_PhysicsUpdate()
    {
        // 밀림 중이면 공중 물리 연산 없이 탈출 (밀림 코루틴이 직접 위치를 옮겨줌)
        if (isPushed)
            return;

        // 점프 중에는 수평 달리기 속도 대신 물리 엔진으로 처리된 속도(impulse + 중력)만 유지
    }

    /// <summary>
    /// Attack 상태에서 매 프레임마다 상태 전환 여부를 검사
    /// - 트럭이 더 이상 앞에 없으면 Run으로 복귀
    /// - 아래 좀비가 밀려들어오면 밀어내기 호출
    /// </summary>
    void Attack_StateUpdate()
    {
        animator.SetBool("IsAttacking", true);

        // 1. 트럭이 더 이상 앞에 없다면 Run 상태로 복귀
        if (!IsTruckInFront())
        {
            currentState = ZombieState.Run;
            return;
        }

        // 2. 아래 좀비가 밀려온다면, 밀어내기 로직 호출
        if (HasZombieBelow())
        {
            // 아래 좀비도 현재 Attack 중이라면 밀어내기를 수행
            if (belowZombie.currentState == ZombieState.Attack)
            {
                belowZombie.PushFromFront(pushForce);
            }
        }
    }

    /// <summary>
    /// Attack 상태에서 필요한 물리 연산(현재는 특별히 없음)
    /// </summary>
    void Attack_PhysicsUpdate()
    {
        // 현재 특별히 필요하지 않으므로 비워둠
    }

    /// <summary>
    /// Die 상태 진입 시 한 번만 호출하여 오브젝트 파괴
    /// </summary>
    void Die_StateUpdate()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 애니메이터 이벤트에서 호출: 공격 판정 시점에 호출
    /// 트럭 블록(TruckBlock)이 있다면 데미지 전달
    /// </summary>
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

    /// <summary>
    /// 외부 호출: 데미지를 입을 때 호출
    /// hp가 0 이하로 떨어지면 Die 상태로 전환
    /// </summary>
    public void TakeDamage(int amount)
    {
        CurrentHP -= amount;
        ShowDamagePopup(amount);

        if (CurrentHP <= 0)
        {
            currentState = ZombieState.Die;
        }
    }

    private void ShowDamagePopup(int damage)
    {
        if (damagePopupPrefab == null)
            return;

        Vector3 spawnPos = transform.position + popupOffset;
        GameObject popup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
        DamagePopup dp = popup.GetComponent<DamagePopup>();
        if (dp != null)
        {
            dp.SetText(damage.ToString());
        }
    }


    /// <summary>
    /// 외부 호출: 앞에서부터 밀리는 힘이 들어올 때 호출
    /// SmoothPush 코루틴을 통해 부드럽게 밀림
    /// </summary>
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

        Collider2D hit = Physics2D.OverlapBox(behindCheck.position, checkBoxSize, 0f, LayerMask.GetMask("Zombie" + spawnerID));
        if (hit != null)
        {
            var next = hit.GetComponent<ZombieController>();
            // 같은 spawnerID 이고 아직 밀리는 중이 아니면 재귀 호출
            if (next != null && next.spawnerID == this.spawnerID && !next.isPushed)
            {
                // 뒷줄엔 좀 약하게 전달 또는 그대로 전달
                next.PushFromFront(force * 0.95f);
                //next.PushFromFront(force);
            }
        }

        // 여기서 아래로 추가 힘 주기
        rb.AddForce(Vector2.down * forwardForce, ForceMode2D.Impulse);

        // 밀림 끝
        yield return new WaitForSeconds(0.05f);
        isPushed = false;
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
        // 동적 레이어 명: "Zombie" + spawnerID
        int myZombieMask = LayerMask.GetMask("Zombie" + spawnerID);

        // "Zombie" 레이어만 검출
        Collider2D hit = OverlapCheck(frontCheck, frontCheckBoxSize, myZombieMask, Color.cyan);

        frontZombie = null;
        if (hit != null)
        {
            ZombieController other = hit.GetComponent<ZombieController>();
            if (other != null && other.spawnerID == this.spawnerID && other != this)
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
        int heroMask = LayerMask.GetMask("Hero");
        Collider2D hit = OverlapCheck(frontCheck, frontCheckBoxSize, heroMask, Color.red);

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
    bool HasZombieBehind()
    {
        int myZombieMask = LayerMask.GetMask("Zombie" + spawnerID);
        Collider2D hit = OverlapCheck(behindCheck, checkBoxSize, myZombieMask, Color.magenta);

        DebugDrawBox(behindCheck.position, checkBoxSize, Color.magenta);

        backZombie = null;

        if (hit != null)
        {
            ZombieController other = hit.GetComponent<ZombieController>();
            if (other != null && other.spawnerID == this.spawnerID && other != this)
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
        int myZombieMask = LayerMask.GetMask("Zombie" + spawnerID);
        Collider2D hit = OverlapCheck(topCheck, checkBoxSize, myZombieMask, Color.cyan);

        topZombie = null;

        if (hit != null)
        {
            ZombieController other = hit.GetComponent<ZombieController>();
            if (other != null && other.spawnerID == this.spawnerID && other != this)
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
        int myZombieMask = LayerMask.GetMask("Zombie" + spawnerID);
        Collider2D hit = OverlapCheck(bottomCheck, checkBoxSize, myZombieMask, Color.green);

        belowZombie = null;
        if (hit != null)
        {
            ZombieController other = hit.GetComponent<ZombieController>();
            if (other != null && other.spawnerID == this.spawnerID && other != this)
            {
                belowZombie = other;
                return true;
            }
        }

        return false;
    }

    //땅 위에 착지 검사
    public bool IsGrounded()
    {
        // 동적 레이어 명: "Ground" + spawnerID
        int myGroundMask = LayerMask.GetMask("Ground" + spawnerID);

        Collider2D hit = OverlapCheck(bottomCheck, checkBoxSize, myGroundMask, Color.red);
        return hit != null;
    }

    //디버그용 박스 그리기
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

