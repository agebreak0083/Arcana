using UnityEngine;

public class BattleMapManager : MonoBehaviour
{
    public GameObject selectedSquadObject = null;
    public GameObject mapObject = null;
    
    [Header("Movement Settings")]
    public float squadMoveSpeed = 5f;
    public LayerMask mapLayerMask = 1; // Default layer
    
    private Camera mainCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
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
                    Debug.Log($"Squad 이동 명령: {targetPosition}");
                }
            }
        }
    }
}
