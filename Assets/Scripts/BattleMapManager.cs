using System;
using Arcana.Tactics.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BattleMapPhase
{
    NONE_PHASE,
    TOWER_PHASE,
    BATTLE_PHASE,
    END_PHASE,
}

public class BattleMapManager : MonoBehaviour
{
    public GameObject selectedSquadObject = null;
    public GameObject mapObject = null;        
    public GameObject battleMapRootObject = null;
    
    [Header("Movement Settings")]
    public float squadMoveSpeed = 5f;
    public LayerMask mapLayerMask = 1; // Default layer
    
    public Camera mainCamera;
    public BattleMapPhase currentPhase = BattleMapPhase.NONE_PHASE;

    public static BattleMapManager Instance { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }               

    }

    // Update is called once per frame
    void Update()
    {
        // 맵(Plane) 클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            HandleMapClick();
        }
    }

    /// <summary>
    /// 맵 클릭 처리
    /// </summary>
    void HandleMapClick()
    {
        // Squad가 선택되어 있지 않으면 무시
        if (selectedSquadObject == null)
        {
            return;
        }

        // TacticsScene이 Active 상태면 무시 
        if (TacticsUIManager.Instance != null && TacticsUIManager.Instance.rootObject.activeSelf)
        {
            return;
        }

        // 마우스 위치에서 Raycast
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 맵 레이어와 충돌하는지 확인
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, mapLayerMask))
        {
            // 맵 오브젝트인지 확인
            if (mapObject != null && hit.collider.gameObject == mapObject)
            {
                // 선택된 Squad를 클릭한 위치로 이동
                Vector3 targetPosition = hit.point;
                targetPosition.y = selectedSquadObject.transform.position.y; // Y축은 유지
                
                BattleSquad squad = selectedSquadObject.GetComponent<BattleSquad>();
                if (squad != null)
                {
                    squad.MoveTo(targetPosition, squadMoveSpeed);
                    //Debug.Log($"Squad 이동 명령: {targetPosition}");
                }
            }
        }
    }

    public void HandleTowerClick()
    {
        ChangeCurrentPhase(BattleMapPhase.TOWER_PHASE);
    }

    public void ShowTacticsScene()
    {
        // 씬이 이미 로드되어 있는지 확인
        Scene tacticsScene = SceneManager.GetSceneByName("TacticsScene");
        
        if (!tacticsScene.isLoaded)
        {
            // Additive 모드로 씬 추가 (현재 씬 유지)
            // 로드가 완료되면, 추가된 씬에 있는 Camera를 비활성화 
            SceneManager.LoadSceneAsync("TacticsScene", LoadSceneMode.Additive).completed += (operation) => {
                battleMapRootObject.SetActive(false);
            };
        }
        else
        {
            battleMapRootObject.SetActive(false);
            TacticsUIManager.Instance.rootObject.SetActive(true);
            StartCoroutine(TacticsUIManager.Instance.Start());
        }        
    }

    public void CreateBattleSquad(string squadName)
    {
        
    }

    public void HandleSquadClick(BattleSquad battleSquad)
    {
        // Player Squad가 선택된 상태에서 Enemy Squad를 클릭하면 이동
        if (selectedSquadObject != null && selectedSquadObject != battleSquad.gameObject)
        {
            BattleSquad selectedSquad = selectedSquadObject.GetComponent<BattleSquad>();
            if (selectedSquad != null && selectedSquad.isPlayerSquad && !battleSquad.isPlayerSquad)
            {
                // Player Squad를 Enemy Squad 위치로 이동
                Vector3 targetPosition = battleSquad.transform.position;
                targetPosition.y = selectedSquad.transform.position.y; // Y축은 유지
                
                selectedSquad.MoveTo(targetPosition, squadMoveSpeed);
                Debug.Log($"Player Squad 이동 명령: {selectedSquad.gameObject.name} -> {battleSquad.gameObject.name} 위치로");
                
                // 선택은 유지 (Player Squad가 계속 선택된 상태)
                return;
            }
        }
        
        // 일반적인 선택 로직
        // 이전 선택 해제
        if (selectedSquadObject != null && selectedSquadObject != battleSquad.gameObject)
        {
            BattleSquad prevSquad = selectedSquadObject.GetComponent<BattleSquad>();
            if (prevSquad != null)
            {
                prevSquad.SetSelected(false);
            }
        }
        
        // 현재 Squad 선택
        selectedSquadObject = battleSquad.gameObject;
        battleSquad.SetSelected(true);
        Debug.Log($"Squad 선택: {battleSquad.gameObject.name}");
    }

    private bool _isPlayerWin = false;
    public void SetPlayerWinLose(bool isPlayerWin)
    {
        _isPlayerWin = isPlayerWin;
    }

    public void ChangeCurrentPhase(BattleMapPhase battlePhase)
    {
        currentPhase = battlePhase;

        if(currentPhase == BattleMapPhase.BATTLE_PHASE)
        {
            ShowTacticsScene();
            // SetPlayerWinLose(true);
            // ChangeCurrentPhase(BattleMapPhase.END_PHASE);
        }
        else if(currentPhase == BattleMapPhase.TOWER_PHASE)
        {
            ShowTacticsScene();
        }
        else if(currentPhase == BattleMapPhase.END_PHASE)
        {
            // BattleScene이 로드되어 있는지 확인 후 언로드
            Scene battleScene = SceneManager.GetSceneByName("BattleScene");
            if (battleScene.IsValid() && battleScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync("BattleScene").completed += (operation) => {                    
                };
            }

            // TacticsScene이 로드되어 있는지 확인 후 언로드
            Scene tacticsScene = SceneManager.GetSceneByName("TacticsScene");
            if (tacticsScene.IsValid() && tacticsScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync("TacticsScene");
            }

            battleMapRootObject.SetActive(true);
            if(_isPlayerWin)
            {
                if(_enemySquad != null && _enemySquad.gameObject != null)                
                {
                    if(_enemySquad.gameObject.CompareTag("Tower"))
                    {
                        // 테스트용 인트로씬으로 전환
                        SceneManager.LoadScene("IntroScene");
                    }
                    else
                    {
                        Destroy(_enemySquad.gameObject);
                    }
                }

                if(_playerSquad != null)
                    _playerSquad.SetTriggerEnabled(true);
            }
            else
            {
                if(_playerSquad != null && _playerSquad.gameObject != null)
                    Destroy(_playerSquad.gameObject);
                if(_enemySquad != null)
                    _enemySquad.SetTriggerEnabled(true);
            }
        }
    }

    private BattleSquad _playerSquad = null;
    private BattleSquad _enemySquad = null;
    public void SetBattleSquad(BattleSquad battleSquad, BattleSquad otherSquad)
    {
        _playerSquad = battleSquad;
        _enemySquad = otherSquad;
    }
}
