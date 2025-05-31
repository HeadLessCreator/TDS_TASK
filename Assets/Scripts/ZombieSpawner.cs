using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    public GameObject zombiePrefab;
    public int thisSpawnerID; // 인스펙터에서 할당
    public float spawnInterval = 1.0f;
    public float fixedY = 0f; //개별 스포너의y축 설정

    //public int tempMax = 2;
    //public int tempCount = 0;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    void SpawnZombie()
    {
        //if (tempCount < tempMax)
        //{
        //    Vector3 spawnPos = new Vector3(transform.position.x, fixedY, transform.position.z);
        //    GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
        //    zombie.GetComponent<ZombieController>().spawnerID = thisSpawnerID;
        //    tempCount++;
        //}

        Vector3 spawnPos = new Vector3(transform.position.x, fixedY, transform.position.z);
        GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
        zombie.GetComponent<ZombieController>().spawnerID = thisSpawnerID;
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnZombie();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

}

