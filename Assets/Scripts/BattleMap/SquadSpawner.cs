using System.Collections.Generic;
using UnityEngine;

public class SquadSpawner : MonoBehaviour
{
    public Transform spawnPoint;
    public Vector2 spawnAngleRange = new Vector2(0, 90);
    public float spawnDistance = 5f;
    public GameObject squadPrefab;
    public List<Texture2D> squadTextures;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    int squadIndex = 0;
    public void SpawnSquad(string squadName)
    {
        GameObject squad = Instantiate(squadPrefab, BattleMapManager.Instance.battleMapRootObject.transform);
        squad.name = squadName;
        
        // spawnPoint 위치에서 SpawnAngleRange 범위 내에서 랜덤한 방향으로 이동하며, SpawnDistance 만큼 이동한다.
        Vector3 spawnDirection = new Vector3(0f, 0f, 1f);
        spawnDirection = Quaternion.Euler(0, Random.Range(spawnAngleRange.x, spawnAngleRange.y), 0) * spawnDirection;
        squad.transform.position = spawnPoint.position + spawnDirection * spawnDistance;
        
        squad.GetComponent<Renderer>().material.mainTexture = squadTextures[squadIndex];

        squadIndex++;
        if(squadIndex >= squadTextures.Count)
        {
            squadIndex = 0;
        }
    }
}
