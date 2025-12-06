using System;
using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

/// <summary>
/// Firebase Realtime Database 관리 클래스
/// - Tactics 데이터를 Firebase에 저장/로드
/// </summary>
[DefaultExecutionOrder(-100)]
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private DatabaseReference databaseReference;
    private bool isFirebaseInitialized = false;

    void Awake()
    {
        // 씬마다 독립적인 인스턴스 사용
        Instance = this;
        InitializeFirebase();
    }

    /// <summary>
    /// Firebase 초기화
    /// </summary>
    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                isFirebaseInitialized = true;
                Debug.Log("Firebase 초기화 성공!");
            }
            else
            {
                Debug.LogError($"Firebase 초기화 실패: {dependencyStatus}");
                isFirebaseInitialized = false;
            }
        });
    }

    /// <summary>
    /// Tactics 데이터를 Firebase에 저장
    /// </summary>
    /// <param name="tacticsJson">저장할 tactics.json 내용</param>
    /// <param name="onComplete">완료 콜백</param>
    public void SaveTacticsToFirebase(string tacticsJson, Action<bool, string> onComplete = null)
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("Firebase가 초기화되지 않았습니다.");
            onComplete?.Invoke(false, "Firebase not initialized");
            return;
        }

        if (UserDataManager.Instance == null || UserDataManager.Instance.currentUserData == null)
        {
            Debug.LogError("UserDataManager가 초기화되지 않았습니다.");
            onComplete?.Invoke(false, "UserDataManager not initialized");
            return;
        }

        // Key 생성: Username_시간 (예: agebreak-wo2_2512061905)
        string username = SanitizeUsername(UserDataManager.Instance.currentUserData.playerName);
        string timestamp = DateTime.Now.ToString("yyMMddHHmm");
        string key = $"{username}_{timestamp}";

        // Firebase에 저장할 데이터 구조
        var tacticsData = new TacticsFirebaseData
        {
            username = UserDataManager.Instance.currentUserData.playerName,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            tacticsJson = tacticsJson
        };

        string json = JsonUtility.ToJson(tacticsData);

        // Firebase에 저장
        databaseReference.Child("tactics").Child(key).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log($"Tactics 데이터 저장 성공: {key}");
                onComplete?.Invoke(true, key);
            }
            else
            {
                Debug.LogError($"Tactics 데이터 저장 실패: {task.Exception}");
                onComplete?.Invoke(false, task.Exception?.Message);
            }
        });
    }

    /// <summary>
    /// Firebase에서 특정 키의 Tactics 데이터 로드
    /// </summary>
    /// <param name="key">로드할 데이터의 키</param>
    /// <param name="onComplete">완료 콜백 (성공 여부, tactics JSON)</param>
    public void LoadTacticsFromFirebase(string key, Action<bool, string> onComplete)
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("Firebase가 초기화되지 않았습니다.");
            onComplete?.Invoke(false, null);
            return;
        }

        databaseReference.Child("tactics").Child(key).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && task.Result.Exists)
            {
                string json = task.Result.GetRawJsonValue();
                TacticsFirebaseData data = JsonUtility.FromJson<TacticsFirebaseData>(json);
                Debug.Log($"Tactics 데이터 로드 성공: {key}");
                onComplete?.Invoke(true, data.tacticsJson);
            }
            else
            {
                Debug.LogError($"Tactics 데이터 로드 실패: {task.Exception}");
                onComplete?.Invoke(false, null);
            }
        });
    }

    /// <summary>
    /// 특정 유저의 모든 Tactics 데이터 키 목록 가져오기
    /// </summary>
    /// <param name="username">유저 이름</param>
    /// <param name="onComplete">완료 콜백 (성공 여부, 키 목록)</param>
    public void GetUserTacticsKeys(string username, Action<bool, System.Collections.Generic.List<string>> onComplete)
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogError("Firebase가 초기화되지 않았습니다.");
            onComplete?.Invoke(false, null);
            return;
        }

        string sanitizedUsername = SanitizeUsername(username);

        databaseReference.Child("tactics").OrderByKey()
            .StartAt(sanitizedUsername)
            .EndAt(sanitizedUsername + "\uf8ff")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    var keys = new System.Collections.Generic.List<string>();
                    foreach (var child in task.Result.Children)
                    {
                        keys.Add(child.Key);
                    }
                    Debug.Log($"유저 {username}의 Tactics 키 {keys.Count}개 로드 완료");
                    onComplete?.Invoke(true, keys);
                }
                else
                {
                    Debug.LogError($"Tactics 키 목록 로드 실패: {task.Exception}");
                    onComplete?.Invoke(false, null);
                }
            });
    }

    /// <summary>
    /// 사용자 이름을 Firebase 키로 사용 가능하도록 정리
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

    /// <summary>
    /// Firebase에 저장할 데이터 구조
    /// </summary>
    [Serializable]
    private class TacticsFirebaseData
    {
        public string username;
        public string timestamp;
        public string tacticsJson;
    }
}
