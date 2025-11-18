using UnityEngine;
using TMPro;

public class BattleUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI roundTurnText;   // 상단 라운드/턴 텍스트
    public TextMeshProUGUI skillNameText;   // 하단 스킬 이름 텍스트
    
    [Header("Animation Settings")]
    public float skillNameDisplayTime = 2f; // 스킬 이름 표시 시간
    
    private float skillNameTimer = 0f;
    
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
    public void ShowSkillName(string skillName)
    {
        if (skillNameText != null)
        {
            skillNameText.text = skillName;
            skillNameText.gameObject.SetActive(true);
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
}

