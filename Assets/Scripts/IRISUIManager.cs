using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 아리엘에게 게임 상황을 전달하는 메세지 정의
public class MessageToIRIS
{
    public static string IRIS_STYLE = "[츤데레 말투. 속마음은 따뜻함. 금지:하아]";
    public static string WELCOME_MESSAGE = "[상황] 처음 접속. 아리엘 소개.";
    public static string MAKE_TACTICS_MESSAGE = "[상황] 작전 코딩 화면. 작전 설명.";
    public static string BATTLE_SIMULATION_RESULT = "[상황] 전투 예측 결과. 승리면 기뻐하며 츤츤, 패배면 걱정하며 츤츤.";
    public static string BATTLE_RESULT_VICTORY = "[상황] 전투 승리. 기뻐하며 츤츤.";
    public static string BATTLE_RESULT_DEFEAT = "[상황] 전투 패배. 걱정하며 츤츤.";
}

public enum GameStatusDataType
{
    BATTLE_SIMULATION,
    BATTLE_RESULT_VICTORY,
    BATTLE_RESULT_DEFEAT,
    WELCOME_MESSAGE,
    TACTICS_MESSAGE,
}
public class GameStatusData
{
    public GameStatusDataType dataType;    
}

public class BattleSimulationGameStatusData : GameStatusData
{
    public BattleSimulationResult battleSimulationResult;
    public BattleSimulationGameStatusData(BattleSimulationResult battleSimulationResult)
    {
        dataType = GameStatusDataType.BATTLE_SIMULATION;
        this.battleSimulationResult = battleSimulationResult;
    }    
}

public class WelcomeGameStatusData : GameStatusData
{
    public WelcomeGameStatusData()
    {
        dataType = GameStatusDataType.WELCOME_MESSAGE;
    }
}

public class TacticsGameStatusData : GameStatusData
{
    public TacticsGameStatusData()
    {
        dataType = GameStatusDataType.TACTICS_MESSAGE;
    }
}

public class BattleResultGameStatusData : GameStatusData
{
    public bool isPlayerWin;
    public int playerHP_Max;
    public int playerHP_Remaining;

    public BattleResultGameStatusData(bool isPlayerWin, int playerHP_Max, int playerHP_Remaining)
    {
        dataType = isPlayerWin ? GameStatusDataType.BATTLE_RESULT_VICTORY : GameStatusDataType.BATTLE_RESULT_DEFEAT;
        this.isPlayerWin = isPlayerWin;
        this.playerHP_Max = playerHP_Max;
        this.playerHP_Remaining = playerHP_Remaining;
    }
}

public class IRISUIManager : MonoBehaviour
{
    public Canvas irisCanvas;
    public TextMeshProUGUI irisMessageText;
    public float displayTime = 3f;
    public float loadingAnimationInterval = 0.5f; // 로딩 애니메이션 변경 간격
    public static IRISUIManager Instance { get; private set; }    
    private AIAdvisorIRIS aiAdvisorIRIS;
    public Button messageButton; // 메시지 창 클릭용 Button
    private Coroutine loadingCoroutine; // 로딩 애니메이션 코루틴
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        aiAdvisorIRIS = GetComponent<AIAdvisorIRIS>();
        
        messageButton.onClick.AddListener(HideIrisUI);
    }

    public void ShowIrisUI( string message, GameStatusData gameStatusData = null)
    { 
        Debug.Log($"ShowIrisUI: {message}");

        // 0. 이전 HideIrisUI Invoke 취소 (중요: 새로운 호출 시 이전 타이머 제거)
        CancelInvoke("HideIrisUI");

        // 1. 먼저 Canvas를 활성화
        irisCanvas.gameObject.SetActive(true);
        
        // 2. 기존 로딩 코루틴이 있으면 중지
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }
        
        // 3. 로딩 애니메이션 시작
        loadingCoroutine = StartCoroutine(LoadingAnimationCoroutine());

        // 4. AI 응답 요청
        message = MessageToIRIS.IRIS_STYLE + message;
        aiAdvisorIRIS.ChatWithIris(message, gameStatusData, (success, response) =>
        {
            // 5. 로딩 애니메이션 중지
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }
            
            if(success)
            {
                // 6. 응답이 오면 텍스트 표시
                irisMessageText.text = response;
                Debug.Log($"아리엘: {response}");
            }
            else
            {
                // 실패 시 에러 메시지 표시
                irisMessageText.text = response;
            }

            // displayTime 후에 아리엘 UI 비활성화 (클릭하면 취소됨)
            CancelInvoke("HideIrisUI"); // 기존 Invoke 취소
            Invoke("HideIrisUI", displayTime);  
        });
    }
    
    /// <summary>
    /// 로딩 애니메이션 코루틴 (".", "..", "..." 반복)
    /// </summary>
    private System.Collections.IEnumerator LoadingAnimationCoroutine()
    {
        string[] loadingFrames = { ".", "..", "..." };
        int currentFrame = 0;
        
        while (true)
        {
            irisMessageText.text = loadingFrames[currentFrame];
            currentFrame = (currentFrame + 1) % loadingFrames.Length;
            yield return new WaitForSeconds(loadingAnimationInterval);
        }
    }

    public void HideIrisUI()
    {
        CancelInvoke("HideIrisUI"); // Invoke 취소
        
        // 로딩 코루틴 중지
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
        
        irisCanvas.gameObject.SetActive(false); 
    }    
}
