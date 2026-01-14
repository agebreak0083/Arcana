using System;
using System.Collections;
using System.Collections.Generic;
using Arcana.Tactics;
using Arcana.Tactics.Data;
using Arcana.Tactics.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public enum BattleMapPhase
{
    NONE_PHASE,
    TOWER_PHASE,
    TACTICS_PHASE,
    BATTLE_PHASE,
    END_PHASE,
    BATTLE_DEFEAT,
}

public enum BattleMapPauseType
{
    PAUSE, 
    PLAY     ,
    PLAY_2X,
}

public class BattleMapManager : MonoBehaviour
{
    public GameObject selectedSquadObject = null;
    public GameObject mapObject = null;        
    public GameObject battleMapRootObject = null;
    public BattleSimulationResultUI battleSimulationResultUI = null;
    public BattleManager battleManager = null;
    public GameObject defeatPanel = null;
    public Button pauseButton = null;
    public Button play2xButton = null;

    public GameObject fxDestroyEffectPrefab = null;
    public GameObject fxClickEffectPrefab = null;
    
    [Header("Movement Settings")]
    public float squadMoveSpeed = 5f;
    public float enemySquadMoveSpeed = 0.5f;
    public LayerMask mapLayerMask = 0; // Default layer
    
    public Camera mainCamera;
    public BattleMapPhase currentPhase = BattleMapPhase.NONE_PHASE;

    public static BattleMapManager Instance { get; private set; }
    public int currentSquadIndex { get; internal set; } = 0;

    private Dictionary<string, CharacterData> _squadCharacterData = new Dictionary<string, CharacterData>();
    public SquadInfoUI _playerSquadInfoUI = null;
    public SquadInfoUI _enemySquadInfoUI = null;
    
    private BattleMapPauseType _pauseType = BattleMapPauseType.PLAY; // 기본 상태: PLAY
    private TextMeshProUGUI pauseButtonText = null;
    private bool is2xSpeed = false; // 2배속 상태
    private Image play2xButtonImage = null; // 2배속 버튼 이미지 (체크 표시용)

    void Awake()
    {
        battleSimulationResultUI.closeButton.onClick.AddListener(() => {
            
        });
        
        // Pause 버튼 초기화
        InitializePauseButton();
        
        // 2배속 버튼 초기화
        InitializePlay2xButton();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        // 초기 상태 적용
        UpdatePauseButtonUI();
        
        // 2배속 상태 적용
        ApplyPlay2xSpeedState();
    }

    // Update is called once per frame
    void Update()
    {
        // ESC 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeKey();
        }
        
        // 스페이스바로 Pause/Play 전환
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // TacticsScene이 Active 상태면 무시
            if (TacticsUIManager.Instance != null && TacticsUIManager.Instance.rootObject.activeSelf)
            {
                return;
            }
            
            // BattleSimulationResultUI가 활성화되어 있으면 무시
            if (battleSimulationResultUI != null && battleSimulationResultUI.gameObject.activeSelf)
            {
                return;
            }
            
            TogglePause();
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
            Debug.Log($"맵 클릭 위치: {hit.point}, 충돌 오브젝트: {hit.collider.gameObject.name}");

            // 맵 오브젝트 또는 그 자식 오브젝트인지 확인
            bool isMapObject = false;
            if (mapObject != null)
            {
                // 직접 mapObject인 경우
                if (hit.collider.gameObject == mapObject)
                {
                    isMapObject = true;
                }
                // mapObject의 자식인지 확인
                else if (hit.collider.transform.IsChildOf(mapObject.transform))
                {
                    isMapObject = true;
                }
            }

            if (isMapObject)
            {
                // 선택된 Squad를 클릭한 위치로 이동
                Vector3 targetPosition = hit.point;
                targetPosition.y = selectedSquadObject.transform.position.y; // Y축은 유지
                
                // 클릭 위치에 FX 재생
                if (fxClickEffectPrefab != null)
                {
                    Vector3 fxPosition = hit.point;
                    GameObject fxInstance = Instantiate(fxClickEffectPrefab, fxPosition, Quaternion.identity);
                    fxInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    
                    // ParticleSystem이 있는 경우 자동으로 파괴되도록 설정
                    ParticleSystem ps = fxInstance.GetComponent<ParticleSystem>();
                    if (ps != null && ps.main.duration > 0)
                    {
                        Destroy(fxInstance, ps.main.duration + ps.main.startLifetime.constantMax);
                    }
                    else
                    {
                        // ParticleSystem이 없거나 duration이 0인 경우 기본 시간 후 파괴
                        Destroy(fxInstance, 2f);
                    }
                }
                
                BattleSquad squad = selectedSquadObject.GetComponent<BattleSquad>();
                if (squad != null)
                {
                    squad.MoveTo(targetPosition);                        
                    Debug.Log($"Squad 이동 명령: {targetPosition}");

                    // _pauseType을 PLAY로 변경
                    _pauseType = BattleMapPauseType.PLAY;
                    UpdatePauseButtonUI();

                    // 선택 해제
                    squad.SetSelected(false);
                    selectedSquadObject = null;
                    _playerSquadInfoUI.gameObject.SetActive(false);
                    _enemySquadInfoUI.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.Log($"맵 오브젝트가 아님: {hit.collider.gameObject.name}");
            }
        }
        else
        {
            Debug.Log($"Raycast 실패: mapLayerMask와 충돌하는 오브젝트가 없습니다. mapLayerMask 값: {mapLayerMask.value}");
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
                    TacticsUIManager.Instance.SetBattleMapPhaseUI(currentPhase);
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
        // 플레이어 편성은 현재 편성 저장. Enemy를 설정된 적 편성으로 설정.
        string playerSquadName = selectedSquad.gameObject.name;
        string enemySquadName = battleSquad.gameObject.name;
        TacticsDataManager.Instance.SetPlayerTactics(playerSquadName);
        TacticsDataManager.Instance.SetEnemyTactics(enemySquadName);        

        // 시뮬 UI 초기화
        _pauseType = BattleMapPauseType.PAUSE;
        UpdatePauseButtonUI();
        battleSimulationResultUI.gameObject.SetActive(true);
        battleSimulationResultUI.InitializeUI(playerSquadName, enemySquadName);

        // 시뮬레이션 모드 스타트 - 완료될 때까지 대기        \        
        battleManager.SetInstanceSelf(); // 임시 코드 : BattleManager의 Instance를 battleManager로 설정
        yield return StartCoroutine(BattleManager.Instance.SimulationModeStart(playerSquadName, enemySquadName));

        battleSimulationResultUI.UpdateUI();         
        battleSimulationResultUI.startBattleButton.onClick.RemoveAllListeners();
        battleSimulationResultUI.startBattleButton.onClick.AddListener(() => 
        {
            _pauseType = BattleMapPauseType.PLAY;
            UpdatePauseButtonUI();
            battleSimulationResultUI.gameObject.SetActive(false);

            // Player Squad를 Enemy Squad 위치로 이동
            Vector3 targetPosition = battleSquad.transform.position;
            targetPosition.y = selectedSquad.transform.position.y; // Y축은 유지

            selectedSquad.MoveTo(targetPosition);
            Debug.Log($"Player Squad 이동 명령: {selectedSquad.gameObject.name} -> {battleSquad.gameObject.name} 위치로");
        });
    }

    private void ShowSquadInfoUI(BattleSquad battleSquad)
    {
        SquadInfoUI squadInfoUI = battleSquad.isPlayerSquad ? _playerSquadInfoUI : _enemySquadInfoUI;        

        // SquadInfoUI에 정보 업데이트 
        if (squadInfoUI != null)
        {
            string squadName = battleSquad.gameObject.name;

            if (battleSquad._loadResult != null && battleSquad._loadResult.unitSlots != null)
            {
                squadInfoUI.gameObject.SetActive(true);
                squadInfoUI.UpdateSquadInfo(squadName, battleSquad._loadResult.unitSlots);
            }
            else
            {
                Debug.LogWarning($"SquadInfoUI 업데이트 실패: {squadName}");                
            }            
        }
    }

    public void UpdateSquadInfoUI()
    {
        if(selectedSquadObject != null)
        {
            ShowSquadInfoUI(selectedSquadObject.GetComponent<BattleSquad>());
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
            // Tactics Scene을 로드하지 않고, 바로 BattleScene을 로드한다.
            SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Additive).completed += (operation) => {
                Scene battleScene = SceneManager.GetSceneByName("BattleScene");
                if(battleScene.isLoaded)
                {
                    battleMapRootObject.SetActive(false);
                    SceneManager.SetActiveScene(battleScene);
                }
            };
        }
        else if(currentPhase == BattleMapPhase.TOWER_PHASE)
        {
            // 아리엘의 Make Tactics Message 표시
            IRISUIManager.Instance.ShowIrisUI(MessageToIRIS.MAKE_TACTICS_MESSAGE, new TacticsGameStatusData());

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
                        // 적 부대 Destroy 시 FX 재생
                        PlayDestroyEffect(_enemySquad.transform.position);
                        Destroy(_enemySquad.gameObject);
                    }
                }

                if(_playerSquad != null)
                    _playerSquad.SetTriggerEnabled(true);
            }
            else // 플레이어 패배 
            {
                // 플레이어 스쿼드를 회수한다.
                if(_playerSquad != null && _playerSquad.gameObject != null)
                {
                    // 아군 부대 Destroy 시 FX 재생
                    PlayDestroyEffect(_playerSquad.transform.position);
                    ReturnSquad(_playerSquad.gameObject.name);
                    Destroy(_playerSquad.gameObject);
                }
                HandleSquadClick(null);

                if(_enemySquad != null)
                    _enemySquad.SetTriggerEnabled(true);
            }
        }
        else if(currentPhase == BattleMapPhase.BATTLE_DEFEAT)
        {
            //패배씬 출력            
            defeatPanel.SetActive(true);    
            defeatPanel.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
            defeatPanel.GetComponentInChildren<Button>().onClick.AddListener(() => {
                SceneManager.LoadScene("IntroScene");
            });
        }
    }

    // 스쿼드를 회수 한다.
    public void ReturnSquad(string squadName)
    {
        // 캐릭터들을 스쿼드 멤버에서 제거한다.
        var loadResult = TacticsDataManager.Instance.LoadSquadTactics(squadName);
        if(loadResult != null && loadResult.unitSlots != null)
        {
            // 스쿼드의 작전을 CharacterPool에 저장 (패배 복귀 시 작전 유지)
            if(loadResult.codingData != null && loadResult.codingData.Count > 0)
            {
                TacticsDataManager.Instance.SaveTacticsToFile(loadResult.codingData);
                Debug.Log($"ReturnSquad: '{squadName}'의 작전을 CharacterPool에 저장했습니다.");
            }

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

    // 스쿼드의 Tactics UI 씬을 표시한다. 
    public void ShowSquadTacticsUI(string squadName)
    {
        currentPhase = BattleMapPhase.TACTICS_PHASE;
        ShowTacticsScene(squadName);
    }


    private BattleSquad _playerSquad = null;
    private BattleSquad _enemySquad = null;
    public void SetBattleSquad(BattleSquad battleSquad, BattleSquad otherSquad)
    {
        _playerSquad = battleSquad;
        _enemySquad = otherSquad;
    }

    public bool IsPause()
    {
        return _pauseType == BattleMapPauseType.PAUSE;
    }
    
    /// <summary>
    /// Pause 버튼 초기화
    /// </summary>
    private void InitializePauseButton()
    {
        if (pauseButton == null)
        {
            Debug.LogWarning("BattleMapManager: pauseButton이 할당되지 않았습니다.");
            return;
        }
        
        // 버튼 클릭 이벤트 등록
        pauseButton.onClick.RemoveAllListeners();
        pauseButton.onClick.AddListener(TogglePause);
        
        // 버튼의 텍스트 컴포넌트 찾기
        pauseButtonText = pauseButton.GetComponentInChildren<TextMeshProUGUI>();        

        UpdatePauseButtonUI();
    }
    
    /// <summary>
    /// Pause/Play 상태 전환
    /// </summary>
    private void TogglePause()
    {
        if (_pauseType == BattleMapPauseType.PAUSE)
        {
            _pauseType = BattleMapPauseType.PLAY;
        }
        else
        {
            _pauseType = BattleMapPauseType.PAUSE;
        }
        
        UpdatePauseButtonUI();
        
        // Pause 상태 변경 시 Time.timeScale도 업데이트
        ApplyPlay2xSpeedState();
        
        Debug.Log($"BattleMapManager: Pause 상태 전환 -> {_pauseType}");
    }
    
    /// <summary>
    /// Pause 버튼 UI 업데이트
    /// </summary>
    private void UpdatePauseButtonUI()
    {
        if (pauseButtonText != null)
        {
            // PAUSE 상태일 때는 "PLAY" 표시, PLAY 상태일 때는 "PAUSE" 표시
            pauseButtonText.text = _pauseType == BattleMapPauseType.PAUSE ? "PAUSE" : "PLAY";
        }       
    }
    
    /// <summary>
    /// 2배속 버튼 초기화
    /// </summary>
    private void InitializePlay2xButton()
    {
        if (play2xButton == null)
        {
            Debug.LogWarning("BattleMapManager: play2xButton이 할당되지 않았습니다.");
            return;
        }
        
        // 버튼 클릭 이벤트 등록
        play2xButton.onClick.RemoveAllListeners();
        play2xButton.onClick.AddListener(TogglePlay2xSpeed);
        
        // 버튼의 Image 컴포넌트 가져오기 (체크 표시용)
        play2xButtonImage = play2xButton.GetComponent<Image>();
        
        // UserDataManager에서 저장된 상태 로드
        if (UserDataManager.Instance != null && UserDataManager.Instance.currentUserData != null)
        {
            is2xSpeed = UserDataManager.Instance.currentUserData.gameSettings.battleMap2xSpeed;
        }
    }
    
    /// <summary>
    /// 2배속 토글
    /// </summary>
    private void TogglePlay2xSpeed()
    {
        is2xSpeed = !is2xSpeed;
        
        // UserDataManager에 저장
        if (UserDataManager.Instance != null && UserDataManager.Instance.currentUserData != null)
        {
            UserDataManager.Instance.currentUserData.gameSettings.battleMap2xSpeed = is2xSpeed;
            UserDataManager.Instance.SaveUserData();
        }
        
        // 상태 적용
        ApplyPlay2xSpeedState();
        
        Debug.Log($"BattleMapManager: 2배속 토글 -> {(is2xSpeed ? "2x" : "1x")}");
    }
    
    /// <summary>
    /// 2배속 상태 적용
    /// </summary>
    private void ApplyPlay2xSpeedState()
    {
        // 버튼 시각적 피드백 (체크 표시)
        if (play2xButtonImage != null)
        {
            // 체크되어 있으면 색상 변경 (또는 체크마크 표시)
            play2xButtonImage.color = is2xSpeed ? new Color(1f, 0.8f, 0.2f, 1f) : Color.white;
        }
    }
    
    /// <summary>
    /// Squad 이동 속도 반환 (2배속 적용)
    /// </summary>
    public float GetSquadMoveSpeed()
    {
        if (_pauseType == BattleMapPauseType.PAUSE)
        {
            return 0f; // PAUSE 상태면 속도 0
        }
        return is2xSpeed ? squadMoveSpeed * 2f : squadMoveSpeed;
    }
    
    /// <summary>
    /// 적 Squad 이동 속도 반환 (2배속 적용)
    /// </summary>
    public float GetEnemySquadMoveSpeed()
    {
        if (_pauseType == BattleMapPauseType.PAUSE)
        {
            return 0f; // PAUSE 상태면 속도 0
        }
        return is2xSpeed ? enemySquadMoveSpeed * 2f : enemySquadMoveSpeed;
    }
    
    /// <summary>
    /// 부대 Destroy 시 FX 재생
    /// </summary>
    private void PlayDestroyEffect(Vector3 position)
    {
        if (fxDestroyEffectPrefab != null)
        {
            GameObject fxInstance = Instantiate(fxDestroyEffectPrefab, position, Quaternion.identity);
            fxInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // ParticleSystem이 있는 경우 자동으로 파괴되도록 설정
            ParticleSystem ps = fxInstance.GetComponent<ParticleSystem>();
            if (ps != null && ps.main.duration > 0)
            {
                Destroy(fxInstance, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // ParticleSystem이 없거나 duration이 0인 경우 기본 시간 후 파괴
                Destroy(fxInstance, 2f);
            }
        }
    }
    
}
