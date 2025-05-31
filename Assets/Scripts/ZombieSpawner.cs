using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [Header("���� ����")]
    [Tooltip("Spawner ���� ID (�ν����Ϳ��� �Ҵ�)")]
    public int thisSpawnerID; // �ν����Ϳ��� �Ҵ�

    [Tooltip("���� ���� ����(��)")]
    public float spawnInterval = 1.0f;

    [Tooltip("������ ������ ���� Y ��ǥ")]
    public float fixedY = 0f;

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private void SpawnZombie()
    {
        // Ǯ �Ŵ����� ���� "thisSpawnerID"�� �ش��ϴ� ���� ���� Ȱ��ȭ
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

