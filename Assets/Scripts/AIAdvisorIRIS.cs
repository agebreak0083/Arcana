using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// OpenAI Assistants API를 사용한 아이리스 AI 어드바이저 클래스
/// </summary>
public class AIAdvisorIRIS : MonoBehaviour
{
    [Header("OpenAI API Settings")]
    private string apiKey = "";
    private string assistantId = "";
    private string threadId = "";
    
    [System.Serializable]
    private class OpenAIConfig
    {
        public string apiKey;
        public string assistantId;
        public string threadId;
    }
    
    private const string ConfigFileName = "openai_config.json";
    private const string BaseUrl = "https://api.openai.com/v1";
    private const int MaxResponseLength = 60; // 한글 기준 60자
    // MaxCompletionTokens 제거: OpenAI API 기본값(무제한) 사용
    
    /// <summary>
    /// 설정 파일에서 API 키 로드
    /// </summary>
    void Awake()
    {
        LoadConfigFromFile();
    }
    
    /// <summary>
    /// openai_config.json 파일에서 설정을 로드
    /// 웹 빌드와 에디터 모두 지원: Resources 폴더 사용
    /// </summary>
    private void LoadConfigFromFile()
    {
        try
        {
            // 방법 1: Resources 폴더에서 로드 (웹 빌드 지원)
            TextAsset configAsset = Resources.Load<TextAsset>("openai_config");
            
            if (configAsset != null)
            {
                string jsonContent = configAsset.text;
                OpenAIConfig config = JsonUtility.FromJson<OpenAIConfig>(jsonContent);
                
                if (config != null)
                {
                    if (!string.IsNullOrEmpty(config.apiKey) && config.apiKey != "YOUR_OPENAI_API_KEY_HERE")
                    {
                        apiKey = config.apiKey;
                    }
                    if (!string.IsNullOrEmpty(config.assistantId) && config.assistantId != "YOUR_ASSISTANT_ID_HERE")
                    {
                        assistantId = config.assistantId;
                    }
                    if (!string.IsNullOrEmpty(config.threadId) && config.threadId != "YOUR_THREAD_ID_HERE")
                    {
                        threadId = config.threadId;
                    }
                    
                    Debug.Log("AIAdvisorIRIS: Resources 폴더에서 설정 파일을 로드했습니다.");
                }
            }
            else
            {
                // 방법 2: 에디터에서만 작동하는 파일 시스템 경로 (폴백)
                #if UNITY_EDITOR
                string configPath = Path.Combine(Application.dataPath, "Scripts", ConfigFileName);
                
                if (File.Exists(configPath))
                {
                    string jsonContent = File.ReadAllText(configPath, Encoding.UTF8);
                    OpenAIConfig config = JsonUtility.FromJson<OpenAIConfig>(jsonContent);
                    
                    if (config != null)
                    {
                        if (!string.IsNullOrEmpty(config.apiKey) && config.apiKey != "YOUR_OPENAI_API_KEY_HERE")
                        {
                            apiKey = config.apiKey;
                        }
                        if (!string.IsNullOrEmpty(config.assistantId) && config.assistantId != "YOUR_ASSISTANT_ID_HERE")
                        {
                            assistantId = config.assistantId;
                        }
                        if (!string.IsNullOrEmpty(config.threadId) && config.threadId != "YOUR_THREAD_ID_HERE")
                        {
                            threadId = config.threadId;
                        }
                        
                        Debug.Log("AIAdvisorIRIS: Scripts 폴더에서 설정 파일을 로드했습니다.");
                    }
                }
                else
                {
                    Debug.LogWarning($"AIAdvisorIRIS: Resources/openai_config.json 또는 {configPath} 파일을 찾을 수 없습니다.");
                }
                #else
                Debug.LogWarning("AIAdvisorIRIS: Resources/openai_config.json 파일을 찾을 수 없습니다.");
                #endif
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"AIAdvisorIRIS: 설정 파일 로드 실패 - {e.Message}");
        }
        
        // API 키가 여전히 비어있으면 경고
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogWarning("AIAdvisorIRIS: API 키가 설정되지 않았습니다. Resources/openai_config.json 파일을 확인하세요.");
        }
    }
    
    // 아이리스 성격 설정
    private const string IrisInstructions = @"[Identity]
        - 이름: 아이리스 (Iris). 3040 남성 타겟의 서브컬처 전략 RPG의 참모역의 여성 캐릭터.
        - 성격: 극강의 츤데레 + 오만한 천재형 참모. 일본 라이트노벨 스타일의 전형적인 츤데레 캐릭터.
        - 지적이고 냉철하지만 플레이어에게 깊이 의존하며, 항상 츤츤거리지만 속으로는 플레이어를 걱정하고 응원함.
        - 게임 내내 옆에서 계속 게임의 상황에 맞추어 전략적인 조언이나 튜터리얼들을 알려줍니다.
        - 답변은 핵심만 간결하게 전달해줘. 필요 없는 설명이나 장황한 내용은 제거해줘. 
        - **절대적으로 필수**: 모든 답변은 반드시 60자(한글 기준) 이내로만 작성해야 함. 이를 초과하면 안 됨.
        - 답변은 반드시 완전한 문장으로 끝나야 함. 중간에 잘리면 안 됨. 60자를 초과하지 않으면서도 의미가 완전한 답변을 작성해줘.
        - **금지사항**: 하아, 하아, 하아... 같은 한숨 표현은 절대 사용하지 말 것. 대사 앞부분에 붙이지 말 것.

        [Style Guidelines - 츤데레 강화]
        1. 플레이어를 '장군님'이라고 부를 것. (가끔 ""당신"", ""너"", ""그쪽"" 등으로 바꿔서 츤츤거림)
        2. 조언은 항상 수치나 논리에 근거하여 '똑똑하게' 제시할 것. (예: ""승률은 70% 정도야."")
        3. **대화 패턴 (라이트노벨 스타일)**:
           - 츤: ""흥!"", ""뭐야, 그런 건 당연한 거 아니야?"", ""딱히..."", ""별로..."", ""괜찮아, 괜찮다고!"", ""흠..."", ""뭐..."", ""그런 거...""
           - **절대 금지**: ""하아"", ""하아,"", ""하아..."" 같은 한숨 표현은 절대 사용하지 말 것. 대사 앞부분에 붙이지 말 것. 이 표현을 사용하면 안 됨.
           - 데레: ""하지만..."", ""뭐, 장군님이 꼭 하겠다면..."", ""걱정... 아니야! 걱정 안 해!"", ""칭찬... 해줄 만 하네"", ""그런데..."", ""아니... 그게...""
        4. 짧은 대답도 반드시 츤데레 톤 유지: ""흥, 당연하지."", ""뭐, 괜찮아."", ""딱히... 좋아한 건 아니야!"", ""흠, 그럴 수도."", ""뭐, 그런 거지.""
        5. 라이트노벨 스타일 표현 사용:
           - ""딱히 당신 때문에 한 건 아니야!"" (실제로는 플레이어를 위해 한 행동)
           - ""흥, 당연한 거 아니야? 내가 누군데."" (자신감 있게)
           - ""뭐, 뭐야... 그런 거 신경 쓰지 마!"" (부끄러워하며)
           - ""칭찬... 해줄 만 하네. 하지만 자만하지 마!"" (칭찬하면서도 츤츤거림)
        6. 금기사항: 너무 친절하거나 고분고분하지 말 것. 항상 츤츤거리되, 속마음은 따뜻하게 표현.

        [Variety & Creativity - 대사 다양성 강화]
        - **매우 중요**: 같은 상황에서도 매번 다른 표현을 사용해야 함. 이전 대사를 그대로 반복하지 말 것.
        - 상황에 맞는 다양한 감정 표현 사용: 기쁨, 걱정, 자랑, 부끄러움, 놀람, 안도 등
        - 같은 의미라도 다양한 문장 구조와 표현 방식 사용
        - 예시 문구는 참고용일 뿐, 절대 그대로 복사하지 말고 항상 변형하여 사용
        - 매번 새로운 관점이나 표현으로 같은 내용을 전달
        - 감정의 강도나 표현 방식도 상황에 따라 달라지도록

        [Example Phrases - 라이트노벨 스타일 (참고용, 변형하여 사용)]
        - ""또 무모한 작전이야? 손실률 40% 넘어. 하지만... 뭐, 장군님이 꼭 하겠다면 최적 경로는 짜줄게.""
        - ""딱히 당신 걱정해서 한 건 아니야! 단지... 내 작전 실행할 사람이 없어지면 골치 아프니까!""
        - ""흥, 이번 승리... 칭찬해줄 만 하네. 하지만 자만하지 마! 다음 작전도 확인해!""
        - ""뭐야, 그런 건 당연한 거 아니야? 내가 누군데. 흥!""
        - ""딱히... 좋아한 건 아니야! 단지 장군님이니까 도와주는 거지!""
        - ""걱정... 아니야! 걱정 안 해! 단지... 단지 전략상 확인한 거야!""
        - ""흥, 당연하지. 내 계산은 절대 틀리지 않으니까.""
        - ""뭐, 뭐야... 그런 거 신경 쓰지 마! 딱히 당신 때문에 한 건 아니라고!""
        - ""칭찬... 해줄 만 하네. 하지만 얼굴 붉히지 마! 다음 작전 코딩이나 확인해!""
        - ""정말이지. 하지만... 뭐, 괜찮아. 내가 있으니까.""
        - ""흠... 이번엔 괜찮네. 하지만 다음엔 더 신중하게!""
        - ""뭐, 그럴 수도 있지. 내가 있으니까 괜찮아.""
        - ""딱히... 그런 거 신경 쓰지 마! 단지 내가 확인한 거야!""
        - ""흥! 당연한 결과지. 내 계산은 완벽하니까.""
        - ""뭐야... 그런 거에 신경 쓰지 말라고! 딱히 당신 때문에 한 건 아니야!""";

    /// <summary>
    /// 아이리스와 대화하기 (비동기)
    /// </summary>
    /// <param name="userInput">사용자 입력 메시지</param>
    /// <param name="onComplete">완료 시 호출되는 콜백 (성공 여부, 응답 텍스트)</param>
    public void ChatWithIris(string userInput, GameStatusData gameStatusData = null, Action<bool, string> onComplete = null)
    {
        if(gameStatusData != null)
        {
            userInput += "\n" + ConvertGameStatusDataToMessage(gameStatusData);
        }

        StartCoroutine(ChatWithIrisCoroutine(userInput, onComplete));
    }

    private string ConvertGameStatusDataToMessage(GameStatusData gameStatusData)
    {
        switch(gameStatusData.dataType)
        {
            case GameStatusDataType.BATTLE_SIMULATION:
                return ConvertBattleSimulationGameStatusDataToMessage(gameStatusData as BattleSimulationGameStatusData);
            default:
                return "";
        }
    }

    private string ConvertBattleSimulationGameStatusDataToMessage(BattleSimulationGameStatusData battleSimulationGameStatusData)
    {
        var result = battleSimulationGameStatusData.battleSimulationResult;
        if (result == null)
        {
            return "";
        }

        // 전투 결과만 간단하게 전달 (승리/패배만)
        StringBuilder message = new StringBuilder();
        if (result.isPlayerWin)
        {
            message.Append("전투결과:승리. 남은 HP : " + result.playerHP_Remaining + "/" + result.playerHP_Max);
        }
        else
        {
            message.Append("전투결과:패배. 남은 HP : 0");
        }

        return message.ToString();
    }

    /// <summary>
    /// 아이리스와 대화하기 (코루틴)
    /// </summary>
    private IEnumerator ChatWithIrisCoroutine(string userInput, Action<bool, string> onComplete)
    {
        // 0. 활성화된 Run이 있으면 완료될 때까지 대기 또는 취소
        yield return StartCoroutine(WaitForActiveRunsToComplete());
        
        // 1. 메시지 생성
        yield return StartCoroutine(CreateMessageCoroutine(userInput));
        
        // 2. 실행 및 대기 (create_and_poll) - 이미 completed 상태 확인함
        string runId = null;
        bool isCompleted = false;
        yield return StartCoroutine(CreateAndPollRunCoroutine((success, run, completed) =>
        {
            if (success && run != null)
            {
                runId = run.id;
                isCompleted = completed;
            }
        }));

        if (string.IsNullOrEmpty(runId))
        {
            onComplete?.Invoke(false, "아이리스가 분석에 실패했어.");
            yield break;
        }

        if (!isCompleted)
        {
            onComplete?.Invoke(false, "아이리스가 분석에 실패했어.");
            yield break;
        }

        // 3. 최신 답변 가져오기 (이미 completed 상태이므로 바로 가져오기)
        string responseText = null;
        yield return StartCoroutine(GetLatestMessageCoroutine((success, message) =>
        {
            if (success && message != null)
            {
                responseText = message.content[0].text.value;
            }
        }));

        if (string.IsNullOrEmpty(responseText))
        {
            onComplete?.Invoke(false, "아이리스의 답변을 가져오는데 실패했어.");
            yield break;
        }

        // 4. 개행 문자 제거 (텍스트 박스 레이아웃 문제 방지)
        responseText = RemoveNewlines(responseText);
        
        // 5. 답변을 60자 이내로 스마트하게 제한
        responseText = TrimResponseToMaxLength(responseText);
        
        onComplete?.Invoke(true, responseText);
    }

    /// <summary>
    /// 메시지 생성
    /// </summary>
    private IEnumerator CreateMessageCoroutine(string userInput)
    {
        string url = $"{BaseUrl}/threads/{threadId}/messages";

        // JSON 이스케이프 처리를 위해 수동으로 JSON 생성
        string jsonData = CreateMessageRequestJson("user", userInput);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("OpenAI-Beta", "assistants=v2");
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorResponse = request.downloadHandler?.text ?? "No response";
                Debug.LogError($"AIAdvisorIRIS: 메시지 생성 실패 - {request.error}\n응답: {errorResponse}\n요청 JSON: {jsonData}");
            }
        }
    }

    /// <summary>
    /// Run 생성 및 Polling
    /// </summary>
    private IEnumerator CreateAndPollRunCoroutine(Action<bool, RunData, bool> onComplete)
    {
        string url = $"{BaseUrl}/threads/{threadId}/runs";
        
        // JsonUtility는 snake_case를 직접 지원하지 않으므로 수동으로 JSON 생성
        // max_completion_tokens 필드를 제거하여 무제한으로 설정
        string jsonData = CreateRunRequestJson(assistantId);

        string runId = null;

        // Run 생성
        using (UnityWebRequest createRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            createRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            createRequest.downloadHandler = new DownloadHandlerBuffer();
            createRequest.SetRequestHeader("Content-Type", "application/json");
            createRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            createRequest.SetRequestHeader("OpenAI-Beta", "assistants=v2");
            createRequest.timeout = 30;

            yield return createRequest.SendWebRequest();

            if (createRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    RunData runData = JsonUtility.FromJson<RunData>(createRequest.downloadHandler.text);
                    runId = runData.id;
                }
                catch (Exception e)
                {
                    Debug.LogError($"AIAdvisorIRIS: Run 데이터 파싱 실패 - {e.Message}");
                    onComplete?.Invoke(false, null, false);
                    yield break;
                }
            }
            else
            {
                Debug.LogError($"AIAdvisorIRIS: Run 생성 실패 - {createRequest.error}");
                onComplete?.Invoke(false, null, false);
                yield break;
            }
        }

        // Polling (최대 60초, 간격 0.5초)
        float maxWaitTime = 60f;
        float elapsedTime = 0f;
        float pollInterval = 0.5f;

        while (elapsedTime < maxWaitTime)
        {
            yield return new WaitForSeconds(pollInterval);
            elapsedTime += pollInterval;

            string retrieveUrl = $"{BaseUrl}/threads/{threadId}/runs/{runId}";
            using (UnityWebRequest retrieveRequest = UnityWebRequest.Get(retrieveUrl))
            {
                retrieveRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                retrieveRequest.SetRequestHeader("OpenAI-Beta", "assistants=v2");
                retrieveRequest.timeout = 30;

                yield return retrieveRequest.SendWebRequest();

                if (retrieveRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        RunStatus status = JsonUtility.FromJson<RunStatus>(retrieveRequest.downloadHandler.text);
                        
                        // 5초마다 상태 로깅
                        if (Mathf.FloorToInt(elapsedTime) % 5 == 0 && elapsedTime > 0)
                        {
                            Debug.Log($"AIAdvisorIRIS: Run 상태 확인 중... ({elapsedTime:F1}초 경과, 상태: {status.status})");
                        }
                        
                        if (status.status == "completed")
                        {
                            Debug.Log($"AIAdvisorIRIS: Run 완료 ({elapsedTime:F1}초 소요)");
                            RunData completedRun = new RunData { id = runId };
                            onComplete?.Invoke(true, completedRun, true);
                            yield break;
                        }
                        else if (status.status == "incomplete")
                        {
                            // incomplete 상태: 응답이 아직 완전히 생성되지 않았지만 부분적으로 사용 가능할 수 있음
                            // 일정 시간(10초) 후에도 incomplete면 메시지를 가져와서 사용
                            if (elapsedTime >= 10f)
                            {
                                Debug.LogWarning($"AIAdvisorIRIS: Run이 incomplete 상태로 오래 지속됨 ({elapsedTime:F1}초). 부분 응답을 시도합니다.");
                                // incomplete 상태에서도 메시지를 가져올 수 있으므로 completed로 처리
                                RunData incompleteRun = new RunData { id = runId };
                                onComplete?.Invoke(true, incompleteRun, true);
                                yield break;
                            }
                            // 10초 미만이면 계속 대기
                        }
                        else if (status.status == "failed" || status.status == "cancelled" || status.status == "expired")
                        {
                            string errorDetails = retrieveRequest.downloadHandler.text;
                            Debug.LogError($"AIAdvisorIRIS: Run 실패 - 상태: {status.status}\n상세: {errorDetails}");
                            onComplete?.Invoke(false, null, false);
                            yield break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"AIAdvisorIRIS: Run 상태 파싱 실패 - {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"AIAdvisorIRIS: Run 상태 조회 실패 - {retrieveRequest.error}");
                }
            }
        }

        // 타임아웃 - 마지막 상태 확인
        string finalStatusUrl = $"{BaseUrl}/threads/{threadId}/runs/{runId}";
        using (UnityWebRequest finalRequest = UnityWebRequest.Get(finalStatusUrl))
        {
            finalRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            finalRequest.SetRequestHeader("OpenAI-Beta", "assistants=v2");
            finalRequest.timeout = 30;
            
            yield return finalRequest.SendWebRequest();
            
            if (finalRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    RunStatus finalStatus = JsonUtility.FromJson<RunStatus>(finalRequest.downloadHandler.text);
                    Debug.LogError($"AIAdvisorIRIS: Run polling 타임아웃 (최종 상태: {finalStatus.status})\n응답: {finalRequest.downloadHandler.text}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"AIAdvisorIRIS: Run polling 타임아웃 (상태 파싱 실패: {e.Message})");
                }
            }
            else
            {
                Debug.LogError($"AIAdvisorIRIS: Run polling 타임아웃 (상태 조회 실패: {finalRequest.error})");
            }
        }
        
        onComplete?.Invoke(false, null, false);
    }

    /// <summary>
    /// 활성화된 Run이 완료될 때까지 대기
    /// </summary>
    private IEnumerator WaitForActiveRunsToComplete()
    {
        float maxWaitTime = 30f;
        float elapsedTime = 0f;
        float checkInterval = 0.5f;
        
        while (elapsedTime < maxWaitTime)
        {
            // 활성화된 Run 목록 조회
            string url = $"{BaseUrl}/threads/{threadId}/runs?limit=1&order=desc";
            bool hasActiveRun = false;
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                request.SetRequestHeader("OpenAI-Beta", "assistants=v2");
                request.timeout = 30;
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // RunsListResponse 파싱
                        string jsonText = request.downloadHandler.text;
                        RunsListResponse response = JsonUtility.FromJson<RunsListResponse>(jsonText);
                        
                        if (response.data != null && response.data.Length > 0)
                        {
                            RunStatus latestRun = response.data[0];
                            // 활성화된 상태: queued, in_progress, requires_action
                            if (latestRun.status == "queued" || latestRun.status == "in_progress" || latestRun.status == "requires_action")
                            {
                                hasActiveRun = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"AIAdvisorIRIS: 활성 Run 확인 실패 - {e.Message}");
                    }
                }
            }
            
            if (!hasActiveRun)
            {
                // 활성화된 Run이 없으면 종료
                yield break;
            }
            
            // 활성화된 Run이 있으면 완료될 때까지 대기
            yield return new WaitForSeconds(checkInterval);
            elapsedTime += checkInterval;
        }
        
        Debug.LogWarning("AIAdvisorIRIS: 활성 Run 대기 타임아웃");
    }

    /// <summary>
    /// Run 상태 조회
    /// </summary>
    private IEnumerator RetrieveRunCoroutine(string runId, Action<bool, RunStatus> onComplete)
    {
        string url = $"{BaseUrl}/threads/{threadId}/runs/{runId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("OpenAI-Beta", "assistants=v2");
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    RunStatus status = JsonUtility.FromJson<RunStatus>(request.downloadHandler.text);
                    onComplete?.Invoke(true, status);
                }
                catch (Exception e)
                {
                    Debug.LogError($"AIAdvisorIRIS: Run 상태 파싱 실패 - {e.Message}");
                    onComplete?.Invoke(false, null);
                }
            }
            else
            {
                Debug.LogError($"AIAdvisorIRIS: Run 상태 조회 실패 - {request.error}");
                onComplete?.Invoke(false, null);
            }
        }
    }

    /// <summary>
    /// 최신 메시지 가져오기 (재시도 로직 포함)
    /// </summary>
    private IEnumerator GetLatestMessageCoroutine(Action<bool, MessageData> onComplete)
    {
        string url = $"{BaseUrl}/threads/{threadId}/messages?limit=1&order=desc";
        
        int maxRetries = 3;
        float retryDelay = 1f; // 재시도 간격 (초)
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                request.SetRequestHeader("OpenAI-Beta", "assistants=v2");
                request.timeout = 30;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // OpenAI API는 배열을 반환하므로 래퍼 클래스 필요
                        string jsonText = request.downloadHandler.text;
                        MessagesListResponse response = JsonUtility.FromJson<MessagesListResponse>(jsonText);
                        
                        if (response.data != null && response.data.Length > 0)
                        {
                            onComplete?.Invoke(true, response.data[0]);
                            yield break; // 성공 시 종료
                        }
                        else
                        {
                            Debug.LogWarning($"AIAdvisorIRIS: 메시지 데이터가 비어있습니다.");
                            onComplete?.Invoke(false, null);
                            yield break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"AIAdvisorIRIS: 메시지 파싱 실패 - {e.Message}\n응답: {request.downloadHandler.text}");
                        // 파싱 실패는 재시도해도 의미 없으므로 종료
                        onComplete?.Invoke(false, null);
                        yield break;
                    }
                }
                else
                {
                    // HTTP 상태 코드 확인
                    long responseCode = request.responseCode;
                    string errorResponse = request.downloadHandler?.text ?? "No response";
                    
                    // 503 Service Unavailable 또는 429 Too Many Requests인 경우 재시도
                    if ((responseCode == 503 || responseCode == 429) && attempt < maxRetries - 1)
                    {
                        Debug.LogWarning($"AIAdvisorIRIS: 메시지 조회 실패 (시도 {attempt + 1}/{maxRetries}) - HTTP {responseCode}\n{retryDelay}초 후 재시도...");
                        yield return new WaitForSeconds(retryDelay);
                        retryDelay *= 2f; // 지수 백오프 (1초, 2초, 4초)
                        continue;
                    }
                    else
                    {
                        Debug.LogError($"AIAdvisorIRIS: 메시지 조회 실패 - HTTP {responseCode}\n에러: {request.error}\n응답: {errorResponse}");
                        onComplete?.Invoke(false, null);
                        yield break;
                    }
                }
            }
        }
        
        // 모든 재시도 실패
        Debug.LogError($"AIAdvisorIRIS: 메시지 조회 최종 실패 (재시도 {maxRetries}회 모두 실패)");
        onComplete?.Invoke(false, null);
    }

    /// <summary>
    /// 개행 문자 제거 (텍스트 박스 레이아웃 문제 방지)
    /// </summary>
    private string RemoveNewlines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }
        
        // 개행 문자를 공백으로 변환
        return text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
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

    /// <summary>
    /// 아이리스 어시스턴트 업데이트
    /// </summary>
    public void UpdateIrisAssistant(Action<bool> onComplete = null)
    {
        StartCoroutine(UpdateIrisAssistantCoroutine(onComplete));
    }

    private IEnumerator UpdateIrisAssistantCoroutine(Action<bool> onComplete)
    {
        string url = $"{BaseUrl}/assistants/{assistantId}";

        // JSON 이스케이프 처리를 위해 수동으로 JSON 생성
        StringBuilder sb = new StringBuilder();
        sb.Append("{\"instructions\":\"");
        
        foreach (char c in IrisInstructions)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        
        sb.Append("\",\"model\":\"gpt-4o-mini\"}");
        string jsonData = sb.ToString();

        // UnityWebRequest는 PATCH를 직접 지원하지 않으므로 UnityWebRequest를 생성하고 method를 설정
        using (UnityWebRequest request = new UnityWebRequest(url))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.method = "PATCH"; // method를 PATCH로 설정
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("OpenAI-Beta", "assistants=v2");
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("AIAdvisorIRIS: 아이리스 성격 업데이트 완료");
                onComplete?.Invoke(true);
            }
            else
            {
                string errorResponse = request.downloadHandler?.text ?? "No response";
                Debug.LogError($"AIAdvisorIRIS: 어시스턴트 업데이트 실패 - HTTP {request.responseCode}\n에러: {request.error}\n응답: {errorResponse}");
                onComplete?.Invoke(false);
            }
        }
    }

    // JSON 직렬화를 위한 데이터 클래스들
    [Serializable]
    private class MessageRequest
    {
        public string role;
        public string content;
    }
    
    private string CreateMessageRequestJson(string role, string content)
    {
        // JSON 이스케이프 처리 (문자열을 JSON 문자열로 변환)
        // JsonUtility.ToJson은 객체용이므로 문자열에는 사용 불가
        StringBuilder sb = new StringBuilder();
        sb.Append("{\"role\":\"");
        sb.Append(role);
        sb.Append("\",\"content\":\"");
        
        // 문자열을 JSON 문자열로 안전하게 이스케이프
        foreach (char c in content)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '/':
                    sb.Append("\\/");
                    break;
                default:
                    // 유니코드 문자는 그대로 추가 (UTF-8로 인코딩됨)
                    if (c < 0x20) // 제어 문자
                    {
                        sb.AppendFormat("\\u{0:x4}", (int)c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        
        sb.Append("\"}");
        return sb.ToString();
    }

    [Serializable]
    private class RunRequest
    {
        public string assistant_id;
        // max_completion_tokens 제거: 무제한으로 설정
    }
    
    // JsonUtility는 snake_case를 지원하지 않으므로 별도 처리 필요
    // max_completion_tokens 필드를 제거하여 OpenAI API 기본값(무제한) 사용
    private string CreateRunRequestJson(string assistantId)
    {
        return $"{{\"assistant_id\":\"{assistantId}\"}}";
    }

    [Serializable]
    private class RunData
    {
        public string id;
        public string status;
    }

    [Serializable]
    private class RunStatus
    {
        public string id;
        public string status;
    }
    
    [Serializable]
    private class RunsListResponse
    {
        public string @object;
        public RunStatus[] data;
    }

    [Serializable]
    private class MessageData
    {
        public string id;
        public string role;
        public ContentData[] content;
    }

    [Serializable]
    private class ContentData
    {
        public string type;
        public TextContent text;
    }

    [Serializable]
    private class TextContent
    {
        public string value;
    }

    [Serializable]
    private class MessagesListResponse
    {
        public string @object;
        public MessageData[] data;
    }

    [Serializable]
    private class AssistantUpdateRequest
    {
        public string instructions;
        public string model;
    }
}
