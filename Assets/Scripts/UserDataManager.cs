using System;
using System.Collections.Generic;
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

    private const string PLAYER_PREFS_KEY = "UserData";

    void Awake()
    {
        // 씬마다 독립적인 인스턴스 사용 (PlayerPrefs에서 로드)
        Instance = this;
        LoadUserData();
    }

    /// <summary>
    /// 사용자 데이터 로드
    /// </summary>
    public void LoadUserData()
    {
        if (PlayerPrefs.HasKey(PLAYER_PREFS_KEY))
        {
            try
            {
                string json = PlayerPrefs.GetString(PLAYER_PREFS_KEY);
                currentUserData = JsonUtility.FromJson<UserData>(json);
                Debug.Log("사용자 데이터 로드 완료 (PlayerPrefs)");
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
            PlayerPrefs.SetString(PLAYER_PREFS_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("사용자 데이터 저장 완료 (PlayerPrefs)");
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
            playerName = "",
            tickets = 10,
            ownedCharacters = new List<string>(),
            gameSettings = new GameSettings
            {
                bgmVolume = 0.7f,
                sfxVolume = 0.8f,
                language = "Korean",
                battleMap2xSpeed = false
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
    public void AddTickets(int amount)
    {
        currentUserData.tickets += amount;
        SaveUserData();
    }

    /// <summary>
    /// 골드 사용
    /// </summary>
    public bool SpendTickets(int amount)
    {
        if (currentUserData.tickets >= amount)
        {
            currentUserData.tickets -= amount;
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

    public int UdpateScore(int addWin, int AddLose)
    {
        currentUserData.winCount += addWin;
        currentUserData.loseCount += AddLose;
        currentUserData.score += (addWin * 3) - (AddLose * 1);
        return currentUserData.score;
    }
}

/// <summary>
/// 사용자 데이터 구조체
/// </summary>
[Serializable]
public class UserData
{
    public string playerName;
    public int tickets;    
    public int score; 
    public int winCount; 
    public int loseCount; 
    public int ranking;
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
    public bool battleMap2xSpeed = false; // BattleMap 2배속 설정
}
