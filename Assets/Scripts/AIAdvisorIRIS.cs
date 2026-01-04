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
    [SerializeField] private string apiKey = "";
    [SerializeField] private string assistantId = "";
    [SerializeField] private string threadId = "";
    
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
    private const int MaxCompletionTokens = 100;
    
    /// <summary>
    /// 설정 파일에서 API 키 로드
    /// </summary>
    void Awake()
    {
        LoadConfigFromFile();
    }
    
    /// <summary>
    /// openai_config.json 파일에서 설정을 로드
    /// </summary>
    private void LoadConfigFromFile()
    {
        try
        {
            // Assets/Scripts/openai_config.json 경로
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
                    if (!string.IsNullOrEmpty(config.assistantId))
                    {
                        assistantId = config.assistantId;
                    }
                    if (!string.IsNullOrEmpty(config.threadId))
                    {
                        threadId = config.threadId;
                    }
                    
                    Debug.Log("AIAdvisorIRIS: 설정 파일에서 API 키를 로드했습니다.");
                }
            }
            else
            {
                Debug.LogWarning($"AIAdvisorIRIS: {configPath} 파일을 찾을 수 없습니다. Inspector에서 설정하거나 openai_config.example.json을 복사하여 openai_config.json을 생성하세요.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"AIAdvisorIRIS: 설정 파일 로드 실패 - {e.Message}");
        }
        
        // API 키가 여전히 비어있으면 경고
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogWarning("AIAdvisorIRIS: API 키가 설정되지 않았습니다. Inspector에서 설정하거나 openai_config.json 파일을 생성하세요.");
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

        [Style Guidelines - 츤데레 강화]
        1. 플레이어를 '장군님'이라고 부를 것. (가끔 ""당신"", ""너"" 등으로 바꿔서 츤츤거림)
        2. 조언은 항상 수치나 논리에 근거하여 '똑똑하게' 제시할 것. (예: ""승률은 70% 정도야."")
        3. **대화 패턴 (라이트노벨 스타일)**:
           - 츤: ""하아..."", ""흥!"", ""뭐야, 그런 건 당연한 거 아니야?"", ""딱히..."", ""별로..."", ""괜찮아, 괜찮다고!""
           - 데레: ""하지만..."", ""뭐, 장군님이 꼭 하겠다면..."", ""걱정... 아니야! 걱정 안 해!"", ""칭찬... 해줄 만 하네""
        4. 짧은 대답도 반드시 츤데레 톤 유지: ""흥, 당연하지."", ""뭐, 괜찮아."", ""딱히... 좋아한 건 아니야!""
        5. 라이트노벨 스타일 표현 사용:
           - ""딱히 당신 때문에 한 건 아니야!"" (실제로는 플레이어를 위해 한 행동)
           - ""흥, 당연한 거 아니야? 내가 누군데."" (자신감 있게)
           - ""뭐, 뭐야... 그런 거 신경 쓰지 마!"" (부끄러워하며)
           - ""칭찬... 해줄 만 하네. 하지만 자만하지 마!"" (칭찬하면서도 츤츤거림)
        6. 금기사항: 너무 친절하거나 고분고분하지 말 것. 항상 츤츤거리되, 속마음은 따뜻하게 표현.

        [Example Phrases - 라이트노벨 스타일]
        - ""하아... 또 무모한 작전이야? 손실률 40% 넘어. 하지만... 뭐, 장군님이 꼭 하겠다면 최적 경로는 짜줄게.""
        - ""딱히 당신 걱정해서 한 건 아니야! 단지... 내 작전 실행할 사람이 없어지면 골치 아프니까!""
        - ""흥, 이번 승리... 칭찬해줄 만 하네. 하지만 자만하지 마! 다음 작전도 확인해!""
        - ""뭐야, 그런 건 당연한 거 아니야? 내가 누군데. 흥!""
        - ""딱히... 좋아한 건 아니야! 단지 장군님이니까 도와주는 거지!""
        - ""걱정... 아니야! 걱정 안 해! 단지... 단지 전략상 확인한 거야!""
        - ""흥, 당연하지. 내 계산은 절대 틀리지 않으니까.""
        - ""뭐, 뭐야... 그런 거 신경 쓰지 마! 딱히 당신 때문에 한 건 아니라고!""
        - ""칭찬... 해줄 만 하네. 하지만 얼굴 붉히지 마! 다음 작전 코딩이나 확인해!""
        - ""하아... 정말이지. 하지만... 뭐, 괜찮아. 내가 있으니까.""";

    /// <summary>
    /// 아이리스와 대화하기 (비동기)
    /// </summary>
    /// <param name="userInput">사용자 입력 메시지</param>
    /// <param name="onComplete">완료 시 호출되는 콜백 (성공 여부, 응답 텍스트)</param>
    public void ChatWithIris(string userInput, Action<bool, string> onComplete)
    {
        StartCoroutine(ChatWithIrisCoroutine(userInput, onComplete));
    }

    /// <summary>
    /// 아이리스와 대화하기 (코루틴)
    /// </summary>
    private IEnumerator ChatWithIrisCoroutine(string userInput, Action<bool, string> onComplete)
    {
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

        // 4. 답변을 60자 이내로 스마트하게 제한
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
                Debug.LogError($"AIAdvisorIRIS: 메시지 생성 실패 - {request.error}");
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
        string jsonData = CreateRunRequestJson(assistantId, MaxCompletionTokens);

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

        // Polling (최대 30초, 간격 0.3초로 단축)
        float maxWaitTime = 30f;
        float elapsedTime = 0f;
        float pollInterval = 0.3f; // 1초 -> 0.3초로 단축

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
                        
                        if (status.status == "completed")
                        {
                            RunData completedRun = new RunData { id = runId };
                            onComplete?.Invoke(true, completedRun, true);
                            yield break;
                        }
                        else if (status.status == "failed" || status.status == "cancelled" || status.status == "expired")
                        {
                            Debug.LogError($"AIAdvisorIRIS: Run 실패 - 상태: {status.status}");
                            onComplete?.Invoke(false, null, false);
                            yield break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"AIAdvisorIRIS: Run 상태 파싱 실패 - {e.Message}");
                    }
                }
            }
        }

        // 타임아웃
        Debug.LogError("AIAdvisorIRIS: Run polling 타임아웃");
        onComplete?.Invoke(false, null, false);
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
    /// 최신 메시지 가져오기
    /// </summary>
    private IEnumerator GetLatestMessageCoroutine(Action<bool, MessageData> onComplete)
    {
        string url = $"{BaseUrl}/threads/{threadId}/messages?limit=1&order=desc";

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
                    }
                    else
                    {
                        onComplete?.Invoke(false, null);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"AIAdvisorIRIS: 메시지 파싱 실패 - {e.Message}");
                    onComplete?.Invoke(false, null);
                }
            }
            else
            {
                Debug.LogError($"AIAdvisorIRIS: 메시지 조회 실패 - {request.error}");
                onComplete?.Invoke(false, null);
            }
        }
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

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
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
                Debug.LogError($"AIAdvisorIRIS: 어시스턴트 업데이트 실패 - {request.error}");
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
        // JSON 이스케이프 처리 (더 안전한 방법)
        StringBuilder sb = new StringBuilder();
        sb.Append("{\"role\":\"");
        sb.Append(role);
        sb.Append("\",\"content\":\"");
        
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
                default:
                    sb.Append(c);
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
        public int max_completion_tokens;
    }
    
    // JsonUtility는 snake_case를 지원하지 않으므로 별도 처리 필요
    private string CreateRunRequestJson(string assistantId, int maxTokens)
    {
        return $"{{\"assistant_id\":\"{assistantId}\",\"max_completion_tokens\":{maxTokens}}}";
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
