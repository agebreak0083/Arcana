using UnityEngine;
using System.Collections;

public enum CameraMode
{
    Shoulder,
    FixedPosition,
}

public class BattleCameraController : MonoBehaviour
{
    [Header("Shoulder View Settings")]
    public Vector3 shoulderOffset = new Vector3(2.0f, 1.5f, -2.0f); // 솔더뷰: 우측 상단
    public Vector3 targetOffset = new Vector3(0.0f, 1.0f, 0.0f); // 타겟 캐릭터 좌표
    

    [Header("Camera Settings")]
    public float defaultZoom = 5.0f;
    public float characterTransitionDuration = 0.5f; // 캐릭터 전환 시 보간 시간

    [Header("Camera Shake Settings")]
    public float hitShakeIntensity = 0.15f;
    public float hitShakeDuration = 0.3f;

    private Camera cam;
    private Character currentCharacter; // 현재 추적 중인 캐릭터
    private Character targetCharacter; // 현재 타겟 캐릭터
    private Vector3 spawnPosition; // 카메라 스폰시의 위치
    private Vector3 originalPosition; // 쉐이크 전 원래 위치
    private Coroutine shakeCoroutine; // 현재 쉐이크 코루틴
    private Coroutine transitionCoroutine; // 캐릭터 전환 보간 코루틴
    private Vector3 targetLookAtPosition; // LookAt 타겟 위치
    
    private CameraMode cameraMode = CameraMode.Shoulder;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }
        
        // spawnPosition 초기화 (카메라가 어디에 있든 초기 위치 저장)
        if (cam != null)
        {
            spawnPosition = cam.transform.position;
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
    /// 카메라 모드 변경
    /// </summary>
    public void SetCameraMode(CameraMode mode)
    {
        cameraMode = mode;
        UpdateCameraPosition();
    }

    /// <summary>
    /// 카메라 위치를 타겟 캐릭터의 솔더뷰로 업데이트
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (currentCharacter == null) return;

        if (cameraMode == CameraMode.FixedPosition)
        {
            // FixedPosition 모드: spawnPosition에 위치하고 Skill을 사용하는 캐릭터를 LookAt
            if (shakeCoroutine == null)
            {
                transform.position = spawnPosition;
                originalPosition = spawnPosition;
            }
            else
            {
                originalPosition = spawnPosition;
            }
            
            // Skill을 사용하는 캐릭터와 타겟 캐릭터의 중앙 위치를 LookAt
            Vector3 targetPos;
            if (currentCharacter != null && targetCharacter != null)
            {
                // 두 캐릭터의 중앙 위치
                Vector3 currentPos = currentCharacter.transform.position + targetOffset;
                Vector3 targetCharPos = targetCharacter.transform.position + targetOffset;
                targetPos = (currentPos + targetCharPos) * 0.5f;
            }
            else if (currentCharacter != null)
            {
                // targetCharacter가 없으면 currentCharacter만 사용
                targetPos = currentCharacter.transform.position + targetOffset;
            }
            else
            {
                return;
            }
            
            targetLookAtPosition = targetPos;
            
            // 보간 중이 아닐 때만 즉시 LookAt
            if (transitionCoroutine == null)
            {
                transform.LookAt(targetPos);
            }
        }
        else // CameraMode.Shoulder
        {
            // 기존 Shoulder 모드 로직
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

            // 카메라 LookAt: 두 캐릭터의 중앙 위치
            Vector3 lookAtPos;
            if (targetCharacter != null)
            {
                // currentCharacter와 targetCharacter의 중앙 위치
                Vector3 currentPos = currentCharacter.transform.position + targetOffset;
                Vector3 targetCharPos = targetCharacter.transform.position + targetOffset;
                lookAtPos = (currentPos + targetCharPos) * 0.5f;
            }
            else
            {
                // targetCharacter가 없으면 currentCharacter만 사용
                lookAtPos = currentCharacter.transform.position + targetOffset;
            }
            transform.LookAt(lookAtPos);
        }
    }

    /// <summary>
    /// 타겟 캐릭터 설정 (보간 전환)
    /// </summary>
    private void SetTargetCharacter(Character character, Character target = null)
    {
        if (character == null) return;

        // 기존 전환 코루틴이 있으면 중지
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        Character previousCharacter = currentCharacter;
        currentCharacter = character;
        targetCharacter = target;

        // 캐릭터 전환 보간 시작
        transitionCoroutine = StartCoroutine(TransitionToCharacter(previousCharacter));
    }

    /// <summary>
    /// 캐릭터 전환 보간 코루틴
    /// </summary>
    private IEnumerator TransitionToCharacter(Character previousCharacter)
    {
        float elapsed = 0f;
        Vector3 startLookAtPos = transform.position + transform.forward;

        while (elapsed < characterTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / characterTransitionDuration;
            
            Vector3 endLookAtPos;
            
            // 두 캐릭터의 중앙 위치를 LookAt
            if (currentCharacter != null && targetCharacter != null)
            {
                // 두 캐릭터의 중앙 위치
                Vector3 currentPos = currentCharacter.transform.position + targetOffset;
                Vector3 targetCharPos = targetCharacter.transform.position + targetOffset;
                endLookAtPos = (currentPos + targetCharPos) * 0.5f;
            }
            else if (currentCharacter != null)
            {
                // targetCharacter가 없으면 currentCharacter만 사용
                endLookAtPos = currentCharacter.transform.position + targetOffset;
            }
            else
            {
                endLookAtPos = startLookAtPos;
            }
            
            // LookAt 위치 보간 (캐릭터 이동에 따라 실시간 업데이트)
            Vector3 currentLookAtPos = Vector3.Lerp(startLookAtPos, endLookAtPos, t);
            transform.LookAt(currentLookAtPos);

            // 카메라 위치 업데이트 (Shoulder 모드일 경우)
            if (cameraMode == CameraMode.Shoulder && currentCharacter != null)
            {
                Vector3 characterPosition = currentCharacter.transform.position;
                Quaternion rotation = Quaternion.Euler(0, currentCharacter.transform.rotation.y, 0);
                Vector3 offset = rotation * shoulderOffset;
                Vector3 targetPos = characterPosition + offset;
                
                if (previousCharacter != null && elapsed < characterTransitionDuration * 0.1f) // 초기 10%만 이전 위치에서 보간
                {
                    Vector3 prevPos = previousCharacter.transform.position;
                    Quaternion prevRotation = Quaternion.Euler(0, previousCharacter.transform.rotation.y, 0);
                    Vector3 prevOffset = prevRotation * shoulderOffset;
                    Vector3 startPos = prevPos + prevOffset;
                    transform.position = Vector3.Lerp(startPos, targetPos, t * 10f); // 빠르게 전환
                }
                else
                {
                    transform.position = targetPos;
                }
            }

            yield return null;
        }

        // 보간 완료 후 최종 위치 설정
        UpdateCameraPosition();
        transitionCoroutine = null;
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
            if (cameraMode == CameraMode.FixedPosition)
            {
                // FixedPosition 모드: spawnPosition 기준
                originalPosition = spawnPosition;
            }
            else // Shoulder 모드
            {
                // Shoulder 모드: 캐릭터 위치 기준
                if (currentCharacter != null)
                {
                    Vector3 characterPosition = currentCharacter.transform.position;
                    Quaternion rotation = Quaternion.Euler(0, currentCharacter.transform.rotation.y, 0);
                    Vector3 offset = rotation * shoulderOffset;
                    originalPosition = characterPosition + offset;
                }
            }
            
            // 랜덤 쉐이크 오프셋 (시간에 따라 감소)
            float shakeAmount = 1f - (elapsed / hitShakeDuration); // 시간이 지날수록 감소
            Vector3 shakeOffset = Random.insideUnitSphere * hitShakeIntensity * shakeAmount;
            
            // 원래 위치 기준으로 쉐이크 적용
            transform.position = originalPosition + shakeOffset;
            
            yield return null;
        }

        // 쉐이크 종료 후 원래 위치로 복귀
        if (cameraMode == CameraMode.FixedPosition)
        {
            originalPosition = spawnPosition;
        }
        else // Shoulder 모드
        {
            if (currentCharacter != null)
            {
                Vector3 characterPosition = currentCharacter.transform.position;
                Quaternion rotation = Quaternion.Euler(0, currentCharacter.transform.rotation.y, 0);
                Vector3 offset = rotation * shoulderOffset;
                originalPosition = characterPosition + offset;
            }
        }
        transform.position = originalPosition;
        shakeCoroutine = null;
    }
}
