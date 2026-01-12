using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 사전 정의된 메시지를 사용하는 아리엘 AI 어드바이저 클래스
/// </summary>
public class AIAdvisorIRIS : MonoBehaviour
{
    private const int MaxResponseLength = 60; // 한글 기준 60자
    
    // 사전 정의된 아리엘 메시지 (조건별) - 한글 기준 50자 내외
    private readonly string[] victoryHighHPMessages = new string[]
    {
        "흥! 장군님, 이 정도는 당연한 결과야. HP도 여유롭고 전투력도 충분해.",
        "딱히... 잘했어. 승률 80% 정도는 예상했지만, 이 정도면 괜찮아.",
        "별로... 놀랄 건 없지만, 그래도 잘했어. 다음 전투도 기대할게.",
        "흠... 승리했지만, 다음엔 더 조심해야 해. 방심은 금물이야.",
        "뭐, 장군님이 꼭 하겠다면... 이 정도는 해야지. 당연한 결과야."
    };
    
    private readonly string[] victoryLowHPMessages = new string[]
    {
        "승리했지만 HP가 거의 없어. 다음 전투는 위험할 거야. 회복이 필요해.",
        "흥! 이긴 건 좋은데, HP 관리 좀 해야 할 것 같은데? 좀 아껴야 해.",
        "딱히... 승리했지만, HP가 30%밖에 안 남았어. 다음엔 조심해야 해.",
        "걱정... 아니야! 하지만 HP가 부족해 보여. 회복 아이템을 챙겨야 할 것 같아.",
        "승리했지만, HP가 적어서 다음 전투가 걱정이야. 좀 더 신중하게 전략을 세워야 해."
    };
    
    private readonly string[] defeatMessages = new string[]
    {
        "흥! 패배했네. 다음엔 더 신중하게 전략을 세워야 해. 작전을 다시 생각해봐.",
        "딱히... 이번엔 운이 없었어. 다음엔 이길 거야. 장군님 실력이니까.",
        "별로... 패배했지만, 다음 전투에서는 승리할 거야. 포기하지 마.",
        "흠... 패배했네. 전략을 다시 생각해봐야 할 것 같아. 작전을 바꿔봐.",
        "뭐, 장군님이 꼭 하겠다면... 다음엔 더 잘할 거야. 이번엔 실수였을 뿐이야."
    };
    
    private readonly string[] generalMessages = new string[]
    {
        "흥! 장군님, 뭘 도와드릴까? 전투나 작전에 대해 물어보면 답해줄게.",
        "딱히... 별로 도울 건 없지만, 물어보면 답해줄게. 장군님이 꼭 필요하다면...",
        "별로... 하지만 장군님이 꼭 물어본다면... 뭐든 물어봐도 돼. 답해줄게.",
        "흠... 뭐가 궁금한 거야? 전투나 작전에 대해 물어보면 도와줄 수 있어.",
        "뭐, 장군님이 꼭 하겠다면... 도와줄게. 뭔가 필요한 게 있으면 말해봐."
    };
    
    private readonly string[] welcomeMessages = new string[]
    {
        "흥! 장군님, 저는 아리엘이야. 전투 참모를 맡고 있어. 앞으로 잘 부탁해.",
        "딱히... 별로 도울 건 없지만, 장군님을 도와주려고 왔어. 잘 부탁해.",
        "별로... 하지만 장군님이 꼭 필요하다면... 도와줄게. 저는 아리엘이야.",
        "흠... 저는 아리엘. 전투에서 승리하도록 도와줄게. 앞으로 함께 해보자.",
        "뭐, 장군님이 꼭 하겠다면... 저는 아리엘이야. 전투 참모로서 잘 부탁해."
    };
    
    private readonly string[] tacticsMessages = new string[]
    {
        "흥! 각 캐릭터별 작전을 잘 세팅하면 승리할 수 있어. 신중하게 설정해봐.",
        "딱히... 작전 코드를 제대로 설정하면 승률이 올라가. 캐릭터 특성에 맞춰서.",
        "별로... 하지만 작전을 잘 짜면 전투에서 이길 수 있어. 각 캐릭터의 역할을 고려해봐.",
        "흠... 캐릭터마다 적절한 작전을 설정하는 게 중요해. 이것이 승리의 열쇠야.",
        "뭐, 장군님이 꼭 하겠다면... 작전 세팅이 승리의 열쇠야. 각 캐릭터의 특성을 살려봐."
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
                        
                        if (result.isPlayerWin)
                        {
                            // HP 비율 계산
                            float hpRatio = result.playerHP_Max > 0 
                                ? (float)result.playerHP_Remaining / result.playerHP_Max 
                                : 0f;
                            
                            if (hpRatio >= 0.7f)
                            {
                                // 승리 + HP 많이 남음 (70% 이상)
                                return victoryHighHPMessages[UnityEngine.Random.Range(0, victoryHighHPMessages.Length)];
                            }
                            else
                            {
                                // 승리 + HP 적게 남음 (70% 미만)
                                return victoryLowHPMessages[UnityEngine.Random.Range(0, victoryLowHPMessages.Length)];
                            }
                        }
                        else
                        {
                            // 패배
                            return defeatMessages[UnityEngine.Random.Range(0, defeatMessages.Length)];
                        }
                    }
                    break;
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
