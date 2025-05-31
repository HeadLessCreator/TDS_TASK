using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 좀비 풀링(Pooling) 매니저
/// - ID별로 서로 다른 좀비 Prefab과 풀(List<GameObject>)을 관리
/// - Spawn 요청 시, spawnerID의 풀에서 비활성 상태의 좀비(GameObject)를 꺼내와 활성화
/// - 풀 부족 시, 해당 프리팹으로 poolExpandSize 만큼 추가 생성
/// </summary>
public class ZombiePoolManager : MonoBehaviour
{
    public static ZombiePoolManager Instance { get; private set; }

    [Header("풀링에 사용할 좀비 Prefab들 (ID 순서대로)")]
    [Tooltip("예: 0번 인덱스에 Zombie0 Prefab, 1번 인덱스에 Zombie1 Prefab, 2번 인덱스에 Zombie2 Prefab.")]
    public GameObject[] zombiePrefabs;

    [Tooltip("각 ID별로 풀에 미리 생성해 둘 좀비 개수")]
    public int initialPoolSize = 100;

    [Tooltip("풀 부족 시 한 번에 더 생성할 개수")]
    public int poolExpandSize = 5;

    // ID별 풀 리스트 (zombiePrefabs.Length 크기)
    private List<GameObject>[] pools;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // zombiePrefabs[] 길이만큼 풀 리스트 배열 초기화
        int numIDs = zombiePrefabs.Length;
        pools = new List<GameObject>[numIDs];
        for (int i = 0; i < numIDs; i++)
        {
            pools[i] = new List<GameObject>();
        }

        // 각 ID별로 initialPoolSize만큼 프리팹을 Instantiate 후 비활성화하여 풀에 등록
        for (int id = 0; id < numIDs; id++)
        {
            for (int j = 0; j < initialPoolSize; j++)
            {
                CreateNewPooledZombie(id);
            }
        }
    }

    /// <summary>
    /// 특정 ID의 풀에 새로운 좀비(GameObject)를 생성하여 비활성화 후 리스트에 추가
    /// </summary>
    private GameObject CreateNewPooledZombie(int id)
    {
        if (id < 0 || id >= zombiePrefabs.Length)
        {
            Debug.LogError($"[ZombiePoolManager] CreateNewPooledZombie: 유효하지 않은 ID {id}");
            return null;
        }

        // id에 해당하는 prefab을 Instantiate
        GameObject prefab = zombiePrefabs[id];
        if (prefab == null)
        {
            Debug.LogError($"[ZombiePoolManager] CreateNewPooledZombie: zombiePrefabs[{id}]가 비어있습니다.");
            return null;
        }

        // PoolManager 아래에 위치시키기 (Hierarchy 정리용)
        GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        go.SetActive(false);

        // 풀에 추가
        pools[id].Add(go);
        return go;
    }

    /// <summary>
    /// ID별 풀에서 비활성화된 좀비를 찾아 반환
    /// 만약 모두 활성 상태이면, poolExpandSize 만큼 더 생성 후 다시 탐색
    /// </summary>
    private GameObject GetPooledZombie(int id)
    {
        if (id < 0 || id >= pools.Length)
        {
            Debug.LogError($"[ZombiePoolManager] GetPooledZombie: 유효하지 않은 ID {id}");
            return null;
        }

        // 비활성화된(GameObject.activeInHierarchy == false) 좀비를 찾아 반환
        List<GameObject> list = pools[id];
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].activeInHierarchy)
                return list[i];
        }

        // 모두 활성 상태라면, 풀 확장
        for (int j = 0; j < poolExpandSize; j++)
        {
            CreateNewPooledZombie(id);
        }

        // 다시 탐색해서 비활성 좀비 반환
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].activeInHierarchy)
                return list[i];
        }

        // 그래도 없으면 null
        return null;
    }

    /// <summary>
    /// 외부에서 호출:
    /// - position: 월드 스폰 좌표
    /// - spawnerID: 어느 ID(줄)에 해당하는 좀비를 꺼내 활성화할 것인지
    /// - (반환값) 활성화한 좀비 GameObject, 실패 시 null
    /// </summary>
    public GameObject SpawnZombieAt(Vector3 position, int spawnerID)
    {
        // ID 유효성 검사
        if (spawnerID < 0 || spawnerID >= zombiePrefabs.Length)
        {
            Debug.LogError($"[ZombiePoolManager] SpawnZombieAt: 유효하지 않은 spawnerID {spawnerID}");
            return null;
        }

        // 해당 ID 풀에서 비활성 좀비 꺼내기
        GameObject zombie = GetPooledZombie(spawnerID);
        if (zombie == null)
        {
            Debug.LogError($"[ZombiePoolManager] SpawnZombieAt: 더 이상 풀에서 꺼낼 좀비가 없습니다. spawnerID={spawnerID}");
            return null;
        }

        // 위치/회전 설정
        zombie.transform.position = position;
        zombie.transform.rotation = Quaternion.identity;

        // 해당 ZombieController에 spawnerID 할당
        ZombieController zc = zombie.GetComponent<ZombieController>();
        if (zc != null)
        {
            zc.spawnerID = spawnerID;
            // 아래 코드를 추가하면, 씬에서 인스펙터 레이어 설정을 자동으로 맞출 수 있다면 옵션:
            // zombie.layer = LayerMask.NameToLayer("Zombie" + spawnerID);
            // (※ 이 부분은 이미 Prefab → Layer를 프리팹에서 “Zombie{id}” 로 맞춰 두었다면 필요 없음)
        }

        // 활성화 → OnEnable()에서 초기화 로직 실행
        zombie.SetActive(true);

        return zombie;
    }
}
