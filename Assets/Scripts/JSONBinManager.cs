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
    
    private string baseUrl = "https://api.jsonbin.io/v3/b";
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

    /// <summary>
    /// TacticsDatabase를 서버에 저장 (외부에서 호출 가능)
    /// </summary>
    public void SaveTacticsDatabase(TacticsDatabase database, Action<bool> onComplete = null)
    {
        Debug.Log($"JsonBinManager: [SaveTacticsDatabase] 호출됨 - {database?.tactics?.Count ?? 0}개 데이터");
        if (!isInitialized)
        {
            Debug.LogError("JsonBinManager: [SaveTacticsDatabase] 초기화되지 않음");
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(SaveAllTactics(database, (success) =>
        {
            if (success)
            {
                Debug.Log($"JsonBinManager: [SaveTacticsDatabase] 저장 완료");
            }
            else
            {
                Debug.LogError("JsonBinManager: [SaveTacticsDatabase] 저장 실패");
            }
            onComplete?.Invoke(success);
        }));
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

        string url = $"{baseUrl}/{binId}/latest";
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
                request.SetRequestHeader("X-Access-Key", accessKey);
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
                                    Debug.LogError($"JsonBinManager: [LoadAllTactics] 모든 파싱 방법 실패. e1: {e1.Message}, e2: {e2.Message}, e3: {e3.Message}");
                                }
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
                    // PROTOCOL_ERROR 또는 네트워크 에러 체크
                    bool isRetryableError = !string.IsNullOrEmpty(request.error) && (
                        request.error.Contains("PROTOCOL_ERROR") || 
                        request.error.Contains("NetworkError") ||
                        request.error.Contains("Unable to complete SSL connection") ||
                        request.error.Contains("ConnectionError") ||
                        request.result == UnityWebRequest.Result.ConnectionError
                    );
                    
                    if (isRetryableError && retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        Debug.LogWarning($"JsonBinManager: [LoadAllTactics] 네트워크 에러 발생 (재시도 {retryCount}/{maxRetries}): {request.error}");
                        yield return new WaitForSeconds(1f * retryCount); // 지수 백오프
                        continue;
                    }
                    
                    Debug.LogError($"JsonBinManager: [LoadAllTactics] 데이터 로드 실패: {request.error} (HTTP {request.responseCode})");
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
        // Score 높은 순으로 최대 100개만 유지
        if (database.tactics != null && database.tactics.Count > 100)
        {
            // 각 tacticsJson을 파싱해서 score 추출
            var tacticsWithScore = new List<(TacticsData tactic, int score)>();
            
            foreach (var tactic in database.tactics)
            {
                int score = 0;
                if (!string.IsNullOrEmpty(tactic.tacticsJson))
                {
                    try
                    {
                        // TacticsFileData 구조를 사용하여 score 추출
                        var tacticsFileData = JsonUtility.FromJson<TacticsFileData>(tactic.tacticsJson);
                        if (tacticsFileData != null)
                        {
                            score = tacticsFileData.score;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"JsonBinManager: [SaveAllTactics] Tactics JSON 파싱 실패 (score 추출): {e.Message}");
                    }
                }
                tacticsWithScore.Add((tactic, score));
            }
            
            // Score 높은 순으로 정렬
            tacticsWithScore.Sort((a, b) => b.score.CompareTo(a.score));
            
            // 상위 100개만 유지
            database.tactics = tacticsWithScore.Take(100).Select(x => x.tactic).ToList();
            
            Debug.Log($"JsonBinManager: [SaveAllTactics] Score 기준 상위 100개만 유지: {tacticsWithScore.Count}개 -> {database.tactics.Count}개");
        }
        
        string url = $"{baseUrl}/{binId}";
        string json = JsonUtility.ToJson(database);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        
        // 데이터 크기 확인 (JSONBin.io Pro Plan 제한: 10MB)
        const int maxSizeBytes = 1 * 1024 * 1024; // 9MB (안전 마진 포함)
        int dataSize = bodyRaw.Length;
        
        Debug.Log($"JsonBinManager: [SaveAllTactics] 저장할 데이터 크기: {dataSize / (1024f * 1024f):F2} MB ({dataSize / 1024f:F2} KB, {dataSize} bytes)");
        
        // 데이터가 너무 크면 오래된 데이터 제거 (score 기준으로 이미 정렬되어 있음)
        if (dataSize > maxSizeBytes)
        {
            Debug.LogWarning($"JsonBinManager: [SaveAllTactics] 데이터 크기 초과 ({dataSize / (1024f * 1024f):F2} MB > {maxSizeBytes / (1024f * 1024f):F2} MB). 낮은 score 데이터 제거");
            
            // Score가 낮은 것부터 제거 (이미 score 높은 순으로 정렬되어 있으므로 뒤에서부터 제거)
            if (database.tactics != null && database.tactics.Count > 0)
            {
                int removedCount = 0;
                while (dataSize > maxSizeBytes && database.tactics.Count > 0)
                {
                    database.tactics.RemoveAt(database.tactics.Count - 1); // 마지막 요소 제거 (낮은 score)
                    removedCount++;
                    
                    // 다시 JSON 변환하여 크기 확인
                    json = JsonUtility.ToJson(database);
                    bodyRaw = Encoding.UTF8.GetBytes(json);
                    dataSize = bodyRaw.Length;
                }
                
                Debug.LogWarning($"JsonBinManager: [SaveAllTactics] {removedCount}개 데이터 제거 완료. 현재 크기: {dataSize / (1024f * 1024f):F2} MB ({dataSize / 1024f:F2} KB)");
            }
        }

        int maxRetries = 3;
        int retryCount = 0;
        bool success = false;

        Debug.Log($"JsonBinManager: [SaveAllTactics] 네트워크 요청 시작 - URL: {url}");

        while (retryCount < maxRetries && !success)
        {
            if (retryCount > 0)
            {
                Debug.Log($"JsonBinManager: [SaveAllTactics] 재시도 {retryCount}/{maxRetries}");
            }

            UnityWebRequest request = new UnityWebRequest(url, "PUT");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Access-Key", accessKey);
            request.timeout = 30; // 타임아웃 설정 (10초 -> 30초로 증가)

            Debug.Log($"JsonBinManager: [SaveAllTactics] HTTP PUT 요청 전송 (시도 {retryCount + 1}/{maxRetries})");
            
            using (request)
            {
                // yield return은 try 블록 밖에서 실행 (C# 제약사항)
                // 하지만 SendWebRequest() 자체에서 예외가 발생할 수 있음
                // Unity의 코루틴은 yield return에서 예외를 던지지 않지만,
                // 네이티브 레벨에서 발생하는 예외는 이후 request 접근 시 발생할 수 있음
                yield return request.SendWebRequest();
                
                // SendWebRequest() 이후 request 접근 시 예외 발생 가능 (PROTOCOL_ERROR 등)
                try
                {
                    string errorMessage = request.error ?? "Unknown error";
                    Debug.Log($"JsonBinManager: [SaveAllTactics] HTTP PUT 응답 수신 - Result: {request.result}, Code: {request.responseCode}, Error: {errorMessage}");

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // database.tactics.count와 용량 사이즈 출력
                        Debug.Log($"JsonBinManager: [SaveAllTactics] 저장 성공 - {database.tactics.Count}개 데이터, {dataSize / (1024f * 1024f):F2} MB ({dataSize / 1024f:F2} KB)");
                        
                        // 캐시 업데이트
                        cachedTacticsDatabase = database;
                        isCacheValid = true;
                        
                        onComplete?.Invoke(true);
                        success = true;
                    }
                    else
                    {
                        if (request.responseCode == 413)
                        {
                            Debug.LogError($"JsonBinManager: [SaveAllTactics] 저장 실패 - 페이로드 너무 큼 (HTTP 413). 크기: {dataSize / (1024f * 1024f):F2} MB ({dataSize / 1024f:F2} KB)");
                            Debug.LogError("JsonBinManager: [SaveAllTactics] 해결 방법: 오래된 Tactics 데이터를 수동으로 삭제하거나, 데이터를 여러 bin으로 나누어 저장하세요.");
                            onComplete?.Invoke(false);
                            break; // 413 에러는 재시도하지 않음
                        }
                        else
                        {
                            // ConnectionError는 request.result로 직접 체크 (request.error가 null일 수 있음)
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
                                Debug.LogWarning($"JsonBinManager: [SaveAllTactics] 네트워크 에러 발생 (재시도 {retryCount}/{maxRetries}): Result={request.result}, Error={errorMessage}");
                                // try 블록 밖에서 재시도 처리
                            }
                            else
                            {
                                Debug.LogError($"JsonBinManager: [SaveAllTactics] 저장 실패: Result={request.result}, Error={errorMessage} (HTTP {request.responseCode})");
                                onComplete?.Invoke(false);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // request.result, request.error 등 접근 시 예외 발생 가능
                    Debug.LogError($"JsonBinManager: [SaveAllTactics] 응답 처리 중 예외 발생: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        Debug.LogError($"JsonBinManager: [SaveAllTactics] 최대 재시도 횟수 도달. 저장 실패 처리");
                        onComplete?.Invoke(false);
                        success = true; // 루프 종료를 위해
                        break;
                    }
                    Debug.LogWarning($"JsonBinManager: [SaveAllTactics] 예외 발생으로 재시도 {retryCount}/{maxRetries}");
                    // catch 블록 밖에서 재시도 처리
                }
            }
            
            // catch 블록이나 재시도가 필요한 경우 여기서 처리
            if (!success && retryCount < maxRetries)
            {
                yield return new WaitForSeconds(1f * retryCount); // 지수 백오프
                continue;
            }
            
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
}


