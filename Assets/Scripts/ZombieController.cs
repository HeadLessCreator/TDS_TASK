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
    //�򰥸��� ���� Truck �±״� ��ǥ ��ġ �����ͼ� �̵��ϴµ� ���
    //������ ���� �ڽ����� Hero �±� �ٿ����� Ȱ��

    [Header("�⺻ ����")]
    public ZombieState currentState = ZombieState.Run;
    public int spawnerID; //0,1,2 -> ���� �����ʿ��� ���� ���񳢸��� ID �񱳷� ����
    public float runSpeed = 2f;
    public float jumpForce = 5f;
    public float forwardForce = 5f; // ���� �� ������ �о��� ��
    public int MaxHP = 10;
    public int CurrentHP;
    public int attackDamage = 1;
    [Tooltip("�� ����� �ʹ� ������� ��, �� ���� ���� ���� �ʵ��� ���� �Ÿ� ����")]
    public float minDistanceToFrontZombie = 0.02f;
    [Tooltip("�� ���� ġ�и� ���� �� �ּ� �Ÿ�")]
    public float minDistanceToBackZombie = 0.05f;

    [Header("�˻�� Transform & Sizes")]
    public Transform topCheck;
    public Transform behindCheck;
    public Transform bottomCheck;
    public Transform frontCheck;

    public Vector2 checkBoxSize = new Vector2(0.25f, 0.25f); // �ʺ�/���� ���� ����
    public Vector2 frontCheckBoxSize = new Vector2(0.05f, 0.5f); // �ʺ�/���� ���� ����

    [Header("Ʈ��(Hero) ������")]
    private Transform truckTarget; // Ʈ�� ��ġ�� ���� (�±� "Hero"�� ������ ������Ʈ)

    [Header("�б�� ��")]
    public Vector3 pushForce = new Vector3(0.1f, 0f, 0f);

    [Header("������ �˾� ����")]
    [Tooltip("������ �˾����� ����� Prefab")]
    public GameObject damagePopupPrefab;

    [Tooltip("�˾��� ��� ������ (���� ��ġ + this offset)")]
    public Vector3 popupOffset = new Vector3(0f, 1f, 0f);

    // ���� ������Ʈ
    private Animator animator;
    private CapsuleCollider2D col;
    private Rigidbody2D rb;

    // ���� & �и� ���¸� ���� �÷���
    [SerializeField] private bool hasAppliedForwardForce = false; // ���� �ְ������� ������ �о��� ���� �ִ���
    [SerializeField] private bool isPushed = false; // �и��� �ڷ�ƾ�� ���� ������

    // Overlap �˻翡�� ã�Ƴ� ���� ����(��, ��, �Ʒ�, ��) ĳ��
    private ZombieController frontZombie = null;
    private ZombieController backZombie = null;
    private ZombieController belowZombie = null;
    private ZombieController topZombie = null;

    // ���� �� Ÿ�� ��� (Ʈ�� ���)
    private GameObject blockTarget; // Hero(TruckBlock) ����

    //���� ���� - ���� ����
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
        // Ǯ���� ���� ������ ���� �ʱ�ȭ �ڵ�
        CurrentHP = MaxHP;
        currentState = ZombieState.Run;
        hasAppliedForwardForce = false;
        isPushed = false;
        heightLimit = GameObject.FindWithTag("HeightLimit");
        rb.velocity = Vector2.zero;                 // ���� �ӵ� �ʱ�ȭ
        animator.SetBool("IsAttacking", false);     // �ִϸ����� �ʱ�ȭ
    }

    private void OnDisable()
    {
        // Ǯ�� ���ư� ��(Disable) �� ���� �۾��� ������ ���⿡ �߰�
    }

    void Update()
    {
        // ���� ���¿� ���� ������ ��ȯ�� �˻硱�� ����
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
                // Die ���´� ������ �صθ�, ���� �ı��� �� ���� ����
                Die_StateUpdate();
                break;
        }
    }

    private void FixedUpdate()
    {
        // ���� ���¿� ���� ������ ����(�̵�/����/�и� ��)���� ����
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
                // Die ���´� ���� ���� ����
                break;
        }
    }

    /// <summary>
    /// Run ���¿��� �� �����Ӹ��� ������ ��ȯ ���Ρ��� �˻�
    /// - ���� ���̺��� �ö� ������ ���� �õ� ����
    /// </summary>
    void Run_StateUpdate()
    {
        animator.SetBool("IsAttacking", false);

        // �и� ó�� ���̶�� �˻� �ߴ�
        if (isPushed)
            return;

        // 1. Ʈ���� �ٷ� �տ� ������ Attack���� ��ȯ
        if (IsTruckInFront())
        {
            currentState = ZombieState.Attack;
            return;
        }

        // 2. �� ���� �ʹ� ������
        if (IsTooCloseToFrontZombie())
        {
            // 2-1. �� ���� ����, �� ���� ������ ���� ������ �� Jump ���·� ��ȯ
            if (!HasZombieBehind() && !frontZombie.HasZombieOnTop())
            {
                EnterJumpState();
                return;
            }
            // 2-2. �� ��(�� ���� �ְų� �� ���� ���� ���� ������) �� �޸��� ����
            return;
        }

        // 3. �� �� �޸���(�ӵ� ������ Run_PhysicsUpdate���� ó��)
    }

    /// <summary>
    /// Run ���¿��� FixedUpdate ���� ���� ó��
    /// </summary>
    void Run_PhysicsUpdate()
    {
        // 1. �и� ���̸� �޸���/���� ���� ���� ���� ���� ����
        if (isPushed)
            return;

        // 2. �� ���� �ʹ� ������ �޸��� ����
        if (IsTooCloseToFrontZombie())
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        // 3. �޸���: ���� Ʈ�� �������� RunSpeed ��ŭ ���� �ӵ� ����
        rb.velocity = new Vector2(-runSpeed, rb.velocity.y);
    }

    /// <summary>
    /// Run -> Jump ���·� ������ �� ȣ��
    /// </summary>
    void EnterJumpState()
    {
        // Animator�� ��IsAttacking�� false(���� ���� �ƴ�) ����
        animator.SetBool("IsAttacking", false);

        // Jump ���·� ��ȯ
        currentState = ZombieState.Jump;

        // ���� ���� �÷��� �ʱ�ȭ
        hasAppliedForwardForce = false;

        // ���� �ӵ� �ʱ�ȭ �� ���� Impulse �߰�
        rb.velocity = Vector2.zero;
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    /// <summary>
    /// Jump ���¿��� �� �����Ӹ��� ���� ��ȯ ���� �˻�
    /// - ���� ���̺��� �ö� ������ ���� �õ� ��ü�� ����
    /// </summary>
    void Jump_StateUpdate()
    {
        //�и� ó�� ���̶��(Ư�� ��Ȳ���� ���߿����� �и� �� �����Ƿ�) Ż��
        if (isPushed)
            return;

        //���� ������Ʈ�� �پ��ִ� heightlimit ������Ʈ�� ���̸� �����ͼ�, 
        //���� ���� ��ġ Y�� heightlimit �̻��̶�� �� �̻� '����' �õ����� �ʴ´�.
        if (heightLimit != null && transform.position.y >= heightLimit.transform.position.y)
        {
            // ���� ���� ���������Ƿ�, ������ ���� ������ �ǳʶٰ� Run_PhysicsUpdate()������ �Ѿ�� �޸��⸸ ����
            return;
        }

        // 1. ���� �ְ���(�ӵ� y�� 0���� ������ �ٲ� ��)���� �� ���� ������ �о���
        if (rb.velocity.y < 0f && !hasAppliedForwardForce)
        {
            // �� ���� ���ٸ� �� ������ �о���� ��
            if (!IsTooCloseToFrontZombie())
            {
                rb.AddForce(Vector2.left * forwardForce, ForceMode2D.Impulse);
            }
            hasAppliedForwardForce = true;
        }

        // 2. ���� �Ǵ�: ���� ��Ұų� �Ʒ��� ���� �ִ� ��� �� Run ���·� ��ȯ
        if (IsGrounded() || HasZombieBelow())
        {
            currentState = ZombieState.Run;
        }
    }

    /// <summary>
    /// Jump ���¿��� FixedUpdate �� ���� ���� ó��
    /// </summary>
    void Jump_PhysicsUpdate()
    {
        // �и� ���̸� ���� ���� ���� ���� Ż�� (�и� �ڷ�ƾ�� ���� ��ġ�� �Ű���)
        if (isPushed)
            return;

        // ���� �߿��� ���� �޸��� �ӵ� ��� ���� �������� ó���� �ӵ�(impulse + �߷�)�� ����
    }

    /// <summary>
    /// Attack ���¿��� �� �����Ӹ��� ���� ��ȯ ���θ� �˻�
    /// - Ʈ���� �� �̻� �տ� ������ Run���� ����
    /// - �Ʒ� ���� �з������� �о�� ȣ��
    /// </summary>
    void Attack_StateUpdate()
    {
        animator.SetBool("IsAttacking", true);

        // 1. Ʈ���� �� �̻� �տ� ���ٸ� Run ���·� ����
        if (!IsTruckInFront())
        {
            currentState = ZombieState.Run;
            return;
        }

        // 2. �Ʒ� ���� �з��´ٸ�, �о�� ���� ȣ��
        if (HasZombieBelow())
        {
            // �Ʒ� ���� ���� Attack ���̶�� �о�⸦ ����
            if (belowZombie.currentState == ZombieState.Attack)
            {
                belowZombie.PushFromFront(pushForce);
            }
        }
    }

    /// <summary>
    /// Attack ���¿��� �ʿ��� ���� ����(����� Ư���� ����)
    /// </summary>
    void Attack_PhysicsUpdate()
    {
        // ���� Ư���� �ʿ����� �����Ƿ� �����
    }

    /// <summary>
    /// Die ���� ���� �� �� ���� ȣ���Ͽ� ������Ʈ �ı�
    /// </summary>
    void Die_StateUpdate()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// �ִϸ����� �̺�Ʈ���� ȣ��: ���� ���� ������ ȣ��
    /// Ʈ�� ���(TruckBlock)�� �ִٸ� ������ ����
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
    /// �ܺ� ȣ��: �������� ���� �� ȣ��
    /// hp�� 0 ���Ϸ� �������� Die ���·� ��ȯ
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
    /// �ܺ� ȣ��: �տ������� �и��� ���� ���� �� ȣ��
    /// SmoothPush �ڷ�ƾ�� ���� �ε巴�� �и�
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

        yield return new WaitForSeconds(0.05f); // �и� �� ��� ��ٸ�

        Collider2D hit = Physics2D.OverlapBox(behindCheck.position, checkBoxSize, 0f, LayerMask.GetMask("Zombie" + spawnerID));
        if (hit != null)
        {
            var next = hit.GetComponent<ZombieController>();
            // ���� spawnerID �̰� ���� �и��� ���� �ƴϸ� ��� ȣ��
            if (next != null && next.spawnerID == this.spawnerID && !next.isPushed)
            {
                // ���ٿ� �� ���ϰ� ���� �Ǵ� �״�� ����
                next.PushFromFront(force * 0.95f);
                //next.PushFromFront(force);
            }
        }

        // ���⼭ �Ʒ��� �߰� �� �ֱ�
        rb.AddForce(Vector2.down * forwardForce, ForceMode2D.Impulse);

        // �и� ��
        yield return new WaitForSeconds(0.05f);
        isPushed = false;
    }

    //���� overlap �˻� �Լ�
    private Collider2D OverlapCheck(Transform checkPoint, Vector2 boxSize, int layerMask, Color debugColor)
    {
        // ������ �ڽ� �׸��� (����׿�)
        DebugDrawBox(checkPoint.position, boxSize, debugColor);
        return Physics2D.OverlapBox(checkPoint.position, boxSize, 0f, layerMask);
    }

    //���� �ٷ� �� �˻�
    bool IsTooCloseToFrontZombie()
    {
        // ���� ���̾� ��: "Zombie" + spawnerID
        int myZombieMask = LayerMask.GetMask("Zombie" + spawnerID);

        // "Zombie" ���̾ ����
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

    //Ʈ��(Hero) �ٷ� �� �˻�
    bool IsTruckInFront()
    {
        int heroMask = LayerMask.GetMask("Hero");
        Collider2D hit = OverlapCheck(frontCheck, frontCheckBoxSize, heroMask, Color.red);

        if (hit != null)
        {
            GameObject hitObj = hit.gameObject;

            // ����� ����
            if (hitObj.CompareTag("Zombie") && hitObj != gameObject)
            {
                blockTarget = null;
                return false;
            }

            // Ʈ���̸� true
            if (hitObj.CompareTag("Hero"))
            {
                blockTarget = hitObj;
                return true;
            }
        }

        // ��Ÿ�� ����
        blockTarget = null;
        return false;
    }

    // ���� �ٷ� �� �˻�
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

    //�Ӹ� �� ���� �˻�
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

    //�� �� ���� �˻�
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

    //�� ���� ���� �˻�
    public bool IsGrounded()
    {
        // ���� ���̾� ��: "Ground" + spawnerID
        int myGroundMask = LayerMask.GetMask("Ground" + spawnerID);

        Collider2D hit = OverlapCheck(bottomCheck, checkBoxSize, myGroundMask, Color.red);
        return hit != null;
    }

    //����׿� �ڽ� �׸���
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

