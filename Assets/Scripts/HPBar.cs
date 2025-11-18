using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    [Header("HP Bar Components")]
    public Slider hpSlider;
    public Image fillImage;
    public TextMeshProUGUI textHP;
    
    [Header("Colors")]
    public Color highHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    
    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2.5f, 0); // 캐릭터 머리 위 오프셋
    public bool smoothTransition = true;
    public float smoothSpeed = 5f;
    
    private Transform targetCharacter;
    private Camera mainCamera;
    private float targetValue;
    
    void Start()
    {
        mainCamera = Camera.main;
        
        if (hpSlider == null)
        {
            hpSlider = GetComponent<Slider>();
        }
        
        if (fillImage == null && hpSlider != null)
        {
            fillImage = hpSlider.fillRect.GetComponent<Image>();
        }
        
        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = 1;
            targetValue = hpSlider.value;
        }
    }
    
    void LateUpdate()
    {
        // 캐릭터 위치를 따라가기
        if (targetCharacter != null)
        {
            transform.position = targetCharacter.position + offset;
            
            // 카메라를 향해 회전 (빌보드 효과)
            if (mainCamera != null)
            {
                transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                                mainCamera.transform.rotation * Vector3.up);
            }
        }
        
        // 부드러운 전환
        if (smoothTransition && hpSlider != null)
        {
            hpSlider.value = Mathf.Lerp(hpSlider.value, targetValue, Time.deltaTime * smoothSpeed);
        }
    }
    
    // HP 바 초기화
    public void Initialize(Transform character, float maxHp, float currentHp)
    {
        targetCharacter = character;
        
        if (hpSlider != null)
        {
            float hpPercent = currentHp / maxHp;
            hpSlider.value = hpPercent;
            targetValue = hpPercent;
            UpdateColor(hpPercent);
        }
    }
    
    // HP 업데이트
    public void UpdateHP(float currentHp, float maxHp)
    {
        if (hpSlider == null) return;
        
        float hpPercent = Mathf.Clamp01(currentHp / maxHp);
        
        if (smoothTransition)
        {
            targetValue = hpPercent;
        }
        else
        {
            hpSlider.value = hpPercent;
        }
        
        UpdateColor(hpPercent);
        textHP.text = $"{currentHp} / {maxHp}";
    }
    
    // HP 비율에 따른 색상 변경
    private void UpdateColor(float hpPercent)
    {
        if (fillImage == null) return;
        
        if (hpPercent > 0.6f)
        {
            fillImage.color = highHealthColor;
        }
        else if (hpPercent > 0.3f)
        {
            fillImage.color = Color.Lerp(lowHealthColor, midHealthColor, (hpPercent - 0.3f) / 0.3f);
        }
        else
        {
            fillImage.color = lowHealthColor;
        }
    }
    
    // 타겟 캐릭터 설정
    public void SetTarget(Transform character)
    {
        targetCharacter = character;
    }
    
    // 오프셋 설정
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    // HP 바 표시/숨김
    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }
}

