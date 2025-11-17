using UnityEngine;

/// <summary>
/// HP 바 테스트를 위한 헬퍼 스크립트
/// 씬의 아무 오브젝트에나 추가하여 사용 가능
/// </summary>
public class HPBarTestHelper : MonoBehaviour
{
    [Header("Test Settings")]
    public Character[] testCharacters; // 테스트할 캐릭터들
    public float damageAmount = 10f;
    public float healAmount = 15f;
    
    [Header("Auto Test")]
    public bool autoTest = false; // 자동 테스트 활성화
    public float testInterval = 2f; // 테스트 간격 (초)
    
    private float testTimer;
    private bool isDamaging = true;
    
    void Start()
    {
        // 캐릭터 배열이 비어있으면 씬의 모든 캐릭터를 찾음
        if (testCharacters == null || testCharacters.Length == 0)
        {
            testCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
            Debug.Log($"테스트 캐릭터 자동 검색: {testCharacters.Length}명 발견");
        }
    }
    
    void Update()
    {
        // 수동 테스트 단축키
        if (Input.GetKeyDown(KeyCode.D))
        {
            DamageAllCharacters();
        }
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            HealAllCharacters();
        }
        
        if (Input.GetKeyDown(KeyCode.K))
        {
            KillAllCharacters();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAllCharacters();
        }
        
        // 자동 테스트
        if (autoTest)
        {
            testTimer += Time.deltaTime;
            
            if (testTimer >= testInterval)
            {
                testTimer = 0f;
                
                if (isDamaging)
                {
                    DamageAllCharacters();
                }
                else
                {
                    HealAllCharacters();
                }
                
                isDamaging = !isDamaging;
            }
        }
    }
    
    // 모든 캐릭터에게 데미지
    public void DamageAllCharacters()
    {
        foreach (Character character in testCharacters)
        {
            if (character != null)
            {
                character.TakeDamage(damageAmount);
                Debug.Log($"{character.characterName}에게 {damageAmount} 데미지");
            }
        }
    }
    
    // 모든 캐릭터 회복
    public void HealAllCharacters()
    {
        foreach (Character character in testCharacters)
        {
            if (character != null)
            {
                character.Heal(healAmount);
                Debug.Log($"{character.characterName}를 {healAmount} 회복");
            }
        }
    }
    
    // 모든 캐릭터 즉사
    public void KillAllCharacters()
    {
        foreach (Character character in testCharacters)
        {
            if (character != null)
            {
                character.TakeDamage(character.hp);
                Debug.Log($"{character.characterName} 즉사");
            }
        }
    }
    
    // 모든 캐릭터 HP 리셋
    public void ResetAllCharacters()
    {
        foreach (Character character in testCharacters)
        {
            if (character != null)
            {
                character.hp = character.maxHp;
                character.Heal(0); // HP 바 업데이트
                Debug.Log($"{character.characterName} HP 리셋");
            }
        }
    }
    
    // 특정 캐릭터에게 데미지
    public void DamageCharacter(int index, float damage)
    {
        if (index >= 0 && index < testCharacters.Length && testCharacters[index] != null)
        {
            testCharacters[index].TakeDamage(damage);
        }
    }
    
    // 특정 캐릭터 회복
    public void HealCharacter(int index, float amount)
    {
        if (index >= 0 && index < testCharacters.Length && testCharacters[index] != null)
        {
            testCharacters[index].Heal(amount);
        }
    }
    
    void OnGUI()
    {
        // 화면에 도움말 표시
        GUI.Box(new Rect(10, 10, 250, 120), "HP 바 테스트 단축키");
        GUI.Label(new Rect(20, 35, 230, 20), "D 키: 모든 캐릭터 데미지");
        GUI.Label(new Rect(20, 55, 230, 20), "H 키: 모든 캐릭터 회복");
        GUI.Label(new Rect(20, 75, 230, 20), "K 키: 모든 캐릭터 즉사");
        GUI.Label(new Rect(20, 95, 230, 20), "R 키: 모든 캐릭터 HP 리셋");
        GUI.Label(new Rect(20, 115, 230, 20), $"Auto Test: {(autoTest ? "ON" : "OFF")}");
    }
}

