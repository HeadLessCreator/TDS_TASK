using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [Tooltip("Spawner 고유 ID (인스펙터에서 할당)")]
    public int thisSpawnerID; // 인스펙터에서 할당

    [Tooltip("좀비 스폰 간격(초)")]
    public float spawnInterval = 1.0f;

    [Tooltip("스폰될 좀비의 고정 Y 좌표")]
    public float fixedY = 0f;

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private void SpawnZombie()
    {
        // 풀 매니저를 통해 "thisSpawnerID"에 해당하는 좀비를 꺼내 활성화
        Vector3 spawnPos = new Vector3(transform.position.x, fixedY, transform.position.z);
        ZombiePoolManager.Instance.SpawnZombieAt(spawnPos, thisSpawnerID);
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnZombie();
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}

