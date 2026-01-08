using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 행동 캐릭터(스킬 사용자 + 타겟)만 보이도록 하고, 나머지 캐릭터는 반투명 처리
/// 원본 Material 속성은 수정하지 않으므로 백업 불필요
/// </summary>
public class CharacterTransparencyManager : MonoBehaviour
{
    [Header("셰이더 설정")]
    [SerializeField] private string opaqueShaderName = "MMD4Mecanim/MMDLit-Edge";
    [SerializeField] private string transparentShaderName = "MMD4Mecanim/MMDLit-Transparent";
    
    [Header("투명도 설정")]
    [SerializeField] private float transparentAlpha = 0.1f;
    
    // 선택된 캐릭터 목록 (이들은 불투명하게 유지)
    private HashSet<Character> focusedCharacters = new HashSet<Character>();
    
    // 각 캐릭터의 원본 셰이더 및 Render Queue 저장 (복원용)
    private Dictionary<Character, Dictionary<Material, MaterialBackup>> materialBackups = new Dictionary<Character, Dictionary<Material, MaterialBackup>>();
    
    // 현재 반투명 상태인 캐릭터들
    private HashSet<Character> currentlyTransparent = new HashSet<Character>();
    
    // Render Queue 상수
    private const int RENDER_QUEUE_GEOMETRY = 2000;      // 불투명 (Opaque)
    private const int RENDER_QUEUE_TRANSPARENT = 3000;  // 반투명 (Transparent)
    
    private static CharacterTransparencyManager _instance;
    public static CharacterTransparencyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CharacterTransparencyManager>();
                if (_instance == null)
                {
                    GameObject managerObj = new GameObject("CharacterTransparencyManager");
                    _instance = managerObj.AddComponent<CharacterTransparencyManager>();
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 포커스할 캐릭터 설정 (스킬 사용자 + 타겟들)
    /// </summary>
    public void SetFocusedCharacters(Character user, List<Character> targets)
    {
        focusedCharacters.Clear();
        
        if (user != null)
            focusedCharacters.Add(user);
        
        if (targets != null)
        {
            foreach (var target in targets)
            {
                if (target != null)
                    focusedCharacters.Add(target);
            }
        }
        
        UpdateTransparency();
    }
    
    /// <summary>
    /// 포커스할 캐릭터 추가 (기존 focusedCharacters에 추가)
    /// </summary>
    public void AddFocusedCharacter(Character character)
    {
        if (character != null && !focusedCharacters.Contains(character))
        {
            focusedCharacters.Add(character);
            SetCharacterOpaque(character);
        }
    }
    
    /// <summary>
    /// 포커스 해제 (모든 캐릭터를 불투명하게)
    /// </summary>
    public void ClearFocus()
    {
        focusedCharacters.Clear();
        RestoreAllCharacters();
    }
    
    /// <summary>
    /// 모든 캐릭터 목록 가져오기
    /// </summary>
    private List<Character> GetAllCharacters()
    {
        List<Character> allCharacters = new List<Character>();
        
        if (BattleManager.Instance == null)
            return allCharacters;
        
        allCharacters.AddRange(BattleManager.Instance.playerCharacters);
        allCharacters.AddRange(BattleManager.Instance.enemyCharacters);
        
        // 유효한 캐릭터만 반환
        return allCharacters.Where(c => c != null && c.hp > 0).ToList();
    }
    
    /// <summary>
    /// 투명도 업데이트
    /// </summary>
    private void UpdateTransparency()
    {
        List<Character> allCharacters = GetAllCharacters();
        
        foreach (var character in allCharacters)
        {
            if (focusedCharacters.Contains(character))
            {
                // 포커스된 캐릭터는 불투명하게
                SetCharacterOpaque(character);
            }
            else
            {
                // 포커스되지 않은 캐릭터는 반투명하게
                SetCharacterTransparent(character);
            }
        }
    }
    
    /// <summary>
    /// 캐릭터를 반투명하게 설정
    /// </summary>
    private void SetCharacterTransparent(Character character)
    {
        if (character == null || currentlyTransparent.Contains(character))
            return;
        
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
        Shader transparentShader = Shader.Find(transparentShaderName);
        
        if (transparentShader == null)
        {
            Debug.LogError($"셰이더를 찾을 수 없습니다: {transparentShaderName}");
            return;
        }
        
        // 원본 셰이더 및 Render Queue 저장 (처음 한 번만)
        if (!materialBackups.ContainsKey(character))
        {
            materialBackups[character] = new Dictionary<Material, MaterialBackup>();
        }
        
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            // material 속성 사용 (인스턴스 생성, 원본 보호)
            Material[] materials = renderer.materials;
            
            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null) continue;
                
                // 원본 셰이더 및 Render Queue 저장 (처음 한 번만)
                if (!materialBackups[character].ContainsKey(mat))
                {
                    materialBackups[character][mat] = new MaterialBackup
                    {
                        shader = mat.shader,
                        renderQueue = mat.renderQueue
                    };
                }
                
                // 셰이더 변경
                mat.shader = transparentShader;
                
                // Render Queue를 Transparent로 설정 (불투명보다 나중에 렌더링)
                mat.renderQueue = RENDER_QUEUE_TRANSPARENT;
                
                // Alpha 값만 조정 (원본 속성은 그대로 유지)
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = transparentAlpha;
                    mat.color = color;
                }
            }
        }
        
        currentlyTransparent.Add(character);
    }
    
    /// <summary>
    /// 캐릭터를 불투명하게 설정
    /// </summary>
    private void SetCharacterOpaque(Character character)
    {
        if (character == null || !currentlyTransparent.Contains(character))
            return;
        
        if (!materialBackups.ContainsKey(character))
            return;
        
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
        
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            Material[] materials = renderer.materials;
            
            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null) continue;
                
                // 원본 셰이더 및 Render Queue로 복원
                if (materialBackups[character].ContainsKey(mat))
                {
                    var backup = materialBackups[character][mat];
                    mat.shader = backup.shader;
                    mat.renderQueue = backup.renderQueue;
                    
                    // Alpha 값 복원 (1.0으로)
                    if (mat.HasProperty("_Color"))
                    {
                        Color color = mat.color;
                        color.a = 1.0f;
                        mat.color = color;
                    }
                }
            }
        }
        
        currentlyTransparent.Remove(character);
    }
    
    /// <summary>
    /// 모든 캐릭터 복원
    /// </summary>
    private void RestoreAllCharacters()
    {
        foreach (var character in materialBackups.Keys.ToList())
        {
            SetCharacterOpaque(character);
        }
        
        currentlyTransparent.Clear();
    }
    
    /// <summary>
    /// Material 백업 데이터 구조
    /// </summary>
    private class MaterialBackup
    {
        public Shader shader;
        public int renderQueue;
    }
    
    private void OnDestroy()
    {
        RestoreAllCharacters();
    }
}
