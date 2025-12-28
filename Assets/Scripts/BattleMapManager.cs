using System;
using System.Collections;
using System.Collections.Generic;
using Arcana.Tactics;
using Arcana.Tactics.Data;
using Arcana.Tactics.UI;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public BattleSimulationResultUI battleSimulationResultUI = null;
    public BattleManager battleManager = null;
    
    [Header("Movement Settings")]
    public float squadMoveSpeed = 5f;
    public LayerMask mapLayerMask = 1; // Default layer
    
    public Camera mainCamera;
    public BattleMapPhase currentPhase = BattleMapPhase.NONE_PHASE;

    public static BattleMapManager Instance { get; private set; }
    public int currentSquadIndex { get; internal set; } = 0;

    private Dictionary<string, CharacterData> _squadCharacterData = new Dictionary<string, CharacterData>();
    public SquadInfoUI _playerSquadInfoUI = null;
    public SquadInfoUI _enemySquadInfoUI = null;

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
        // ESC 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeKey();
        }

        // 맵(Plane) 클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            HandleMapClick();
        }
    }

    /// <summary>
    /// ESC 키 처리 - 타이틀로 돌아가기 팝업 표시
    /// </summary>
    void HandleEscapeKey()
    {
        // TacticsScene이 Active 상태면 무시 
        if (TacticsUIManager.Instance != null && TacticsUIManager.Instance.rootObject.activeSelf)
        {
            return;
        }

        // 팝업이 이미 표시되어 있는지 확인
        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.ShowPopup(
                PopupManager.PopupType.Confirm,
                "타이틀로 돌아가시겠습니까?",
                () => {
                    // 확인 버튼 클릭 시 IntroScene으로 이동
                    SceneManager.LoadScene("IntroScene");
                }
            );
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

        // UI 위에서 클릭했는지 확인 (EventSystem 사용)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // BattleSimulationResultUI가 활성화되어 있으면 무시 (추가 안전장치)
        if (battleSimulationResultUI != null && battleSimulationResultUI.gameObject.activeSelf)
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

    public void ShowTacticsScene(string squadName, string enemyName = null)
    {
        // 씬이 이미 로드되어 있는지 확인
        Scene tacticsScene = SceneManager.GetSceneByName("TacticsScene");
        
        if (!tacticsScene.isLoaded)
        {
            // Additive 모드로 씬 추가 (현재 씬 유지)
            // 로드가 완료되면, 추가된 씬에 있는 Camera를 비활성화 
            SceneManager.LoadSceneAsync("TacticsScene", LoadSceneMode.Additive).completed += (operation) => {
                battleMapRootObject.SetActive(false);
                // 씬 로드 완료 후 squadName 전달
                if (TacticsUIManager.Instance != null)
                {
                    // Start() 코루틴이 실행되기 전에 squadName을 설정
                    TacticsUIManager.Instance.SetSquadName(squadName);
                    TacticsUIManager.Instance.SetEnemyName(enemyName);
                }
            };
        }
        else
        {
            TacticsUIManager.Instance.rootObject.SetActive(true);
            // squadName 전달
            TacticsUIManager.Instance.SetSquadName(squadName);
            TacticsUIManager.Instance.SetEnemyName(enemyName);
            // 이미 로드된 경우 바로 LoadPlayerFormation 호출
            TacticsUIManager.Instance.LoadPlayerFormation(squadName);
            TacticsUIManager.Instance.SetBattleMapPhaseUI(currentPhase);
            
            battleMapRootObject.SetActive(false);
        }        
    }

    private SquadSpawner _squadSpawner = null;
    public void CreateBattleSquad(string squadName, CharacterData[] unitSlots)
    {
        if(_squadSpawner == null)
        {
            _squadSpawner = GetComponent<SquadSpawner>();
        }

        bool isEmpySquad = true;

        // 출격한 캐릭터를 체크하기 위해 저장해 둔다. 
        foreach(var unitSlot in unitSlots)
        {
            if(unitSlot != null)
            {
                _squadCharacterData[unitSlot.characterName] = unitSlot;
                isEmpySquad = false;
            }
        }

        // 슬롯에 캐릭터가 없는 경우는 스쿼드를 생성하지 않는다. 
        if(!isEmpySquad && _squadSpawner != null)
        {
            BattleSquad battleSquad = _squadSpawner.SpawnSquad(squadName);            
            battleSquad._loadResult = TacticsDataManager.Instance.LoadSquadTactics(squadName);
        }
    }

    public bool IsSquadCharacter(string characterName)
    {
        return _squadCharacterData.ContainsKey(characterName);
    }

    public void HandleSquadClick(BattleSquad battleSquad)
    {
        // 선택 해제
        if(battleSquad == null) 
        {
            if(selectedSquadObject != null)
            {
                selectedSquadObject.GetComponent<BattleSquad>().SetSelected(false);
                selectedSquadObject = null;
            }
            _playerSquadInfoUI.gameObject.SetActive(false);
            _enemySquadInfoUI.gameObject.SetActive(false);
            return;
        }

        // Player Squad가 선택된 상태에서 Enemy Squad를 클릭하면 이동
        if (selectedSquadObject != null && selectedSquadObject != battleSquad.gameObject)
        {
            BattleSquad selectedSquad = selectedSquadObject.GetComponent<BattleSquad>();
            if (selectedSquad != null && selectedSquad.isPlayerSquad && !battleSquad.isPlayerSquad)
            {
                StartCoroutine(OnEnemySquadSelected(selectedSquad, battleSquad));
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

        ShowSquadInfoUI(battleSquad);
    }

    private IEnumerator OnEnemySquadSelected(BattleSquad selectedSquad, BattleSquad battleSquad)
    {
        battleSimulationResultUI.gameObject.SetActive(true);
        

        // 플레이어 편성은 현재 편성 저장. Enemy를 설정된 적 편성으로 설정.
        string playerSquadName = selectedSquad.gameObject.name;
        string enemySquadName = battleSquad.gameObject.name;
        TacticsDataManager.Instance.SetPlayerTactics(playerSquadName);
        TacticsDataManager.Instance.SetEnemyTactics(enemySquadName);        

        // 시뮬레이션 모드 스타트 - 완료될 때까지 대기        \        
        battleManager.SetInstanceSelf(); // 임시 코드 : BattleManager의 Instance를 battleManager로 설정
        yield return StartCoroutine(BattleManager.Instance.SimulationModeStart(playerSquadName, enemySquadName));

        battleSimulationResultUI.UpdateUI();         
        battleSimulationResultUI.startBattleButton.onClick.RemoveAllListeners();
        battleSimulationResultUI.startBattleButton.onClick.AddListener(() => 
        {
            battleSimulationResultUI.gameObject.SetActive(false);

            // Player Squad를 Enemy Squad 위치로 이동
            Vector3 targetPosition = battleSquad.transform.position;
            targetPosition.y = selectedSquad.transform.position.y; // Y축은 유지

            selectedSquad.MoveTo(targetPosition, squadMoveSpeed);
            Debug.Log($"Player Squad 이동 명령: {selectedSquad.gameObject.name} -> {battleSquad.gameObject.name} 위치로");
        });
    }

    private void ShowSquadInfoUI(BattleSquad battleSquad)
    {
        SquadInfoUI squadInfoUI = battleSquad.isPlayerSquad ? _playerSquadInfoUI : _enemySquadInfoUI;
        squadInfoUI.gameObject.SetActive(true);

        // SquadInfoUI에 정보 업데이트 
        if (squadInfoUI != null)
        {
            string squadName = battleSquad.gameObject.name;

            if (battleSquad._loadResult != null && battleSquad._loadResult.unitSlots != null)
            {
                squadInfoUI.UpdateSquadInfo(squadName, battleSquad._loadResult.unitSlots);
            }
        }
    }

    private bool _isPlayerWin = false;
    public void SetPlayerWinLose(bool isPlayerWin)
    {
        _isPlayerWin = isPlayerWin;
    }

    public void ChangeCurrentPhase(BattleMapPhase battlePhase, string squadName = null, string enemyName = null)
    {
        currentPhase = battlePhase;

        if(currentPhase == BattleMapPhase.BATTLE_PHASE)
        {
            ShowTacticsScene(squadName, enemyName);            
        }
        else if(currentPhase == BattleMapPhase.TOWER_PHASE)
        {
            ShowTacticsScene(null);
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
            else // 플레이어 패배 
            {
                // 플레이어 스쿼드를 회수한다.
                ReturnSquad(_playerSquad.gameObject.name);
                Destroy(_playerSquad.gameObject);
                HandleSquadClick(null);

                if(_enemySquad != null)
                    _enemySquad.SetTriggerEnabled(true);
            }
        }
    }

    // 스쿼드를 회수 한다.
    public void ReturnSquad(string squadName)
    {
        // 캐릭터들을 스쿼드 멤버에서 제거한다.
        var loadResult = TacticsDataManager.Instance.LoadSquadTactics(squadName);
        if(loadResult != null && loadResult.unitSlots != null)
        {
            foreach(var unitSlot in loadResult.unitSlots)
            {
                if(unitSlot != null)
                {
                    _squadCharacterData.Remove(unitSlot.characterName);
                }
            }
        }

        // GameObject를 제거한다.
        if(_squadSpawner != null)
        {
            _squadSpawner.DestroySquad(squadName);
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
