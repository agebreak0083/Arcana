using UnityEngine;
using System.Collections;

public class BattleCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float followSpeed = 2.0f;
    public float zoomSpeed = 1.0f;
    public float minZoom = 3.0f;
    public float maxZoom = 8.0f;
    public float defaultZoom = 5.0f;

    [Header("Cinematic Settings")]
    public float actionZoom = 4.0f;
    public float actionDuration = 0.8f;
    public float transitionDuration = 0.5f;
    public Vector3 offset = new Vector3(0, 0, -3.0f); // 2D 게임이므로 Y는 0

    private Camera cam;
    private Transform currentTarget;
    private float targetZoom;
    private bool isTransitioning = false;
    private Coroutine currentTransition;
    private float fixedCameraY; // 2D 게임: 카메라 Y 위치 고정
    private Quaternion fixedCameraRotation; // 2D 게임: 카메라 회전 고정

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }
        
        if (cam != null)
        {
            cam.orthographicSize = defaultZoom;
        }
        
        targetZoom = defaultZoom;
        
        // 2D 게임: 카메라 Y 위치 및 회전 고정
        fixedCameraY = transform.position.y;
        fixedCameraRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (currentTarget == null) return;

        // 부드러운 위치 추적 (좌우 패닝만) - 2D 게임: Y 위치 및 회전 고정
        if (!isTransitioning)
        {
            Vector3 targetPos = currentTarget.position;
            Vector3 desiredPosition = new Vector3(targetPos.x + offset.x, fixedCameraY, targetPos.z + offset.z);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

            // 2D 게임: 회전 고정
            transform.rotation = fixedCameraRotation;

            // 부드러운 줌 조정
            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
            }
        }
    }

    /// <summary>
    /// 현재 턴의 캐릭터를 따라가도록 설정
    /// </summary>
    public void FollowCharacter(Character character)
    {
        if (character == null) return;

        GameObject characterObj = character.gameObject;
        if (characterObj == null) return;

        SetTarget(characterObj.transform);
    }

    /// <summary>
    /// 타겟 설정 (부드러운 전환)
    /// </summary>
    public void SetTarget(Transform target)
    {
        if (target == null) return;

        currentTarget = target;
        
        // 기존 전환 중단
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        // 부드러운 전환 시작
        currentTransition = StartCoroutine(SmoothTransitionToTarget(target));
    }

    /// <summary>
    /// 액션 시작 시 영화적 연출
    /// </summary>
    public void OnActionStart(Character character, Character target = null)
    {
        if (character == null) return;

        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        currentTransition = StartCoroutine(ActionCinematicSequence(character, target));
    }

    /// <summary>
    /// 액션 종료 시 기본 상태로 복귀
    /// </summary>
    public void OnActionEnd()
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        targetZoom = defaultZoom;
        isTransitioning = false;
    }

    /// <summary>
    /// 부드러운 타겟 전환
    /// </summary>
    private IEnumerator SmoothTransitionToTarget(Transform target)
    {
        isTransitioning = true;
        Vector3 startPos = transform.position;
        float startZoom = cam != null ? cam.orthographicSize : defaultZoom;

        Vector3 targetPos = target.position;
        Vector3 endPos = new Vector3(targetPos.x + offset.x, fixedCameraY, targetPos.z + offset.z);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(elapsed / transitionDuration);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y = fixedCameraY; // Y 위치 항상 고정
            transform.position = currentPos;
            transform.rotation = fixedCameraRotation; // 회전 고정
            
            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = Mathf.Lerp(startZoom, targetZoom, t);
            }

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = fixedCameraRotation;
        isTransitioning = false;
    }

    /// <summary>
    /// 액션 시 영화적 카메라 연출
    /// </summary>
    private IEnumerator ActionCinematicSequence(Character actor, Character target)
    {
        isTransitioning = true;

        // 1. 액터에게 줌인 (2D 게임: Y 위치 및 회전 고정)
        Vector3 actorPos = actor.transform.position;
        Vector3 cinematicOffset = new Vector3(0, 0, -2.5f); // Y는 0
        Vector3 startPos = transform.position;
        float startZoom = cam != null ? cam.orthographicSize : defaultZoom;

        // 액터 중심으로 이동 및 줌인
        Vector3 targetPos = new Vector3(actorPos.x + cinematicOffset.x, fixedCameraY, actorPos.z + cinematicOffset.z);

        float elapsed = 0f;
        while (elapsed < transitionDuration * 0.6f)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(elapsed / (transitionDuration * 0.6f));

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y = fixedCameraY; // Y 위치 항상 고정
            transform.position = currentPos;
            transform.rotation = fixedCameraRotation; // 회전 고정
            
            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = Mathf.Lerp(startZoom, actionZoom, t);
            }

            yield return null;
        }

        // 2. 타겟이 있으면 타겟으로 부드럽게 이동 (2D 게임: Y 위치 및 회전 고정)
        if (target != null)
        {
            Vector3 targetCharPos = target.transform.position;
            Vector3 midPoint = new Vector3(
                (actorPos.x + targetCharPos.x) / 2f,
                fixedCameraY,
                (actorPos.z + targetCharPos.z) / 2f
            );
            Vector3 cinematicPos = new Vector3(midPoint.x, fixedCameraY, midPoint.z - 3.5f);

            elapsed = 0f;
            Vector3 actionStartPos = transform.position;
            float actionStartZoom = cam != null ? cam.orthographicSize : actionZoom;

            while (elapsed < actionDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = EaseInOutCubic(elapsed / (actionDuration * 0.5f));

                Vector3 currentPos = Vector3.Lerp(actionStartPos, cinematicPos, t);
                currentPos.y = fixedCameraY; // Y 위치 항상 고정
                transform.position = currentPos;
                transform.rotation = fixedCameraRotation; // 회전 고정
                
                if (cam != null && cam.orthographic)
                {
                    cam.orthographicSize = Mathf.Lerp(actionStartZoom, actionZoom * 1.1f, t);
                }

                yield return null;
            }
        }

        // 3. 액션 중 약간의 카메라 쉐이크 (2D 게임: X, Z만 쉐이크, 회전 고정)
        Vector3 shakeStartPos = transform.position;
        float shakeIntensity = 0.1f;
        float shakeDuration = 0.2f;
        elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 shakeOffset2D = Random.insideUnitCircle * shakeIntensity;
            Vector3 shakeOffset = new Vector3(shakeOffset2D.x, 0, shakeOffset2D.y); // Y는 0
            transform.position = new Vector3(shakeStartPos.x + shakeOffset.x, fixedCameraY, shakeStartPos.z + shakeOffset.z);
            transform.rotation = fixedCameraRotation; // 회전 고정
            yield return null;
        }

        transform.position = new Vector3(shakeStartPos.x, fixedCameraY, shakeStartPos.z);
        transform.rotation = fixedCameraRotation;
        isTransitioning = false;
    }

    // Easing Functions
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f 
            ? 4f * t * t * t 
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    private float EaseInQuad(float t)
    {
        return t * t;
    }

    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}
