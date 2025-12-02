using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 사용자 데이터를 관리하는 매니저 클래스
/// - 게임 진행 상황, 설정, 캐릭터 보유 현황 등을 저장/로드
/// - 싱글톤 패턴으로 구현
/// </summary>
[DefaultExecutionOrder(-100)]
public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance { get; private set; }

    [Header("User Data")]
    public UserData currentUserData;

    private string saveFilePath;
    private const string SAVE_FILE_NAME = "userdata.json";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeSavePath();
        LoadUserData();
    }

    /// <summary>
    /// 저장 파일 경로 초기화
    /// </summary>
    private void InitializeSavePath()
    {
#if UNITY_SWITCH
        // Nintendo Switch에서는 persistentDataPath 사용 불가
        saveFilePath = Path.Combine(Application.dataPath, SAVE_FILE_NAME);
#else
        saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
#endif
        Debug.Log($"UserData 저장 경로: {saveFilePath}");
    }

    /// <summary>
    /// 사용자 데이터 로드
    /// </summary>
    public void LoadUserData()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                currentUserData = JsonUtility.FromJson<UserData>(json);
                Debug.Log("사용자 데이터 로드 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"사용자 데이터 로드 실패: {e.Message}");
                CreateNewUserData();
            }
        }
        else
        {
            Debug.Log("저장된 데이터가 없습니다. 새로운 데이터를 생성합니다.");
            CreateNewUserData();
        }
    }

    /// <summary>
    /// 사용자 데이터 저장
    /// </summary>
    public void SaveUserData()
    {
        try
        {
            string json = JsonUtility.ToJson(currentUserData, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log("사용자 데이터 저장 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"사용자 데이터 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 새로운 사용자 데이터 생성
    /// </summary>
    private void CreateNewUserData()
    {
        currentUserData = new UserData
        {
            playerName = System.Environment.MachineName, // PC의 HostName 사용
            gold = 1000,
            currentStage = 1,
            ownedCharacters = new List<string>(),
            gameSettings = new GameSettings
            {
                bgmVolume = 0.7f,
                sfxVolume = 0.8f,
                language = "Korean"
            },
            lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        SaveUserData();
    }

    /// <summary>
    /// 데이터 초기화 (새 게임 시작)
    /// </summary>
    public void ResetUserData()
    {
        CreateNewUserData();
        Debug.Log("사용자 데이터가 초기화되었습니다.");
    }

    /// <summary>
    /// 골드 추가
    /// </summary>
    public void AddGold(int amount)
    {
        currentUserData.gold += amount;
        SaveUserData();
    }

    /// <summary>
    /// 골드 사용
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (currentUserData.gold >= amount)
        {
            currentUserData.gold -= amount;
            SaveUserData();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 캐릭터 보유 여부 확인
    /// </summary>
    public bool HasCharacter(string characterName)
    {
        return currentUserData.ownedCharacters.Contains(characterName);
    }

    /// <summary>
    /// 캐릭터 추가
    /// </summary>
    public void AddCharacter(string characterName)
    {
        if (!HasCharacter(characterName))
        {
            currentUserData.ownedCharacters.Add(characterName);
            SaveUserData();
            Debug.Log($"캐릭터 '{characterName}' 획득!");
        }
    }

    /// <summary>
    /// 스테이지 진행
    /// </summary>
    public void UpdateStage(int stageNumber)
    {
        if (stageNumber > currentUserData.currentStage)
        {
            currentUserData.currentStage = stageNumber;
            SaveUserData();
        }
    }

    /// <summary>
    /// 게임 설정 업데이트
    /// </summary>
    public void UpdateSettings(GameSettings settings)
    {
        currentUserData.gameSettings = settings;
        SaveUserData();
    }

    /// <summary>
    /// 애플리케이션 종료 시 자동 저장
    /// </summary>
    private void OnApplicationQuit()
    {
        if (currentUserData != null)
        {
            currentUserData.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveUserData();
        }
    }

    /// <summary>
    /// 애플리케이션 일시정지 시 자동 저장 (모바일용)
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && currentUserData != null)
        {
            currentUserData.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveUserData();
        }
    }
}

/// <summary>
/// 사용자 데이터 구조체
/// </summary>
[Serializable]
public class UserData
{
    public string playerName;
    public int gold;
    public int currentStage;
    public List<string> ownedCharacters;
    public GameSettings gameSettings;
    public string lastSaveTime;
}

/// <summary>
/// 게임 설정 구조체
/// </summary>
[Serializable]
public class GameSettings
{
    public float bgmVolume;
    public float sfxVolume;
    public string language;
}
