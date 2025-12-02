using System;
using System.Collections;
using System.Collections.Generic;
using Arcana.Tactics;
using UnityEngine;
using static Arcana.Tactics.TacticsDataManager;

[DefaultExecutionOrder(-50)]
public class BattleManager : MonoBehaviour
{
    [Header("Positions")]
    public List<GameObject> playerPositions;
    public List<GameObject> enemyPositions;

    public GameObject dummyObject;

    private List<Character> playerCharacters = new List<Character>();
    private List<Character> enemyCharacters = new List<Character>();
    private List<Character> charactersTurnList = new List<Character>();
    private List<Character> waitingCharacters = new List<Character>();
    private int currentIndex = 0;
    private StrategyManager strategyManager;
    private SkillManager skillManager;
    private ClassManager classManager;
    private bool isWaitingForActionComplete = false;

    private int currentRound = 1;   // 현재 라운드
    private int currentTurn = 0;    // 현재 턴 (한 캐릭터가 행동할 때마다 증가)

    public static BattleManager Instance { get; private set; }

    // Awake는 Manager 초기화용
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

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

        // 플레이어 캐릭터 생성
        playerCharacters = CreateCharacters(true);
        // 적 캐릭터 생성
        enemyCharacters = CreateCharacters(false);

        // 테스트용 
        InitializeCharactersTurnList();

        // 1초 후 턴 시작
        yield return new WaitForSeconds(1f);

        StartCoroutine(ProcessCharactersTurn());
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
                        // 모델 경로 처리 (Assets/Resources/Models/... -> Models/...)
                        string modelPath = classInfo.model;
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

    // Update is called once per frame
    void Update()
    {

    }

    // 캐릭터들의 턴 리스트 초기화 (speed가 높은 순으로 정렬)
    public void InitializeCharactersTurnList()
    {
        charactersTurnList.Clear();
        currentIndex = 0;

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
            // 대기중인 캐릭터가 없으면 다음턴 진행
            isWaitingForActionComplete = false;
        }
    }

    // 캐릭터들의 턴을 진행한다. (코루틴)
    // 턴 진행 순서는 speed가 높은 순으로 정렬된 리스트를 사용한다.
    // 각 캐릭터는 OnCharacterActionFinished()가 호출될 때까지 대기
    private IEnumerator ProcessCharactersTurn()
    {
        Debug.Log($"=== 라운드 {currentRound} 시작 ===");
        UpdateBattleUI();

        while (true)
        {
            bool anyActionExecuted = false;

            // 모든 캐릭터가 행동할 수 없으면 라운드 종료
            for (int i = 0; i < charactersTurnList.Count; i++)
            {
                Character character = charactersTurnList[currentIndex];
                currentIndex = (currentIndex + 1) % charactersTurnList.Count;

                if (character == null)
                {
                    continue;
                }

                // 턴 증가
                currentTurn++;
                UpdateBattleUI();

                Debug.Log($"--- {character.characterName}의 턴 (Round {currentRound} - Turn {currentTurn}) ---");

                // 캐릭터 행동 실행
                StrategyAction action = character.RunAction();
                if (action != null)
                {
                    anyActionExecuted = true;
                    // 행동 완료 대기 플래그 설정
                    isWaitingForActionComplete = true;

                    // OnCharacterActionFinished()가 호출될 때까지 대기
                    yield return new WaitUntil(() => !isWaitingForActionComplete);

                    break; // 한 캐릭터가 행동하면 다음 루프로
                }
            }

            // 모든 캐릭터가 행동할 수 없으면 라운드 종료
            if (!anyActionExecuted)
            {
                Debug.Log("=== 모든 캐릭터가 행동할 수 없습니다. 라운드 종료 ===");
                OnRoundFinished();
                yield break; // 코루틴 종료
            }
        }
    }

    void OnRoundFinished()
    {
        Debug.Log("라운드 종료");

        // 라운드 종료 후 처리
        // 예: 다음 라운드 시작, 게임 종료 체크 등

        // 다음 라운드 시작 (테스트용)
        // currentRound++;
        // currentTurn = 0;
        // StartCoroutine(ProcessCharactersTurn());
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
    public void ShowSkillName(string skillName)
    {
        if (BattleUI.Instance != null)
        {
            BattleUI.Instance.ShowSkillName(skillName);
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
}
