using UnityEngine;

public class BattleSquad : MonoBehaviour
{
    private BattleMapManager mapManager;
    private bool isSelected = false;
    
    // 이동 관련 변수
    private Vector3 currentTargetPosition;
    private float currentMoveSpeed;
    private bool isMoving = false;
    private System.Collections.IEnumerator moveCoroutine;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mapManager = FindFirstObjectByType<BattleMapManager>();
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
            // 이전 선택 해제
            if (mapManager.selectedSquadObject != null && mapManager.selectedSquadObject != this.gameObject)
            {
                BattleSquad prevSquad = mapManager.selectedSquadObject.GetComponent<BattleSquad>();
                if (prevSquad != null)
                {
                    prevSquad.SetSelected(false);
                }
            }
            
            // 현재 Squad 선택
            mapManager.selectedSquadObject = this.gameObject;
            SetSelected(true);
            Debug.Log($"Squad 선택: {gameObject.name}");
        }
    }

    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        // 선택 시 시각적 피드백 (예: 하이라이트)
        // 필요시 여기에 선택 효과 추가
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
        BattleSquad otherSquad = other.GetComponent<BattleSquad>();
        if (otherSquad != null && otherSquad != this)
        {
            Debug.Log($"Squad 충돌 감지: {gameObject.name} <-> {other.gameObject.name}");
            // TacticsScene으로 전환
            UnityEngine.SceneManagement.SceneManager.LoadScene("TacticsScene");
        }
    }
}
