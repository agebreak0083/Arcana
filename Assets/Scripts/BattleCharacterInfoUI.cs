using Arcana.Tactics;
using Arcana.Tactics.Data;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BattleCharacterInfoUI : MonoBehaviour
{
    public Image portraitImage;
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    private Character _character = null;
    
    [Header("Colors")]
    public Color highHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    
    [Header("HP Animation Settings")]
    public bool smoothTransition = true;
    public float smoothSpeed = 5f;
    
    private Image fillImage;
    private float targetHPPercent = 1f;
    private float targetCurrentHp = 100f;
    private float targetMaxHp = 100f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Slider의 fillImage 가져오기
        if (hpSlider != null && hpSlider.fillRect != null)
        {
            fillImage = hpSlider.fillRect.GetComponent<Image>();
        }
        
        // 초기 targetValue 설정
        if (hpSlider != null)
        {
            targetHPPercent = hpSlider.value;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_character != null)
        {
            // 실제 캐릭터 HP와 목표 HP가 다르면 업데이트
            if (Mathf.Abs(_character.hp - targetCurrentHp) > 0.01f || 
                Mathf.Abs(_character.maxHp - targetMaxHp) > 0.01f)
            {
                UpdateHP(_character.hp, _character.maxHp);
            }
        }
        
        // 부드러운 HP 전환
        if (smoothTransition && hpSlider != null)
        {
            float currentValue = hpSlider.value;
            hpSlider.value = Mathf.Lerp(currentValue, targetHPPercent, Time.deltaTime * smoothSpeed);
            
            // 현재 슬라이더 값에 비례하여 표시할 HP 계산
            float displayHp = targetMaxHp * hpSlider.value;
            
            hpText.text = $"{(int)displayHp} / {(int)targetMaxHp}";
            UpdateColor(hpSlider.value);
        }
    }

    public void SetCharacter(Character character)
    {
        _character = character;
        
        if (character == null)
        {
            // 캐릭터가 null이면 UI 초기화
            return;
        }
        
        // CharacterData에서 portrait를 가져온다
        CharacterData characterData = TacticsDataManager.Instance.availableCharacters?.Find(c => c.characterName == character.characterName);
        
        if (characterData != null && characterData.portrait != null)
        {
            portraitImage.sprite = characterData.portrait;
        }
        else
        {
            Debug.LogError($"Portrait not found for character: {character.characterName}");
        }

        UpdateHP(character.hp, character.maxHp);
    }

    public void UpdateHP(float currentHp, float maxHp)
    {
        if (hpSlider == null) return;
        
        float hpPercent = Mathf.Clamp01(currentHp / maxHp);
        
        // 목표 값 설정
        targetCurrentHp = currentHp;
        targetMaxHp = maxHp;
        targetHPPercent = hpPercent;
        
        if (smoothTransition)
        {
            // 부드러운 전환을 위해 targetValue만 설정 (실제 업데이트는 Update에서)
        }
        else
        {
            // 즉시 업데이트
            hpSlider.value = hpPercent;
            hpText.text = $"{(int)currentHp} / {(int)maxHp}";
            UpdateColor(hpPercent);
        }
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
}
