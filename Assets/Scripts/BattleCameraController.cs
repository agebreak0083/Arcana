using UnityEngine;
using System.Collections;

public class BattleCameraController : MonoBehaviour
{
    [Header("Shoulder View Settings")]
    public Vector3 shoulderOffset = new Vector3(2.0f, 1.5f, -2.0f); // 솔더뷰: 우측 상단
    public Vector3 targetOffset = new Vector3(0.0f, 1.0f, 0.0f); // 타겟 캐릭터 좌표

    [Header("Camera Settings")]
    public float defaultZoom = 5.0f;

    [Header("Camera Shake Settings")]
    public float hitShakeIntensity = 0.15f;
    public float hitShakeDuration = 0.3f;

    private Camera cam;
    private Character currentCharacter; // 현재 추적 중인 캐릭터
    private Character targetCharacter; // 현재 타겟 캐릭터
    private Vector3 originalPosition; // 쉐이크 전 원래 위치
    private Coroutine shakeCoroutine; // 현재 쉐이크 코루틴
    

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }       
        
    }

    void LateUpdate()
    {
        UpdatePlayerTarget();
    }

    /// <summary>
    /// Player 캐릭터를 찾아서 솔더뷰로 고정
    /// </summary>
    private void UpdatePlayerTarget()
    {
        // 현재 타겟 캐릭터의 솔더뷰 위치로 고정
        if (currentCharacter != null)
        {
            UpdateCameraPosition();
        }
    }

    /// <summary>
    /// 카메라 위치를 타겟 캐릭터의 솔더뷰로 업데이트
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (currentCharacter == null) return;

        // 캐릭터의 World Position 좌표
        Vector3 characterPosition = currentCharacter.transform.position;        

        // 캐릭터의 회전 방향 적용
        Quaternion rotation = Quaternion.Euler(0, currentCharacter.transform.rotation.y, 0);    
        Vector3 offset = rotation * shoulderOffset;       

        // 쉐이크 중이 아닐 때만 원래 위치 업데이트
        if (shakeCoroutine == null)
        {
            originalPosition = characterPosition + offset;
            transform.position = originalPosition;
        }
        else
        {
            // 쉐이크 중일 때는 원래 위치만 업데이트 (실제 위치는 쉐이크 코루틴에서 처리)
            originalPosition = characterPosition + offset;
        }

        // 카메라 LookAt 타겟 캐릭터
        if (targetCharacter != null)
        {
            transform.LookAt(targetCharacter.transform.position + targetOffset);
        }       
        else
        {
            // 정면 방향으로 LookAt
            transform.LookAt(transform.position + currentCharacter.transform.forward);
        }
    }

    /// <summary>
    /// 타겟 캐릭터 설정 (즉시 Jump)
    /// </summary>
    private void SetTargetCharacter(Character character, Character target = null)
    {
        if (character == null) return;

        currentCharacter = character;
        targetCharacter = target;
        UpdateCameraPosition();
    }

    /// <summary>
    /// 현재 턴의 캐릭터를 따라가도록 설정 (즉시 Jump)
    /// </summary>
    public void FollowCharacter(Character character)
    {
        if (character == null) return;

        // 다른 캐릭터로 변경 시 즉시 Jump
        SetTargetCharacter(character);
    }

    /// <summary>
    /// 액션 시작 시 카메라 설정
    /// </summary>
    public void OnActionStart(Character character, Character target = null)
    {
        if (character == null) return;

        Debug.Log("OnActionStart: " + character.characterName + " -> " + target.characterName);

        if (character.isPlayer)
        {
            // Player 공격: Player의 솔더뷰로 고정
            if (character != null)
            {
                SetTargetCharacter(character, target);
            }
        }
        else
        {
            // Enemy 공격: 피격 받는 Player(target)의 솔더뷰로 고정
            if (target != null && target.isPlayer)
            {
                SetTargetCharacter(target, character);
            }            
        }
    }

    /// <summary>
    /// 액션 종료 시 카메라 상태 유지
    /// </summary>
    public void OnActionEnd()
    {
        
    }

    /// <summary>
    /// 피격 시 카메라 쉐이크
    /// </summary>
    public void OnHit(Character hitCharacter)
    {
        if (hitCharacter == null) return;

        // 피격 받는 캐릭터가 현재 추적 중인 캐릭터일 때만 쉐이크
        if (hitCharacter == currentCharacter || hitCharacter == targetCharacter)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(HitShakeCoroutine());
        }
    }

    /// <summary>
    /// 피격 시 카메라 쉐이크 코루틴
    /// </summary>
    private IEnumerator HitShakeCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < hitShakeDuration)
        {
            elapsed += Time.deltaTime;
            
            // 원래 위치 업데이트 (캐릭터 이동 대응)
            if (currentCharacter != null)
            {
                Vector3 characterPosition = currentCharacter.transform.position;
                Quaternion rotation = Quaternion.Euler(0, currentCharacter.transform.rotation.y, 0);
                Vector3 offset = rotation * shoulderOffset;
                originalPosition = characterPosition + offset;
            }
            
            // 랜덤 쉐이크 오프셋 (시간에 따라 감소)
            float shakeAmount = 1f - (elapsed / hitShakeDuration); // 시간이 지날수록 감소
            Vector3 shakeOffset = Random.insideUnitSphere * hitShakeIntensity * shakeAmount;
            
            // 원래 위치 기준으로 쉐이크 적용
            transform.position = originalPosition + shakeOffset;
            
            yield return null;
        }

        // 쉐이크 종료 후 원래 위치로 복귀
        if (currentCharacter != null)
        {
            Vector3 characterPosition = currentCharacter.transform.position;
            Quaternion rotation = Quaternion.Euler(0, currentCharacter.transform.rotation.y, 0);
            Vector3 offset = rotation * shoulderOffset;
            originalPosition = characterPosition + offset;
        }
        transform.position = originalPosition;
        shakeCoroutine = null;
    }
}
