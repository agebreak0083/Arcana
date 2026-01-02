using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemySquadSpawner : MonoBehaviour
{
    public GameObject rootObject;
    public GameObject enemySquadPrefab;
    public Vector2 spawnAngleRange = new Vector2(-180, -90);
    public float spawnInterval = 10f;
    public float spawnDistance = 10f;

    private float _remainingTime = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 코루틴은 안됨. Active상태에서만 실행되도록 해야 함.
        _remainingTime = spawnInterval;
    }

    void Update()
    {
        if(BattleMapManager.Instance.currentPhase == BattleMapPhase.BATTLE_DEFEAT || 
           BattleMapManager.Instance.IsPause())
        {
            return; 
        }

        _remainingTime -= Time.deltaTime;
        if(_remainingTime <= 0f)
        {
            _remainingTime = spawnInterval;
            GameObject enemySquad = Instantiate(enemySquadPrefab, rootObject.transform);                    

            // Tower 에서 일정 각도 이내에 정해진 거리만큰 떨어진 위치에 랜덤 생성 
            Vector3 spawnDirection = new Vector3(1, 0, 0);
            spawnDirection = Quaternion.Euler(0, Random.Range(spawnAngleRange.x, spawnAngleRange.y), 0) * spawnDirection;
            Vector3 spawnPosition = transform.position + spawnDirection * spawnDistance;
            
            // NavMesh 위에 있는 위치로 조정 (NavMeshAgent 텔레포트 방지)
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas))
            {
                spawnPosition = hit.position;
            }
            
            enemySquad.transform.position = spawnPosition;
        }
    }
}
