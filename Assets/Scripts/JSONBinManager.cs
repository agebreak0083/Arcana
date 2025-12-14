using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Linq;

/// <summary>
/// JSONBin.io를 사용한 Tactics 데이터 관리 클래스
/// </summary>
[DefaultExecutionOrder(-100)]
public class JSONBinManager : MonoBehaviour
{
    public static JSONBinManager Instance { get; private set; }

    [Header("JSONBin.io Settings")]
    [SerializeField] private string binId = "69363c8eae596e708f8a838f";
    [SerializeField] private string accessKey = "$2a$10$sXEbuoWJjS8yOOTbWOJqhev4XtCLCNUqJurky/QhQqFu1ZRBLYRu6";
    
    private string baseUrl = "https://api.jsonbin.io/v3/b";
    public bool isInitialized { get; private set; } = false;

    void Awake()
    {
        // 씬마다 독립적인 인스턴스 사용
        Instance = this;
        
        // 비정상 종료 후 재시작 시 이전 연결 정리
        CleanupPreviousConnections();
        
        // JSONBin.io는 별도 초기화 불필요 (항상 사용 가능)
        isInitialized = !string.IsNullOrEmpty(binId) && !string.IsNullOrEmpty(accessKey);
        
        if (!isInitialized)
        {
            Debug.LogWarning("JSONBinManager: Bin ID 또는 Access Key가 설정되지 않았습니다.");
        }
        else
        {
            Debug.Log("JSONBinManager: JSONBin.io 초기화 완료");
        }
    }

    /// <summary>
    /// 비정상 종료 후 재시작 시 이전 연결 정리
    /// </summary>
    private void CleanupPreviousConnections()
    {
        try
        {
            // UnityWebRequest의 쿠키 캐시 정리
            UnityWebRequest.ClearCookieCache();
            
            // 추가 정리 작업이 필요한 경우 여기에 추가
            Debug.Log("JSONBinManager: 이전 연결 정리 완료");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"JSONBinManager: 연결 정리 중 오류 발생 (무시 가능): {e.Message}");
        }
    }

    /// <summary>
    /// Tactics 데이터를 JSONBin.io에 저장
    /// </summary>
    /// <param name="tacticsJson">저장할 tactics.json 내용</param>
    /// <param name="onComplete">완료 콜백</param>
    public void SaveTactics(string tacticsJson, Action<bool, string> onComplete = null)
    {
        if (!isInitialized)
        {
            Debug.LogError("JSONBinManager가 초기화되지 않았습니다.");
            onComplete?.Invoke(false, "Not initialized");
            return;
        }

        if (UserDataManager.Instance == null || UserDataManager.Instance.currentUserData == null)
        {
            Debug.LogError("UserDataManager가 초기화되지 않았습니다.");
            onComplete?.Invoke(false, "UserDataManager not initialized");
            return;
        }

        // Key 생성: Username_시간 (Firebase와 동일한 형식)
        string username = SanitizeUsername(UserDataManager.Instance.currentUserData.playerName);
        string timestamp = DateTime.Now.ToString("yyMMddHHmm");
        string key = $"{username}_{timestamp}";

        // 기존 데이터 로드 후 새 항목 추가
        StartCoroutine(LoadAllTactics((success, allTactics) =>
        {
            if (!success)
            {
                allTactics = new TacticsDatabase { tactics = new List<TacticsData>() };
                Debug.LogError("Tactics 데이터 로드 실패");
            }

            // 중복 키 체크 (같은 키가 있으면 제거)
            allTactics.tactics.RemoveAll(t => t.key == key);

            // 새 데이터 추가
            var newTactic = new TacticsData
            {
                key = key,
                username = UserDataManager.Instance.currentUserData.playerName,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                tacticsJson = tacticsJson
            };

            allTactics.tactics.Add(newTactic);

            // 최대 100개로 제한 (가장 최근 것만 유지)
            const int MAX_TACTICS = 100;
            if (allTactics.tactics.Count > MAX_TACTICS)
            {
                // timestamp 기준으로 정렬 (최신순)
                allTactics.tactics.Sort((a, b) =>
                {
                    if (DateTime.TryParse(a.timestamp, out DateTime dateA) && 
                        DateTime.TryParse(b.timestamp, out DateTime dateB))
                    {
                        return dateB.CompareTo(dateA); // 내림차순 (최신이 먼저)
                    }
                    return string.Compare(b.timestamp, a.timestamp, StringComparison.Ordinal);
                });

                // 가장 최근 100개만 유지
                int removeCount = allTactics.tactics.Count - MAX_TACTICS;
                allTactics.tactics.RemoveRange(MAX_TACTICS, removeCount);
                Debug.Log($"Tactics 데이터가 {MAX_TACTICS}개를 초과하여 오래된 {removeCount}개를 제거했습니다.");
            }

            // 전체 데이터 저장
            StartCoroutine(SaveAllTactics(allTactics, (saveSuccess, errorMessage) =>
            {
                if (saveSuccess)
                {
                    Debug.Log($"Tactics 데이터 저장 성공: {key}");
                    onComplete?.Invoke(true, key);
                }
                else
                {
                    Debug.LogError($"Tactics 데이터 저장 실패: {errorMessage}");
                    onComplete?.Invoke(false, errorMessage ?? "Save failed");
                }
            }));
        }));
    }

    /// <summary>
    /// JSONBin.io에서 특정 키의 Tactics 데이터 로드
    /// </summary>
    /// <param name="key">로드할 데이터의 키</param>
    /// <param name="onComplete">완료 콜백 (성공 여부, tactics JSON)</param>
    public void LoadTactics(string key, Action<bool, string> onComplete)
    {
        if (!isInitialized)
        {
            onComplete?.Invoke(false, null);
            return;
        }

        StartCoroutine(LoadAllTactics((success, allTactics) =>
        {
            if (!success || allTactics == null)
            {
                onComplete?.Invoke(false, null);
                return;
            }

            // 키로 검색
            var tactic = allTactics.tactics.Find(t => t.key == key);
            if (tactic != null)
            {
                Debug.Log($"Tactics 데이터 로드 성공: {key}");
                onComplete?.Invoke(true, tactic.tacticsJson);
            }
            else
            {
                Debug.LogWarning($"Tactics 데이터를 찾을 수 없습니다: {key}");
                onComplete?.Invoke(false, null);
            }
        }));
    }

    /// <summary>
    /// 특정 유저의 모든 Tactics 데이터 키 목록 가져오기
    /// </summary>
    /// <param name="username">유저 이름</param>
    /// <param name="onComplete">완료 콜백 (성공 여부, 키 목록)</param>
    public void GetUserTacticsKeys(string username, Action<bool, List<string>> onComplete)
    {
        if (!isInitialized)
        {
            onComplete?.Invoke(false, null);
            return;
        }

        string sanitizedUsername = SanitizeUsername(username);

        StartCoroutine(LoadAllTactics((success, allTactics) =>
        {
            if (!success || allTactics == null)
            {
                onComplete?.Invoke(false, null);
                return;
            }

            var keys = new List<string>();
            foreach (var tactic in allTactics.tactics)
            {
                if (SanitizeUsername(tactic.username) == sanitizedUsername)
                {
                    keys.Add(tactic.key);
                }
            }

            Debug.Log($"유저 {username}의 Tactics 키 {keys.Count}개 로드 완료");
            onComplete?.Invoke(true, keys);
        }));
    }

    /// <summary>
    /// JSONBin.io에서 랜덤 Tactics 데이터 가져오기 (적 편성용)
    /// </summary>
    /// <param name="onComplete">완료 콜백 (성공 여부, tactics JSON, username)</param>
    public void GetRandomTactics(Action<bool, string, string> onComplete)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("JSONBinManager가 초기화되지 않았습니다. 로컬 파일을 사용합니다.");
            onComplete?.Invoke(false, null, null);
            return;
        }

        StartCoroutine(LoadAllTactics((success, allTactics) =>
        {
            if (!success || allTactics == null || allTactics.tactics.Count == 0)
            {
                Debug.LogWarning("JSONBin.io에 저장된 Tactics 데이터가 없습니다. 로컬 파일을 사용합니다.");
                onComplete?.Invoke(false, null, null);
                return;
            }

            // 랜덤 선택
            int randomIndex = UnityEngine.Random.Range(0, allTactics.tactics.Count);
            var randomTactic = allTactics.tactics[randomIndex];

            Debug.Log($"랜덤 Tactics 로드 성공: {randomTactic.key} (유저: {randomTactic.username})");
            onComplete?.Invoke(true, randomTactic.tacticsJson, randomTactic.key);
        }));
    }

    // ========== 내부 메서드 ==========

    /// <summary>
    /// JSONBin.io에서 모든 Tactics 데이터 로드
    /// </summary>
    private IEnumerator LoadAllTactics(Action<bool, TacticsDatabase> onComplete)
    {
        string url = $"{baseUrl}/{binId}/latest";

        // 재시도 로직 (최대 5회)
        const int maxRetries = 5;
        int retryCount = 0;
        bool success = false;

        while (retryCount < maxRetries && !success)
        {
            if (retryCount > 0)
            {
                Debug.Log($"JSONBin.io 로드 재시도 {retryCount}/{maxRetries - 1}...");
                
                // Connection 에러인 경우 더 긴 대기 시간 제공
                bool isConnectionError = retryCount == 1; // 첫 재시도는 Connection 에러일 가능성 높음
                float waitTime = isConnectionError ? 3f : Mathf.Max(2f, Mathf.Pow(2, retryCount - 1));
                yield return new WaitForSeconds(waitTime);
                
                // Connection 에러인 경우 연결 정리
                if (isConnectionError)
                {
                    CleanupPreviousConnections();
                }
            }

            // 요청 전에 이전 요청이 완전히 정리될 시간 제공 (첫 요청인 경우)
            if (retryCount == 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            UnityWebRequest request = null;
            try
            {
                request = UnityWebRequest.Get(url);
                request.SetRequestHeader("X-Access-Key", accessKey);
                
                // HTTP/2 프로토콜 오류 방지를 위한 설정
                // 타임아웃 설정 (30초)
                request.timeout = 30;
                
                // User-Agent 헤더 추가 (일부 서버에서 HTTP/1.1을 선호할 수 있음)
                request.SetRequestHeader("User-Agent", "Unity-WebRequest/1.0");
                request.SetRequestHeader("Connection", "close"); // HTTP/2 문제 해결

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string responseText = request.downloadHandler.text;
                        
                        TacticsDatabase database = null;
                        
                        // JSONBin.io v3 API 응답 파싱
                        // 응답 형식: {"record": {...}, "metadata": {...}}
                        // 방법 1: JSONBinResponseWrapper로 시도 (record가 객체인 경우)
                        try
                        {
                            var wrapper = JsonUtility.FromJson<JSONBinResponseWrapper>(responseText);
                            if (wrapper != null && wrapper.record != null && wrapper.record.tactics != null)
                            {
                                database = wrapper.record;
                            }
                        }
                        catch (Exception e1)
                        {
                            // 방법 2: JSONBinResponse로 시도 (record가 문자열인 경우)
                            try
                            {
                                var response = JsonUtility.FromJson<JSONBinResponse>(responseText);
                                if (response != null && !string.IsNullOrEmpty(response.record))
                                {
                                    // record가 JSON 문자열인 경우 파싱
                                    if (response.record.Trim().StartsWith("{"))
                                    {
                                        database = JsonUtility.FromJson<TacticsDatabase>(response.record);
                                    }
                                }
                            }
                            catch (Exception e2)
                            {
                                // 방법 3: 직접 TacticsDatabase로 파싱 시도
                                try
                                {
                                    database = JsonUtility.FromJson<TacticsDatabase>(responseText);
                                }
                                catch (Exception e3)
                                {
                                    Debug.LogError($"모든 파싱 방법 실패. e1: {e1.Message}, e2: {e2.Message}, e3: {e3.Message}");
                                }
                            }
                        }
                        
                        // 최종 체크: database가 null이거나 tactics가 null이면 빈 데이터베이스 생성
                        if (database == null || database.tactics == null)
                        {
                            database = new TacticsDatabase { tactics = new List<TacticsData>() };
                        }
                        
                        success = true;
                        onComplete?.Invoke(true, database);
                        yield break;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"JSON 파싱 실패: {e.Message}\nResponse: {request.downloadHandler.text}");
                        // 빈 데이터베이스 반환
                        success = true;
                        onComplete?.Invoke(true, new TacticsDatabase { tactics = new List<TacticsData>() });
                        yield break;
                    }
                }
                else
                {
                    // Connection 에러 또는 HTTP/2 프로토콜 오류인 경우 재시도
                    bool isConnectionError = request.error != null && 
                        (request.error.Contains("Connection") || 
                         request.error.Contains("Clean Clear") ||
                         request.error.Contains("was not closed cleanly"));
                    
                    bool isProtocolError = request.error != null && 
                        (request.error.Contains("PROTOCOL_ERROR") || 
                         request.error.Contains("stream") || 
                         request.error.Contains("Curl error 92") ||
                         request.responseCode == 0);
                    
                    if (isConnectionError || isProtocolError)
                    {
                        string errorType = isConnectionError ? "Connection" : "HTTP/2 프로토콜";
                        Debug.LogWarning($"{errorType} 오류: {request.error} - 재시도 예정...");
                        
                        // Connection 에러인 경우 연결 정리
                        if (isConnectionError)
                        {
                            CleanupPreviousConnections();
                        }
                        
                        retryCount++;
                        continue;
                    }
                    
                    Debug.LogError($"데이터 로드 실패: {request.error} (HTTP {request.responseCode})");
                    // 빈 데이터베이스 반환 (새로 시작)
                    success = true;
                    onComplete?.Invoke(true, new TacticsDatabase { tactics = new List<TacticsData>() });
                    yield break;
                }
            }
            finally
            {
                // 명시적으로 Dispose 호출
                if (request != null)
                {
                    request.Dispose();
                }
            }
        }

        // 모든 재시도 실패 시 빈 데이터베이스 반환
        if (!success)
        {
            Debug.LogError($"데이터 로드 실패 (재시도 {maxRetries}회 모두 실패)");
            onComplete?.Invoke(true, new TacticsDatabase { tactics = new List<TacticsData>() });
        }
    }

    /// <summary>
    /// JSONBin.io에 모든 Tactics 데이터 저장
    /// </summary>
    private IEnumerator SaveAllTactics(TacticsDatabase database, Action<bool, string> onComplete)
    {
        string url = $"{baseUrl}/{binId}";
        
        // JSON 직렬화 (pretty print 비활성화로 크기 최소화)
        string json = JsonUtility.ToJson(database, false);
        
        // JSON 크기 확인 (크기 제한은 10MB)
        int jsonSize = Encoding.UTF8.GetByteCount(json);
        if (jsonSize > 1024 * 1024 * 10) // 10MB
        {
            string errorMessage = $"JSON 데이터가 너무 큽니다: {jsonSize} bytes (최대 10MB)";
            Debug.LogError(errorMessage);
            onComplete?.Invoke(false, errorMessage);
            yield break;
        }
        
        // JSON 유효성 검사 (디버깅용)
        Debug.Log($"JSONBin.io 저장 시도: {jsonSize} bytes, {database.tactics.Count}개의 tactics");
        
        // JSONBin.io v3 API는 때때로 JSON을 문자열로 감싸야 할 수 있음
        // 하지만 일반적으로는 직접 JSON 객체를 보내는 것이 맞음
        // 먼저 직접 JSON 객체로 시도
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // 재시도 로직 (최대 5회)
        const int maxRetries = 5;
        int retryCount = 0;
        bool success = false;
        string lastError = null;

        while (retryCount < maxRetries && !success)
        {
            if (retryCount > 0)
            {
                Debug.Log($"JSONBin.io 저장 재시도 {retryCount}/{maxRetries - 1}...");
                
                // Connection 에러인 경우 더 긴 대기 시간 제공
                bool isConnectionError = retryCount == 1; // 첫 재시도는 Connection 에러일 가능성 높음
                float waitTime = isConnectionError ? 3f : Mathf.Max(2f, Mathf.Pow(2, retryCount - 1));
                yield return new WaitForSeconds(waitTime);
                
                // Connection 에러인 경우 연결 정리
                if (isConnectionError)
                {
                    CleanupPreviousConnections();
                }
            }

            // 요청 전에 이전 요청이 완전히 정리될 시간 제공 (첫 요청인 경우)
            if (retryCount == 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            UnityWebRequest request = null;
            try
            {
                request = new UnityWebRequest(url, "PUT");
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Access-Key", accessKey);
                request.SetRequestHeader("Connection", "close"); // HTTP/2 문제 해결
                
                // HTTP/2 프로토콜 오류 방지를 위한 설정
                // 타임아웃 설정 (30초)
                request.timeout = 30;
                
                // User-Agent 헤더 추가 (일부 서버에서 HTTP/1.1을 선호할 수 있음)
                request.SetRequestHeader("User-Agent", "Unity-WebRequest/1.0");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("JSONBin.io 저장 성공");
                    success = true;
                    onComplete?.Invoke(true, null);
                    yield break;
                }
                else
                {
                    string errorResponse = request.downloadHandler?.text ?? "No response";
                    string errorMessage = $"HTTP {request.responseCode}: {request.error}";
                    
                    // Connection 에러 또는 HTTP/2 프로토콜 오류인 경우 재시도
                    bool isConnectionError = request.error != null && 
                        (request.error.Contains("Connection") || 
                         request.error.Contains("Clean Clear") ||
                         request.error.Contains("was not closed cleanly"));
                    
                    bool isProtocolError = request.error != null && 
                        (request.error.Contains("PROTOCOL_ERROR") || 
                         request.error.Contains("stream") || 
                         request.error.Contains("Curl error 92") ||
                         request.responseCode == 0);
                    
                    if (isConnectionError || isProtocolError)
                    {
                        string errorType = isConnectionError ? "Connection" : "HTTP/2 프로토콜";
                        lastError = $"{errorType} 오류: {request.error}";
                        Debug.LogWarning($"{lastError} - 재시도 예정...");
                        
                        // Connection 에러인 경우 연결 정리
                        if (isConnectionError)
                        {
                            CleanupPreviousConnections();
                        }
                        
                        retryCount++;
                        continue;
                    }
                    
                    // 400 에러인 경우 상세 정보 추가
                    if (request.responseCode == 400)
                    {
                        errorMessage = $"400 Bad Request - 요청 형식이 잘못되었습니다.";
                        Debug.LogError(errorMessage);
                        Debug.LogError($"에러 응답: {errorResponse}");
                        Debug.LogError($"전송 시도한 JSON 크기: {jsonSize} bytes");
                        Debug.LogError($"전송 시도한 JSON (처음 1000자):\n{json.Substring(0, Math.Min(1000, json.Length))}...");
                        
                        // JSON 유효성 검사 시도
                        try
                        {
                            var testParse = JsonUtility.FromJson<TacticsDatabase>(json);
                            Debug.Log("JSON 파싱 테스트: 성공 (JSON 형식은 유효함)");
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"JSON 파싱 테스트 실패: {e.Message}");
                        }
                        
                        // 400 에러는 재시도하지 않음
                        onComplete?.Invoke(false, errorMessage);
                        yield break;
                    }
                    else if (request.responseCode == 403)
                    {
                        errorMessage = "403 Forbidden - Access Key 권한을 확인하세요. Read/Write 권한이 필요합니다.";
                        Debug.LogError(errorMessage);
                        // 403 에러는 재시도하지 않음
                        onComplete?.Invoke(false, errorMessage);
                        yield break;
                    }
                    else if (request.responseCode == 401)
                    {
                        errorMessage = "401 Unauthorized - Access Key가 유효하지 않습니다.";
                        Debug.LogError(errorMessage);
                        // 401 에러는 재시도하지 않음
                        onComplete?.Invoke(false, errorMessage);
                        yield break;
                    }
                    else if (request.responseCode == 404)
                    {
                        errorMessage = "404 Not Found - Bin ID가 존재하지 않습니다.";
                        Debug.LogError(errorMessage);
                        // 404 에러는 재시도하지 않음
                        onComplete?.Invoke(false, errorMessage);
                        yield break;
                    }
                    else
                    {
                        // 기타 네트워크 오류는 재시도
                        lastError = errorMessage;
                        Debug.LogWarning($"데이터 저장 실패: {errorMessage}\nResponse: {errorResponse} - 재시도 예정...");
                        retryCount++;
                        continue;
                    }
                }
            }
            finally
            {
                // 명시적으로 Dispose 호출
                if (request != null)
                {
                    request.Dispose();
                }
            }
        }

        // 모든 재시도 실패
        if (!success)
        {
            string finalError = lastError ?? "Unknown Error";
            Debug.LogError($"데이터 저장 실패 (재시도 {maxRetries}회 모두 실패): {finalError}");
            onComplete?.Invoke(false, finalError);
        }
    }

    /// <summary>
    /// 사용자 이름을 키로 사용 가능하도록 정리
    /// (특수문자 제거, 공백을 하이픈으로 변경)
    /// </summary>
    private string SanitizeUsername(string username)
    {
        if (string.IsNullOrEmpty(username))
            return "unknown";

        // 공백을 하이픈으로, 특수문자 제거
        string sanitized = username.Replace(" ", "-");
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[^a-zA-Z0-9\-_]", "");

        return sanitized.ToLower();
    }

    // ========== 데이터 클래스 ==========

    /// <summary>
    /// JSONBin.io 응답 구조 (record가 문자열인 경우)
    /// </summary>
    [Serializable]
    private class JSONBinResponse
    {
        public string record; // JSON 문자열 또는 객체
    }

    /// <summary>
    /// JSONBin.io 응답 구조 (record가 객체인 경우)
    /// </summary>
    [Serializable]
    private class JSONBinResponseWrapper
    {
        public TacticsDatabase record;
    }

    /// <summary>
    /// Tactics 데이터베이스 구조
    /// </summary>
    [Serializable]
    private class TacticsDatabase
    {
        public List<TacticsData> tactics = new List<TacticsData>();
    }

    /// <summary>
    /// 개별 Tactics 데이터 구조
    /// </summary>
    [Serializable]
    private class TacticsData
    {
        public string key;
        public string username;
        public string timestamp;
        public string tacticsJson;
    }
}

