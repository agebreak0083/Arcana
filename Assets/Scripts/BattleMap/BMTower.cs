using Arcana.Tactics.UI;
using UnityEngine;

public class BMTower : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnMouseDown()
    {
        BattleMapManager.Instance.HandleTowerClick();
    }
}
