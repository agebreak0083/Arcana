using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

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
    
    private string baseUrl = "https://arcana.koreacentral.cloudapp.azure.com/api";
    public bool isInitialized { get; private set; } = false;
    
    // 캐시된 Tactics 데이터
    private static TacticsDatabase cachedTacticsDatabase = null;
    private static bool isCacheValid = false;
    
    // 중복 호출 방지: 진행 중인 요청 추적
    private static bool isLoadingInProgress = false;
    private static List<Action<bool, TacticsDatabase>> pendingCallbacks = new List<Action<bool, TacticsDatabase>>();

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // JSONBin.io는 별도 초기화 불필요 (항상 사용 가능)
        isInitialized = !string.IsNullOrEmpty(binId) && !string.IsNullOrEmpty(accessKey);
        
        if (!isInitialized)
        {
            Debug.LogWarning("JsonBinManager: Bin ID 또는 Access Key가 설정되지 않았습니다.");
        }
        else
        {
            Debug.Log("JsonBinManager: JSONBin.io 초기화 완료");
        }
        
        // 모든 데이터를 로드 한다. 
        Debug.Log("JsonBinManager: [Awake] LoadAllTactics 호출 시작");
        StartCoroutine(LoadAllTactics((success, database) =>
        {
            if (success)
            {
                cachedTacticsDatabase = database;
                isCacheValid = true;
                Debug.Log($"JsonBinManager: [Awake] LoadAllTactics 완료 - {database.tactics.Count}개 데이터");
            }
            else
            {
                Debug.LogError("JsonBinManager: [Awake] LoadAllTactics 실패");
            }
        }));
    }

    /// <summary>
    /// Tactics 데이터를 JSONBin.io에 저장
    /// </summary>
    /// <param name="tacticsJson">저장할 tactics.json 내용</param>
    /// <param name="onComplete">완료 콜백</param>
    public void SaveTactics(string tacticsJson, Action<bool, string> onComplete = null)
    {
        Debug.Log("JsonBinManager: [SaveTactics] 호출됨");
        if (!isInitialized)
        {
            Debug.LogError("JsonBinManager: [SaveTactics] 초기화되지 않음");
            onComplete?.Invoke(false, "Not initialized");
            return;
        }

        if (UserDataManager.Instance == null || UserDataManager.Instance.currentUserData == null)
        {
            Debug.LogError("JsonBinManager: [SaveTactics] UserDataManager 초기화되지 않음");
            onComplete?.Invoke(false, "UserDataManager not initialized");
            return;
        }

        // Key 생성: Username_시간 (Firebase와 동일한 형식)
        string username = SanitizeUsername(UserDataManager.Instance.currentUserData.playerName);
        string timestamp = DateTime.Now.ToString("yyMMddHHmm");
        string key = $"{username}_{timestamp}";
        Debug.Log($"JsonBinManager: [SaveTactics] Key 생성: {key}");

        // 기존 데이터 로드 후 새 항목 추가
        Debug.Log("JsonBinManager: [SaveTactics] LoadAllTactics 호출 (저장 전 데이터 로드)");
        StartCoroutine(LoadAllTactics((success, allTactics) =>
        {
            if (!success)
            {
                allTactics = new TacticsDatabase { tactics = new List<TacticsData>() };
                Debug.LogError("JsonBinManager: [SaveTactics] Tactics 데이터 로드 실패");
            }
            else
            {
                Debug.Log($"JsonBinManager: [SaveTactics] 기존 데이터 로드 완료 - {allTactics.tactics.Count}개");
            }

            // 중복 키 체크 (같은 키가 있으면 제거)
            int removedCount = allTactics.tactics.RemoveAll(t => t.key == key);
            if (removedCount > 0)
            {
                Debug.Log($"JsonBinManager: [SaveTactics] 중복 키 제거: {removedCount}개");
            }

            // 새 데이터 추가
            var newTactic = new TacticsData
            {
                key = key,
                username = UserDataManager.Instance.currentUserData.playerName,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                tacticsJson = tacticsJson
            };

            allTactics.tactics.Add(newTactic);
            Debug.Log($"JsonBinManager: [SaveTactics] 새 데이터 추가 완료. 총 {allTactics.tactics.Count}개");

            // 전체 데이터 저장
            Debug.Log("JsonBinManager: [SaveTactics] SaveAllTactics 호출 시작");
            StartCoroutine(SaveAllTactics(allTactics, (saveSuccess) =>
            {
                if (saveSuccess)
                {
                    Debug.Log($"JsonBinManager: [SaveTactics] 저장 성공: {key}");
                    onComplete?.Invoke(true, key);
                }
                else
                {
                    Debug.LogError("JsonBinManager: [SaveTactics] 저장 실패");
                    onComplete?.Invoke(false, "Save failed");
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
        Debug.Log($"JsonBinManager: [LoadTactics] 호출됨 - Key: {key}");
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
                Debug.Log($"JsonBinManager: [LoadTactics] 데이터 로드 성공: {key}");
                onComplete?.Invoke(true, tactic.tacticsJson);
            }
            else
            {
                Debug.LogWarning($"JsonBinManager: [LoadTactics] 데이터를 찾을 수 없음: {key}");
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
        Debug.Log($"JsonBinManager: [GetUserTacticsKeys] 호출됨 - Username: {username}");
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

            Debug.Log($"JsonBinManager: [GetUserTacticsKeys] 완료 - 유저 {username}의 Tactics 키 {keys.Count}개");
            onComplete?.Invoke(true, keys);
        }));
    }

    /// <summary>
    /// JSONBin.io에서 랜덤 Tactics 데이터 가져오기 (적 편성용)
    /// </summary>
    /// <param name="onComplete">완료 콜백 (성공 여부, tactics JSON, username)</param>
    public void GetRandomTactics(string enemyName, Action<bool, string, string> onComplete)
    {
        Debug.Log($"JsonBinManager: [GetRandomTactics] 호출됨 - EnemyName: {enemyName}");
        if (!isInitialized)
        {
            Debug.LogWarning("JsonBinManager: [GetRandomTactics] 초기화되지 않음. 로컬 파일 사용");
            onComplete?.Invoke(false, null, null);
            return;
        }

        StartCoroutine(LoadAllTactics((success, allTactics) =>
        {
            if (!success || allTactics == null || allTactics.tactics.Count == 0)
            {
                Debug.LogWarning("JsonBinManager: [GetRandomTactics] JSONBin.io에 저장된 데이터 없음. 로컬 파일 사용");
                onComplete?.Invoke(false, null, null);
                return;
            }

            // enemyName과 일치하는 데이터 선택
            var matchingTactics = allTactics.tactics.FindAll(t => t.key == enemyName);
            if(matchingTactics.Count == 0)
            {
                Debug.Log($"JsonBinManager: [GetRandomTactics] enemyName {enemyName}의 데이터를 찾을 수 없음. 랜덤 선택");                

                int randomIndex = UnityEngine.Random.Range(0, allTactics.tactics.Count);
                var randomTactic = allTactics.tactics[randomIndex];
                matchingTactics.Add(randomTactic);
            }

            var selectedTactic = matchingTactics[0];

            if (string.IsNullOrEmpty(selectedTactic.tacticsJson))
            {
                Debug.LogError($"JsonBinManager: [GetRandomTactics] tacticsJson이 비어있음! Key: {selectedTactic.key}, Username: {selectedTactic.username}");
                onComplete?.Invoke(false, null, null);
                return;
            }

            Debug.Log($"JsonBinManager: [GetRandomTactics] 로드 성공: {selectedTactic.key} (유저: {selectedTactic.username})");
            onComplete?.Invoke(true, selectedTactic.tacticsJson, selectedTactic.key);
        }));
    }

    /// <summary>
    /// JSONBin.io에서 모든 Tactics 데이터 로드 (public, 랭킹 계산용)
    /// </summary>
    /// <param name="onComplete">완료 콜백 (성공 여부, TacticsDatabase)</param>
    public void GetAllTactics(System.Action<bool, TacticsDatabase> onComplete)
    {
        Debug.Log("JsonBinManager: [GetAllTactics] 호출됨");
        if (!isInitialized)
        {
            onComplete?.Invoke(false, null);
            return;
        }

        StartCoroutine(LoadAllTactics(onComplete));
    }

    /// <summary>
    /// 캐시를 무효화합니다 (새 데이터를 강제로 로드하려는 경우 사용)
    /// </summary>
    public void InvalidateCache()
    {
        Debug.Log("JsonBinManager: [InvalidateCache] 캐시 무효화");
        isCacheValid = false;
        cachedTacticsDatabase = null;
    }

    // ========== 내부 메서드 ==========

    /// <summary>
    /// JSONBin.io에서 모든 Tactics 데이터 로드
    /// </summary>
    private IEnumerator LoadAllTactics(Action<bool, TacticsDatabase> onComplete)
    {
        // 캐시된 데이터가 있고 유효하면 바로 반환
        if(cachedTacticsDatabase != null && isCacheValid)
        {
            Debug.Log($"JsonBinManager: [LoadAllTactics] 캐시 사용 - {cachedTacticsDatabase.tactics.Count}개 데이터 (네트워크 요청 없음)");
            onComplete?.Invoke(true, cachedTacticsDatabase);
            yield break;
        }

        // 이미 진행 중인 요청이 있으면 콜백만 추가하고 대기
        if (isLoadingInProgress)
        {
            Debug.Log($"JsonBinManager: [LoadAllTactics] 이미 진행 중인 요청이 있음. 콜백 대기 목록에 추가 (대기 중인 콜백: {pendingCallbacks.Count + 1}개)");
            pendingCallbacks.Add(onComplete);
            yield break;
        }

        // 요청 시작 표시
        isLoadingInProgress = true;
        pendingCallbacks.Add(onComplete);

        string url = $"{baseUrl}/data/tactics";        

        int maxRetries = 3;
        int retryCount = 0;
        bool success = false;
        TacticsDatabase resultDatabase = null;

        Debug.Log($"JsonBinManager: [LoadAllTactics] 네트워크 요청 시작 - URL: {url} (대기 중인 콜백: {pendingCallbacks.Count}개)");

        while (retryCount < maxRetries && !success)
        {
            if (retryCount > 0)
            {
                Debug.Log($"JsonBinManager: [LoadAllTactics] 재시도 {retryCount}/{maxRetries}");
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                //request.SetRequestHeader("X-Access-Key", accessKey);
                request.timeout = 30; // 타임아웃 설정

                Debug.Log($"JsonBinManager: [LoadAllTactics] HTTP GET 요청 전송 (시도 {retryCount + 1}/{maxRetries})");
                yield return request.SendWebRequest();
                Debug.Log($"JsonBinManager: [LoadAllTactics] HTTP GET 응답 수신 - Result: {request.result}, Code: {request.responseCode}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {                        
                        string responseText = request.downloadHandler.text;
                        
                        TacticsDatabase database = null;
                        
                        // 커스텀 서버 응답 파싱
                        // 응답 형식: {"id": "tactics", "content": {"tactics": [...]}}
                        // 방법 1: CustomServerResponse로 시도 (커스텀 서버 형식)
                        try
                        {
                            var customResponse = JsonUtility.FromJson<CustomServerResponse>(responseText);
                            if (customResponse != null && customResponse.content != null && customResponse.content.tactics != null)
                            {
                                database = customResponse.content;
                                Debug.Log($"JsonBinManager: [LoadAllTactics] 커스텀 서버 응답 파싱 성공 - {database.tactics.Count}개 데이터");
                            }
                        }
                        catch (Exception e1)
                        {
                            // 방법 2: 직접 TacticsDatabase로 파싱 시도 (content 없이 직접 tactics 배열인 경우)
                            try
                            {
                                database = JsonUtility.FromJson<TacticsDatabase>(responseText);
                                if (database != null && database.tactics != null)
                                {
                                    Debug.Log($"JsonBinManager: [LoadAllTactics] 직접 TacticsDatabase 파싱 성공 - {database.tactics.Count}개 데이터");
                                }
                            }
                            catch (Exception e2)
                            {
                                Debug.LogError($"JsonBinManager: [LoadAllTactics] 모든 파싱 방법 실패. e1: {e1.Message}, e2: {e2.Message}");
                            }
                        }
                        
                        // 최종 체크: database가 null이거나 tactics가 null이면 빈 데이터베이스 생성
                        if (database == null || database.tactics == null)
                        {
                            database = new TacticsDatabase { tactics = new List<TacticsData>() };
                        }
                        
                        // 캐시에 저장
                        cachedTacticsDatabase = database;
                        isCacheValid = true;

                        Debug.Log($"JsonBinManager: [LoadAllTactics] 로드 완료 - {database.tactics.Count}개 데이터 (캐시 저장됨)");
                        
                        resultDatabase = database;
                        success = true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"JsonBinManager: [LoadAllTactics] JSON 파싱 실패: {e.Message}");
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            Debug.LogError($"JsonBinManager: [LoadAllTactics] 최대 재시도 횟수 도달. 빈 데이터베이스 반환");
                            // 빈 데이터베이스 생성 및 캐시
                            var emptyDatabase = new TacticsDatabase { tactics = new List<TacticsData>() };
                            cachedTacticsDatabase = emptyDatabase;
                            isCacheValid = true;
                            resultDatabase = emptyDatabase;
                            success = true; // 재시도하지 않도록 설정
                        }
                        // retryCount < maxRetries인 경우 success는 false로 유지되어 재시도됨
                    }
                }
                else
                {
                    // 에러 메시지 및 응답 본문 추출
                    string errorMessage = request.error ?? "Unknown Error";
                    string responseText = request.downloadHandler?.text ?? "";
                    
                    // 서버 응답 본문 로그 출력 (에러 시 중요)
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        string responsePreview = responseText.Length > 500 ? responseText.Substring(0, 500) + "..." : responseText;
                        Debug.LogWarning($"JsonBinManager: [LoadAllTactics] 서버 응답 본문: {responsePreview}");
                    }
                    
                    // PROTOCOL_ERROR 또는 네트워크 에러 체크
                    bool isRetryableError = (
                        request.result == UnityWebRequest.Result.ConnectionError ||
                        request.result == UnityWebRequest.Result.ProtocolError ||
                        (!string.IsNullOrEmpty(errorMessage) && (
                            errorMessage.Contains("PROTOCOL_ERROR") || 
                            errorMessage.Contains("NetworkError") ||
                            errorMessage.Contains("Unable to complete SSL connection") ||
                            errorMessage.Contains("ConnectionError")
                        )) ||
                        // HTTP 500 에러도 재시도 가능 (서버 일시적 오류)
                        (request.responseCode >= 500 && request.responseCode < 600)
                    );
                    
                    if (isRetryableError && retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        Debug.LogWarning($"JsonBinManager: [LoadAllTactics] 네트워크 에러 발생 (재시도 {retryCount}/{maxRetries}): {errorMessage} (HTTP {request.responseCode})");
                        yield return new WaitForSeconds(1f * retryCount); // 지수 백오프
                        continue;
                    }
                    
                    // 최종 에러 로그 (더 상세한 정보 포함)
                    string finalErrorMessage = string.IsNullOrEmpty(errorMessage) ? "Unknown Error" : errorMessage;
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        Debug.LogError($"JsonBinManager: [LoadAllTactics] 데이터 로드 실패: {finalErrorMessage} (HTTP {request.responseCode})\n서버 응답: {responseText}");
                    }
                    else
                    {
                        Debug.LogError($"JsonBinManager: [LoadAllTactics] 데이터 로드 실패: {finalErrorMessage} (HTTP {request.responseCode})");
                    }
                    
                    // 빈 데이터베이스 생성 및 캐시
                    var emptyDatabase = new TacticsDatabase { tactics = new List<TacticsData>() };
                    cachedTacticsDatabase = emptyDatabase;
                    isCacheValid = true;
                    resultDatabase = emptyDatabase;
                    break;
                }
                
                // catch 블록에서 재시도가 필요한 경우 여기서 처리
                if (!success && retryCount < maxRetries)
                {
                    Debug.LogWarning($"JsonBinManager: [LoadAllTactics] 재시도 대기 {retryCount}/{maxRetries}...");
                    yield return new WaitForSeconds(1f * retryCount); // 지수 백오프
                    continue;
                }
            }
        }

        // 모든 대기 중인 콜백 호출
        if (resultDatabase != null)
        {
            Debug.Log($"JsonBinManager: [LoadAllTactics] 모든 대기 중인 콜백 호출 ({pendingCallbacks.Count}개)");
            foreach (var callback in pendingCallbacks)
            {
                callback?.Invoke(success, resultDatabase);
            }
        }
        else
        {
            // 실패한 경우 빈 데이터베이스 반환
            var emptyDatabase = new TacticsDatabase { tactics = new List<TacticsData>() };
            Debug.LogWarning($"JsonBinManager: [LoadAllTactics] 요청 실패. 빈 데이터베이스로 모든 콜백 호출 ({pendingCallbacks.Count}개)");
            foreach (var callback in pendingCallbacks)
            {
                callback?.Invoke(false, emptyDatabase);
            }
        }

        // 상태 초기화
        pendingCallbacks.Clear();
        isLoadingInProgress = false;
        Debug.Log($"JsonBinManager: [LoadAllTactics] 요청 완료. 상태 초기화");
    }

    /// <summary>
    /// JSONBin.io에 모든 Tactics 데이터 저장
    /// </summary>
    private IEnumerator SaveAllTactics(TacticsDatabase database, Action<bool> onComplete)
    {
        Debug.Log($"JsonBinManager: [SaveAllTactics] 시작 - {database?.tactics?.Count ?? 0}개 데이터");
        // 최신 100개만 유지 (timestamp 기준)
        if (database.tactics != null && database.tactics.Count > 100)
        {
            // timestamp를 기준으로 정렬 (최신순)
            var tacticsWithTimestamp = new List<(TacticsData tactic, DateTime timestamp)>();
            
            foreach (var tactic in database.tactics)
            {                
                DateTime timestamp = DateTime.MinValue;
                if (!string.IsNullOrEmpty(tactic.timestamp))
                {
                    try
                    {
                        // timestamp 파싱 ("yyyy-MM-dd HH:mm:ss" 형식)
                        if (DateTime.TryParse(tactic.timestamp, out DateTime parsedTime))
                        {
                            timestamp = parsedTime;
                        }
                        else
                        {
                            // 파싱 실패 시 현재 시간 사용 (최신으로 처리)
                            timestamp = DateTime.Now;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"JsonBinManager: [SaveAllTactics] Timestamp 파싱 실패: {e.Message}, timestamp: {tactic.timestamp}");
                        // 파싱 실패 시 현재 시간 사용
                        timestamp = DateTime.Now;
                    }
                }
                else
                {
                    // timestamp가 없으면 현재 시간 사용 (최신으로 처리)
                    timestamp = DateTime.Now;
                }                

                tacticsWithTimestamp.Add((tactic, timestamp));
            }
            
            // Timestamp 최신순으로 정렬 (내림차순)
            tacticsWithTimestamp.Sort((a, b) => b.timestamp.CompareTo(a.timestamp));
            
            // 최신 100개만 유지
            database.tactics = tacticsWithTimestamp.Take(100).Select(x => x.tactic).ToList();
            
            Debug.Log($"JsonBinManager: [SaveAllTactics] 최신 100개만 유지: {tacticsWithTimestamp.Count}개 -> {database.tactics.Count}개");
        }       

        // 커스텀 서버에도 저장 
        yield return StartCoroutine(SaveToCustomServer(database, onComplete));                       
    }

    /// <summary>
    /// 커스텀 서버에 Tactics 데이터 저장
    /// </summary>
    /// <param name="database">저장할 database</param>
    /// <param name="onComplete">완료 콜백</param>
    private IEnumerator SaveToCustomServer(TacticsDatabase database, Action<bool> onComplete)
    {
        string customServerUrl = baseUrl + "/data";
        
        // 요청 Body 생성 - content는 JSON 객체여야 함
        var requestBody = new CustomServerRequest
        {
            id = "tactics",
            content = database
        };        
        
        string requestJson = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
        
        Debug.Log($"JsonBinManager: [SaveToCustomServer] 커스텀 서버 저장 시작 - URL: {customServerUrl}");
        
        int maxRetries = 2;
        int retryCount = 0;
        bool success = false;
        
        while (retryCount < maxRetries && !success)
        {
            if (retryCount > 0)
            {
                Debug.Log($"JsonBinManager: [SaveToCustomServer] 재시도 {retryCount}/{maxRetries}");
                yield return new WaitForSeconds(1f * retryCount);
            }
            
            // UnityWebRequest.Post를 사용하여 POST 요청 생성
            using (UnityWebRequest request = UnityWebRequest.PostWwwForm(customServerUrl, ""))
            {
                // POST 요청의 body를 설정
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 30;
                
                Debug.Log($"JsonBinManager: [SaveToCustomServer] HTTP POST 요청 전송 (시도 {retryCount + 1}/{maxRetries})");
                yield return request.SendWebRequest();
                
                try
                {
                    string errorMessage = request.error ?? "Unknown error";
                    string responseText = request.downloadHandler?.text ?? "";
                    
                    Debug.Log($"JsonBinManager: [SaveToCustomServer] HTTP POST 응답 수신 - Result: {request.result}, Code: {request.responseCode}, Error: {errorMessage}");
                    
                    // 서버 응답 본문 로그 출력 (에러 시 중요)
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        string responsePreview = responseText.Length > 500 ? responseText.Substring(0, 500) + "..." : responseText;
                        Debug.Log($"JsonBinManager: [SaveToCustomServer] 서버 응답 본문: {responsePreview}");
                    }
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"JsonBinManager: [SaveToCustomServer] 커스텀 서버 저장 성공");
                        success = true;
                    }
                    else
                    {
                        // 400, 405 에러는 재시도하지 않음 (클라이언트 요청 형식 문제 또는 서버가 메서드를 허용하지 않음)
                        if (request.responseCode == 400)
                        {
                            Debug.LogError($"JsonBinManager: [SaveToCustomServer] HTTP 400 Bad Request - 서버가 요청을 이해하지 못했습니다.");
                            Debug.LogError($"JsonBinManager: [SaveToCustomServer] 전송한 JSON 형식을 확인하세요. 서버 응답: {responseText}");
                            break; // 재시도하지 않음
                        }
                        else if (request.responseCode == 405)
                        {
                            Debug.LogError($"JsonBinManager: [SaveToCustomServer] HTTP 405 Method Not Allowed - 서버가 POST 메서드를 허용하지 않습니다.");
                            Debug.LogError($"JsonBinManager: [SaveToCustomServer] 서버 URL 또는 메서드를 확인하세요. URL: {customServerUrl}, 서버 응답: {responseText}");
                            break; // 재시도하지 않음
                        }
                        
                        bool isRetryableError = request.result == UnityWebRequest.Result.ConnectionError ||
                                               request.result == UnityWebRequest.Result.ProtocolError ||
                                               (!string.IsNullOrEmpty(errorMessage) && (
                                                   errorMessage.Contains("PROTOCOL_ERROR") || 
                                                   errorMessage.Contains("NetworkError") ||
                                                   errorMessage.Contains("Unable to complete SSL connection") ||
                                                   errorMessage.Contains("ConnectionError")
                                               ));
                        
                        if (isRetryableError && retryCount < maxRetries - 1)
                        {
                            retryCount++;
                            Debug.LogWarning($"JsonBinManager: [SaveToCustomServer] 네트워크 에러 발생 (재시도 {retryCount}/{maxRetries}): Result={request.result}, Error={errorMessage}");
                            continue;
                        }
                        else
                        {
                            Debug.LogWarning($"JsonBinManager: [SaveToCustomServer] 커스텀 서버 저장 실패 (무시됨): Result={request.result}, Error={errorMessage} (HTTP {request.responseCode})");
                            // 커스텀 서버 저장 실패는 무시 (JSONBin.io 저장은 성공했으므로)
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"JsonBinManager: [SaveToCustomServer] 응답 처리 중 예외 발생 (무시됨): {ex.GetType().Name} - {ex.Message}");
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        Debug.LogWarning($"JsonBinManager: [SaveToCustomServer] 최대 재시도 횟수 도달. 커스텀 서버 저장 실패 (무시됨)");
                        break;
                    }
                }
            }
        }

        onComplete?.Invoke(success);
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
    /// TacticsFileData 구조 (score 추출용)
    /// </summary>
    [Serializable]
    private class TacticsFileData
    {
        public string username;
        public int score = 0;
        public int winCount = 0;
        public int loseCount = 0;
    }

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
    public class TacticsDatabase
    {
        public List<TacticsData> tactics = new List<TacticsData>();
    }

    /// <summary>
    /// 개별 Tactics 데이터 구조
    /// </summary>
    [Serializable]
    public class TacticsData
    {
        public string key;
        public string username;
        public string timestamp;
        public string tacticsJson;
    }

    /// <summary>
    /// 커스텀 서버 요청 Body 구조
    /// </summary>
    [Serializable]
    private class CustomServerRequest
    {
        public string id;
        public TacticsDatabase content;  // string → TacticsDatabase로 변경 (JSON 객체)
    }

    /// <summary>
    /// 커스텀 서버 응답 구조
    /// </summary>
    [Serializable]
    private class CustomServerResponse
    {
        public string id;
        public TacticsDatabase content;
    }
}


