using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class BattleManager : MonoBehaviour
{
    public Character[] playerCharacters;
    public Character[] enemyCharacters;
    List<Character> charactersTurnList = new List<Character>();
    int currentIndex = 0;
    private StrategyManager strategyManager;
    private ActionManager actionManager;
    private bool isWaitingForActionComplete = false;

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
        
        // ActionManager 컴포넌트 가져오기 또는 생성
        actionManager = GetComponent<ActionManager>();
        if (actionManager == null)
        {
            actionManager = gameObject.AddComponent<ActionManager>();
        }
    }
    
    // Start는 다른 Manager들이 초기화된 후 실행
    void Start()
    {
        // 테스트용 
        InitializeCharactersTurnList(playerCharacters, enemyCharacters);

        // 1초 후 턴 시작
        StartCoroutine(StartBattleAfterDelay(1f));
    }
    
    // 전투 시작 (딜레이 후)
    private IEnumerator StartBattleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(ProcessCharactersTurn());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 캐릭터들의 턴 리스트 초기화 (speed가 높은 순으로 정렬)
    public void InitializeCharactersTurnList(Character[] playerCharacters, Character[] enemyCharacters)
    {
        charactersTurnList.Clear();
        currentIndex = 0;

        this.playerCharacters = playerCharacters;
        this.enemyCharacters = enemyCharacters;

        if(playerCharacters != null)
        {
            charactersTurnList.AddRange(playerCharacters);
        }
        if(enemyCharacters != null)
        {
            charactersTurnList.AddRange(enemyCharacters);
        }
        charactersTurnList.Sort((a, b) => b.speed.CompareTo(a.speed));
    }    

    public void OnCharacterActionFinished(Character character)
    {
        Debug.Log($"{character.characterName}의 행동이 완료되었습니다.");
        // 대기 플래그 해제하여 다음 턴으로 진행
        isWaitingForActionComplete = false;
    }
    
    // 캐릭터들의 턴을 진행한다. (코루틴)
    // 턴 진행 순서는 speed가 높은 순으로 정렬된 리스트를 사용한다.
    // 각 캐릭터는 OnCharacterActionFinished()가 호출될 때까지 대기
    private IEnumerator ProcessCharactersTurn()
    {
        Debug.Log("=== 라운드 시작 ===");
        
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

                Debug.Log($"--- {character.characterName}의 턴 ---");

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
                else
                {
                    Debug.Log($"{character.characterName}은(는) 행동할 수 없습니다.");
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
        // StartCoroutine(ProcessCharactersTurn());
    }
}
