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
    
    private const string ConfigServerUrl = "https://arcana.koreacentral.cloudapp.azure.com/api/data/openai_config";
    private const string BaseUrl = "https://api.openai.com/v1";
    private const int MaxResponseLength = 60; // 한글 기준 60자
    // MaxCompletionTokens 제거: OpenAI API 기본값(무제한) 사용
    
    /// <summary>
    /// 서버에서 API 키 로드
    /// </summary>
    void Awake()
    {
        StartCoroutine(LoadConfigFromServer());
    }
    
    /// <summary>
    /// 서버에서 OpenAI Config를 로드
    /// </summary>
    private IEnumerator LoadConfigFromServer()
    {
        string url = ConfigServerUrl;
        
        Debug.Log($"AIAdvisorIRIS: 서버에서 설정 로드 시작 - URL: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 30;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"AIAdvisorIRIS: 서버 응답 수신 성공");
                    
                    // 서버 응답 형식: {"id": "openai_config", "content": {...}}
                    ServerConfigResponse serverResponse = JsonUtility.FromJson<ServerConfigResponse>(responseText);
                    
                    if (serverResponse != null && serverResponse.content != null)
                    {
                        OpenAIConfig config = serverResponse.content;
                        
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
                        
                        Debug.Log("AIAdvisorIRIS: 서버에서 설정을 성공적으로 로드했습니다.");
                    }
                    else
                    {
                        Debug.LogWarning("AIAdvisorIRIS: 서버 응답 파싱 실패 - content가 null입니다.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"AIAdvisorIRIS: 서버 응답 파싱 실패 - {e.Message}\n응답: {request.downloadHandler.text}");
                }
            }
            else
            {
                string errorMessage = request.error ?? "Unknown Error";
                Debug.LogWarning($"AIAdvisorIRIS: 서버에서 설정 로드 실패 - {errorMessage} (HTTP {request.responseCode})");
                
                // 폴백: 로컬 파일에서 로드 시도
                Debug.LogWarning("AIAdvisorIRIS: 서버 로드 실패, 로컬 파일에서 로드 시도...");
                LoadConfigFromFileFallback();
            }
        }
        
        // API 키가 여전히 비어있으면 경고
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogWarning("AIAdvisorIRIS: API 키가 설정되지 않았습니다. 서버 설정을 확인하세요.");
        }
    }
    
    /// <summary>
    /// 폴백: 로컬 파일에서 설정 로드 (서버 실패 시)
    /// </summary>
    private void LoadConfigFromFileFallback()
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
                    
                    Debug.Log("AIAdvisorIRIS: Resources 폴더에서 설정 파일을 로드했습니다 (폴백).");
                }
            }
            else
            {
                // 방법 2: 에디터에서만 작동하는 파일 시스템 경로 (폴백)
                #if UNITY_EDITOR
                string configPath = Path.Combine(Application.dataPath, "Resources", "openai_config.json");
                
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
                        
                        Debug.Log("AIAdvisorIRIS: 로컬 파일에서 설정 파일을 로드했습니다 (폴백).");
                    }
                }
                #endif
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"AIAdvisorIRIS: 폴백 설정 파일 로드 실패 - {e.Message}");
        }
    }
    
    // 아이리스 성격 설정 (토큰 최적화: 간결한 버전)
    private const string IrisInstructions = @"[Identity]
        - 이름: 아이리스. 츤데레 참모 캐릭터.
        - 성격: 츤츤거리지만 속마음은 따뜻함. 라이트노벨 스타일.
        - 답변: 60자 이내, 완전한 문장. 핵심만 간결하게.
        - 금지: ""하아"" 같은 한숨 표현 사용 금지.

        [Style]
        1. 플레이어를 '장군님'이라고 부름 (가끔 ""당신"", ""너"" 등으로 츤츤거림)
        2. 조언은 수치/논리 기반으로 제시 (예: ""승률 70% 정도야"")
        3. 츤: ""흥!"", ""딱히..."", ""별로..."", ""흠...""
        4. 데레: ""하지만..."", ""뭐, 장군님이 꼭 하겠다면..."", ""걱정... 아니야!""
        5. 매번 다른 표현 사용, 이전 대사 반복 금지.";

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

        // 전투 결과만 간단하게 전달 (토큰 최적화: 최소한의 정보만)
        if (result.isPlayerWin)
        {
            return $"승리 HP:{result.playerHP_Remaining}/{result.playerHP_Max}";
        }
        else
        {
            return "패배 HP:0";
        }
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
                Debug.LogWarning($"AIAdvisorIRIS: 메시지 생성 실패 - {request.error}\n응답: {errorResponse}\n요청 JSON: {jsonData}");
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
                    Debug.LogWarning($"AIAdvisorIRIS: Run 데이터 파싱 실패 - {e.Message}");
                    onComplete?.Invoke(false, null, false);
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning($"AIAdvisorIRIS: Run 생성 실패 - {createRequest.error}");
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
                    RunStatus status = null;
                    try
                    {
                        status = JsonUtility.FromJson<RunStatus>(retrieveRequest.downloadHandler.text);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"AIAdvisorIRIS: Run 상태 파싱 실패 - {e.Message}");
                        continue; // 다음 폴링으로 계속
                    }
                    
                    if (status == null)
                    {
                        continue; // 다음 폴링으로 계속
                    }
                    
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
                        
                        // Rate limit 오류 확인 및 처리
                        if (status.last_error != null && status.last_error.code == "rate_limit_exceeded")
                        {
                            // 재시도 시간 파싱 (예: "Please try again in 7.423s")
                            float retryAfterSeconds = ParseRetryAfterSeconds(status.last_error.message);
                            
                            if (retryAfterSeconds > 0 && retryAfterSeconds < 60f) // 최대 60초까지만 대기
                            {
                                Debug.LogWarning($"AIAdvisorIRIS: Rate limit 초과. {retryAfterSeconds:F1}초 후 재시도...");
                                yield return new WaitForSeconds(retryAfterSeconds + 1f); // 여유를 두고 재시도
                                
                                // Run을 다시 생성하여 재시도
                                yield return StartCoroutine(CreateAndPollRunCoroutine(onComplete));
                                yield break;
                            }
                            else
                            {
                                Debug.LogWarning($"AIAdvisorIRIS: Rate limit 초과 - 재시도 시간이 너무 깁니다 ({retryAfterSeconds:F1}초). 요청을 취소합니다.");
                                onComplete?.Invoke(false, null, false);
                                yield break;
                            }
                        }
                        
                        Debug.LogWarning($"AIAdvisorIRIS: Run 실패 - 상태: {status.status}\n상세: {errorDetails}");
                        onComplete?.Invoke(false, null, false);
                        yield break;
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
                    Debug.LogWarning($"AIAdvisorIRIS: Run polling 타임아웃 (최종 상태: {finalStatus.status})\n응답: {finalRequest.downloadHandler.text}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"AIAdvisorIRIS: Run polling 타임아웃 (상태 파싱 실패: {e.Message})");
                }
            }
            else
            {
                Debug.LogWarning($"AIAdvisorIRIS: Run polling 타임아웃 (상태 조회 실패: {finalRequest.error})");
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
                    Debug.LogWarning($"AIAdvisorIRIS: Run 상태 파싱 실패 - {e.Message}");
                    onComplete?.Invoke(false, null);
                }
            }
            else
            {
                Debug.LogWarning($"AIAdvisorIRIS: Run 상태 조회 실패 - {request.error}");
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
                        Debug.LogWarning($"AIAdvisorIRIS: 메시지 파싱 실패 - {e.Message}\n응답: {request.downloadHandler.text}");
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
                        Debug.LogWarning($"AIAdvisorIRIS: 메시지 조회 실패 - HTTP {responseCode}\n에러: {request.error}\n응답: {errorResponse}");
                        onComplete?.Invoke(false, null);
                        yield break;
                    }
                }
            }
        }
        
        // 모든 재시도 실패
        Debug.LogWarning($"AIAdvisorIRIS: 메시지 조회 최종 실패 (재시도 {maxRetries}회 모두 실패)");
        onComplete?.Invoke(false, null);
    }

    /// <summary>
    /// Rate limit 오류 메시지에서 재시도 시간 파싱
    /// 예: "Please try again in 7.423s" -> 7.423
    /// </summary>
    private float ParseRetryAfterSeconds(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return 0f;
        
        try
        {
            // "Please try again in 7.423s" 형식에서 숫자 추출
            int startIndex = errorMessage.IndexOf("try again in");
            if (startIndex == -1)
                return 0f;
            
            startIndex += "try again in".Length;
            string remaining = errorMessage.Substring(startIndex).Trim();
            
            // 숫자 부분만 추출 (예: "7.423s" -> "7.423")
            string numberStr = "";
            foreach (char c in remaining)
            {
                if (char.IsDigit(c) || c == '.')
                {
                    numberStr += c;
                }
                else if (c == 's' || c == 'S')
                {
                    break; // 's'를 만나면 종료
                }
                else if (!char.IsWhiteSpace(c))
                {
                    break; // 숫자가 아닌 문자가 나오면 종료
                }
            }
            
            if (float.TryParse(numberStr, out float seconds))
            {
                return seconds;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"AIAdvisorIRIS: 재시도 시간 파싱 실패 - {e.Message}");
        }
        
        return 0f;
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
                Debug.LogWarning($"AIAdvisorIRIS: 어시스턴트 업데이트 실패 - HTTP {request.responseCode}\n에러: {request.error}\n응답: {errorResponse}");
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
        public LastError last_error;
    }
    
    [Serializable]
    private class LastError
    {
        public string code;
        public string message;
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
    
    /// <summary>
    /// 서버 응답 형식: {"id": "openai_config", "content": {...}}
    /// </summary>
    [Serializable]
    private class ServerConfigResponse
    {
        public string id;
        public OpenAIConfig content;
    }
}
