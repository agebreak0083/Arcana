using UnityEngine;

public class BattleSquad : MonoBehaviour
{
    public bool isPlayerSquad = false;
    private BattleMapManager mapManager;
    private bool isSelected = false;
    
    // 이동 관련 변수
    private Vector3 currentTargetPosition;
    private float currentMoveSpeed;
    private bool isMoving = false;
    private System.Collections.IEnumerator moveCoroutine;
    private Color originalColor;
    public Color selectColor = Color.blue;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mapManager = FindFirstObjectByType<BattleMapManager>();

        // 머테리얼 인스턴스 생성. Renderer에 할당된 것에서 복제한다.
        Material material = new Material(GetComponent<Renderer>().material);
        originalColor = material.color;
        GetComponent<Renderer>().material = material;        
    }

    // Update is called once per frame
    void Update()
    {
        
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
        // 선택시 빨간색으로 변경         
        if(selected)
        {
            GetComponent<Renderer>().material.color = selectColor;
        }
        else
        {
            GetComponent<Renderer>().material.color = originalColor;
        }
    }

    /// <summary>
    /// 목표 위치로 이동 (중간에 목표 위치 변경 가능)
    /// </summary>
    public void MoveTo(Vector3 targetPosition, float speed)
    {
        // 목표 위치와 속도 업데이트
        currentTargetPosition = targetPosition;
        currentMoveSpeed = speed;
        
        // 이미 이동 중이면 코루틴을 재시작하지 않고 목표만 변경
        if (!isMoving)
        {
            isMoving = true;
            moveCoroutine = MoveCoroutine();
            StartCoroutine(moveCoroutine);
        }
    }

    private System.Collections.IEnumerator MoveCoroutine()
    {
        while (Vector3.Distance(transform.position, currentTargetPosition) > 0.1f)
        {
            // 매 프레임 업데이트된 목표 위치로 이동
            transform.position = Vector3.MoveTowards(transform.position, currentTargetPosition, currentMoveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = currentTargetPosition;
        isMoving = false;
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
            
            // BattlePhase로 전환            
            mapManager.SetBattleSquad(this, otherSquad);
            SetTriggerEnabled(false);
            otherSquad.SetTriggerEnabled(false);

            mapManager.ChangeCurrentPhase(BattleMapPhase.BATTLE_PHASE);
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
