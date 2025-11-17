using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public Character[] playerCharacters;
    public Character[] enemyCharacters;
    List<Character> charactersTurnList = new List<Character>();
    int currentIndex = 0;
    private StrategyManager strategyManager;

    public static BattleManager Instance { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
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

        // 테스트용 
        InitializeCharactersTurnList(playerCharacters, enemyCharacters);

        Invoke("ProcessCharactersTurn", 1f);
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
        bool bFinished = !ProcessCharactersTurn();
        if(bFinished)
        {
            OnRoundFinished();
        }
    }
    
    // 캐릭터들의 턴을 진행한다. 
    // 턴 진행 순서는 speed가 높은 순으로 정렬된 리스트를 사용한다. 현재 진행 순서를 기록해서, 함수 호출할때마다 다음 캐릭터의 턴을 진행한다. 
    // 턴 진행 순서는 턴 진행 중에 변경되지 않는다.
    // 리스트의 마지막에 도착하면 다시 처음 부터 시작한다. 즉, 리스트를 순환하며 턴을 진행한다.
    public bool ProcessCharactersTurn()
    {
        int finishedCount = 0;

        // 모든 캐릭터가 행동할 수 없으면(행동이 다 소모됐으면) 라운드를 종료한다.
        while(finishedCount < charactersTurnList.Count)
        {
            Character character = charactersTurnList[currentIndex];
            currentIndex = (currentIndex + 1) % charactersTurnList.Count;

            if(character == null) 
                continue;
            
            StrategyAction action = character.RunAction();
            if(action != null)
            {
                return true;
            }
            else
            {
                finishedCount++;
                continue;
            }
        }        

        return false;
    }        

    void OnRoundFinished()
    {
        Debug.Log("라운드 종료");
    }
}
