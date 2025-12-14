using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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
    
    // HttpClient 인스턴스 (HTTP/2 문제 해결을 위해 사용)
    private static HttpClient httpClient = null;
    private static readonly object httpClientLock = new object();

    void Awake()
    {
        // 씬마다 독립적인 인스턴스 사용
        Instance = this;
        
        // HttpClient 초기화 (HTTP/2 문제 해결)
        InitializeHttpClient();
        
        // JSONBin.io는 별도 초기화 불필요 (항상 사용 가능)
        isInitialized = !string.IsNullOrEmpty(binId) && !string.IsNullOrEmpty(accessKey);
        
        if (!isInitialized)
        {
            Debug.LogWarning("JSONBinManager: Bin ID 또는 Access Key가 설정되지 않았습니다.");
        }
        else
        {
            Debug.Log("JSONBinManager: JSONBin.io 초기화 완료 (HttpClient 사용)");
        }
    }

    void OnDestroy()
    {
        // HttpClient 정리 (선택적 - 정적 인스턴스이므로 유지할 수도 있음)
        // DisposeHttpClient();
    }

    /// <summary>
    /// HttpClient 초기화 (HTTP/2 문제 해결)
    /// </summary>
    private void InitializeHttpClient()
    {
        lock (httpClientLock)
        {
            if (httpClient == null)
            {
                var handler = new HttpClientHandler();
                
                // HTTP/1.1을 강제하기 위한 설정
                // HttpClient는 기본적으로 HTTP/2를 지원하지만, 
                // 서버가 HTTP/1.1만 지원하면 자동으로 다운그레이드됨
                
                httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
                
                // 기본 헤더 설정
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Unity-HttpClient/1.0");
                
                Debug.Log("JSONBinManager: HttpClient 초기화 완료");
            }
        }
    }

    /// <summary>
    /// HttpClient 정리 (필요시 사용)
    /// </summary>
    private void DisposeHttpClient()
    {
        lock (httpClientLock)
        {
            if (httpClient != null)
            {
                httpClient.Dispose();
                httpClient = null;
                Debug.Log("JSONBinManager: HttpClient 정리 완료");
            }
        }
    }

    /// <summary>
    /// 비정상 종료 후 재시작 시 이전 연결 정리
    /// </summary>
    private void CleanupPreviousConnections()
    {
        try
        {
            // UnityWebRequest의 쿠키 캐시 정리 (이전 코드와의 호환성)
            UnityWebRequest.ClearCookieCache();
            
            // HttpClient는 정적 인스턴스이므로 별도 정리 불필요
            // 필요시 HttpClient를 재생성할 수 있음
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
    /// JSONBin.io에서 모든 Tactics 데이터 로드 (HttpClient 사용)
    /// </summary>
    private IEnumerator LoadAllTactics(Action<bool, TacticsDatabase> onComplete)
    {
        string url = $"{baseUrl}/{binId}/latest";
        
        // HttpClient를 사용한 비동기 요청을 코루틴으로 변환
        Task<TacticsDatabase> loadTask = LoadAllTacticsAsync(url);
        
        // Task가 완료될 때까지 대기
        while (!loadTask.IsCompleted)
        {
            yield return null;
        }
        
        // 결과 처리
        if (loadTask.IsFaulted)
        {
            Debug.LogError($"데이터 로드 실패: {loadTask.Exception?.GetBaseException()?.Message}");
            onComplete?.Invoke(true, new TacticsDatabase { tactics = new List<TacticsData>() });
        }
        else
        {
            onComplete?.Invoke(true, loadTask.Result);
        }
    }

    /// <summary>
    /// HttpClient를 사용한 비동기 로드
    /// </summary>
    private async Task<TacticsDatabase> LoadAllTacticsAsync(string url)
    {
        const int maxRetries = 3;
        int retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            try
            {
                lock (httpClientLock)
                {
                    if (httpClient == null)
                    {
                        InitializeHttpClient();
                    }
                }
                
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Add("X-Access-Key", accessKey);
                    
                    var response = await httpClient.SendAsync(request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();
                        
                        TacticsDatabase database = null;
                        
                        // JSONBin.io v3 API 응답 파싱
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
                            try
                            {
                                var jsonResponse = JsonUtility.FromJson<JSONBinResponse>(responseText);
                                if (jsonResponse != null && !string.IsNullOrEmpty(jsonResponse.record))
                                {
                                    if (jsonResponse.record.Trim().StartsWith("{"))
                                    {
                                        database = JsonUtility.FromJson<TacticsDatabase>(jsonResponse.record);
                                    }
                                }
                            }
                            catch (Exception e2)
                            {
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
                        
                        if (database == null || database.tactics == null)
                        {
                            database = new TacticsDatabase { tactics = new List<TacticsData>() };
                        }
                        
                        Debug.Log("JSONBin.io 로드 성공 (HttpClient)");
                        return database;
                    }
                    else
                    {
                        string errorText = await response.Content.ReadAsStringAsync();
                        Debug.LogWarning($"HTTP {response.StatusCode}: {errorText}");
                        
                        // 4xx 에러는 재시도하지 않음
                        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                        {
                            return new TacticsDatabase { tactics = new List<TacticsData>() };
                        }
                        
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            await Task.Delay(1000 * retryCount); // 지수 백오프
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"JSONBin.io 로드 오류 (재시도 {retryCount + 1}/{maxRetries}): {e.Message}");
                retryCount++;
                
                if (retryCount < maxRetries)
                {
                    await Task.Delay(1000 * retryCount);
                }
                else
                {
                    Debug.LogError($"데이터 로드 실패 (재시도 {maxRetries}회 모두 실패): {e.Message}");
                    return new TacticsDatabase { tactics = new List<TacticsData>() };
                }
            }
        }
        
        return new TacticsDatabase { tactics = new List<TacticsData>() };
    }

    /// <summary>
    /// JSONBin.io에 모든 Tactics 데이터 저장 (HttpClient 사용)
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
        
        Debug.Log($"JSONBin.io 저장 시도: {jsonSize} bytes, {database.tactics.Count}개의 tactics");
        
        // HttpClient를 사용한 비동기 요청을 코루틴으로 변환
        Task<bool> saveTask = SaveAllTacticsAsync(url, json, jsonSize);
        
        // Task가 완료될 때까지 대기
        while (!saveTask.IsCompleted)
        {
            yield return null;
        }
        
        // 결과 처리
        if (saveTask.IsFaulted)
        {
            string errorMessage = saveTask.Exception?.GetBaseException()?.Message ?? "Unknown Error";
            Debug.LogError($"데이터 저장 실패: {errorMessage}");
            onComplete?.Invoke(false, errorMessage);
        }
        else
        {
            bool success = saveTask.Result;
            if (success)
            {
                Debug.Log("JSONBin.io 저장 성공 (HttpClient)");
                onComplete?.Invoke(true, null);
            }
            else
            {
                onComplete?.Invoke(false, "Save failed");
            }
        }
    }

    /// <summary>
    /// HttpClient를 사용한 비동기 저장
    /// </summary>
    private async Task<bool> SaveAllTacticsAsync(string url, string json, int jsonSize)
    {
        const int maxRetries = 3;
        int retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            try
            {
                lock (httpClientLock)
                {
                    if (httpClient == null)
                    {
                        InitializeHttpClient();
                    }
                }
                
                using (var request = new HttpRequestMessage(HttpMethod.Put, url))
                {
                    request.Headers.Add("X-Access-Key", accessKey);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.SendAsync(request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Debug.Log("JSONBin.io 저장 성공 (HttpClient)");
                        return true;
                    }
                    else
                    {
                        string errorText = await response.Content.ReadAsStringAsync();
                        string errorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                        
                        // 4xx 에러는 재시도하지 않음
                        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                        {
                            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                            {
                                Debug.LogError($"400 Bad Request - 요청 형식이 잘못되었습니다.");
                                Debug.LogError($"에러 응답: {errorText}");
                                Debug.LogError($"전송 시도한 JSON 크기: {jsonSize} bytes");
                                
                                // JSON 유효성 검사
                                try
                                {
                                    var testParse = JsonUtility.FromJson<TacticsDatabase>(json);
                                    Debug.Log("JSON 파싱 테스트: 성공 (JSON 형식은 유효함)");
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"JSON 파싱 테스트 실패: {e.Message}");
                                }
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                            {
                                errorMessage = "403 Forbidden - Access Key 권한을 확인하세요. Read/Write 권한이 필요합니다.";
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                errorMessage = "401 Unauthorized - Access Key가 유효하지 않습니다.";
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                errorMessage = "404 Not Found - Bin ID가 존재하지 않습니다.";
                            }
                            
                            Debug.LogError(errorMessage);
                            return false;
                        }
                        
                        // 5xx 에러는 재시도
                        Debug.LogWarning($"{errorMessage} - 재시도 예정...");
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            await Task.Delay(1000 * retryCount);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"JSONBin.io 저장 오류 (재시도 {retryCount + 1}/{maxRetries}): {e.Message}");
                retryCount++;
                
                if (retryCount < maxRetries)
                {
                    await Task.Delay(1000 * retryCount);
                }
                else
                {
                    Debug.LogError($"데이터 저장 실패 (재시도 {maxRetries}회 모두 실패): {e.Message}");
                    return false;
                }
            }
        }
        
        return false;
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

