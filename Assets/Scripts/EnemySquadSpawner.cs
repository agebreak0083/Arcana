using System.Collections;
using UnityEngine;

public class EnemySquadSpawner : MonoBehaviour
{
    public GameObject rootObject;
    public GameObject enemySquadPrefab;
    public Vector2 spawnAngleRange = new Vector2(-180, -90);
    public float spawnInterval = 10f;
    public float spawnDistance = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnEnemySquad());
    }

    private IEnumerator SpawnEnemySquad()
    {
        while(true)
        {
            yield return new WaitForSeconds(spawnInterval);
            GameObject enemySquad = Instantiate(enemySquadPrefab, rootObject.transform);                    
            
            // Tower 에서 일정 각도 이내에 정해진 거리만큰 떨어진 위치에 랜덤 생성 
            Vector3 spawnDirection = new Vector3(1, 0, 0);
            spawnDirection = Quaternion.Euler(0, Random.Range(spawnAngleRange.x, spawnAngleRange.y), 0) * spawnDirection;
            Vector3 spawnPosition = transform.position + spawnDirection * spawnDistance;
            enemySquad.transform.position = spawnPosition;

            if(BattleMapManager.Instance.currentPhase == BattleMapPhase.BATTLE_DEFEAT)
            {
                break;
            }
        }
    }
}
