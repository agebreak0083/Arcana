using System;
using System.Collections;
using System.Collections.Generic;
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

    void Awake()
    {
        // 씬마다 독립적인 인스턴스 사용
        Instance = this;
        
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

            // 전체 데이터 저장
            StartCoroutine(SaveAllTactics(allTactics, (saveSuccess) =>
            {
                if (saveSuccess)
                {
                    Debug.Log($"Tactics 데이터 저장 성공: {key}");
                    onComplete?.Invoke(true, key);
                }
                else
                {
                    Debug.LogError("Tactics 데이터 저장 실패");
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

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("X-Access-Key", accessKey);

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
                    
                    onComplete?.Invoke(true, database);
                }
                catch (Exception e)
                {
                    Debug.LogError($"JSON 파싱 실패: {e.Message}\nResponse: {request.downloadHandler.text}");
                    // 빈 데이터베이스 반환
                    onComplete?.Invoke(true, new TacticsDatabase { tactics = new List<TacticsData>() });
                }
            }
            else
            {
                Debug.LogError($"데이터 로드 실패: {request.error} (HTTP {request.responseCode})");
                // 빈 데이터베이스 반환 (새로 시작)
                onComplete?.Invoke(true, new TacticsDatabase { tactics = new List<TacticsData>() });
            }
        }
    }

    /// <summary>
    /// JSONBin.io에 모든 Tactics 데이터 저장
    /// </summary>
    private IEnumerator SaveAllTactics(TacticsDatabase database, Action<bool> onComplete)
    {
        string url = $"{baseUrl}/{binId}";
        string json = JsonUtility.ToJson(database);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Access-Key", accessKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"데이터 저장 실패: {request.error} (HTTP {request.responseCode})");
                onComplete?.Invoke(false);
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

