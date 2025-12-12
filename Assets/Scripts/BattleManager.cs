using System;
using System.Collections;
using System.Collections.Generic;
using Arcana.Tactics;
using Arcana.Tactics.Data;
using UnityEngine;
using static Arcana.Tactics.TacticsDataManager;

[DefaultExecutionOrder(-50)]
public class BattleManager : MonoBehaviour
{
    public BattleCameraController battleCameraController;


    [Header("Positions")]
    public List<GameObject> playerPositions;
    public List<GameObject> enemyPositions;

    [Header("UI Prefabs")]
    public GameObject hpBarPrefab; // HP 바 프리팹
    public Vector3 hpBarOffset = new Vector3(0, 1.2f, 0); // HP 바 위치 오프셋
    private HPBar hpBar; // HP 바 인스턴스

    [Header("Dummy Object")]
    public GameObject dummyObject;

    private List<Character> playerCharacters = new List<Character>();
    private List<Character> enemyCharacters = new List<Character>();
    private List<Character> charactersTurnList = new List<Character>();
    private List<Character> waitingCharacters = new List<Character>();
    private StrategyManager strategyManager;
    private SkillManager skillManager;
    private ClassManager classManager;
    private bool isWaitingForActionComplete = false;

    private int currentRound = 0;   // 현재 라운드
    private int currentTurn = 0;    // 현재 턴

    public static BattleManager Instance { get; private set; }

    // Awake는 Manager 초기화용
    void Awake()
    {
        // 씬마다 독립적인 인스턴스 사용
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
        // Dummy 게임 오브젝트 클리어 
        if (dummyObject != null)
        {
            Destroy(dummyObject);
        }

        // TacticsDataManager의 데이터 로딩 완료 대기 (Firebase 비동기 로딩 포함)
        Debug.Log("BattleManager: TacticsDataManager 데이터 로딩 대기 중...");
        yield return new WaitUntil(() => Arcana.Tactics.TacticsDataManager.Instance != null &&
                                         Arcana.Tactics.TacticsDataManager.Instance.isDataLoaded);
        Debug.Log("BattleManager: TacticsDataManager 데이터 로딩 완료!");

        // 플레이어 캐릭터 생성
        playerCharacters = CreateCharacters(true);
        // 적 캐릭터 생성
        enemyCharacters = CreateCharacters(false);

        // 턴 리스트 초기화
        InitializeCharactersTurnList();

        // 1초 후 전투 시작
        yield return new WaitForSeconds(1f);

        StartCoroutine(BattleRoutine());
    }

    private List<Character> CreateCharacters(bool isPlayer = true)
    {
        FormationLoadResult formationResult = null;
        List<GameObject> positions = null;
        List<Character> createdCharacters = new List<Character>();

        if (isPlayer)
        {
            formationResult = TacticsDataManager.Instance.GetPlayerFormationLoadResult();
            positions = playerPositions;
        }
        else
        {
            formationResult = TacticsDataManager.Instance.GetEnemyFormationLoadResult();
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

                    // ClassInfo에서 모델 경로 가져오기
                    var classInfo = Arcana.Tactics.TacticsDataManager.Instance.GetClassInfo(characterData.characterClass);
                    if (classInfo != null && !string.IsNullOrEmpty(classInfo.model))
                    {
                        // 모델 경로 처리 (... -> Models/...)
                        string modelPath = "Models/";
                        if (string.IsNullOrEmpty(characterData.model))
                        {
                            modelPath += classInfo.model;
                        }
                        else
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

                        GameObject prefab = Resources.Load<GameObject>(modelPath);
                        if (prefab != null)
                        {
                            GameObject charObj = Instantiate(prefab);

                            // 위치 설정
                            if (i < positions.Count && positions[i] != null)
                            {
                                charObj.transform.position = positions[i].transform.position;
                                charObj.transform.rotation = positions[i].transform.rotation * Quaternion.Euler(0, 90, 0);
                            }

                            // 캐릭터 클래스를 생성한다. 
                            Character character = charObj.AddComponent<Character>();
                            character.characterName = characterData.characterName;
                            character.className = characterData.characterClass;
                            character.position = i + 1;
                            character.hpBarPrefab = hpBarPrefab;
                            character.hpBarOffset = hpBarOffset;
                            character.originalPosition = positions[i].transform.position;
                            character.isPlayer = isPlayer;

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
                        else
                        {
                            Debug.LogError($"Failed to load model from Resources: {modelPath}");
                        }
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
        if (waitingCharacters.Count == 0)
        {
            // 대기중인 캐릭터가 없으면 다음턴 진행 (TurnRoutine에서 WaitUntil로 처리됨)
            isWaitingForActionComplete = false;
        }
    }

    // ==================================================================================
    // Battle Flow Logic
    // ==================================================================================

    // 메인 전투 루틴
    private IEnumerator BattleRoutine()
    {
        Debug.Log("=== 전투 시작 ===");
        currentRound = 0; // 라운드 초기화

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
            UserDataManager.Instance.AddTickets(BattleSetting.TICKET_FOR_LOSE);
            BattleUI.Instance.ShowDefeatPanel();
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

        UpdateBattleUI();
        yield return new WaitForSeconds(0.5f); // 라운드 시작 연출 대기

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
                yield return StartCoroutine(TurnRoutine(character, (result) => actionExecuted = result));

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

        // 3. Round End Phase
        yield return new WaitForSeconds(1.0f);
    }

    // 턴 루틴 (개별 캐릭터 행동)
    private IEnumerator TurnRoutine(Character character, System.Action<bool> onResult)
    {
        currentTurn++;
        Debug.Log($"--- {character.characterName}의 턴 (Round {currentRound} - Turn {currentTurn}) ---");

        // 턴 시작 로그를 먼저 출력 (행동 전에)
        if (BattleLogManager.Instance != null)
            BattleLogManager.Instance.LogTurnStart(character.characterName, currentRound, currentTurn);

        // 행동 실행 (RunAction 내부에서 조건 체크)
        StrategyAction action = character.RunAction();

        if (action != null)
        {
            UpdateBattleUI();

            // 카메라가 현재 턴 캐릭터를 따라가도록 설정
            if (battleCameraController != null)
            {
                battleCameraController.FollowCharacter(character);

                // 액션 타겟 찾기 (Character의 GetTarget 메서드 사용)
                Character targetChar = GetActionTarget(character, action);
                if (targetChar != null)
                {
                    battleCameraController.OnActionStart(character, targetChar);
                }
                else
                {
                    battleCameraController.OnActionStart(character);
                }
            }

            // 액션 종료 시 카메라 복귀
            if (battleCameraController != null)
            {
                battleCameraController.OnActionEnd();
            }

            // 턴 종료 이벤트 호출
            OnTurnEnd(character);

            // 행동 완료 대기
            isWaitingForActionComplete = true;
            yield return new WaitUntil(() => !isWaitingForActionComplete);

            onResult?.Invoke(true);
        }
        else
        {
            // 행동하지 않음 (조건 불만족, AP 부족 등)
            onResult?.Invoke(false);
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
            UserDataManager.Instance.AddTickets(BattleSetting.TICKET_FOR_LOSE);
            BattleUI.Instance.ShowDefeatPanel();
            Debug.Log("패배... (플레이어 전멸)");
            isBattleOver = true;
            return true;
        }
        if (!enemyAlive)
        {
            UserDataManager.Instance.AddTickets(BattleSetting.TICKET_FOR_WIN);
            BattleUI.Instance.ShowVictoryPanel();
            Debug.Log("승리! (적 전멸)");
            isBattleOver = true;
            return true;
        }

        UserDataManager.Instance.SaveUserData();

        return false;
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
    public PassiveSkillResult OnBeforeSkillUse(Character user, List<Character> targets, Skill skill)
    {
        passiveSkillResult.Initialize();

        // 모든 캐릭터에게 누가 누구에게 스킬을 썼는지 알려준다. 
        foreach (var character in charactersTurnList)
        {
            if (IsValidCharacter(character))
            {
                passiveSkillResult = character.OnBeforeSkillUse(user, targets, skill, passiveSkillResult);
            }
        }

        return passiveSkillResult;
    }

    public void OnAfterSkillUse(Character user, List<Character> targets, Skill skill)
    {
        // 모든 캐릭터에게 스킬 사용 후 이벤트를 호출한다. 
        foreach (var character in charactersTurnList)
        {
            if (IsValidCharacter(character))
            {
                passiveSkillResult = character.OnAfterSkillUse(user, targets, skill, passiveSkillResult);
            }
        }
    }
}

public class PassiveSkillResult
{
    public bool isGuard = false;
    public string guardLevel = "";
    public bool isSureHit = false;
    public Character passiveCharacter = null;

    public void Initialize()
    {
        isGuard = false;
        guardLevel = "";
        isSureHit = false;
        passiveCharacter = null;
    }
}
