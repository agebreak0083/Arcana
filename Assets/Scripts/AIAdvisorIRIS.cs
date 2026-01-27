using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 사전 정의된 메시지를 사용하는 아리엘 AI 어드바이저 클래스
/// </summary>
public class AIAdvisorIRIS : MonoBehaviour
{
    private const int MaxResponseLength = 60; // 한글 기준 60자
    
    // 사전 정의된 아리엘 메시지 (조건별) - 차분하고 친절한 부관 참모 톤
    private readonly string[] victoryHighHPMessages = new string[]
    {
        "마왕님, 훌륭한 승리였어요. 전투력도 여유롭고 결과도 만족스러워요.",
        "잘하셨어요, 마왕님. 예상했던 대로 승리하셨네요. 정말 대단해요.",
        "승리 축하드려요. 다음 전투도 이렇게 잘하실 수 있을 거예요.",
        "마왕님의 전략이 완벽했어요. 이 정도면 충분히 만족스러운 결과예요.",
        "정말 잘하셨어요. 마왕님의 실력이 빛을 발했네요. 자랑스러워요."
    };    
    
    private readonly string[] defeatMessages = new string[]
    {
        "아쉽게 패배하셨네요. 걱정하지 마세요. 다음에는 더 나은 전략으로 승리하실 수 있을 거예요.",
        "이번엔 운이 좋지 않았나 봐요. 마왕님의 실력이니 다음엔 분명 이기실 거예요.",
        "패배했지만 포기하지 마세요. 작전을 다시 세워보시면 다음 전투에서 승리하실 수 있어요.",
        "아쉬운 결과네요. 전략을 조금 수정해보시면 더 좋은 결과가 나올 거예요.",
        "걱정하지 마세요. 이번엔 실수였을 뿐이에요. 다음엔 더 잘하실 수 있을 거예요."
    };
    
    private readonly string[] generalMessages = new string[]
    {
        "마왕님, 무엇을 도와드릴까요? 전투나 작전에 대해 궁금한 점이 있으시면 언제든 물어보세요.",
        "안녕하세요, 마왕님. 제가 도와드릴 수 있는 것이 있으면 말씀해 주세요.",
        "마왕님, 전투나 작전에 대해 궁금한 것이 있으시면 편하게 물어보세요. 제가 도와드릴게요.",
        "무엇이든 물어보세요. 마왕님을 위해 최선을 다해 도와드리겠어요.",
        "마왕님, 필요한 것이 있으시면 언제든 말씀해 주세요. 제가 곁에서 도와드릴게요."
    };
    
    private readonly string[] welcomeMessages = new string[]
    {
        "안녕하세요, 마왕님. 저는 아리엘이에요. 전투 참모로서 마왕님을 도와드리겠어요.",
        "처음 뵙겠어요, 마왕님. 아리엘이라고 해요. 앞으로 함께 전투를 준비해 나가요.",
        "마왕님, 안녕하세요. 저는 아리엘이에요. 전투에서 승리할 수 있도록 도와드리겠어요.",
        "반갑습니다, 마왕님. 아리엘이에요. 전투 참모로서 최선을 다해 도와드릴게요.",
        "마왕님, 만나서 기뻐요. 저는 아리엘이고, 앞으로 함께 전투를 준비해 나가요."
    };
    
    private readonly string[] tacticsMessages = new string[]
    {
        "각 캐릭터별로 작전을 잘 설정하시면 승리 확률이 높아져요. 신중하게 설정해 보세요.",
        "작전 코드를 제대로 설정하시면 전투에서 유리해져요. 캐릭터 특성에 맞춰서 설정하시는 게 좋아요.",
        "작전을 잘 짜시면 전투에서 승리할 수 있어요. 각 캐릭터의 역할을 고려해서 설정해 보세요.",
        "캐릭터마다 적절한 작전을 설정하는 것이 중요해요. 이것이 승리의 열쇠예요.",
        "작전 세팅이 승리의 핵심이에요. 각 캐릭터의 특성을 살려서 설정하시면 좋을 것 같아요."
    };
    
    private readonly string[] simulationVictoryMessages = new string[]
    {
        "시뮬레이션 결과 승리예요. 이대로 가시면 실제 전투에서도 승리하실 가능성이 높아요.",
        "시뮬레이션에서 승리하셨네요. 실제 전투도 이 정도면 좋은 결과가 나올 거예요.",
        "시뮬레이션 결과가 좋아요. 실제 전투에서도 승리하실 수 있을 것 같아요.",
        "시뮬레이션 승리 축하드려요. 실제 전투에서도 좋은 결과가 나올 거예요.",
        "시뮬레이션 결과 승리예요. 실제 전투도 기대해 볼 만해요."
    };
    
    private readonly string[] simulationDefeatMessages = new string[]
    {
        "시뮬레이션 결과가 아쉽네요. 작전을 다시 검토해 보시는 게 좋을 것 같아요.",
        "시뮬레이션에서 패배하셨네요. 실제 전투 전에 작전을 수정해 보시는 것을 권장드려요.",
        "시뮬레이션 결과가 좋지 않아요. 작전을 다시 세워 보시면 더 나은 결과가 나올 거예요.",
        "시뮬레이션 패배가 나왔네요. 실제 전투 전에 편성이나 작전을 조정해 보시는 게 어떨까요?",
        "시뮬레이션 결과가 아쉽네요. 작전을 다시 생각해 보시면 더 좋은 결과가 나올 거예요."
    };
    

    /// <summary>
    /// 아리엘과 대화하기 (사전 정의된 메시지 사용)
    /// </summary>
    /// <param name="userInput">사용자 입력 메시지 (현재는 사용하지 않음)</param>
    /// <param name="gameStatusData">게임 상태 데이터</param>
    /// <param name="onComplete">완료 시 호출되는 콜백 (성공 여부, 응답 텍스트)</param>
    public void ChatWithIris(string userInput, GameStatusData gameStatusData = null, Action<bool, string> onComplete = null)
    {
        // 사전 정의된 메시지 중에서 랜덤으로 선택
        string response = GetPredefinedMessage(gameStatusData);
        
        // 즉시 응답 (비동기 시뮬레이션을 위해 코루틴 사용)
        StartCoroutine(RespondWithDelay(response, onComplete));
    }
    
    /// <summary>
    /// 게임 상태에 따라 사전 정의된 메시지 선택
    /// </summary>
    private string GetPredefinedMessage(GameStatusData gameStatusData)
    {
        if (gameStatusData == null)
        {
            // 일반 대화 (gameStatusData가 null인 경우)
            return generalMessages[UnityEngine.Random.Range(0, generalMessages.Length)];
        }
        
        switch (gameStatusData.dataType)
        {
            case GameStatusDataType.BATTLE_SIMULATION:
                {
                    var battleData = gameStatusData as BattleSimulationGameStatusData;
                    if (battleData != null && battleData.battleSimulationResult != null)
                    {
                        var result = battleData.battleSimulationResult;
                        
                        // 시뮬레이션 전용 메시지 사용
                        if (result.isPlayerWin)
                        {
                            // 시뮬레이션 승리 메시지
                            return simulationVictoryMessages[UnityEngine.Random.Range(0, simulationVictoryMessages.Length)];
                        }
                        else
                        {
                            // 시뮬레이션 패배 메시지
                            return simulationDefeatMessages[UnityEngine.Random.Range(0, simulationDefeatMessages.Length)];
                        }
                    }
                    break;
                }
            case GameStatusDataType.BATTLE_RESULT_VICTORY:
                {
                    // 실제 전투 결과 - 승리
                    // HP 비율과 관계없이 승리 메시지만 표시 (victoryHighHPMessages 사용)
                    return victoryHighHPMessages[UnityEngine.Random.Range(0, victoryHighHPMessages.Length)];
                }
            case GameStatusDataType.BATTLE_RESULT_DEFEAT:
                {
                    // 실제 전투 결과 - 패배
                    return defeatMessages[UnityEngine.Random.Range(0, defeatMessages.Length)];
                }
            case GameStatusDataType.WELCOME_MESSAGE:
                {
                    // 게임 접속 시 인사 및 자기 소개
                    return welcomeMessages[UnityEngine.Random.Range(0, welcomeMessages.Length)];
                }
            case GameStatusDataType.TACTICS_MESSAGE:
                {
                    // 작전 코드 설명
                    return tacticsMessages[UnityEngine.Random.Range(0, tacticsMessages.Length)];
                }
        }
        
        // 기본값: 일반 대화
        return generalMessages[UnityEngine.Random.Range(0, generalMessages.Length)];
    }
    
    /// <summary>
    /// 약간의 지연 후 응답 (비동기 시뮬레이션)
    /// </summary>
    private IEnumerator RespondWithDelay(string response, Action<bool, string> onComplete)
    {
        // 자연스러운 응답 시간을 위해 약간의 지연 (0.1~0.3초)
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.3f));
        
        // 응답 길이 제한
        if (response.Length > MaxResponseLength)
        {
            response = TrimResponseToMaxLength(response);
        }
        
        onComplete?.Invoke(true, response);
    }

    /// <summary>
    /// 답변을 최대 길이로 스마트하게 자르기
    /// </summary>
    private string TrimResponseToMaxLength(string responseText)
    {
        if (string.IsNullOrEmpty(responseText) || responseText.Length <= MaxResponseLength)
        {
            return responseText;
        }

        // 마지막 문장 부호(., !, ?)를 찾아서 그 지점까지만 유지
        int lastSentenceEnd = -1;
        int searchEnd = Mathf.Min(MaxResponseLength, responseText.Length - 1);
        
        for (int i = searchEnd; i >= 0; i--)
        {
            if (responseText[i] == '.' || responseText[i] == '!' || responseText[i] == '?' ||
                responseText[i] == '。' || responseText[i] == '！' || responseText[i] == '？')
            {
                lastSentenceEnd = i + 1;
                break;
            }
        }

        if (lastSentenceEnd > 0)
        {
            return responseText.Substring(0, lastSentenceEnd);
        }

        // 문장 부호가 없으면 공백을 기준으로 자름
        int lastSpace = responseText.Substring(0, MaxResponseLength).LastIndexOf(' ');
        if (lastSpace > 0)
        {
            return responseText.Substring(0, lastSpace);
        }

        // 공백도 없으면 그냥 MaxResponseLength로 자름 (최후의 수단)
        return responseText.Substring(0, MaxResponseLength);
    }

}
