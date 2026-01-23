using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Arcana.Tactics.Data;
using UnityEngine.Networking;
using System.Collections;

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
        private Dictionary<string, FormationLoadResult> _squadFormationJson = new Dictionary<string, FormationLoadResult>();  // 스쿼드 전술 JSON
        private FormationLoadResult _playerFormationLoadResult;
        private FormationLoadResult _enemyFormationLoadResult;
        private CharacterDefinition[] _allCharacterDefinitions; // 모든 캐릭터 정의 목록

        public bool isDataLoaded { get; private set; } = false;

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
            LoadFormationFromTacticsFile(true);
            Debug.Log("TacticsDataManager: Player formation 로드 완료");

            // CharacterPool.json에서 작전 코딩 데이터 로드 및 병합
            LoadTacticsFromCharacterPool();
            Debug.Log("TacticsDataManager: CharacterPool tactics 로드 완료");

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
                _enemyFormationLoadResult = FormationManager.LoadFormationFromTacticsFile(availableCharacters, CreateDefaultPlan);
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
            RankingManager.GetRanking(score, onComplete);
        }

        /// <summary>
        /// 모든 유저의 TacticsData를 score 순으로 가져옵니다 (비동기)
        /// </summary>
        /// <param name="onComplete">완료 콜백 (유저 데이터 리스트: username, score, winCount, loseCount)</param>
        public void GetAllUsersSortedByScore(System.Action<List<(string username, int score, int winCount, int loseCount)>> onComplete)
        {
            RankingManager.GetAllUsersSortedByScore(onComplete);
        }

        /// <summary>
        /// 사용자의 랭킹을 가져옵니다 (비동기)
        /// </summary>
        /// <param name="key">랭킹을 확인할 사용자 key</param>
        /// <param name="onComplete">완료 콜백 (랭킹, 0이면 사용자를 찾을 수 없음)</param>
        public void GetRankingByUsername(string key, System.Action<int> onComplete)
        {
            RankingManager.GetRankingByKey(key, onComplete);
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
        /// 캐릭터 이름으로 CharacterDefinition을 찾습니다
        /// </summary>
        public CharacterDefinition GetCharacterDefinitionByName(string characterName)
        {
            if (_allCharacterDefinitions == null || _allCharacterDefinitions.Length == 0)
            {
                return null;
            }

            return System.Array.Find(_allCharacterDefinitions, c => c.Name == characterName);
        }

        /// <summary>
        /// 캐릭터 이름으로 CharacterData를 가져옵니다. 보유 여부와 관계없이 전체 CharacterList(정의)에서 조회합니다.
        /// (Enemy 등 미보유 캐릭터의 초상화 등 정보 표시용)
        /// </summary>
        public CharacterData GetCharacterDataByName(string characterName)
        {
            if (string.IsNullOrEmpty(characterName)) return null;

            // 1) 전체 캐릭터 정의에서 검색
            CharacterDefinition def = GetCharacterDefinitionByName(characterName);
            if (def != null)
            {
                CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
                data.characterName = def.Name;
                data.characterClass = def.Class;
                data.cost = def.Cost;
                data.portrait = null;
                if (!string.IsNullOrEmpty(def.Portrait))
                {
                    string spriteName = System.IO.Path.GetFileNameWithoutExtension(def.Portrait);
                    data.portrait = Resources.Load<Sprite>($"Portraits/{spriteName}");
                    if (data.portrait == null)
                        data.portrait = Resources.Load<Sprite>(spriteName);
                }
                return data;
            }

            // 2) 정의가 없을 때만 (로딩 전 등) availableCharacters에서 폴백
            return availableCharacters?.Find(c => c.characterName == characterName);
        }

        /// <summary>
        /// JSON 파일에서 캐릭터 데이터 로드
        /// </summary>
        private const string CharacterListUrl = "";// "https://docs.google.com/spreadsheets/d/e/2PACX-1vTeCHZPMcs6QJuZeS7k2MosrZrhChNL5FrRH3ePRd5fQx-O-nSUmR4VwZI6VGhHg65tFcWMmIr2tBha/pub?gid=0&single=true&output=csv";

        /// <summary>
        /// 웹 CSV에서 캐릭터 데이터 로드 (Web Request)
        /// </summary>
        public IEnumerator LoadCharactersFromWeb()
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
                        allCharacters = CSVParser.ParseCharacterCSV(csvText);
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

            LoadCharacterPool();
        }
        public void LoadCharacterPool()
        {
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
                CharacterDefinition def = System.Array.Find(_allCharacterDefinitions, c => c.Name == poolItem.Name);

                if (def != null)
                {
                    // 5. Create CharacterData
                    CharacterData newData = ScriptableObject.CreateInstance<CharacterData>();
                    newData.characterName = def.Name;
                    newData.characterClass = def.Class;

                    // Defaults for missing data
                    newData.cost = def.Cost;
                    newData.speed = 10;
                    newData.arcana = "None";
                    newData.description = "No description available.";
                    newData.model = def.Model ?? "";

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
                            name = "Attack", 
                            type = "active", 
                            costAP = 1,
                            effects = new List<SkillEffect>(),
                            traits = new List<string>()
                        });
                        newData.skills.Add(new Skill { 
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

        /// <summary>
        /// 모든 캐릭터 정의를 CharacterData로 변환 (Enemy Squad용 - 플레이어가 가지고 있지 않은 캐릭터도 포함)
        /// </summary>
        public List<CharacterData> GetAllCharactersFromDefinitions()
        {
            List<CharacterData> allCharacters = new List<CharacterData>();

            if (_allCharacterDefinitions == null || _allCharacterDefinitions.Length == 0)
            {
                Debug.LogWarning("TacticsDataManager: [GetAllCharactersFromDefinitions] _allCharacterDefinitions가 비어있습니다. availableCharacters 사용.");
                return availableCharacters ?? new List<CharacterData>();
            }

            foreach (var def in _allCharacterDefinitions)
            {
                // 이미 availableCharacters에 있는 캐릭터는 재사용
                CharacterData existingChar = availableCharacters?.Find(c => c.characterName == def.Name);
                if (existingChar != null)
                {
                    allCharacters.Add(existingChar);
                    continue;
                }

                // 새로운 CharacterData 생성
                CharacterData newData = ScriptableObject.CreateInstance<CharacterData>();
                newData.characterName = def.Name;
                newData.characterClass = def.Class;
                newData.cost = def.Cost;
                newData.speed = 10;
                newData.arcana = "None";
                newData.description = "No description available.";
                newData.model = def.Model ?? "";

                // Load Portrait
                string spriteName = System.IO.Path.GetFileNameWithoutExtension(def.Portrait);
                newData.portrait = Resources.Load<Sprite>($"Portraits/{spriteName}");
                if (newData.portrait == null)
                {
                    newData.portrait = Resources.Load<Sprite>(spriteName);
                }
                if (newData.portrait == null)
                {
                    Debug.LogWarning($"Portrait not found for {def.Name}: {def.Portrait}");
                }

                // Assign skills based on class
                newData.skills = new List<Skill>();

                // Find matching key in skill map
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
                    foreach (var s in classSkills)
                    {
                        newData.skills.Add(new Skill
                        {
                            name = s.name,
                            type = s.type,
                            description = s.description,
                            target = s.target,
                            costAP = s.costAP,
                            costPP = s.costPP,
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
                        name = "Attack", 
                        type = "active", 
                        costAP = 1,
                        effects = new List<SkillEffect>(),
                        traits = new List<string>()
                    });
                    newData.skills.Add(new Skill { 
                        name = "Guard", 
                        type = "passive", 
                        costPP = 1,
                        effects = new List<SkillEffect>(),
                        traits = new List<string>()
                    });
                }

                allCharacters.Add(newData);
            }

            Debug.Log($"TacticsDataManager: [GetAllCharactersFromDefinitions] {allCharacters.Count}개 캐릭터 생성 완료");
            return allCharacters;
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
                    classList = CSVParser.ParseClassCSV(csvText);

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
            var plan = new TacticsPlan(data.characterName);

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
        // 데이터 클래스들은 TacticsDataModels.cs로 이동되었습니다.
        // CharacterDefinition, ClassListWrapper, ClassInfo, JsonHelper 등은 TacticsDataModels에서 사용합니다.

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
                    if (codingData.TryGetValue(character.characterName, out var plan))
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
        public void SaveFormationToTacticsFile(CharacterData[] unitSlots, Dictionary<string, TacticsPlan> codingData, string fileName = "tactics")
        {
            try
            {
                string json = GetTacticsJson(unitSlots, codingData);

                // 모든 플랫폼에서 PlayerPrefs 사용
                PlayerPrefs.SetString(fileName, json);
                PlayerPrefs.Save();
                Debug.Log("Formation saved to PlayerPrefs");

#if UNITY_EDITOR
                // 에디터에서는 Resources에도 저장 (사용자 가시성)
                string resourcesPath = System.IO.Path.Combine(Application.dataPath, "Resources/" + fileName + ".json");
                System.IO.File.WriteAllText(resourcesPath, json);
                Debug.Log($"Formation saved to {resourcesPath} (Editor only)");
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save formation: {e.Message}");
            }
        }         

        public string LoadFormationFromTacticsFile(string fileName = "tactics")
        {
            return PlayerPrefs.GetString(fileName, "");
        }

        public void SavePlayerTacticsToServer(Action<bool> onComplete)
        {
            string tacticsJson = LoadFormationFromTacticsFile("tactics");
            SavePlayerTacticsToServer(tacticsJson, onComplete);
        }

        public void SavePlayerTacticsToServer(string tacticsJson, Action<bool> onComplete)
        {
            if(string.IsNullOrEmpty(tacticsJson))
            {
                Debug.LogError("Failed to save player tactics to server");
                onComplete?.Invoke(false);
                return;
            }

            JSONBinManager.Instance.SaveTactics(tacticsJson, (success, message) =>
            {
                if(success)
                {
                    Debug.Log("BattleManager: SaveTactics - " + message);
                    onComplete?.Invoke(success);
                }
                else
                {
                    Debug.LogError("Failed to save player tactics to server");
                    onComplete?.Invoke(false);
                }
            });
        }

        public void SaveSquadTactics(string squadName, CharacterData[] unitSlots, Dictionary<string, TacticsPlan> codingData)
        {
            try
            {
                string json = GetTacticsJson(unitSlots, codingData);
                var loadResult = FormationManager.LoadFormationFromJson(json, availableCharacters, CreateDefaultPlan);
                loadResult.username = squadName;
                loadResult.score = 0; 
                loadResult.winCount = 0;
                loadResult.loseCount = 0;
                _squadFormationJson[squadName] = loadResult;
                
                // PlayerPrefs에도 저장하여 씬이 바뀌어도 유지되도록 함
                PlayerPrefs.SetString($"Squad_{squadName}", json);
                PlayerPrefs.Save();
                Debug.Log($"Squad '{squadName}'의 데이터를 메모리와 PlayerPrefs에 저장했습니다.");                
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save squad tactics: {e.Message}");
            }
        }       

        public void SaveSquadTactics(string squadName, FormationLoadResult loadResult)
        {
            _squadFormationJson[squadName] = loadResult;
            string json = GetTacticsJson(loadResult.unitSlots, loadResult.codingData);
            PlayerPrefs.SetString($"Squad_{squadName}", json);
            PlayerPrefs.Save();            
        }
      
        /// <summary>
        /// CharacterPool에 새 캐릭터를 추가합니다 (가챠 시스템용)
        /// </summary>
        public void AddCharacterToPool(string characterName)
        {
            FormationManager.AddCharacterToPool(characterName);
        }

        /// <summary>
        /// CharacterPool 데이터를 파일에 저장
        /// </summary>
        public void SaveTacticsToFile(Dictionary<string, TacticsPlan> codingData)
        {
            FormationManager.SaveTacticsToFile(availableCharacters, codingData);
            
            // 저장 후 메모리 상의 codingData도 갱신 (파일과 메모리 동기화)
            LoadTacticsFromCharacterPool();
        }

        /// <summary>
        /// tactics.json에서 포메이션 로드
        /// </summary>
        public FormationLoadResult LoadFormationFromTacticsFile(bool isPlayer)
        {
            FormationLoadResult result = FormationManager.LoadFormationFromTacticsFile(availableCharacters, CreateDefaultPlan);
            if(isPlayer)
            {
                _playerFormationLoadResult = result;
            }
            else
            {
                _enemyFormationLoadResult = result;
            }   
            return result;
        }

        public FormationLoadResult LoadSquadTactics(string squadName)
        {
            // _squadFormationJson에 키가 있는지 확인
            if (!_squadFormationJson.ContainsKey(squadName))
            {
                Debug.LogWarning($"Squad '{squadName}'의 데이터가 _squadFormationJson에 없습니다. PlayerPrefs에서 로드를 시도합니다.");
                
                // PlayerPrefs에서 로드 시도
                string json = PlayerPrefs.GetString($"Squad_{squadName}", "");
                if (!string.IsNullOrEmpty(json))
                {
                    Debug.Log($"PlayerPrefs에서 Squad '{squadName}' 데이터를 찾았습니다.");
                    var loadResult = FormationManager.LoadFormationFromJson(json, availableCharacters, CreateDefaultPlan);
                    loadResult.username = squadName;
                    _squadFormationJson[squadName] = loadResult;

                    return loadResult;
                }
                
                Debug.LogWarning($"Squad '{squadName}'의 데이터를 찾을 수 없습니다. null을 반환합니다.");
                return null; // null을 반환하여 기본 포메이션 사용을 알림
            }

            try
            {
                return _squadFormationJson[squadName];                
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load squad tactics: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// CharacterPool.json에서 작전 코딩 데이터 로드 및 병합
        /// </summary>
        private void LoadTacticsFromCharacterPool()
        {
            try
            {
                // CharacterPool.json 로드
                string poolJson = "";
                poolJson = PlayerPrefs.GetString("CharacterPool", "");
                if (string.IsNullOrEmpty(poolJson))
                {
#if UNITY_EDITOR
                    TextAsset poolAsset = Resources.Load<TextAsset>("CharacterPool");
                    if (poolAsset != null)
                    {
                        poolJson = poolAsset.text;
                    }
#endif
                }

                if (string.IsNullOrWhiteSpace(poolJson) || poolJson.Trim() == "")
                {
                    Debug.Log("CharacterPool.json이 비어있습니다. 작전 코딩 데이터를 로드하지 않습니다.");
                    return;
                }

                // CharacterPoolData 배열로 파싱
                CharacterPoolData[] poolData = JsonHelper.FromJson<CharacterPoolData>(poolJson);
                if (poolData == null)
                {
                    Debug.LogWarning("CharacterPool JSON 파싱 실패");
                    return;
                }

                // 각 캐릭터의 tactics 데이터를 로드하여 병합
                foreach (var poolItem in poolData)
                {
                    if (string.IsNullOrEmpty(poolItem.Name)) continue;
                    if (poolItem.tactics == null || poolItem.tactics.Length == 0) continue;

                    // characterName으로 매칭
                    CharacterData character = availableCharacters.Find(c => c.characterName == poolItem.Name);
                    if (character == null)
                    {
                        Debug.LogWarning($"CharacterPool의 캐릭터 '{poolItem.Name}'를 availableCharacters에서 찾을 수 없습니다.");
                        continue;
                    }

                    // tactics 데이터 로드
                    var tacticData = poolItem.tactics[0];
                    if (tacticData.plan != null && tacticData.plan.Length > 0)
                    {
                        var plan = new TacticsPlan(character.characterName);

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
                                rowData.skill ?? "---",
                                skillType,
                                rowData.condition1 ?? "조건 없음",
                                rowData.condition2 ?? "조건 없음"
                            );
                        }

                        // _playerFormationLoadResult.codingData에 병합 (characterName을 키로 사용)
                        // tactics.json에서 이미 로드된 데이터가 있으면 덮어쓰지 않고, 없으면 추가
                        if (!_playerFormationLoadResult.codingData.ContainsKey(character.characterName))
                        {
                            _playerFormationLoadResult.codingData[character.characterName] = plan;
                            Debug.Log($"CharacterPool에서 '{poolItem.Name}'의 작전 코딩 로드 완료");
                        }
                        else
                        {
                            // 이미 tactics.json에서 로드된 데이터가 있으면 CharacterPool 데이터로 덮어쓰기
                            // (CharacterPool이 더 최신 데이터일 수 있음)
                            _playerFormationLoadResult.codingData[character.characterName] = plan;
                            Debug.Log($"CharacterPool에서 '{poolItem.Name}'의 작전 코딩을 덮어쓰기 완료");
                        }
                    }
                }

                Debug.Log($"CharacterPool에서 {poolData.Length}개 캐릭터의 작전 코딩 데이터 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CharacterPool에서 작전 코딩 데이터 로드 실패: {e.Message}");
            }
        }

        /// <summary>
        /// JSONBin.io에서 랜덤 적 편성 로드
        /// </summary>
        public void LoadEnemyFormationFromJsonBin(string enemyName, System.Action<bool> onComplete)
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
                        // FormationManager를 사용하여 JSON에서 포메이션 로드
                        _enemyFormationLoadResult = FormationManager.LoadFormationFromJson(tacticsJson, availableCharacters, CreateDefaultPlan);
                        
                        if (_enemyFormationLoadResult == null)
                        {
                            Debug.LogError("TacticsDataManager: [LoadEnemyFormationFromJsonBin] FormationLoadResult가 null입니다!");
                            _enemyFormationLoadResult = LoadFormationFromTacticsFile(false);
                            onComplete?.Invoke(true);
                            return;
                        }
                        
                        if (_enemyFormationLoadResult.unitSlots == null)
                        {
                            Debug.LogError("TacticsDataManager: [LoadEnemyFormationFromJsonBin] unitSlots가 null입니다!");
                            _enemyFormationLoadResult = LoadFormationFromTacticsFile(false);
                            onComplete?.Invoke(true);
                            return;
                        }
                        
                        _enemyFormationLoadResult.username = username; // Username 설정
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

        public void GetRandomEnemySquad(System.Action<FormationLoadResult> onComplete)
        {
            // availableCharacters가 로드되지 않았으면 대기
            if (availableCharacters == null || availableCharacters.Count == 0)
            {
                StartCoroutine(WaitForCharactersAndGetRandomEnemySquad(onComplete));
                return;
            }

            // _allCharacterDefinitions가 로드되지 않았으면 대기
            if (_allCharacterDefinitions == null || _allCharacterDefinitions.Length == 0)
            {
                StartCoroutine(WaitForAllCharactersAndGetRandomEnemySquad(onComplete));
                return;
            }

            JSONBinManager.Instance.GetRandomTactics("", (success, tacticsJson, username) =>
            {
                if(success && !string.IsNullOrEmpty(tacticsJson))
                {
                    try
                    {
                        // Enemy Squad는 모든 캐릭터 정의를 사용 (플레이어가 가지고 있지 않은 캐릭터도 포함)
                        List<CharacterData> allCharacters = GetAllCharactersFromDefinitions();
                        
                        // FormationManager를 사용하여 JSON에서 포메이션 로드
                        var loadResult = FormationManager.LoadFormationFromJson(tacticsJson, allCharacters, CreateDefaultPlan);
                        
                        if (loadResult == null)
                        {
                            Debug.LogError("TacticsDataManager: [GetRandomEnemySquad] FormationLoadResult가 null입니다!");
                            onComplete?.Invoke(null);
                            return;
                        }
                        
                        if (loadResult.unitSlots == null)
                        {
                            Debug.LogError("TacticsDataManager: [GetRandomEnemySquad] unitSlots가 null입니다!");
                            onComplete?.Invoke(null);
                            return;
                        }
                        
                        loadResult.username = username; // Username 설정
                        Debug.Log($"Jsonbin.io에서 적 편성 로드 완료 (유저: {username})");
                        onComplete?.Invoke(loadResult);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"TacticsDataManager: [GetRandomEnemySquad] 데이터 처리 실패: {e.Message}");
                        onComplete?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogWarning("Jsonbin.io에서 데이터를 가져오지 못했습니다.");
                    onComplete?.Invoke(null);
                }
            });
        }

        private IEnumerator WaitForCharactersAndGetRandomEnemySquad(System.Action<FormationLoadResult> onComplete)
        {
            // availableCharacters가 로드될 때까지 대기 (최대 10초)
            float waitTime = 0f;
            const float maxWaitTime = 10f;
            
            while ((availableCharacters == null || availableCharacters.Count == 0) && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }

            if (availableCharacters == null || availableCharacters.Count == 0)
            {
                Debug.LogError($"TacticsDataManager: [GetRandomEnemySquad] availableCharacters 로드 타임아웃 ({maxWaitTime}초). 로컬 파일 사용.");
                var fallbackResult = LoadFormationFromTacticsFile(false);
                onComplete?.Invoke(fallbackResult);
                yield break;
            }

            GetRandomEnemySquad(onComplete);
        }

        private IEnumerator WaitForAllCharactersAndGetRandomEnemySquad(System.Action<FormationLoadResult> onComplete)
        {
            // _allCharacterDefinitions가 로드될 때까지 대기 (최대 10초)
            float waitTime = 0f;
            const float maxWaitTime = 10f;
            
            while ((_allCharacterDefinitions == null || _allCharacterDefinitions.Length == 0) && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }

            if (_allCharacterDefinitions == null || _allCharacterDefinitions.Length == 0)
            {
                Debug.LogError($"TacticsDataManager: [GetRandomEnemySquad] _allCharacterDefinitions 로드 타임아웃 ({maxWaitTime}초). availableCharacters 사용.");
                // Fallback to availableCharacters
                GetRandomEnemySquad(onComplete);
                yield break;
            }

            GetRandomEnemySquad(onComplete);
        }

        public void SetEnemyTactics(string enemyName)
        {
            _enemyFormationLoadResult = LoadSquadTactics(enemyName);
        }

        public void SetPlayerTactics(string playerName)
        {
            _playerFormationLoadResult = LoadSquadTactics(playerName);
        }


        // 모든 데이터 클래스들은 TacticsDataModels.cs로 이동되었습니다.
        // TacticsFileData, PositionData, TacticsData, TacticRowData, CharacterPoolData,
        // FormationLoadResult, CharacterPoolDataWrapper, TacticsRecommendWrapper 등은
        // TacticsDataModels에서 사용합니다.

        #endregion
    }
}
