using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BattleUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI roundTurnText;   // 상단 라운드/턴 텍스트
    public TextMeshProUGUI skillNameText;   // 하단 스킬 이름 텍스트
    public GameObject highSpeedBtn; // 2배속 버튼 

    public GameObject victoryPanelPrefab;
    public GameObject defeatPanelPrefab;
    public GameObject damageTextPrefab; // 데미지 텍스트 프리팹
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI debugText;

    [Header("Animation Settings")]
    public float skillNameDisplayTime = 2f; // 스킬 이름 표시 시간

    private float skillNameTimer = 0f;
    private bool isHighSpeed = false;
    private Button highSpeedButton;
    private TextMeshProUGUI highSpeedButtonText;
    private Image highSpeedButtonImage;

    private const string HIGH_SPEED_PREF_KEY = "BattleHighSpeed";

    public static BattleUI Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 초기 상태
        if (skillNameText != null)
        {
            skillNameText.gameObject.SetActive(false);
        }

        // 2배속 버튼 초기화
        InitializeHighSpeedButton();

        UpdateRoundTurnText(1, 1);        
    }

    void Update()
    {
        // 스킬 이름 자동 숨김
        if (skillNameTimer > 0)
        {
            skillNameTimer -= Time.deltaTime;
            if (skillNameTimer <= 0)
            {
                HideSkillName();
            }
        }

        // 플레이어/적 이름 설정
        if (Arcana.Tactics.TacticsDataManager.Instance != null)
        {
            var playerResult = Arcana.Tactics.TacticsDataManager.Instance.GetPlayerFormationLoadResult();
            var enemyResult = Arcana.Tactics.TacticsDataManager.Instance.GetEnemyFormationLoadResult();

            if (playerNameText != null && playerResult != null)
            {
                playerNameText.text = playerResult.username;
            }
            if (enemyNameText != null && enemyResult != null)
            {
                enemyNameText.text = enemyResult.username;
            }
        }
    }

    /// <summary>
    /// 2배속 버튼 초기화
    /// </summary>
    private void InitializeHighSpeedButton()
    {
        if (highSpeedBtn == null)
        {
            Debug.LogWarning("BattleUI: highSpeedBtn is not assigned!");
            return;
        }

        // Button 컴포넌트 가져오기
        highSpeedButton = highSpeedBtn.GetComponent<Button>();
        if (highSpeedButton == null)
        {
            highSpeedButton = highSpeedBtn.AddComponent<Button>();
        }

        // Text 컴포넌트 가져오기 (자식에서 찾기)
        highSpeedButtonText = highSpeedBtn.GetComponentInChildren<TextMeshProUGUI>();

        // Image 컴포넌트 가져오기 (배경색 변경용)
        highSpeedButtonImage = highSpeedBtn.GetComponent<Image>();

        // 버튼 클릭 이벤트 등록
        highSpeedButton.onClick.RemoveAllListeners();
        highSpeedButton.onClick.AddListener(ToggleHighSpeed);

        // PlayerPrefs에서 저장된 상태 로드 (기본값: false)
        isHighSpeed = PlayerPrefs.GetInt(HIGH_SPEED_PREF_KEY, 0) == 1;

        // 초기 상태 적용
        ApplyHighSpeedState();

        Debug.Log($"BattleUI: High-speed button initialized. Current state: {(isHighSpeed ? "2x" : "1x")}");
    }

    /// <summary>
    /// 2배속 토글
    /// </summary>
    public void ToggleHighSpeed()
    {
        isHighSpeed = !isHighSpeed;

        // PlayerPrefs에 저장
        PlayerPrefs.SetInt(HIGH_SPEED_PREF_KEY, isHighSpeed ? 1 : 0);
        PlayerPrefs.Save();

        // 상태 적용
        ApplyHighSpeedState();

        Debug.Log($"BattleUI: High-speed toggled to {(isHighSpeed ? "2x" : "1x")}");
    }

    /// <summary>
    /// 2배속 상태 적용
    /// </summary>
    private void ApplyHighSpeedState()
    {
        // Time.timeScale 조정
        Time.timeScale = isHighSpeed ? 2f : 1f;

        // 버튼 텍스트 변경
        if (highSpeedButtonText != null)
        {
            highSpeedButtonText.text = isHighSpeed ? ">>>" : ">>";
        }

        // 버튼 배경색 변경 (선택적)
        if (highSpeedButtonImage != null)
        {
            highSpeedButtonImage.color = isHighSpeed ? new Color(1f, 0.8f, 0.2f, 1f) : Color.white;
        }
    }

    /// <summary>
    /// 씬 종료 시 Time.timeScale 복원
    /// </summary>
    private void OnDestroy()
    {
        // 다른 씬으로 이동할 때 timeScale을 1로 복원
        Time.timeScale = 1f;
    }

    // 라운드/턴 정보 업데이트
    public void UpdateRoundTurnText(int round, int turn)
    {
        if (roundTurnText != null)
        {
            roundTurnText.text = $"ROUND {round} - TURN {turn}";
        }
    }

    // 스킬 이름 표시
    public void ShowSkillName(bool isPlayer, string skillName)
    {
        if (skillNameText != null)
        {
            skillNameText.text = skillName;
            skillNameText.gameObject.SetActive(true);

            if (isPlayer)
            {
                // 연한 파란색
                skillNameText.color = new Color(0.4f, 0.6f, 1f);
            }
            else
            {
                // 연한 빨간색
                skillNameText.color = new Color(1f, 0.4f, 0.4f);
            }
            skillNameTimer = skillNameDisplayTime;
        }
    }

    // 스킬 이름 숨김
    public void HideSkillName()
    {
        if (skillNameText != null)
        {
            skillNameText.gameObject.SetActive(false);
        }
    }

    // 스킬 이름 즉시 숨김 (턴 종료 시 등)
    public void ClearSkillName()
    {
        skillNameTimer = 0f;
        HideSkillName();
    }

    public void ShowDefeatPanel()
    {
        if (defeatPanelPrefab != null)
        {
            GameObject defeatPanel = Instantiate(defeatPanelPrefab);
            Transform endPanel = transform.Find("EndPanel");
            defeatPanel.transform.SetParent(endPanel, false);
        }
    }

    public void ShowVictoryPanel()
    {
        if (victoryPanelPrefab != null)
        {
            GameObject victoryPanel = Instantiate(victoryPanelPrefab);
            Transform endPanel = transform.Find("EndPanel");
            victoryPanel.transform.SetParent(endPanel, false);
        }
    }

    /// <summary>
    /// 데미지 텍스트 표시
    /// </summary>
    /// <param name="damage">데미지 양</param>
    /// <param name="worldPosition">월드 좌표 (캐릭터 위치)</param>
    /// <param name="isCritical">크리티컬 여부</param>
    public void ShowDamageText(int damage, Vector3 worldPosition, bool isCritical = false)
    {
        if (damageTextPrefab == null)
        {
            Debug.LogWarning("BattleUI: damageTextPrefab is not assigned!");
            return;
        }

        // 캔버스 찾기
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("BattleUI: Canvas not found for damage text!");
            return;
        }

        // 데미지 텍스트 생성
        GameObject damageTextObj = Instantiate(damageTextPrefab, canvas.transform);

        // 월드 좌표를 스크린 좌표로 변환
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);

            // 스크린 좌표를 캔버스 로컬 좌표로 변환
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                out localPoint);

            damageTextObj.GetComponent<RectTransform>().localPosition = localPoint;
        }

        // DamageText 컴포넌트 설정
        DamageText damageText = damageTextObj.GetComponent<DamageText>();
        if (damageText != null)
        {
            damageText.Setup(damage, isCritical);
        }
    }
}

