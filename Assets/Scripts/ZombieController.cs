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
    //�򰥸��� ���� Truck �±״� ��ǥ ��ġ �����ͼ� �̵��ϴµ� ���
    //������ ���� �ڽ����� Hero �±� �ٿ����� Ȱ��

    [Header("�⺻ ����")]
    public ZombieState currentState = ZombieState.Run;
    public int spawnerID; //0,1,2 -> ���� �����ʿ��� ���� ���񳢸��� ID �񱳷� ����
    public float runSpeed = 2f;
    public float jumpForce = 5f;
    public float forwardForce = 5f; // ���� �� ������ �о��� ��
    public int hp = 3;
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

    // ���� ������Ʈ ����
    private Animator animator;
    private CapsuleCollider2D col;
    private Rigidbody2D rb;

    // ���� & �и� ���¸� ���� �÷���
    [SerializeField] private bool isJumping = false; // ���� ���� �ִϸ��̼� ������
    [SerializeField] private bool hasAppliedForwardForce = false; // ���� �ְ������� ������ �о��� ���� �ִ���
    [SerializeField] private bool isPushed = false; // �и��� �ڷ�ƾ�� ���� ������

    // Overlap �˻翡�� ã�Ƴ� ���� ����(��, ��, �Ʒ�, ��) ĳ�̿�
    private ZombieController frontZombie = null;
    private ZombieController backZombie = null;
    private ZombieController belowZombie = null;
    private ZombieController topZombie = null;

    // ���� �� Ÿ�� ��� (Ʈ�� ���)
    private GameObject blockTarget; // Hero(TruckBlock) ����

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
                // ���� �ְ��� ����
                if (!IsTooCloseToFrontZombie()) // �տ� �ƹ��� ���ٸ�
                {
                    rb.AddForce(Vector2.left * forwardForce, ForceMode2D.Impulse); // �������� �̵� (Ʈ�� ����)
                }

                hasAppliedForwardForce = true;
            }

            if (isGrounded() || HasZombieBelow())
            {
                isJumping = false;
            }
        }
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
        // "Zombie" ���̾ ����
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

    //Ʈ��(Hero) �ٷ� �� �˻�
    bool IsTruckInFront()
    {
        Collider2D hit = OverlapCheck(frontCheck, frontCheckBoxSize, LayerMask.GetMask("Hero"), Color.red);

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
    bool hasZombieBehind()
    {
        Collider2D hit = OverlapCheck(behindCheck, checkBoxSize, LayerMask.GetMask("Zombie"), Color.magenta);

        // ����� �ð�ȭ
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

    //�Ӹ� �� ���� �˻�
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

    //�� �� ���� �˻�
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

    //�� ���� ���� �˻�
    public bool isGrounded()
    {
        Collider2D hit = OverlapCheck(bottomCheck, checkBoxSize, LayerMask.GetMask("Ground"), Color.red);
        return hit != null;
    }

    void Run() //�⺻ ������
    {
        animator.SetBool("IsAttacking", false);

        // ���� ���� ���̰ų� �и��� ���̸� Run ���� �������� ����
        if (isJumping || isPushed) return;

        // Ʈ���� �տ� ������ �ٷ� ���� ���� ��ȯ
        if (IsTruckInFront())
        {
            currentState = ZombieState.Attack;
            return;
        }

        //�� ���� �ʹ� ������ �پ����� Jump ���� �õ�
        //(��, ������ ����, �� ���� �Ӹ� ������ ���� ����� ��)
        if (IsTooCloseToFrontZombie() && !hasZombieBehind() && !frontZombie.HasZombieOnTop())
        {
            StartJump();
            return;            
        }

        //�� ���� �پ� ������ ���߱�
        if (IsTooCloseToFrontZombie())
        {
            rb.velocity = new Vector2(0f, rb.velocity.y); // �̵� ����
            return;
        }

        //�� �� ����(Ʈ�� ����)���� �޸���
        rb.velocity = new Vector2(-runSpeed, rb.velocity.y); // ����(Ʈ�� ����) �̵�
    }

    void Attack()
    {
        animator.SetBool("IsAttacking", true); //�ִϸ��̼� �̺�Ʈ OnAttack

        // Ʈ�� ������ �� �Ǹ� �ٽ� Run ���·� ��ȯ
        if (!IsTruckInFront())
        {
            currentState = ZombieState.Run;
            return;
        }

        // ���� �� �Ʒ� ���� �˻�
        if (HasZombieBelow())
        {
            // ���� ���̶�� �о
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

        rb.velocity = Vector2.zero; // �ӵ� �ʱ�ȭ
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

        yield return new WaitForSeconds(0.05f); // �и� �� ��� ��ٸ�

        Collider2D hit = Physics2D.OverlapBox(behindCheck.position, checkBoxSize, 0f, LayerMask.GetMask("Zombie"));
        if (hit != null)
        {
            var next = hit.GetComponent<ZombieController>();
            // ���� spawnerID �̰� ���� �и��� ���� �ƴϸ� ��� ȣ��
            if (next != null && next.spawnerID == this.spawnerID && !next.isPushed)
            {
                // ���ٿ� �� ���ϰ� ���� �Ǵ� �״�� ����
                next.PushFromFront(force * 0.9f);
                //next.PushFromFront(force);
            }
        }

        // �� ���⼭ �Ʒ��� �߰� �� �ֱ� ��
        rb.AddForce(Vector2.down * forwardForce * 0.5f, ForceMode2D.Impulse);

        // �и� ��
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

