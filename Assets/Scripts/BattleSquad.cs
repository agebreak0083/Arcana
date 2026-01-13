using System;
using System.Collections.Generic;
using Arcana.Tactics;
using UnityEngine;
using UnityEngine.AI;

public class BattleSquad : MonoBehaviour
{
    public bool isPlayerSquad = false;
    public bool isBossSquad = false;
    private BattleMapManager mapManager;
    private bool isSelected = false;
    
    // 이동 관련 변수
    private GameObject targetTower;
    private Vector3 currentTargetPosition;    
    private System.Collections.IEnumerator moveCoroutine;
    private Color originalColor;
    public Color selectColor = Color.blue;
    public FormationLoadResult _loadResult;
    private MaterialPropertyBlock propertyBlock;
    private Renderer squadRenderer;
    private NavMeshAgent navMeshAgent;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mapManager = FindFirstObjectByType<BattleMapManager>();

        // Renderer와 MaterialPropertyBlock 초기화
        squadRenderer = GetComponent<Renderer>();
        navMeshAgent = GetComponent<NavMeshAgent>();       

        propertyBlock = new MaterialPropertyBlock();
        
        // 머테리얼 인스턴스 생성. Renderer에 할당된 것에서 복제한다.
        Material material = new Material(squadRenderer.material);
        squadRenderer.material = material;
        
        // 셰이더가 _Color 프로퍼티를 가지고 있는지 확인
        if (material.HasProperty("_Color"))
        {
            originalColor = material.color;
        }
        else
        {
            // _Color 프로퍼티가 없으면 기본 색상(흰색) 사용
            originalColor = Color.white;
        }

        // 적 Squad인 경우, Tactics 세팅 하기 
        if(!isPlayerSquad)
        {
            LoadEnemyTactics();
            
            targetTower = GameObject.Find("Tower_Player");
            
            // NavMeshAgent 활성화 전에 위치를 NavMesh 위로 조정 (텔레포트 방지)
            if(navMeshAgent != null)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                }
            }
        }

             
    }

    private void LoadEnemyTactics()
    {
        // 보스 스쿼드인 경우 tactics_CH.json 파일을 로드
        if (isBossSquad)
        {
            LoadBossTactics();
            return;
        }

        // 일반 적 스쿼드는 랜덤 전술 로드
        TacticsDataManager.Instance.GetRandomEnemySquad((loadResult) =>
        {
            if(loadResult != null)
            {
                _loadResult = loadResult;
                gameObject.name = loadResult.username;
                TacticsDataManager.Instance.SaveSquadTactics(gameObject.name, loadResult);
            }
        });
    }

    private void LoadBossTactics()
    {
        // Resources 폴더에서 tactics_CH.json 로드
        TextAsset tacticsAsset = Resources.Load<TextAsset>("tactics_CH");
        if (tacticsAsset == null)
        {
            Debug.LogError("tactics_CH.json 파일을 찾을 수 없습니다!");
            return;
        }

        string json = tacticsAsset.text;
        
        // availableCharacters와 _allCharacterDefinitions가 로드될 때까지 대기
        if (TacticsDataManager.Instance.availableCharacters == null || TacticsDataManager.Instance.availableCharacters.Count == 0)
        {
            StartCoroutine(WaitForCharactersAndLoadBossTactics(json));
            return;
        }

        // 보스 스쿼드는 모든 캐릭터 정의를 사용 (플레이어가 가지고 있지 않은 캐릭터도 포함)
        List<Arcana.Tactics.Data.CharacterData> allCharacters = TacticsDataManager.Instance.GetAllCharactersFromDefinitions();

        // FormationManager를 사용하여 JSON에서 포메이션 로드
        var loadResult = Arcana.Tactics.FormationManager.LoadFormationFromJson(
            json, 
            allCharacters, 
            TacticsDataManager.Instance.CreateDefaultPlan
        );

        if (loadResult != null)
        {
            _loadResult = loadResult;
            gameObject.name = loadResult.username;
            TacticsDataManager.Instance.SaveSquadTactics(gameObject.name, loadResult);
            Debug.Log($"Boss Squad 전술 로드 완료: {loadResult.username}");
        }
        else
        {
            Debug.LogError("Boss Squad 전술 로드 실패!");
        }
    }

    private System.Collections.IEnumerator WaitForCharactersAndLoadBossTactics(string json)
    {
        // availableCharacters와 _allCharacterDefinitions가 로드될 때까지 대기 (최대 10초)
        float waitTime = 0f;
        const float maxWaitTime = 10f;
        
        while ((TacticsDataManager.Instance.availableCharacters == null || TacticsDataManager.Instance.availableCharacters.Count == 0) && waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }

        if (TacticsDataManager.Instance.availableCharacters == null || TacticsDataManager.Instance.availableCharacters.Count == 0)
        {
            Debug.LogError($"Boss Squad 전술 로드: availableCharacters 로드 타임아웃 ({maxWaitTime}초)");
            yield break;
        }

        // 보스 스쿼드는 모든 캐릭터 정의를 사용 (플레이어가 가지고 있지 않은 캐릭터도 포함)
        List<Arcana.Tactics.Data.CharacterData> allCharacters = TacticsDataManager.Instance.GetAllCharactersFromDefinitions();

        // FormationManager를 사용하여 JSON에서 포메이션 로드
        var loadResult = Arcana.Tactics.FormationManager.LoadFormationFromJson(
            json, 
            allCharacters, 
            TacticsDataManager.Instance.CreateDefaultPlan
        );

        if (loadResult != null)
        {
            _loadResult = loadResult;
            gameObject.name = loadResult.username;
            TacticsDataManager.Instance.SaveSquadTactics(gameObject.name, loadResult);
            Debug.Log($"Boss Squad 전술 로드 완료: {loadResult.username}");
        }
        else
        {
            Debug.LogError("Boss Squad 전술 로드 실패!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(gameObject.CompareTag("Tower"))
        {
            return;
        }

        if(navMeshAgent != null)
        {
            // Deactive->active 되면, 활성화가 자동으로 안되서 수동으로 활성화 한다.
            navMeshAgent.enabled = true;

            if(isPlayerSquad)
            {
                if(currentTargetPosition.magnitude < 0.01f)
                {
                    navMeshAgent.enabled = false;
                }
                else
                {                    
                    navMeshAgent.speed = mapManager.GetSquadMoveSpeed();                    
                    navMeshAgent.SetDestination(currentTargetPosition);
                }
            }
            else
            {
                navMeshAgent.speed = mapManager.GetEnemySquadMoveSpeed();
                navMeshAgent.SetDestination(targetTower.transform.position);
            }
        }

        if(!isPlayerSquad && targetTower != null && mapManager.currentPhase != BattleMapPhase.BATTLE_DEFEAT)
        {
            if(Vector3.Distance(transform.position, targetTower.transform.position) < 1.0f)
            {
                if(navMeshAgent != null)
                {
                    navMeshAgent.enabled = false;
                }

                mapManager.ChangeCurrentPhase(BattleMapPhase.BATTLE_DEFEAT);
            }
        }
    }

    /// <summary>
    /// Squad 클릭 시 호출
    /// </summary>
    void OnMouseDown()
    {
        if (mapManager != null)
        {
            mapManager.HandleSquadClick(this);            
        }
    }

    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // 선택 시 시각적 피드백 (예: 하이라이트)
        // 선택시 색상 변경
        if (squadRenderer == null)
        {
            squadRenderer = GetComponent<Renderer>();
        }
        
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        
        Material material = squadRenderer.material;
        Color targetColor = selected ? selectColor : originalColor;
        
        // 셰이더가 _Color 프로퍼티를 가지고 있는지 확인
        if (material.HasProperty("_Color"))
        {
            // MaterialPropertyBlock을 사용하여 색상 변경 (원본 머테리얼 수정 방지)
            squadRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", targetColor);
            squadRenderer.SetPropertyBlock(propertyBlock);
        }
        // _Color 프로퍼티가 없으면 색상 변경을 시도하지 않음
    }

    /// <summary>
    /// 목표 위치로 이동 (중간에 목표 위치 변경 가능)
    /// </summary>
    /// <param name="targetPosition">목표 위치</param>
    public void MoveTo(Vector3 targetPosition)
    {
        // 목표 위치와 원본 속도 저장
        currentTargetPosition = targetPosition;        
    }

    /// <summary>
    /// 다른 Squad와의 충돌 감지
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Squad 충돌 감지: {gameObject.name} <-> {other.gameObject.name}");
        // 플레이어의 충돌만 처리 한다.
        if(!isPlayerSquad)
        {
            return;
        }

        BattleSquad otherSquad = other.GetComponent<BattleSquad>();
        if (otherSquad != null && otherSquad != this && otherSquad.isPlayerSquad != isPlayerSquad)
        {
            Debug.Log($"Squad 충돌 감지: {gameObject.name} <-> {other.gameObject.name}");

            StopMoving();
            SetTriggerEnabled(false);
            otherSquad.SetTriggerEnabled(false);
            
            // BattlePhase로 전환            
            string playerSquadName = gameObject.name;
            string enemySquadName = otherSquad.gameObject.name;
            mapManager.SetBattleSquad(this, otherSquad);            
            mapManager.ChangeCurrentPhase(BattleMapPhase.BATTLE_PHASE, playerSquadName, enemySquadName);
        }
    }

    public void SetTriggerEnabled(bool enabled)
    {
        GetComponent<Collider>().enabled = enabled;
    }

    public void StopMoving()
    {
        currentTargetPosition = transform.position;
    }
}
