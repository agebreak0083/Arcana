using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Arcana.Tactics.Data;
using UnityEngine.Networking;

namespace Arcana.Tactics
{
    /// <summary>
    /// Tactics 관련 데이터 및 테이블 관리
    /// - 캐릭터, 클래스, 스킬 데이터 로드
    /// - BattleScene과 TacticsScene 모두에서 사용 가능
    /// - 싱글톤 패턴으로 구현
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TacticsDataManager : MonoBehaviour
    {
        public static TacticsDataManager Instance { get; private set; }

        public bool isNeedServerData = false;

        [Header("Loaded Data")]
        public List<CharacterData> availableCharacters;

        private Dictionary<string, ClassInfo> _classData = new Dictionary<string, ClassInfo>();
        private Dictionary<string, List<Skill>> _skillMap = new Dictionary<string, List<Skill>>();
        private Dictionary<string, TacticsPlan> _recommendedTactics = new Dictionary<string, TacticsPlan>(); // 클래스별 추천 전술
        private FormationLoadResult _playerFormationLoadResult;
        private FormationLoadResult _enemyFormationLoadResult;
        private CharacterDefinition[] _allCharacterDefinitions; // 모든 캐릭터 정의 목록

        public bool isDataLoaded { get; private set; } = false;

        void Awake()
        {
            // 씬마다 독립적인 인스턴스 사용
            Instance = this;
            string enemyName = BattleManager.battleSimulationResult.enemyName;
            StartCoroutine(LoadAllDataAsync(enemyName));
        }

        /// <summary>
        /// 모든 데이터 비동기 로드
        /// </summary>
        private System.Collections.IEnumerator LoadAllDataAsync(string enemyName)
        {
            Debug.Log("TacticsDataManager: 모든 데이터 비동기 로드 시작");
            
            isDataLoaded = false;

            LoadSkillList();
            LoadTacticsRecommend();

            // Wait for characters and classes to load from Web
            yield return StartCoroutine(LoadClassesFromWeb());
            yield return StartCoroutine(LoadCharactersFromWeb());

            _playerFormationLoadResult = new FormationLoadResult
            {
                unitSlots = new CharacterData[6],
                codingData = new Dictionary<string, TacticsPlan>()
            };
            _enemyFormationLoadResult = new FormationLoadResult
            {
                unitSlots = new CharacterData[6],
                codingData = new Dictionary<string, TacticsPlan>()
            };

            // Player formation 로드 (로컬 파일)
            _playerFormationLoadResult = LoadFormationFromTacticsFile(true);
            Debug.Log("TacticsDataManager: Player formation 로드 완료");

            // Enemy formation 로드는 서버 데이터가 필요할 때만 로드
            if (isNeedServerData)
            {
                // JSONBin.io 초기화 대기 (최대 5초)
                float waitTime = 0f;
                const float maxWaitTime = 5f;

                if (JSONBinManager.Instance != null)
                {
                    Debug.Log("TacticsDataManager: JSONBin.io 초기화 대기 중...");
                    while (!JSONBinManager.Instance.isInitialized && waitTime < maxWaitTime)
                    {
                        yield return new WaitForSeconds(0.1f);
                        waitTime += 0.1f;
                    }

                    if (JSONBinManager.Instance.isInitialized)
                    {
                        Debug.Log("TacticsDataManager: JSONBin.io 초기화 완료!");
                    }
                    else
                    {
                        Debug.LogWarning($"TacticsDataManager: JSONBin.io 초기화 타임아웃 ({maxWaitTime}초). 로컬 파일 사용.");
                    }
                }

                // Enemy formation 로드 (JSONBin.io에서 랜덤 또는 로컬 파일)
                bool enemyLoadComplete = false;
                LoadEnemyFormationFromJsonBin(enemyName, (success) =>
                {
                    enemyLoadComplete = true;
                });

                // JSONBin.io 로딩 완료 대기
                yield return new WaitUntil(() => enemyLoadComplete);
            }
            else
            {
                // 서버 데이터가 필요 없으면 기본 Enemy formation 설정
                Debug.Log("TacticsDataManager: 서버 데이터 불필요. 기본 Enemy formation 사용.");
                _enemyFormationLoadResult = LoadFormationFromTacticsFile(false);
            }

            isDataLoaded = true;
            Debug.Log("TacticsDataManager: 모든 데이터 로드 완료");
        }

        public FormationLoadResult GetPlayerFormationLoadResult()
        {
            return _playerFormationLoadResult;
        }

        public FormationLoadResult GetEnemyFormationLoadResult()
        {
            return _enemyFormationLoadResult;
        }

        /// <summary>
        /// 점수를 받아서 전체 랭킹을 반환합니다 (비동기)
        /// </summary>
        /// <param name="score">랭킹을 확인할 점수</param>
        /// <param name="onComplete">완료 콜백 (랭킹, 0이면 데이터 로드 실패)</param>
        public void GetRanking(int score, System.Action<int> onComplete)
        {
            if (JSONBinManager.Instance == null || !JSONBinManager.Instance.isInitialized)
            {
                Debug.LogWarning("JSONBinManager가 초기화되지 않았습니다.");
                onComplete?.Invoke(0);
                return;
            }

            // JSONBinManager에서 모든 Tactics 데이터 로드
            JSONBinManager.Instance.GetAllTactics((success, database) =>
            {
                if (!success || database == null || database.tactics == null)
                {
                    Debug.LogWarning("Tactics 데이터 로드 실패");
                    onComplete?.Invoke(0);
                    return;
                }

                // 모든 Tactics JSON을 파싱하여 TacticsFileData로 변환
                Dictionary<string, int> userScores = new Dictionary<string, int>();

                foreach (JSONBinManager.TacticsData tactic in database.tactics)
                {
                    if (string.IsNullOrEmpty(tactic.tacticsJson)) continue;

                    try
                    {
                        TacticsFileData tacticsData = JsonUtility.FromJson<TacticsFileData>(tactic.tacticsJson);
                        if (tacticsData != null && !string.IsNullOrEmpty(tacticsData.username))
                        {
                            // key 값 설정 (JSONBinManager의 key 사용)
                            if (string.IsNullOrEmpty(tacticsData.key))
                            {
                                tacticsData.key = tactic.key;
                            }
                            
                            userScores[tacticsData.key] = tacticsData.score;
                            
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Tactics JSON 파싱 실패: {e.Message}");
                        continue;
                    }
                }

                // score가 높은 순서대로 정렬
                var sortedScores = userScores.Values.OrderByDescending(x => x).ToList();

                // 주어진 score보다 높은 점수를 가진 사용자 수를 세어서 랭킹 계산
                // 예: [100, 90, 80, 70], 내 점수 85 -> 랭킹 3 (100, 90이 더 높음)
                int ranking = sortedScores.Count(s => s > score) + 1;

                // 가장 높은 score를 가진 사용자의 이름과 score 가져오기
                string highestScoreUsername = userScores.Keys.FirstOrDefault(k => userScores[k] == sortedScores.First());
                int highestScore = sortedScores.First();

                Debug.Log($"가장 높은 score를 가진 사용자: {highestScoreUsername}, score: {highestScore}");

                onComplete?.Invoke(ranking);
            });
        }

        /// <summary>
        /// 모든 유저의 TacticsData를 score 순으로 가져옵니다 (비동기)
        /// </summary>
        /// <param name="onComplete">완료 콜백 (유저 데이터 리스트: username, score, winCount, loseCount)</param>
        public void GetAllUsersSortedByScore(System.Action<List<(string username, int score, int winCount, int loseCount)>> onComplete)
        {
            if (JSONBinManager.Instance == null || !JSONBinManager.Instance.isInitialized)
            {
                Debug.LogWarning("JSONBinManager가 초기화되지 않았습니다.");
                onComplete?.Invoke(new List<(string, int, int, int)>());
                return;
            }

            // JSONBinManager에서 모든 Tactics 데이터 로드
            JSONBinManager.Instance.GetAllTactics((success, database) =>
            {
                if (!success || database == null || database.tactics == null)
                {
                    Debug.LogWarning("Tactics 데이터 로드 실패");
                    onComplete?.Invoke(new List<(string, int, int, int)>());
                    return;
                }

                // 모든 Tactics JSON을 파싱하여 TacticsFileData로 변환
                Dictionary<string, (int score, int winCount, int loseCount)> userData = new Dictionary<string, (int, int, int)>();

                foreach (JSONBinManager.TacticsData tactic in database.tactics)
                {
                    if (string.IsNullOrEmpty(tactic.tacticsJson)) continue;

                    try
                    {
                        TacticsFileData tacticsData = JsonUtility.FromJson<TacticsFileData>(tactic.tacticsJson);                        
                        if (tacticsData != null && !string.IsNullOrEmpty(tacticsData.username))
                        {
                            userData[tacticsData.username] = (tacticsData.score, tacticsData.winCount, tacticsData.loseCount);                            
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Tactics JSON 파싱 실패: {e.Message}");
                        continue;
                    }
                }

                // score가 높은 순서대로 정렬하여 리스트로 변환
                var sortedUsers = userData
                    .Select(kvp => (username: kvp.Key, score: kvp.Value.score, winCount: kvp.Value.winCount, loseCount: kvp.Value.loseCount))
                    .OrderByDescending(x => x.score)
                    .ToList();

                onComplete?.Invoke(sortedUsers);
            });
        }

        /// <summary>
        /// 사용자의 랭킹을 가져옵니다 (비동기)
        /// </summary>
        /// <param name="username">랭킹을 확인할 사용자 이름</param>
        /// <param name="onComplete">완료 콜백 (랭킹, 0이면 사용자를 찾을 수 없음)</param>
        public void GetRankingByUsername(string key, System.Action<int> onComplete)
        {
            if (JSONBinManager.Instance == null || !JSONBinManager.Instance.isInitialized)
            {
                Debug.LogWarning("JSONBinManager가 초기화되지 않았습니다.");
                onComplete?.Invoke(0);
                return;
            }

            // JSONBinManager에서 모든 Tactics 데이터 로드
            JSONBinManager.Instance.GetAllTactics((success, database) =>
            {
                if (!success || database == null || database.tactics == null)
                {
                    Debug.LogWarning("Tactics 데이터 로드 실패");
                    onComplete?.Invoke(0);
                    return;
                }

                // 모든 Tactics JSON을 파싱하여 TacticsFileData로 변환
                Dictionary<string, int> userScores = new Dictionary<string, int>();

                foreach (JSONBinManager.TacticsData tactic in database.tactics)
                {
                    if (string.IsNullOrEmpty(tactic.tacticsJson)) continue;

                    try
                    {
                        TacticsFileData tacticsData = JsonUtility.FromJson<TacticsFileData>(tactic.tacticsJson);
                        if (tacticsData != null && !string.IsNullOrEmpty(tacticsData.username))
                        {
                            // key 값 설정 (JSONBinManager의 key 사용)
                            if (string.IsNullOrEmpty(tacticsData.key))
                            {
                                tacticsData.key = tactic.key;
                            }

                            userScores[tacticsData.key] = tacticsData.score;                           
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Tactics JSON 파싱 실패: {e.Message}");
                        continue;
                    }
                }

                // score가 높은 순서대로 정렬
                var sortedUsers = userScores.OrderByDescending(x => x.Value).ToList();

                // 주어진 username의 순서 찾기
                for (int i = 0; i < sortedUsers.Count; i++)
                {
                    if (sortedUsers[i].Key == key)
                    {
                        onComplete?.Invoke(i + 1); // 1부터 시작하는 랭킹
                        return;
                    }
                }

                // 사용자를 찾을 수 없음
                onComplete?.Invoke(0);
            });
        }

        public int UpdateScore(string key, int addWin, int addLose)
        {
            // UserName에 해당하는 Tactis Data를 찾아서, 
            // win/lose Count를 업데이트하고, Score를 업데이트한다. (Win : + 3점, Lose : - 1점)
            // 이 값은 저장할때 서버에도 업데이트하여 저장한다.
            
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("UpdateScore: userName이 비어있습니다.");
                return 0;
            }

            int newScore = 0;

            // 서버 데이터 업데이트
            if (JSONBinManager.Instance != null && JSONBinManager.Instance.isInitialized)
            {
                JSONBinManager.Instance.LoadTactics(key, (success, tacticsJson) =>
                {
                    if (!success || string.IsNullOrEmpty(tacticsJson))
                    {
                        Debug.LogWarning("서버 데이터 로드 실패. 로컬 데이터만 업데이트되었습니다.");
                        return;
                    }

                    // 해당 userName의 모든 tactics 데이터 찾아서 업데이트
                    try
                    {
                        TacticsFileData tacticsData = JsonUtility.FromJson<TacticsFileData>(tacticsJson);
                        if (tacticsData != null && !string.IsNullOrEmpty(tacticsData.key) && tacticsData.key == key)
                        {
                            // winCount, loseCount 업데이트
                            tacticsData.winCount += addWin;
                            tacticsData.loseCount += addLose;

                            // Score 업데이트 (Win: +3점, Lose: -1점)
                            tacticsData.score += (addWin * 3) - (addLose * 1);
                            newScore = tacticsData.score;                                

                            // TODO : JSON 값에 업데이트 
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Tactics JSON 파싱 실패: {e.Message}");                        
                    }
                }); 
            };
            return newScore;
        }

        /// <summary>
        /// 모든 캐릭터 정의 목록을 가져옵니다 (가챠 시스템용)
        /// </summary>
        public CharacterDefinition[] GetAllCharacterDefinitions()
        {
            return _allCharacterDefinitions;
        }

        /// <summary>
        /// JSON 파일에서 캐릭터 데이터 로드
        /// </summary>
        private const string CharacterListUrl = "";// "https://docs.google.com/spreadsheets/d/e/2PACX-1vTeCHZPMcs6QJuZeS7k2MosrZrhChNL5FrRH3ePRd5fQx-O-nSUmR4VwZI6VGhHg65tFcWMmIr2tBha/pub?gid=0&single=true&output=csv";

        /// <summary>
        /// 웹 CSV에서 캐릭터 데이터 로드 (Web Request)
        /// </summary>
        private System.Collections.IEnumerator LoadCharactersFromWeb()
        {
            availableCharacters = new List<CharacterData>();
            CharacterDefinition[] allCharacters = null;

            // 1. Fetch CSV from Web
            if(string.IsNullOrEmpty(CharacterListUrl))
            {
                Debug.LogWarning("CharacterListUrl is empty. Loading from local resources.");
                
                // Fallback to local JSON
                TextAsset listAsset = Resources.Load<TextAsset>("Table/CharacterList");
                if (listAsset != null)
                {
                    allCharacters = JsonHelper.FromJson<CharacterDefinition>(listAsset.text);
                }
            }
            else
            {
                using (UnityWebRequest www = UnityWebRequest.Get(CharacterListUrl))
                {
                    www.timeout = 30; // 타임아웃 설정
                    yield return www.SendWebRequest();

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"Failed to load CharacterList from Web: {www.error}. Fallback to local.");
                        // Fallback to local JSON
                        TextAsset listAsset = Resources.Load<TextAsset>("Table/CharacterList");
                        if (listAsset != null)
                        {
                            allCharacters = JsonHelper.FromJson<CharacterDefinition>(listAsset.text);
                        }
                    }
                    else
                    {
                        Debug.Log("Successfully loaded CharacterList from Web CSV.");
                        string csvText = www.downloadHandler.text;
                        allCharacters = ParseCharacterCSV(csvText);
                    }
                }
            }

            if (allCharacters == null)
            {
                Debug.LogError("Failed to load Character definitions from both Web and Local.");
                yield break;
            }

            // Store all character definitions for gacha system
            _allCharacterDefinitions = allCharacters;

            // 2. Load CharacterPool (Dynamic Data)
            string poolJson = "";

            // 모든 플랫폼에서 PlayerPrefs 사용
            poolJson = PlayerPrefs.GetString("CharacterPool", "");
            if (!string.IsNullOrEmpty(poolJson))
            {
                Debug.Log("Loaded CharacterPool from PlayerPrefs");
            }
            else
            {
                // PlayerPrefs에 없으면 Resources에서 로드 (에디터에서 주로 사용)
#if UNITY_EDITOR
                TextAsset poolAsset = Resources.Load<TextAsset>("CharacterPool");
                if (poolAsset != null)
                {
                    poolJson = poolAsset.text;
                    Debug.Log("Loaded CharacterPool from Resources (no PlayerPrefs found)");
                }
#endif
            }

            // 3. Validate and parse Pool JSON
            CharacterPoolData[] myPool = null;
            
            // 빈 파일이거나 유효하지 않은 JSON인 경우 빈 배열로 처리
            if (string.IsNullOrWhiteSpace(poolJson) || poolJson.Trim() == "")
            {
                Debug.LogWarning("CharacterPool.json is empty. Starting with empty pool.");
                myPool = new CharacterPoolData[0];
            }
            else
            {
                try
                {
                    // CharacterPoolData 형식으로 파싱
                    myPool = JsonHelper.FromJson<CharacterPoolData>(poolJson);
                    if (myPool == null)
                    {
                        Debug.LogWarning("CharacterPool JSON parsing returned null. Starting with empty pool.");
                        myPool = new CharacterPoolData[0];
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"CharacterPool JSON parsing failed: {e.Message}. Starting with empty pool.");
                    myPool = new CharacterPoolData[0];
                }
            }

            // 4. Match and Create Data
            foreach (var poolItem in myPool)
            {
                // Find matching definition
                CharacterDefinition def = System.Array.Find(allCharacters, c => c.Name == poolItem.Name);

                if (def != null)
                {
                    // 5. Create CharacterData
                    CharacterData newData = ScriptableObject.CreateInstance<CharacterData>();
                    newData.id = System.Guid.NewGuid().ToString();
                    newData.characterName = def.Name;
                    newData.characterClass = def.Class;

                    // Defaults for missing data
                    newData.cost = def.Cost;
                    newData.speed = 10;
                    newData.arcana = "None";
                    newData.description = "No description available.";
                    newData.model = def.Model ?? ""; // Model 필드 설정

                    // Load Portrait
                    string spriteName = System.IO.Path.GetFileNameWithoutExtension(def.Portrait);
                    newData.portrait = Resources.Load<Sprite>($"Portraits/{spriteName}");
                    if (newData.portrait == null)
                    {
                        // Try loading without extension if the csv had it, or vice versa
                        newData.portrait = Resources.Load<Sprite>(spriteName);
                    }
                    if (newData.portrait == null)
                    {
                        Debug.LogWarning($"Portrait not found for {def.Name}: {def.Portrait}");
                    }

                    // Assign skills based on class
                    newData.skills = new List<Skill>();

                    // Find matching key in skill map (e.g. "파이터" in "파이터 / 뱅가드")
                    string matchedKey = null;
                    foreach (var key in _skillMap.Keys)
                    {
                        if (key.Contains(def.Class))
                        {
                            matchedKey = key;
                            break;
                        }
                    }

                    if (matchedKey != null && _skillMap.TryGetValue(matchedKey, out var classSkills))
                    {
                        // Clone skills to avoid shared references if we modify them later
                        foreach (var s in classSkills)
                        {
                            newData.skills.Add(new Skill
                            {
                                id = s.id,
                                name = s.name,
                                type = s.type,
                                description = s.description,
                                target = s.target,
                                costAP = s.costAP,
                                costPP = s.costPP,
                                // Skill의 모든 필드 초기화 (기본값 사용)
                                damageType = s.damageType ?? "",
                                power = s.power,
                                hitCount = s.hitCount,
                                accuracyRate = s.accuracyRate,
                                buttonType = s.buttonType ?? "",
                                animation = s.animation ?? "",
                                triggerTiming = s.triggerTiming ?? "",
                                triggerCondition = s.triggerCondition ?? "",
                                effects = s.effects != null ? new List<SkillEffect>(s.effects) : new List<SkillEffect>(),
                                traits = s.traits != null ? new List<string>(s.traits) : new List<string>()
                            });
                        }
                    }
                    else
                    {
                        // Fallback if no skills found
                        newData.skills.Add(new Skill { 
                            id = "attack_default",
                            name = "Attack", 
                            type = "active", 
                            costAP = 1,
                            effects = new List<SkillEffect>(),
                            traits = new List<string>()
                        });
                        newData.skills.Add(new Skill { 
                            id = "guard_default",
                            name = "Guard", 
                            type = "passive", 
                            costPP = 1,
                            effects = new List<SkillEffect>(),
                            traits = new List<string>()
                        });
                    }

                    // 5. Add to availableCharacters
                    availableCharacters.Add(newData);
                }
            }

            Debug.Log($"Loaded {availableCharacters.Count} characters");
        }

        private CharacterDefinition[] ParseCharacterCSV(string csvText)
        {
            var list = new List<CharacterDefinition>();
            string[] lines = csvText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Assume header is first line or check content
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Simple comma split (assuming no commas in values)
                string[] parts = line.Split(',');

                // Skip header (Name,Portrait,Class,Cost)
                if (parts.Length >= 4 && parts[0] == "Name" && parts[2] == "Class")
                    continue;

                if (parts.Length >= 4)
                {
                    CharacterDefinition def = new CharacterDefinition();
                    def.Name = parts[0].Trim();
                    def.Portrait = parts[1].Trim();
                    def.Class = parts[2].Trim();

                    if (int.TryParse(parts[3].Trim(), out int cost))
                    {
                        def.Cost = cost;
                    }
                    else
                    {
                        def.Cost = 2; // default
                    }

                    // Model 필드는 CSV에 없을 수 있으므로 기본값으로 빈 문자열
                    def.Model = parts.Length >= 5 ? parts[4].Trim() : "";

                    list.Add(def);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// (Deprecated) JSON 파일에서 캐릭터 데이터 로드 - Kept for compatibility if called externally, but now just starts the coroutine
        /// </summary>
        public void LoadCharactersFromJSON()
        {
            // This is now async in LoadAllDataAsync. 
            // If called synchronously from outside, it won't work as expected for Web Request.
            Debug.LogWarning("LoadCharactersFromJSON is deprecated. Use async loading.");
            // We can't easily wait here.
        }

        private const string ClassListUrl = ""; // "https://docs.google.com/spreadsheets/d/e/2PACX-1vTeCHZPMcs6QJuZeS7k2MosrZrhChNL5FrRH3ePRd5fQx-O-nSUmR4VwZI6VGhHg65tFcWMmIr2tBha/pub?gid=1123298632&single=true&output=csv"; // TODO: 여기에 Google Sheet CSV 링크를 넣어주세요

        /// <summary>
        /// 웹 CSV에서 클래스 데이터 로드 (Web Request)
        /// </summary>
        private System.Collections.IEnumerator LoadClassesFromWeb()
        {
            _classData.Clear();
            ClassListWrapper wrapper = new ClassListWrapper();
            List<ClassInfo> classList = new List<ClassInfo>();

            // 1. Fetch CSV from Web
            if (string.IsNullOrEmpty(ClassListUrl))
            {
                Debug.LogWarning("ClassListUrl is empty. Loading from local resources.");
                LoadClassList(); // Fallback to local
                yield break;
            }

            using (UnityWebRequest www = UnityWebRequest.Get(ClassListUrl))
            {
                www.timeout = 30; // 타임아웃 설정
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load ClassList from Web: {www.error}. Fallback to local.");
                    LoadClassList(); // Fallback to local
                }
                else
                {
                    Debug.Log("Successfully loaded ClassList from Web CSV.");
                    string csvText = www.downloadHandler.text;
                    classList = ParseClassCSV(csvText);

                    if (classList != null)
                    {
                        foreach (var classInfo in classList)
                        {
                            _classData[classInfo.name] = classInfo;
                        }
                    }
                }
            }

            Debug.Log($"Loaded {_classData.Count} classes.");
        }

        private List<ClassInfo> ParseClassCSV(string csvText)
        {
            var list = new List<ClassInfo>();
            string[] lines = csvText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Header: name,description,cost,model,advantage,hp,physicalAttack,physicalDefense,magicalAttack,magicalDefense,accuracy,evasion,criticalRate,guardRate,actionSpeed,actionPoint,passivePoint

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] parts = line.Split(',');

                // Skip header based on content
                if (parts.Length > 0 && parts[0] == "name") continue;

                if (parts.Length >= 17) // Ensure we have enough columns
                {
                    try
                    {
                        ClassInfo info = new ClassInfo();
                        info.name = parts[0].Trim();
                        info.description = parts[1].Trim();
                        info.cost = int.Parse(parts[2].Trim());
                        info.model = parts[3].Trim();

                        // Advantage (semicolon separated)
                        string advRaw = parts[4].Trim();
                        if (!string.IsNullOrEmpty(advRaw))
                        {
                            info.advantage = new List<string>(advRaw.Split(';'));
                        }
                        else
                        {
                            info.advantage = new List<string>();
                        }

                        info.stats = new ClassStats();
                        info.stats.hp = parts[5].Trim();
                        info.stats.physicalAttack = parts[6].Trim();
                        info.stats.physicalDefense = parts[7].Trim();
                        info.stats.magicalAttack = parts[8].Trim();
                        info.stats.magicalDefense = parts[9].Trim();
                        info.stats.accuracy = parts[10].Trim();
                        info.stats.evasion = parts[11].Trim();
                        info.stats.criticalRate = parts[12].Trim();
                        info.stats.guardRate = parts[13].Trim();
                        info.stats.actionSpeed = parts[14].Trim();
                        info.stats.actionPoint = int.Parse(parts[15].Trim());
                        info.stats.passivePoint = int.Parse(parts[16].Trim());

                        list.Add(info);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error parsing class CSV line: {line}. Error: {e.Message}");
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// (Local) JSON 파일에서 클래스 데이터 로드
        /// </summary>
        private void LoadClassList()
        {
            _classData.Clear();

            TextAsset classListAsset = Resources.Load<TextAsset>("Table/ClassList");
            if (classListAsset == null)
            {
                Debug.LogError("Failed to load ClassList.json");
                return;
            }

            ClassListWrapper wrapper = JsonUtility.FromJson<ClassListWrapper>(classListAsset.text);
            if (wrapper != null && wrapper.classes != null)
            {
                foreach (var classInfo in wrapper.classes)
                {
                    _classData[classInfo.name] = classInfo;
                }
                Debug.Log($"Loaded {_classData.Count} classes from ClassList.json");
            }
        }

        /// <summary>
        /// 스킬 데이터 로드
        /// </summary>
        private void LoadSkillList()
        {
            _skillMap.Clear();
            TextAsset skillAsset = Resources.Load<TextAsset>("Table/SkillList");
            if (skillAsset == null)
            {
                Debug.LogError("Failed to load SkillList.json");
                return;
            }

            string json = skillAsset.text;
            int index = 0;

            // Skip the first opening brace
            int firstBrace = json.IndexOf('{');
            if (firstBrace != -1) index = firstBrace + 1;

            while (index < json.Length)
            {
                // Find key
                int keyStart = json.IndexOf("\"", index);
                if (keyStart == -1) break;
                int keyEnd = json.IndexOf("\"", keyStart + 1);
                if (keyEnd == -1) break;

                string key = json.Substring(keyStart + 1, keyEnd - keyStart - 1);

                // Find array start
                int arrayStart = json.IndexOf("[", keyEnd);
                if (arrayStart == -1) break;

                // Find array end (balancing brackets)
                int arrayEnd = -1;
                int depth = 0;
                for (int i = arrayStart; i < json.Length; i++)
                {
                    if (json[i] == '[') depth++;
                    else if (json[i] == ']')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            arrayEnd = i;
                            break;
                        }
                    }
                }

                if (arrayEnd != -1)
                {
                    string arrayJson = json.Substring(arrayStart, arrayEnd - arrayStart + 1);
                    try
                    {
                        Skill[] skills = JsonHelper.FromJson<Skill>(arrayJson);
                        if (skills != null)
                        {
                            _skillMap[key] = new List<Skill>(skills);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to parse skills for {key}: {e.Message}");
                    }
                    index = arrayEnd + 1;
                }
                else
                {
                    break;
                }
            }
            Debug.Log($"Loaded skills for {_skillMap.Count} classes.");
        }

        /// <summary>
        /// 추천 전술 데이터 로드
        /// </summary>
        private void LoadTacticsRecommend()
        {
            _recommendedTactics.Clear();
            TextAsset recommendAsset = Resources.Load<TextAsset>("Table/TacticsRecommend");
            if (recommendAsset == null)
            {
                Debug.LogError("Failed to load TacticsRecommend.json");
                return;
            }

            try
            {
                TacticsRecommendWrapper wrapper = JsonUtility.FromJson<TacticsRecommendWrapper>(recommendAsset.text);
                if (wrapper != null && wrapper.classes != null)
                {
                    foreach (var classData in wrapper.classes)
                    {
                        if (classData.tactics != null && classData.tactics.Length > 0)
                        {
                            var tacticsData = classData.tactics[0]; // 첫 번째 tactics 사용
                            if (tacticsData.plan != null && tacticsData.plan.Length > 0)
                            {
                                // TacticsPlan 생성 (characterId는 나중에 설정됨)
                                var plan = new TacticsPlan("");
                                
                                // plan 데이터를 TacticRow로 변환
                                for (int i = 0; i < tacticsData.plan.Length && i < TacticsDatabase.MAX_TACTICS_ROW; i++)
                                {
                                    var rowData = tacticsData.plan[i];
                                    
                                    // 스킬 타입 결정 (스킬 이름으로 찾기)
                                    string skillType = "AP";
                                    if (!string.IsNullOrEmpty(rowData.skill) && rowData.skill != "---")
                                    {
                                        // 클래스의 스킬 목록에서 찾기
                                        var classSkills = GetClassSkills(classData.name);
                                        var skill = classSkills.Find(s => s.name == rowData.skill);
                                        if (skill != null)
                                        {
                                            skillType = skill.skillType;
                                        }
                                    }
                                    
                                    plan.rows[i] = new TacticRow(
                                        rowData.skill ?? "---",
                                        skillType,
                                        rowData.condition1 ?? "조건 없음",
                                        rowData.condition2 ?? "조건 없음"
                                    );
                                }
                                
                                _recommendedTactics[classData.name] = plan;
                            }
                        }
                    }
                    Debug.Log($"Loaded recommended tactics for {_recommendedTactics.Count} classes.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load TacticsRecommend.json: {e.Message}");
            }
        }

        /// <summary>
        /// 클래스별 추천 전술 가져오기
        /// </summary>
        public TacticsPlan GetRecommendedTactics(string className)
        {
            if (_recommendedTactics.TryGetValue(className, out TacticsPlan plan))
            {
                // 새로운 TacticsPlan 인스턴스를 반환 (characterId는 나중에 설정됨)
                var newPlan = new TacticsPlan("");
                for (int i = 0; i < plan.rows.Count && i < TacticsDatabase.MAX_TACTICS_ROW; i++)
                {
                    var row = plan.rows[i];
                    newPlan.rows[i] = new TacticRow(
                        row.skillName,
                        row.skillType,
                        row.condition1,
                        row.condition2
                    );
                }
                return newPlan;
            }
            return null;
        }

        /// <summary>
        /// 클래스 정보 가져오기
        /// </summary>
        public ClassInfo GetClassInfo(string className)
        {
            if (_classData.TryGetValue(className, out ClassInfo classInfo))
            {
                return classInfo;
            }
            return null;
        }

        /// <summary>
        /// 클래스의 스킬 목록 가져오기
        /// </summary>
        public List<Skill> GetClassSkills(string className)
        {
            foreach (var key in _skillMap.Keys)
            {
                if (key.Contains(className))
                {
                    return _skillMap[key];
                }
            }
            return new List<Skill>();
        }

        /// <summary>
        /// 기본 작전 플랜 생성
        /// </summary>
        public TacticsPlan CreateDefaultPlan(CharacterData data)
        {
            var plan = new TacticsPlan(data.id);

            // TacticsPlan은 이미 8개의 기본 Row를 가지고 있음
            // 캐릭터의 스킬로 앞부분을 채움
            for (int i = 0; i < data.skills.Count && i < TacticsDatabase.MAX_TACTICS_ROW; i++)
            {
                var skill = data.skills[i];
                plan.rows[i] = new TacticRow(skill.name, skill.skillType, TacticsDatabase.DEFAULT_CONDITION, TacticsDatabase.DEFAULT_CONDITION);
            }

            return plan;
        }

        #region Data Classes

        [System.Serializable]
        public class CharacterDefinition
        {
            public string Name;
            public string Portrait;
            public string Model;
            public string Class;
            public int Cost;
        }

        [System.Serializable]
        public class CharacterPoolItem
        {
            public string Name;
        }

        // Note: CharacterPoolItem은 더 이상 사용하지 않음. CharacterPoolData를 사용합니다.

        [System.Serializable]
        public class ClassListWrapper
        {
            public ClassInfo[] classes;
        }

        [System.Serializable]
        public class ClassInfo
        {
            public string name;
            public string description;
            public int cost;
            public string model;
            public List<string> advantage;
            public ClassStats stats;
        }

        /// <summary>
        /// Helper for array JSONs
        /// </summary>
        public static class JsonHelper
        {
            public static T[] FromJson<T>(string json)
            {
                string newJson = "{ \"array\": " + json + "}";
                Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
                return wrapper.array;
            }

            [System.Serializable]
            private class Wrapper<T>
            {
                public T[] array;
            }
        }

        /// <summary>
        /// Tactics 데이터를 JSON 문자열로 변환
        /// </summary>
        public string GetTacticsJson(CharacterData[] unitSlots, Dictionary<string, TacticsPlan> codingData)
        {
            // Build positions data using unified structure
            var positionsList = new List<PositionData>();

            string username = "";
            int score = 0;
            int winCount = 0;
            int loseCount = 0;

            if (UserDataManager.Instance != null && UserDataManager.Instance.currentUserData != null)
            {
                score = UserDataManager.Instance.currentUserData.score;
                winCount = UserDataManager.Instance.currentUserData.winCount;
                loseCount = UserDataManager.Instance.currentUserData.loseCount;            
                
                // username : playername_날짜시간
                username = UserDataManager.Instance.currentUserData.playerName + "_" + DateTime.Now.ToString("yyMMddHHmm");

                Debug.Log($"GetTacticsJson: username: {username}, score: {score}, winCount: {winCount}, loseCount: {loseCount}");
            }

            for (int i = 0; i < 6; i++)
            {
                var posData = new PositionData
                {
                    position = (i + 1).ToString(),
                    name = ""
                };

                // If there's a character in this slot
                if (unitSlots[i] != null)
                {
                    var character = unitSlots[i];
                    posData.name = character.characterName;

                    // If this character has tactics data, add it
                    if (codingData.TryGetValue(character.id, out var plan))
                    {
                        var tacticRowsList = new List<TacticRowData>();
                        foreach (var row in plan.rows)
                        {
                            tacticRowsList.Add(new TacticRowData 
                            {
                                skill = row.skillName,
                                condition1 = row.condition1,
                                condition2 = row.condition2
                            });
                        }

                        posData.tactics = new TacticsData[]
                        {
                            new TacticsData
                            {
                                characterClass = character.characterClass,
                                plan = tacticRowsList.ToArray()
                            }
                        };
                    }
                }

                positionsList.Add(posData);
            }

            // Key 생성: username_날짜시간 (JSONBinManager와 동일한 형식)
            string key = username;

            // Serialize to JSON using JsonUtility
            var tacticsFileData = new TacticsFileData
            {
                key = key,
                username = username,
                score = score,
                winCount = winCount,
                loseCount = loseCount,
                positions = positionsList.ToArray()
            };

            return JsonUtility.ToJson(tacticsFileData, true);
        }

        /// <summary>
        /// tactics.json에 포메이션 저장 (로컬 파일만)
        /// </summary>
        public void SaveFormationToTacticsFile(CharacterData[] unitSlots, Dictionary<string, TacticsPlan> codingData)
        {
            try
            {
                string json = GetTacticsJson(unitSlots, codingData);

                // 모든 플랫폼에서 PlayerPrefs 사용
                PlayerPrefs.SetString("tactics", json);
                PlayerPrefs.Save();
                Debug.Log("Formation saved to PlayerPrefs");

#if UNITY_EDITOR
                // 에디터에서는 Resources에도 저장 (사용자 가시성)
                string resourcesPath = System.IO.Path.Combine(Application.dataPath, "Resources/tactics.json");
                System.IO.File.WriteAllText(resourcesPath, json);
                Debug.Log($"Formation saved to {resourcesPath} (Editor only)");
#endif

                // Note: Firebase 저장은 BattleScene으로 이동할 때만 수행됨 (OnRunBattleClicked에서)
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save formation: {e.Message}");
            }
        }


        /// <summary>
        /// CharacterPool에 새 캐릭터를 추가합니다 (가챠 시스템용)
        /// </summary>
        public void AddCharacterToPool(string characterName)
        {
            try
            {
                // CharacterPool.json 로드
                string poolJson = "";

                // 모든 플랫폼에서 PlayerPrefs 사용
                poolJson = PlayerPrefs.GetString("CharacterPool", "");
                if (string.IsNullOrEmpty(poolJson))
                {
                    // PlayerPrefs에 없으면 Resources에서 로드 (에디터에서 주로 사용)
#if UNITY_EDITOR
                    TextAsset poolAsset = Resources.Load<TextAsset>("CharacterPool");
                    if (poolAsset != null)
                    {
                        poolJson = poolAsset.text;
                    }
#endif
                }

                // 빈 파일이거나 유효하지 않은 JSON인 경우 빈 리스트로 시작
                List<CharacterPoolData> poolList = new List<CharacterPoolData>();
                
                if (!string.IsNullOrWhiteSpace(poolJson) && poolJson.Trim() != "")
                {
                    try
                    {
                        // JSON 파싱
                        CharacterPoolData[] poolData = JsonHelper.FromJson<CharacterPoolData>(poolJson);
                        if (poolData != null)
                        {
                            poolList = new List<CharacterPoolData>(poolData);
                        }
                    }
                    catch (System.Exception parseEx)
                    {
                        Debug.LogWarning($"CharacterPool JSON 파싱 실패: {parseEx.Message}. 빈 리스트로 시작합니다.");
                        poolList = new List<CharacterPoolData>();
                    }
                }
                else
                {
                    Debug.Log("CharacterPool.json이 비어있습니다. 새로 시작합니다.");
                }

                // 이미 존재하는지 확인
                if (poolList.Any(c => c.Name == characterName))
                {
                    Debug.Log($"캐릭터 '{characterName}'는 이미 보유하고 있습니다.");
                    return;
                }

                // 새 캐릭터 추가 (기본 tactics 없이 - 빈 배열로 저장)
                var newChar = new CharacterPoolData
                {
                    Name = characterName,
                    tactics = new TacticsData[0] // 빈 배열로 저장 (null이 아닌 빈 배열)
                };
                poolList.Add(newChar);

                // JSON으로 변환
                var wrapper = new CharacterPoolDataWrapper { characters = poolList.ToArray() };
                string newJson = JsonUtility.ToJson(wrapper, true);

                // 배열 부분만 추출
                int startIndex = newJson.IndexOf('[');
                int endIndex = newJson.LastIndexOf(']');
                if (startIndex >= 0 && endIndex >= 0)
                {
                    newJson = newJson.Substring(startIndex, endIndex - startIndex + 1);
                }

                // 저장 - 모든 플랫폼에서 PlayerPrefs 사용
                PlayerPrefs.SetString("CharacterPool", newJson);
                PlayerPrefs.Save();
                Debug.Log($"CharacterPool에 '{characterName}' 추가 완료");

#if UNITY_EDITOR
                // 에디터에서는 Resources에도 저장 (사용자 가시성)
                string resourcesPath = System.IO.Path.Combine(Application.dataPath, "Resources/CharacterPool.json");
                System.IO.File.WriteAllText(resourcesPath, newJson);
                Debug.Log($"CharacterPool also saved to {resourcesPath} (Editor only)");
#endif

                // Note: ReloadCharacterPool()은 호출하지 않음
                // 각 씬은 독립적으로 동작하며, TacticsScene으로 넘어갈 때 TacticsDataManager가 새로 생성되어
                // LoadCharactersFromWeb()에서 최신 CharacterPool.json을 자동으로 로드함
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CharacterPool에 캐릭터 추가 실패: {e.Message}");
            }
        }

        /// <summary>
        /// CharacterPool 데이터를 파일에 저장
        /// </summary>
        public void SaveTacticsToFile(Dictionary<string, TacticsPlan> codingData)
        {
            try
            {
                // Build the save data structure using unified classes
                var poolData = new List<CharacterPoolData>();

                foreach (var character in availableCharacters)
                {
                    var saveData = new CharacterPoolData
                    {
                        Name = character.characterName
                    };

                    // If this character has tactics data, save it
                    if (codingData.TryGetValue(character.id, out var plan))
                    {
                        var tacticRowsList = new List<TacticRowData>();
                        foreach (var row in plan.rows)
                        {
                            tacticRowsList.Add(new TacticRowData
                            {
                                skill = row.skillName,
                                condition1 = row.condition1,
                                condition2 = row.condition2
                            });
                        }

                        saveData.tactics = new TacticsData[]
                        {
                            new TacticsData
                            {
                                characterClass = character.characterClass,
                                plan = tacticRowsList.ToArray()
                            }
                        };
                    }

                    poolData.Add(saveData);
                }

                // Serialize to JSON using JsonUtility
                // Note: JsonUtility doesn't support List<T> at root level, so we need a wrapper
                var wrapper = new CharacterPoolDataWrapper { characters = poolData.ToArray() };
                string json = JsonUtility.ToJson(wrapper, true);

                // Extract the array part (remove wrapper)
                // This keeps the JSON format compatible with existing files
                int startIndex = json.IndexOf('[');
                int endIndex = json.LastIndexOf(']');
                if (startIndex >= 0 && endIndex >= 0)
                {
                    json = json.Substring(startIndex, endIndex - startIndex + 1);
                }

                // 모든 플랫폼에서 PlayerPrefs 사용
                PlayerPrefs.SetString("CharacterPool", json);
                PlayerPrefs.Save();
                Debug.Log("CharacterPool saved to PlayerPrefs");

#if UNITY_EDITOR
                // 에디터에서는 Resources에도 저장 (사용자 가시성)
                string resourcesPath = System.IO.Path.Combine(Application.dataPath, "Resources/CharacterPool.json");
                System.IO.File.WriteAllText(resourcesPath, json);
                Debug.Log($"CharacterPool also saved to {resourcesPath} (Editor only)");
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save CharacterPool: {e.Message}");
            }
        }

        /// <summary>
        /// tactics.json에서 포메이션 로드
        /// </summary>
        public FormationLoadResult LoadFormationFromTacticsFile(bool isPlayer)
        {
            FormationLoadResult result = isPlayer ? _playerFormationLoadResult : _enemyFormationLoadResult;

            try
            {
                string json = "";

                // 모든 플랫폼에서 PlayerPrefs 사용
                json = PlayerPrefs.GetString("tactics", "");
                if (!string.IsNullOrEmpty(json))
                {
                    Debug.Log("Loaded tactics.json from PlayerPrefs");
                }
                else
                {
                    // PlayerPrefs에 없으면 Resources에서 로드 (에디터에서 주로 사용)
#if UNITY_EDITOR
                    TextAsset tacticsAsset = Resources.Load<TextAsset>("tactics");
                    if (tacticsAsset != null)
                    {
                        json = tacticsAsset.text;
                        Debug.Log("Loaded tactics.json from Resources (no PlayerPrefs found)");
                    }
                    else
                    {
                        Debug.LogWarning("tactics.json not found in PlayerPrefs or Resources");
                        return result;
                    }
#else
                    Debug.LogWarning("tactics.json not found in PlayerPrefs");
                    return result;
#endif
                }

                TacticsFileData tacticsData = JsonUtility.FromJson<TacticsFileData>(json);
                if (tacticsData == null || tacticsData.positions == null)
                {
                    Debug.LogWarning("Failed to parse tactics.json");
                    return result;
                }

                // Set Username and stats
                result.username = tacticsData.username;
                result.score = tacticsData.score;
                result.winCount = tacticsData.winCount;
                result.loseCount = tacticsData.loseCount;

                // Load each position
                foreach (var posData in tacticsData.positions)
                {
                    if (string.IsNullOrEmpty(posData.name)) continue;

                    int slotIndex = int.Parse(posData.position) - 1;
                    if (slotIndex < 0 || slotIndex >= 6) continue;

                    // Find the character by name
                    CharacterData character = availableCharacters.Find(c => c.characterName.ToLower() == posData.name.ToLower());
                    if (character == null)
                    {
                        Debug.LogWarning($"Character {posData.name} not found in available characters");
                        continue;
                    }

                    // Place character in slot
                    result.unitSlots[slotIndex] = character;

                    // Load tactics if present
                    if (posData.tactics != null && posData.tactics.Length > 0)
                    {
                        var tacticData = posData.tactics[0];
                        if (tacticData.plan != null && tacticData.plan.Length > 0)
                        {
                            var plan = new TacticsPlan(character.id);

                            // TacticsPlan은 이미 8개의 기본 Row를 가지고 있음
                            // 로드한 데이터로 앞부분을 채움 (최대 8개까지)
                            for (int i = 0; i < tacticData.plan.Length && i < TacticsDatabase.MAX_TACTICS_ROW; i++)
                            {
                                var rowData = tacticData.plan[i];

                                // Determine skill type from character's skills
                                string skillType = "AP";
                                var skill = character.skills.Find(s => s.name == rowData.skill);
                                if (skill != null)
                                {
                                    skillType = skill.skillType;
                                }

                                plan.rows[i] = new TacticRow(
                                    rowData.skill,
                                    skillType,
                                    rowData.condition1,
                                    rowData.condition2
                                );
                            }

                            result.codingData[character.id] = plan;
                        }
                    }
                    else
                    {
                        // No saved tactics, create default plan
                        if (!result.codingData.ContainsKey(character.id))
                        {
                            result.codingData[character.id] = CreateDefaultPlan(character);
                        }
                    }
                }

                Debug.Log("Formation loaded successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load formation: {e.Message}");
            }

            return result;
        }

        /// <summary>
        /// JSONBin.io에서 랜덤 적 편성 로드
        /// </summary>
        private void LoadEnemyFormationFromJsonBin(string enemyName, System.Action<bool> onComplete)
        {
            if (JSONBinManager.Instance == null)
            {
                Debug.LogWarning("JSONBinManager가 없습니다. 로컬 파일에서 적 편성을 로드합니다.");
                _enemyFormationLoadResult = LoadFormationFromTacticsFile(false);
                onComplete?.Invoke(true);
                return;
            }

            JSONBinManager.Instance.GetRandomTactics(enemyName, (success, tacticsJson, username) =>
            {
                if (success && !string.IsNullOrEmpty(tacticsJson))
                {
                    try
                    {
                        // JSONBin.io에서 가져온 JSON 파싱
                        TacticsFileData tacticsData = JsonUtility.FromJson<TacticsFileData>(tacticsJson);
                        if (tacticsData == null || tacticsData.positions == null)
                        {
                            Debug.LogWarning("JSONBin.io 데이터 파싱 실패. 로컬 파일 사용.");
                            _enemyFormationLoadResult = LoadFormationFromTacticsFile(false);
                            onComplete?.Invoke(true);
                            return;
                        }

                        // Username 설정 (tacticsJson 안에 포함된 username 사용)
                        _enemyFormationLoadResult.username = username;

                        // Load each position
                        foreach (var posData in tacticsData.positions)
                        {
                            if (string.IsNullOrEmpty(posData.name)) continue;

                            int slotIndex = int.Parse(posData.position) - 1;
                            if (slotIndex < 0 || slotIndex >= 6) continue;

                            // Find the character by name
                            CharacterData character = availableCharacters.Find(c => c.characterName.ToLower() == posData.name.ToLower());
                            if (character == null)
                            {
                                Debug.LogWarning($"Character {posData.name} not found in available characters");
                                continue;
                            }

                            // Place character in slot
                            _enemyFormationLoadResult.unitSlots[slotIndex] = character;

                            // Load tactics if present
                            if (posData.tactics != null && posData.tactics.Length > 0)
                            {
                                var tacticData = posData.tactics[0];
                                if (tacticData.plan != null && tacticData.plan.Length > 0)
                                {
                                    var plan = new TacticsPlan(character.id);

                                    for (int i = 0; i < tacticData.plan.Length && i < TacticsDatabase.MAX_TACTICS_ROW; i++)
                                    {
                                        var rowData = tacticData.plan[i];

                                        string skillType = "AP";
                                        var skill = character.skills.Find(s => s.name == rowData.skill);
                                        if (skill != null)
                                        {
                                            skillType = skill.skillType;
                                        }

                                        plan.rows[i] = new TacticRow(
                                            rowData.skill,
                                            skillType,
                                            rowData.condition1,
                                            rowData.condition2
                                        );
                                    }

                                    _enemyFormationLoadResult.codingData[character.id] = plan;
                                }
                            }
                            else
                            {
                                if (!_enemyFormationLoadResult.codingData.ContainsKey(character.id))
                                {
                                    _enemyFormationLoadResult.codingData[character.id] = CreateDefaultPlan(character);
                                }
                            }
                        }

                        Debug.Log($"Firebase에서 적 편성 로드 완료 (유저: {username})");
                        onComplete?.Invoke(true);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Firebase 데이터 처리 실패: {e.Message}. 로컬 파일 사용.");
                        _enemyFormationLoadResult = LoadFormationFromTacticsFile(false);
                        onComplete?.Invoke(true);
                    }
                }
                else
                {
                    Debug.LogWarning("Firebase에서 데이터를 가져오지 못했습니다. 로컬 파일 사용.");
                    _enemyFormationLoadResult = LoadFormationFromTacticsFile(false);
                    onComplete?.Invoke(true);
                }
            });
        }


        /// <summary>
        /// Tactics 파일 데이터 구조 (Save/Load 공용)
        /// </summary>
        [System.Serializable]
        public class TacticsFileData
        {
            public string key;
            public string username;            
            public int score = 0;
            public int winCount = 0;
            public int loseCount = 0;
            public PositionData[] positions;
        }

        [System.Serializable]
        public class PositionData
        {
            public string position;
            public string name;
            public TacticsData[] tactics;
        }

        [System.Serializable]
        public class TacticsData
        {
            public string characterClass;  // Save용 필드명

            [System.NonSerialized]
            private string _class;  // Load용 필드명 (@class)

            // JSON에서 "class" 필드를 읽을 때 사용
            public string @class
            {
                get => string.IsNullOrEmpty(_class) ? characterClass : _class;
                set
                {
                    _class = value;
                    characterClass = value;
                }
            }

            public TacticRowData[] plan;
        }

        [System.Serializable]
        public class TacticRowData
        {
            public string skill;
            public string condition1;
            public string condition2;
        }

        /// <summary>
        /// CharacterPool 데이터 구조 (Save/Load 공용)
        /// </summary>
        [System.Serializable]
        public class CharacterPoolData
        {
            public string Name;
            public TacticsData[] tactics;
        }

        public class FormationLoadResult
        {
            public string username;
            public int score = 0;
            public int winCount = 0;
            public int loseCount = 0;
            public CharacterData[] unitSlots;
            public Dictionary<string, TacticsPlan> codingData;
        }

        /// <summary>
        /// JsonUtility용 Wrapper (배열 직렬화를 위해 필요)
        /// </summary>
        [System.Serializable]
        public class CharacterPoolDataWrapper
        {
            public CharacterPoolData[] characters;
        }

        /// <summary>
        /// TacticsRecommend.json 데이터 구조
        /// </summary>
        [System.Serializable]
        public class TacticsRecommendWrapper
        {
            public TacticsRecommendClass[] classes;
        }

        [System.Serializable]
        public class TacticsRecommendClass
        {
            public string name;
            public TacticsRecommendTactics[] tactics;
        }

        [System.Serializable]
        public class TacticsRecommendTactics
        {
            public string characterClass;
            public TacticsRecommendRow[] plan;
        }

        [System.Serializable]
        public class TacticsRecommendRow
        {
            public string skill;
            public string condition1;
            public string condition2;
        }

        #endregion
    }
}
