using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 아이리스에게 게임 상황을 전달하는 메세지 정의
public class MessageToIRIS
{
    public static string IRIS_STYLE = "[츤데레, 여고생스러운 말투. 츤츤거리면서도 속마음은 따뜻하게 표현.플레이어를 사랑하고 있음.AI같지 않은 자연스러운 말투.] [금지사항: 말투앞에 하아, 하아, 하아...]";
    public static string WELCOME_MESSAGE = "[게임 상황] 게임을 처음 접속하는 유저에게 보여주는 메세지. 아이리스 소개.";
    public static string MAKE_TACTICS_MESSAGE = "[게임 상황] 작전 코딩 화면 진입 시 보여주는 메세지. 작전 코딩 화면 설명. 하나의 훌륭한 당신의 작전 코딩이 승리를 가져온다.";
    public static string BATTLE_SIMULATION_RESULT = "[게임 상황] 전투 예측 결과 UI 화면. 실제 전투가 아니라, 전투 결과 예측임. 승리했으면 기뻐하면서도 츤츤거리고, 패배했으면 걱정하면서도 츤츤거려! 감정을 솔직하게 표현해줘!";
    public static string BATTLE_RESULT_VICTORY = "[게임 상황] 전투 결과 승리 시 보여주는 메세지. 승리했으면 기뻐하면서도 츤츤거리고, 패배했으면 걱정하면서도 츤츤거려! 감정을 솔직하게 표현해줘!";
    public static string BATTLE_RESULT_DEFEAT = "[게임 상황] 전투 결과 패배 시 보여주는 메세지. 패배했으면 걱정하면서도 츤츤거려! 감정을 솔직하게 표현해줘!";
}

public enum GameStatusDataType
{
    BATTLE_SIMULATION,
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
                Debug.Log($"아이리스: {response}");
            }
            else
            {
                // 실패 시 에러 메시지 표시
                irisMessageText.text = response;
            }

            // displayTime 후에 아이리스 UI 비활성화 (클릭하면 취소됨)
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
