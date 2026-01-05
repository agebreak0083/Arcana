using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arcana.Tactics;
using Arcana.Tactics.Data;
using UnityEngine;
using static Arcana.Tactics.TacticsDataManager;

public class BattleSimulationResult
{
    public static int randomSeed = 0;
    public bool isPlayerWin = false;
    public FormationLoadResult playerFormationLoadResult = null;
    public FormationLoadResult enemyFormationLoadResult = null;

    public string playerName = "";
    public string enemyName = "";
    public int playerHP_Max = 100;
    public int playerHP_Remaining = 100;
    public int enemyHP_Max = 100;
    public int enemyHP_Remaining = 100;

    // 랜덤 시드 값을 다음 값으로 설정한다.
    public static void NextRandomSeed() 
    {
        randomSeed = UnityEngine.Random.Range(0, 1000000);
        UnityEngine.Random.InitState(randomSeed);

        Debug.LogWarning("BattleSimulationResult: NextRandomSeed - randomSeed: " + randomSeed);
    }

    // 랜덤 시드 값을 현재 값으로 설정한다.
    public static void SetRandomSeed()
    {
        UnityEngine.Random.InitState(randomSeed);
    }
}

[DefaultExecutionOrder(-50)]
public class BattleManager : MonoBehaviour
{
    [HideInInspector] public bool isSimulationMode = false;
    private GameObject simulationObject;
    public BattleCameraController battleCameraController;
    public bool isAutoStart = false;


    [Header("Positions")]
    public List<GameObject> playerPositions;
    public List<GameObject> enemyPositions;

    [Header("UI Prefabs")]
    public GameObject hpBarPrefab; // HP 바 프리팹
    public Vector3 hpBarOffset = new Vector3(0, 1.2f, 0); // HP 바 위치 오프셋
    private HPBar hpBar; // HP 바 인스턴스

    public List<Character> playerCharacters = new List<Character>();
    public List<Character> enemyCharacters = new List<Character>();
    public FormationLoadResult playerFormationLoadResult;
    public FormationLoadResult enemyFormationLoadResult;
    private List<Character> charactersTurnList = new List<Character>();
    private List<Character> waitingCharacters = new List<Character>();
    private StrategyManager strategyManager;
    private SkillManager skillManager;
    private ClassManager classManager;
    public static BattleSimulationResult battleSimulationResult = new BattleSimulationResult();

    private int currentRound = 0;   // 현재 라운드
    private int currentTurn = 0;    // 현재 턴

    public static BattleManager Instance;

    // Awake는 Manager 초기화용
    void Awake()
    {
        Instance = this;        

        // StrategyManager 컴포넌트 가져오기 또는 생성
        strategyManager = GetComponent<StrategyManager>();
        if (strategyManager == null)
        {
            strategyManager = gameObject.AddComponent<StrategyManager>();
        }

        // SkillManager 컴포넌트 가져오기 또는 생성
        skillManager = GetComponent<SkillManager>();
        if (skillManager == null)
        {
            skillManager = gameObject.AddComponent<SkillManager>();
        }

        // ClassManager 컴포넌트 가져오기 또는 생성
        classManager = GetComponent<ClassManager>();
        if (classManager == null)
        {
            classManager = gameObject.AddComponent<ClassManager>();
        }
    }

    // Start는 다른 Manager들이 초기화된 후 실행
    IEnumerator Start()
    {
        // BattleSetting 로드 (구글 시트에서)
        yield return StartCoroutine(BattleSetting.LoadFromGoogleSheet());          

        if(isAutoStart)
        {
            yield return StartCoroutine(BattleModeStart());
        } 
    }
    
    public IEnumerator BattleModeStart()
    {
        isSimulationMode = false;

        BattleSetting.PrintAllSettings(BattleUI.Instance.debugText);

        // 플레이어 캐릭터 생성
        playerCharacters = CreateCharacters(true);
        // 적 캐릭터 생성
        enemyCharacters = CreateCharacters(false);

        // 턴 리스트 초기화
        InitializeCharactersTurnList();

        // 1초 후 전투 시작
        yield return new WaitForSeconds(1f);      
        BattleSimulationResult.SetRandomSeed();

        StartCoroutine(BattleRoutine());
    }

    public IEnumerator SimulationModeStart(string playerSquadName = "", string enemySquadName = "")
    {
        isSimulationMode = true;      
        BattleSimulationResult.SetRandomSeed();

        // 시뮬레이션 오브젝트 생성 
        if(simulationObject != null)
        {
            Destroy(simulationObject);
        }
        simulationObject = new GameObject("SimulationObject");        

        // 플레이어 캐릭터 생성
        playerCharacters = CreateCharacters(true, playerSquadName);
        // 적 캐릭터 생성
        enemyCharacters = CreateCharacters(false, enemySquadName);
        // 턴 리스트 초기화
        InitializeCharactersTurnList();        

        yield return StartCoroutine(BattleRoutine());
        
        battleSimulationResult.playerFormationLoadResult = playerFormationLoadResult;
        battleSimulationResult.enemyFormationLoadResult = enemyFormationLoadResult;
        battleSimulationResult.playerName = playerFormationLoadResult.username;
        battleSimulationResult.enemyName = enemyFormationLoadResult.username;
        battleSimulationResult.playerHP_Max = (int)playerCharacters.Sum(x => x.maxHp);
        battleSimulationResult.enemyHP_Max = (int)enemyCharacters.Sum(x => x.maxHp);
        battleSimulationResult.playerHP_Remaining = (int)playerCharacters.Sum(x => x.hp);
        battleSimulationResult.enemyHP_Remaining = (int)enemyCharacters.Sum(x => x.hp);

        isSimulationMode = false;
        yield break;
    }

    private List<Character> CreateCharacters(bool isPlayer = true, string squadName = "")
    {
        FormationLoadResult formationResult = null;
        List<GameObject> positions = null;
        List<Character> createdCharacters = new List<Character>();

        if (isPlayer)
        {
            if(string.IsNullOrEmpty(squadName))
            {
                playerFormationLoadResult = TacticsDataManager.Instance.LoadFormationFromTacticsFile(true);
            }
            else
            {
                playerFormationLoadResult = TacticsDataManager.Instance.LoadSquadTactics(squadName);
            }
            formationResult = playerFormationLoadResult;
            positions = playerPositions;
        }
        else
        {
            if(string.IsNullOrEmpty(squadName))
            {
                enemyFormationLoadResult = TacticsDataManager.Instance.GetEnemyFormationLoadResult();
            }
            else
            {
                enemyFormationLoadResult = TacticsDataManager.Instance.LoadSquadTactics(squadName);
            }
            formationResult = enemyFormationLoadResult;
            positions = enemyPositions;
        }

        // Formation 로드 및 캐릭터 생성        
        if (formationResult != null && formationResult.unitSlots != null)
        {
            for (int i = 0; i < formationResult.unitSlots.Length; i++)
            {
                var characterData = formationResult.unitSlots[i];

                if (characterData != null)
                {
                    Debug.Log($"Character Data: {characterData.characterClass}");

                    // 모델 경로 처리 (... -> Models/...)
                    string modelPath = "Models/";
                    if (!string.IsNullOrEmpty(characterData.model))
                    {
                        modelPath += characterData.model;
                    }

                    if (modelPath.StartsWith("Assets/Resources/"))
                    {
                        modelPath = modelPath.Substring("Assets/Resources/".Length);
                    }
                    if (modelPath.EndsWith(".prefab"))
                    {
                        modelPath = modelPath.Substring(0, modelPath.Length - ".prefab".Length);
                    }

                    Character character = null;

                        if(isSimulationMode)
                        {
                            character = simulationObject.AddComponent<Character>();
                        }
                        else                                                   
                        {
                            GameObject prefab = Resources.Load<GameObject>(modelPath);
                            if(prefab == null)
                            {
                                Debug.LogError($"Failed to load model from Resources: {modelPath}");
                                continue;
                            }

                            GameObject charObj = Instantiate(prefab);

                            // 위치 설정
                            if (i < positions.Count && positions[i] != null)
                            {
                                charObj.transform.position = positions[i].transform.position;
                                charObj.transform.rotation = positions[i].transform.rotation * Quaternion.Euler(0, 90, 0);
                            }

                            character = charObj.AddComponent<Character>();
                        }

                        // 캐릭터 클래스를 생성한다.                             
                        character.characterName = characterData.characterName;
                        character.className = characterData.characterClass;
                        character.position = i + 1;
                        character.hpBarPrefab = hpBarPrefab;
                        character.hpBarOffset = hpBarOffset;                        
                        character.isPlayer = isPlayer;

                        if(positions != null && i < positions.Count && positions[i] != null)
                        {
                            character.originalPosition = positions[i].transform.position;
                        }                        

                        // 클래스 스탯 적용
                        character.ApplyClassStatsToCharacter();

                        // 작전 설정
                        if (formationResult.codingData.TryGetValue(characterData.id, out var plan))
                        {
                            Strategy strategy = StrategyManager.Instance.CreateStrategy(plan);
                            character.SetStrategy(strategy);
                        }

                        createdCharacters.Add(character);                        
                    }
                }
            }
        }

        return createdCharacters;
    }

    // 캐릭터들의 턴 리스트 초기화 (speed가 높은 순으로 정렬)
    public void InitializeCharactersTurnList()
    {
        charactersTurnList.Clear();

        if (playerCharacters != null)
        {
            charactersTurnList.AddRange(playerCharacters);
        }
        if (enemyCharacters != null)
        {
            charactersTurnList.AddRange(enemyCharacters);
        }

        // speed가 높은 순으로 정렬 (null 체크 포함)
        if (charactersTurnList.Count > 0)
        {
            charactersTurnList.Sort((a, b) =>
            {
                if (a == null || a.stats == null) return 1;
                if (b == null || b.stats == null) return -1;
                return b.stats.GetActionSpeedValue().CompareTo(a.stats.GetActionSpeedValue());
            });
        }
    }

    public void OnCharacterActionFinished(Character character)
    {
        Debug.Log($"{character.characterName}의 행동이 완료되었습니다.");

        waitingCharacters.Remove(character);        
    }

    // ==================================================================================
    // Battle Flow Logic
    // ==================================================================================

    // 메인 전투 루틴
    private IEnumerator BattleRoutine()
    {
        Debug.Log("=== 전투 시작 ===");
        currentRound = 0; // 라운드 초기화
        isBattleOver = false;

        while (!CheckBattleOver())
        {
            yield return StartCoroutine(RoundRoutine());
        }

        Debug.Log("=== 전투 종료 ===");
    }

    // 라운드 루틴
    private IEnumerator RoundRoutine()
    {
        // 1. Round Start Phase
        currentRound++;
        currentTurn = 0;
        Debug.Log($"=== 라운드 {currentRound} 시작 ===");

        // 최대 라운드 초과 체크
        if (currentRound > BattleSetting.MAX_ROUNDS)
        {
            Debug.Log($"패배... (최대 라운드 {BattleSetting.MAX_ROUNDS} 초과)");
            SetPlayerWinLose(false);
            
            isBattleOver = true;
            yield break;
        }

        if (BattleLogManager.Instance != null)
            BattleLogManager.Instance.LogRoundStart(currentRound);

        // 라운드 시작 시 모든 캐릭터 AP/PP 회복
        foreach (var character in charactersTurnList)
        {
            if (IsValidCharacter(character))
            {
                character.RestoreAPPP();
            }
        }

        // 라운드 시작 시 모든 캐릭터의 Buff/Debuff 제거
        foreach (var character in charactersTurnList)
        {
            if (IsValidCharacter(character))
            {
                character.RemoveAllBuffs();
            }
        }

        if(!isSimulationMode)
        {
            UpdateBattleUI();
            yield return new WaitForSeconds(0.5f); // 라운드 시작 연출 대기            
        }

        // 2. Action Phase
        bool roundActive = true;
        while (roundActive)
        {
            bool anyActionInCycle = false;

            // 속도 순으로 정렬된 리스트 순회 (한 사이클)
            for (int i = 0; i < charactersTurnList.Count; i++)
            {
                Character character = charactersTurnList[i];
                if (!IsValidCharacter(character)) continue;

                // 턴 실행 시도
                bool actionExecuted = false;
                
                // 시뮬레이션 모드에서는 동기적으로 처리 (WebGL 성능 최적화)
                if (isSimulationMode)
                {
                    actionExecuted = TurnRoutineSync(character);
                }
                else
                {
                    yield return StartCoroutine(TurnRoutine(character, (result) => actionExecuted = result));
                }

                if (actionExecuted)
                {
                    anyActionInCycle = true;
                }

                // 행동 후 전투 종료 조건 체크
                if (CheckBattleOver())
                {
                    roundActive = false;
                    break;
                }
            }

            // 한 사이클 동안 아무도 행동하지 않았다면 라운드 종료 (모두 AP 소진 등)
            if (!anyActionInCycle)
            {
                roundActive = false;
                Debug.Log($"라운드 {currentRound} 종료: 더 이상 행동 가능한 캐릭터가 없습니다.");
            }
        }

        if(!isSimulationMode)
        {
            // 3. Round End Phase
            yield return new WaitForSeconds(1.0f);
        }
    }

    // 턴 루틴 동기 버전 (시뮬레이션 모드용, WebGL 성능 최적화)
    private bool TurnRoutineSync(Character character)
    {
        currentTurn++;
        
        // 행동 실행 (동기 버전)
        StrategyAction action = character.RunActionSync();

        if (action != null)
        {
            // 턴 종료 이벤트 호출
            OnTurnEnd(character);
            return true;
        }
        else
        {
            // 행동하지 않음 (조건 불만족, AP 부족 등)
            return false;
        }
    }

    // 턴 루틴 (개별 캐릭터 행동)
    private IEnumerator TurnRoutine(Character character, System.Action<bool> onResult)
    {
        currentTurn++;
        
        // 시뮬레이션 모드에서는 Debug.Log 최소화 (WebGL 성능 최적화)
        if (!isSimulationMode)
        {
            Debug.Log($"--- {character.characterName}의 턴 (Round {currentRound} - Turn {currentTurn}) ---");
        }

        // 턴 시작 로그를 먼저 출력 (행동 전에)
        if (BattleLogManager.Instance != null && !isSimulationMode)
            BattleLogManager.Instance.LogTurnStart(character.characterName, currentRound, currentTurn);

        // 행동 실행 (RunAction 내부에서 조건 체크 및 애니메이션 완료까지 대기)
        StrategyAction action = null;
        yield return StartCoroutine(character.RunAction((result) => action = result));

        if (action != null)
        {
            if(!isSimulationMode)
            {
                UpdateBattleUI();            
            }

            // 턴 종료 이벤트 호출
            OnTurnEnd(character);

            // 시뮬레이션 모드에서는 waitingCharacters 체크 건너뛰기 (WebGL 성능 최적화)
            if (!isSimulationMode)
            {
                // 행동 완료 대기 (모든 waitingCharacters가 완료될 때까지)            
                yield return new WaitUntil(() => waitingCharacters.Count == 0);
            }

            onResult?.Invoke(true);
        }
        else
        {
            // 행동하지 않음 (조건 불만족, AP 부족 등)
            onResult?.Invoke(false);
        }
    }

    public void CameraActionStart(Character character, Character target)
    {
        if (battleCameraController != null)
        {
            battleCameraController.FollowCharacter(character);
            battleCameraController.OnActionStart(character, target);
        }
    }

    public void CameraActionEnd()
    {
        if (battleCameraController != null)
        {
            battleCameraController.OnActionEnd();
        }
    }

    private void OnTurnEnd(Character targetCharacter)
    {
        // 모든 캐릭터에게 턴 종료 이벤트를 호출한다. 
        foreach (var character in charactersTurnList)
        {
            if (IsValidCharacter(character))
            {
                character.OnTurnEnd(targetCharacter);
            }
        }
    }

    /// <summary>
    /// 액션의 타겟 캐릭터를 가져옴 (Character의 GetTarget 로직 재사용)
    /// </summary>
    private Character GetActionTarget(Character actor, StrategyAction action)
    {
        // Character의 GetTarget 메서드와 동일한 로직 사용
        List<Character> originalTargets = GetEnemyTargets(actor);
        List<Character> candidates = new List<Character>(originalTargets);

        // 사망한 캐릭터 제거
        candidates.RemoveAll(c => c == null || c.hp <= 0);

        if (candidates.Count == 0)
        {
            return null;
        }

        // Condition2 적용 (필터링)
        if (!string.IsNullOrEmpty(action.condition2) && action.condition2 != "조건 없음")
        {
            var filter = TargetConditionFactory.CreateFilter(action.condition2);
            if (filter != null)
            {
                var filtered = filter.Filter(candidates, actor);
                if (filtered.Count > 0)
                {
                    candidates = filtered;
                }
                else
                {
                    return null;
                }
            }
        }

        // Condition1 적용 (선택)
        var selector = TargetConditionFactory.CreateSelector(action.condition1);
        List<Character> targets = selector.Select(candidates, actor);
        if (targets.Count > 0)
        {
            return targets[0];
        }
        return null;
    }

    bool isBattleOver = false;
    // 전투 종료 조건 체크
    private bool CheckBattleOver()
    {
        if (isBattleOver)
        {
            return true;
        }

        bool playerAlive = false;
        bool enemyAlive = false;

        if (playerCharacters != null) playerAlive = playerCharacters.Exists(c => c.hp > 0);
        if (enemyCharacters != null) enemyAlive = enemyCharacters.Exists(c => c.hp > 0);

        if (!playerAlive)
        {
            SetPlayerWinLose(false);
            isBattleOver = true;

            return true;
        }
        if (!enemyAlive)
        {
            SetPlayerWinLose(true);
            isBattleOver = true;

            return true;
        }        

        UserDataManager.Instance.SaveUserData();

        return false;
    }

    void SetPlayerWinLose(bool isPlayerWin)
    {
        if(isSimulationMode)
        {
            battleSimulationResult.isPlayerWin = isPlayerWin;
            Debug.Log("BattleManager: SetPlayerWinLose - " + (isPlayerWin ? "승리" : "패배"));
        }
        else
        {
            if(isPlayerWin)
            {
                UserDataManager.Instance.AddTickets(BattleSetting.TICKET_FOR_WIN);
                if(BattleUI.Instance != null)
                {
                    BattleUI.Instance.ShowVictoryPanel();                    
                }            

                // 아이리스 메세지 출력 
                IRISUIManager.Instance.ShowIrisUI(MessageToIRIS.BATTLE_RESULT_VICTORY);

                // 이겼을때만, 서버에 택틱스 저장 
                string tacticsJson = TacticsDataManager.Instance.GetTacticsJson(playerFormationLoadResult.unitSlots, playerFormationLoadResult.codingData);
                TacticsDataManager.Instance.SavePlayerTacticsToServer(tacticsJson, (success) =>
                {
                    Debug.Log("BattleManager: SavePlayerTacticsToServer - " + (success ? "Success" : "Failed"));
                });
            }            
            else
            {
                UserDataManager.Instance.AddTickets(BattleSetting.TICKET_FOR_LOSE);
                if(BattleUI.Instance != null)
                {
                    BattleUI.Instance.ShowDefeatPanel();
                }

                // 아이리스 메세지 출력 
                IRISUIManager.Instance.ShowIrisUI(MessageToIRIS.BATTLE_RESULT_DEFEAT);
            }

            // 서버 스코어 업데이트 (player, enemy 모두)
            TacticsDataManager.Instance.UpdateScore(playerFormationLoadResult.username, isPlayerWin ? 1 : 0, isPlayerWin ? 0 : 1);
            TacticsDataManager.Instance.UpdateScore(enemyFormationLoadResult.username, isPlayerWin ? 0 : 1, isPlayerWin ? 1 : 0);
            
            int newScore = UserDataManager.Instance.UdpateScore(isPlayerWin ? 1 : 0, isPlayerWin ? 0 : 1);
            TacticsDataManager.Instance.GetRanking(newScore, (ranking) =>
            {
                UserDataManager.Instance.currentUserData.ranking = ranking;
                UserDataManager.Instance.SaveUserData();
            });
            
            // 시뮬레이션 모드가 아니라 Battle Scene이라면, 끝나면 시뮬레이션 결과 리셋. 
            battleSimulationResult = new BattleSimulationResult();
            BattleSimulationResult.NextRandomSeed();

            // BattleMap이라면 승패 결과를 저장한다.
            if(BattleMapManager.Instance != null) 
            {
                BattleMapManager.Instance.SetPlayerWinLose(isPlayerWin);
            }
        }
    }

    private bool IsValidCharacter(Character character)
    {
        return character != null && character.hp > 0;
    }

    // UI 업데이트
    private void UpdateBattleUI()
    {
        if (BattleUI.Instance != null)
        {
            BattleUI.Instance.UpdateRoundTurnText(currentRound, currentTurn);
        }
    }

    // 스킬 이름 표시
    public void ShowSkillName(bool isPlayer, string skillName)
    {
        if (BattleUI.Instance != null)
        {
            BattleUI.Instance.ShowSkillName(isPlayer, skillName);
        }
    }

    // player가 포함되지 않은, 상대방 타겟들 배열을 반환한다.
    public List<Character> GetEnemyTargets(Character player)
    {
        List<Character> targetCharacters = null;
        if (playerCharacters.Contains(player))
        {
            targetCharacters = enemyCharacters;
        }
        else
        {
            targetCharacters = playerCharacters;
        }

        return targetCharacters;
    }

    public void AddWaitFinished(Character character)
    {
        waitingCharacters.Add(character);
    }

    public PassiveSkillResult passiveSkillResult = new PassiveSkillResult();
    // 시뮬레이션 모드용 동기 버전 (WebGL 성능 최적화)
    public void OnBeforeSkillUseSync(Character user, List<Character> targets, Skill skill)
    {
        // 모든 캐릭터에게 누가 누구에게 스킬을 썼는지 알려준다 (동기 버전)
        foreach (var character in charactersTurnList)
        {
            if (IsValidCharacter(character) && character.stats.passivePoint > 0)
            {
                character.OnBeforeSkillUseSync(user, targets, skill, passiveSkillResult);
            }
        }
    }

    public void OnAfterSkillUseSync(Character user, List<Character> targets, Skill skill)
    {
        // 모든 캐릭터에게 스킬 사용 후 이벤트를 호출한다 (동기 버전)
        foreach (var character in charactersTurnList)
        {
            if (IsValidCharacter(character) && character.stats.passivePoint > 0)
            {
                character.OnAfterSkillUseSync(user, targets, skill, passiveSkillResult);
            }
        }
    }

    public IEnumerator OnBeforeSkillUse(Character user, List<Character> targets, Skill skill)
    {
        // 시뮬레이션 모드에서는 동기 버전 사용 (WebGL 성능 최적화)
        if (isSimulationMode)
        {
            OnBeforeSkillUseSync(user, targets, skill);
            yield break;
        }

        // 모든 캐릭터에게 누가 누구에게 스킬을 썼는지 알려준다. 
        foreach (var character in charactersTurnList)
        {
            if (IsValidCharacter(character))
            {
                yield return StartCoroutine(character.OnBeforeSkillUse(user, targets, skill, passiveSkillResult));
            }
        }        
    }

    public IEnumerator OnAfterSkillUse(Character user, List<Character> targets, Skill skill)
    {
        // 시뮬레이션 모드에서는 동기 버전 사용 (WebGL 성능 최적화)
        if (isSimulationMode)
        {
            OnAfterSkillUseSync(user, targets, skill);
            yield break;
        }

        // 모든 캐릭터에게 스킬 사용 후 이벤트를 호출한다. 
        foreach (var character in charactersTurnList)
        {
            if (IsValidCharacter(character))
            {
                yield return StartCoroutine(character.OnAfterSkillUse(user, targets, skill, passiveSkillResult));
            }
        }
    }

    // 임시용 방어 코드. BattleMap에 시뮬을 돌리기 위한, 싱글톤 사용 회피 땜빵 코드 
    public void SetInstanceSelf()
    {
        Instance = this;
        StrategyManager.Instance = strategyManager;
        SkillManager.Instance = skillManager;
        ClassManager.Instance = classManager;
    }
}

public class PassiveSkillResult
{
    public bool isGuard = false;
    public string guardLevel = "";
    public bool isSureHit = false;
    public Character passiveCharacter = null;
    public List<SkillEffect> enchantEffects = new List<SkillEffect>();

    public void Initialize()
    {
        isGuard = false;
        guardLevel = "";
        isSureHit = false;
        passiveCharacter = null;
        enchantEffects.Clear();
    }

    public float GetEnchantMagicalAttackValue()
    {
        float value = 0f;
        foreach (var enchantEffect in enchantEffects)
        {
            if (enchantEffect.stat == "magical_attack")
            {
                value += enchantEffect.value;
            }
        }
        return value;
    }

    public float GetEnchantPhysicalAttackValue()
    {
            float value = 0f;
            foreach (var enchantEffect in enchantEffects)
            {
                if (enchantEffect.stat == "physical_attack")
                {
                    value += enchantEffect.value;
                }
            }
            return value;
    }
}
