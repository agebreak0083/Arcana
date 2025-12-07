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

        [Header("Loaded Data")]
        public List<CharacterData> availableCharacters;

        private Dictionary<string, ClassInfo> _classData = new Dictionary<string, ClassInfo>();
        private Dictionary<string, List<SkillData>> _skillMap = new Dictionary<string, List<SkillData>>();
        private FormationLoadResult _playerFormationLoadResult;
        private FormationLoadResult _enemyFormationLoadResult;
        private CharacterDefinition[] _allCharacterDefinitions; // 모든 캐릭터 정의 목록

        public bool isDataLoaded { get; private set; } = false;

        void Awake()
        {
            // 씬마다 독립적인 인스턴스 사용
            Instance = this;
            StartCoroutine(LoadAllDataAsync());
        }

        /// <summary>
        /// 모든 데이터 비동기 로드
        /// </summary>
        private System.Collections.IEnumerator LoadAllDataAsync()
        {
            isDataLoaded = false;

            LoadSkillList();

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

            // Firebase 초기화 대기 (최대 5초)
            float waitTime = 0f;
            const float maxWaitTime = 5f;

            if (FirebaseManager.Instance != null)
            {
                Debug.Log("TacticsDataManager: Firebase 초기화 대기 중...");
                while (!FirebaseManager.Instance.isFirebaseInitialized && waitTime < maxWaitTime)
                {
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }

                if (FirebaseManager.Instance.isFirebaseInitialized)
                {
                    Debug.Log("TacticsDataManager: Firebase 초기화 완료!");
                }
                else
                {
                    Debug.LogWarning($"TacticsDataManager: Firebase 초기화 타임아웃 ({maxWaitTime}초). 로컬 파일 사용.");
                }
            }

            // Enemy formation 로드 (Firebase에서 랜덤 또는 로컬 파일)
            bool enemyLoadComplete = false;
            LoadEnemyFormationFromFirebase((success) =>
            {
                enemyLoadComplete = true;
            });

            // Firebase 로딩 완료 대기
            yield return new WaitUntil(() => enemyLoadComplete);

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
            string persistentPath = System.IO.Path.Combine(Application.persistentDataPath, "CharacterPool.json");

            // 1. Try loading from PersistentDataPath first
            if (System.IO.File.Exists(persistentPath))
            {
                poolJson = System.IO.File.ReadAllText(persistentPath);
                Debug.Log("Loaded CharacterPool from PersistentDataPath");
            }
            else
            {
                // 2. Fallback to Resources
                TextAsset poolAsset = Resources.Load<TextAsset>("CharacterPool");
                if (poolAsset != null)
                {
                    poolJson = poolAsset.text;
                    Debug.Log("Loaded CharacterPool from Resources (no PersistentDataPath file found)");
                }
                else
                {
                    Debug.LogError("Failed to load CharacterPool from both PersistentDataPath and Resources");
                    yield break;
                }
            }

            // 3. Parse Pool JSON
            CharacterPoolItem[] myPool = JsonHelper.FromJson<CharacterPoolItem>(poolJson);

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
                    newData.skills = new List<SkillData>();

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
                            newData.skills.Add(new SkillData
                            {
                                id = s.id,
                                name = s.name,
                                type = s.type,
                                description = s.description,
                                target = s.target,
                                costAP = s.costAP,
                                costPP = s.costPP
                            });
                        }
                    }
                    else
                    {
                        // Fallback if no skills found
                        newData.skills.Add(new SkillData { name = "Attack", type = "active", costAP = 1 });
                        newData.skills.Add(new SkillData { name = "Guard", type = "passive", costPP = 1 });
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
                        SkillData[] skills = JsonHelper.FromJson<SkillData>(arrayJson);
                        if (skills != null)
                        {
                            _skillMap[key] = new List<SkillData>(skills);
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
        public List<SkillData> GetClassSkills(string className)
        {
            foreach (var key in _skillMap.Keys)
            {
                if (key.Contains(className))
                {
                    return _skillMap[key];
                }
            }
            return new List<SkillData>();
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
                plan.rows[i] = new TacticRow(skill.name, skill.skillType.ToString(), TacticsDatabase.DEFAULT_CONDITION, TacticsDatabase.DEFAULT_CONDITION);
            }

            return plan;
        }

        #region Data Classes

        [System.Serializable]
        public class CharacterDefinition
        {
            public string Name;
            public string Portrait;
            public string Class;
            public int Cost;
        }

        [System.Serializable]
        public class CharacterPoolItem
        {
            public string Name;
        }

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
        /// tactics.json에 포메이션 저장
        /// </summary>
        public void SaveFormationToTacticsFile(CharacterData[] unitSlots, Dictionary<string, TacticsPlan> codingData)
        {
            try
            {
                // Build positions data using unified structure
                var positionsList = new List<PositionData>();

                string username;
                if (UserDataManager.Instance != null && UserDataManager.Instance.currentUserData != null)
                {
                    // username : playername_날짜시간
                    username = UserDataManager.Instance.currentUserData.playerName + "_" + DateTime.Now.ToString("yyMMddHHmm");
                }
                else
                {
                    username = "Player_" + DateTime.Now.ToString("yyMMddHHmm");
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

                // Serialize to JSON using JsonUtility
                var tacticsFileData = new TacticsFileData
                {
                    username = username,
                    positions = positionsList.ToArray()
                };

                string json = JsonUtility.ToJson(tacticsFileData, true);

                // 1. Save to PersistentDataPath (Runtime usage)
                string persistentPath = System.IO.Path.Combine(Application.persistentDataPath, "tactics.json");
                System.IO.File.WriteAllText(persistentPath, json);
                Debug.Log($"Formation saved to {persistentPath}");

#if UNITY_EDITOR
                // 2. Save to Resources (Editor usage - for user visibility)
                string resourcesPath = System.IO.Path.Combine(Application.dataPath, "Resources/tactics.json");
                System.IO.File.WriteAllText(resourcesPath, json);
                Debug.Log($"Formation saved to {resourcesPath}");
#endif

                // 3. Save to Firebase (모든 유저의 tactics 데이터 저장)
                if (FirebaseManager.Instance != null)
                {
                    FirebaseManager.Instance.SaveTacticsToFirebase(json, (success, key) =>
                    {
                        if (success)
                        {
                            Debug.Log($"Firebase에 Tactics 저장 완료: {key}");
                        }
                        else
                        {
                            Debug.LogWarning($"Firebase 저장 실패: {key}");
                        }
                    });
                }
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
                string persistentPath = System.IO.Path.Combine(Application.persistentDataPath, "CharacterPool.json");

                if (System.IO.File.Exists(persistentPath))
                {
                    poolJson = System.IO.File.ReadAllText(persistentPath);
                }
                else
                {
                    TextAsset poolAsset = Resources.Load<TextAsset>("CharacterPool");
                    if (poolAsset != null)
                    {
                        poolJson = poolAsset.text;
                    }
                    else
                    {
                        // CharacterPool이 없으면 새로 생성
                        poolJson = "[]";
                    }
                }

                // JSON 파싱
                CharacterPoolData[] poolData = JsonHelper.FromJson<CharacterPoolData>(poolJson);
                List<CharacterPoolData> poolList = poolData != null ? new List<CharacterPoolData>(poolData) : new List<CharacterPoolData>();

                // 이미 존재하는지 확인
                if (poolList.Any(c => c.Name == characterName))
                {
                    Debug.Log($"캐릭터 '{characterName}'는 이미 보유하고 있습니다.");
                    return;
                }

                // 새 캐릭터 추가 (기본 tactics 없이)
                var newChar = new CharacterPoolData
                {
                    Name = characterName,
                    tactics = null // 새로 획득한 캐릭터는 tactics가 없음
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

                // 저장
                System.IO.File.WriteAllText(persistentPath, newJson);
                Debug.Log($"CharacterPool에 '{characterName}' 추가 완료");

#if UNITY_EDITOR
                // Editor에서는 Resources에도 저장
                string resourcesPath = System.IO.Path.Combine(Application.dataPath, "Resources/CharacterPool.json");
                System.IO.File.WriteAllText(resourcesPath, newJson);
                Debug.Log($"CharacterPool also saved to {resourcesPath} (Editor only)");
#endif
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

                // 1. Save to PersistentDataPath (Runtime usage)
                string persistentPath = System.IO.Path.Combine(Application.persistentDataPath, "CharacterPool.json");
                System.IO.File.WriteAllText(persistentPath, json);
                Debug.Log($"CharacterPool saved to {persistentPath}");

#if UNITY_EDITOR
                // 2. Save to Resources (Editor usage - for user visibility)
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
                string persistentPath = System.IO.Path.Combine(Application.persistentDataPath, "tactics.json");

                // 1. Try loading from PersistentDataPath first
                if (System.IO.File.Exists(persistentPath))
                {
                    json = System.IO.File.ReadAllText(persistentPath);
                    Debug.Log("Loaded tactics.json from PersistentDataPath");
                }
                else
                {
                    // 2. Fallback to Resources
                    TextAsset tacticsAsset = Resources.Load<TextAsset>("tactics");
                    if (tacticsAsset != null)
                    {
                        json = tacticsAsset.text;
                        Debug.Log("Loaded tactics.json from Resources");
                    }
                    else
                    {
                        Debug.LogWarning("tactics.json not found in Resources or PersistentDataPath");
                        return result;
                    }
                }

                TacticsFileData tacticsData = JsonUtility.FromJson<TacticsFileData>(json);
                if (tacticsData == null || tacticsData.positions == null)
                {
                    Debug.LogWarning("Failed to parse tactics.json");
                    return result;
                }

                // Set Username                
                result.username = tacticsData.username;

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
                                    skillType = skill.skillType.ToString();
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
        /// Firebase에서 랜덤 적 편성 로드
        /// </summary>
        private void LoadEnemyFormationFromFirebase(System.Action<bool> onComplete)
        {
            if (FirebaseManager.Instance == null)
            {
                Debug.LogWarning("FirebaseManager가 없습니다. 로컬 파일에서 적 편성을 로드합니다.");
                _enemyFormationLoadResult = LoadFormationFromTacticsFile(false);
                onComplete?.Invoke(true);
                return;
            }

            FirebaseManager.Instance.GetRandomTacticsFromFirebase((success, tacticsJson, username) =>
            {
                if (success && !string.IsNullOrEmpty(tacticsJson))
                {
                    try
                    {
                        // Firebase에서 가져온 JSON 파싱
                        TacticsFileData tacticsData = JsonUtility.FromJson<TacticsFileData>(tacticsJson);
                        if (tacticsData == null || tacticsData.positions == null)
                        {
                            Debug.LogWarning("Firebase 데이터 파싱 실패. 로컬 파일 사용.");
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
                                            skillType = skill.skillType.ToString();
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
            public string username;
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

        #endregion
    }
}
