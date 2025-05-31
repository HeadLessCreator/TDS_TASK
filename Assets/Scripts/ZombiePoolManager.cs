using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���� Ǯ��(Pooling) �Ŵ���
/// - ID���� ���� �ٸ� ���� Prefab�� Ǯ(List<GameObject>)�� ����
/// - Spawn ��û ��, spawnerID�� Ǯ���� ��Ȱ�� ������ ����(GameObject)�� ������ Ȱ��ȭ
/// - Ǯ ���� ��, �ش� ���������� poolExpandSize ��ŭ �߰� ����
/// </summary>
public class ZombiePoolManager : MonoBehaviour
{
    public static ZombiePoolManager Instance { get; private set; }

    [Header("Ǯ���� ����� ���� Prefab�� (ID �������)")]
    [Tooltip("��: 0�� �ε����� Zombie0 Prefab, 1�� �ε����� Zombie1 Prefab, 2�� �ε����� Zombie2 Prefab.")]
    public GameObject[] zombiePrefabs;

    [Tooltip("�� ID���� Ǯ�� �̸� ������ �� ���� ����")]
    public int initialPoolSize = 100;

    [Tooltip("Ǯ ���� �� �� ���� �� ������ ����")]
    public int poolExpandSize = 5;

    // ID�� Ǯ ����Ʈ (zombiePrefabs.Length ũ��)
    private List<GameObject>[] pools;

    private void Awake()
    {
        // �̱��� �ʱ�ȭ
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // zombiePrefabs[] ���̸�ŭ Ǯ ����Ʈ �迭 �ʱ�ȭ
        int numIDs = zombiePrefabs.Length;
        pools = new List<GameObject>[numIDs];
        for (int i = 0; i < numIDs; i++)
        {
            pools[i] = new List<GameObject>();
        }

        // �� ID���� initialPoolSize��ŭ �������� Instantiate �� ��Ȱ��ȭ�Ͽ� Ǯ�� ���
        for (int id = 0; id < numIDs; id++)
        {
            for (int j = 0; j < initialPoolSize; j++)
            {
                CreateNewPooledZombie(id);
            }
        }
    }

    /// <summary>
    /// Ư�� ID�� Ǯ�� ���ο� ����(GameObject)�� �����Ͽ� ��Ȱ��ȭ �� ����Ʈ�� �߰�
    /// </summary>
    private GameObject CreateNewPooledZombie(int id)
    {
        if (id < 0 || id >= zombiePrefabs.Length)
        {
            Debug.LogError($"[ZombiePoolManager] CreateNewPooledZombie: ��ȿ���� ���� ID {id}");
            return null;
        }

        // id�� �ش��ϴ� prefab�� Instantiate
        GameObject prefab = zombiePrefabs[id];
        if (prefab == null)
        {
            Debug.LogError($"[ZombiePoolManager] CreateNewPooledZombie: zombiePrefabs[{id}]�� ����ֽ��ϴ�.");
            return null;
        }

        // PoolManager �Ʒ��� ��ġ��Ű�� (Hierarchy ������)
        GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        go.SetActive(false);

        // Ǯ�� �߰�
        pools[id].Add(go);
        return go;
    }

    /// <summary>
    /// ID�� Ǯ���� ��Ȱ��ȭ�� ���� ã�� ��ȯ
    /// ���� ��� Ȱ�� �����̸�, poolExpandSize ��ŭ �� ���� �� �ٽ� Ž��
    /// </summary>
    private GameObject GetPooledZombie(int id)
    {
        if (id < 0 || id >= pools.Length)
        {
            Debug.LogError($"[ZombiePoolManager] GetPooledZombie: ��ȿ���� ���� ID {id}");
            return null;
        }

        // ��Ȱ��ȭ��(GameObject.activeInHierarchy == false) ���� ã�� ��ȯ
        List<GameObject> list = pools[id];
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].activeInHierarchy)
                return list[i];
        }

        // ��� Ȱ�� ���¶��, Ǯ Ȯ��
        for (int j = 0; j < poolExpandSize; j++)
        {
            CreateNewPooledZombie(id);
        }

        // �ٽ� Ž���ؼ� ��Ȱ�� ���� ��ȯ
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].activeInHierarchy)
                return list[i];
        }

        // �׷��� ������ null
        return null;
    }

    /// <summary>
    /// �ܺο��� ȣ��:
    /// - position: ���� ���� ��ǥ
    /// - spawnerID: ��� ID(��)�� �ش��ϴ� ���� ���� Ȱ��ȭ�� ������
    /// - (��ȯ��) Ȱ��ȭ�� ���� GameObject, ���� �� null
    /// </summary>
    public GameObject SpawnZombieAt(Vector3 position, int spawnerID)
    {
        // ID ��ȿ�� �˻�
        if (spawnerID < 0 || spawnerID >= zombiePrefabs.Length)
        {
            Debug.LogError($"[ZombiePoolManager] SpawnZombieAt: ��ȿ���� ���� spawnerID {spawnerID}");
            return null;
        }

        // �ش� ID Ǯ���� ��Ȱ�� ���� ������
        GameObject zombie = GetPooledZombie(spawnerID);
        if (zombie == null)
        {
            Debug.LogError($"[ZombiePoolManager] SpawnZombieAt: �� �̻� Ǯ���� ���� ���� �����ϴ�. spawnerID={spawnerID}");
            return null;
        }

        // ��ġ/ȸ�� ����
        zombie.transform.position = position;
        zombie.transform.rotation = Quaternion.identity;

        // �ش� ZombieController�� spawnerID �Ҵ�
        ZombieController zc = zombie.GetComponent<ZombieController>();
        if (zc != null)
        {
            zc.spawnerID = spawnerID;
            // �Ʒ� �ڵ带 �߰��ϸ�, ������ �ν����� ���̾� ������ �ڵ����� ���� �� �ִٸ� �ɼ�:
            // zombie.layer = LayerMask.NameToLayer("Zombie" + spawnerID);
            // (�� �� �κ��� �̹� Prefab �� Layer�� �����տ��� ��Zombie{id}�� �� ���� �ξ��ٸ� �ʿ� ����)
        }

        // Ȱ��ȭ �� OnEnable()���� �ʱ�ȭ ���� ����
        zombie.SetActive(true);

        return zombie;
    }
}
