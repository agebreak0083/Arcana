using System;
using Arcana.Tactics;
using UnityEngine;
using UnityEngine.AI;

public class BattleSquad : MonoBehaviour
{
    public bool isPlayerSquad = false;
    private BattleMapManager mapManager;
    private bool isSelected = false;
    
    // 이동 관련 변수
    private GameObject targetTower;
    private Vector3 currentTargetPosition;
    private float currentMoveSpeed;
    private bool isMoving = false;
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
        }

             
    }

    private void LoadEnemyTactics()
    {
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
                    navMeshAgent.speed = currentMoveSpeed;
                    navMeshAgent.SetDestination(currentTargetPosition);
                }
            }
            else
            {
                navMeshAgent.speed = mapManager.enemySquadMoveSpeed;
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
    public void MoveTo(Vector3 targetPosition, float speed)
    {
        // 목표 위치와 속도 업데이트
        currentTargetPosition = targetPosition;
        currentMoveSpeed = speed;       
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
        isMoving = false;
        currentTargetPosition = transform.position;
    }
}
