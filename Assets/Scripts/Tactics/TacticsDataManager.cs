using System;
using System.Collections.Generic;
using UnityEngine;
using Arcana.Tactics.Data;

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

        void Awake()
        {
            // 씬마다 독립적인 인스턴스 사용
            Instance = this;
            LoadAllData();
        }

        /// <summary>
        /// 모든 데이터 로드
        /// </summary>
        private void LoadAllData()
        {
            LoadSkillList();
            LoadClassList();
            LoadCharactersFromJSON();

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

            _playerFormationLoadResult = LoadFormationFromTacticsFile(true);
            _enemyFormationLoadResult = LoadFormationFromTacticsFile(false);
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
        /// JSON 파일에서 캐릭터 데이터 로드
        /// </summary>
        public void LoadCharactersFromJSON()
        {
            availableCharacters = new List<CharacterData>();

            // 1. Load CharacterList (Static Data)
            TextAsset listAsset = Resources.Load<TextAsset>("Table/CharacterList");
            if (listAsset == null)
            {
                Debug.LogError("Failed to load CharacterList.json");
                return;
            }

            // 2. Load CharacterPool (Dynamic Data)
            string poolJson = "";

            // Try to load from PlayerPrefs first
            if (PlayerPrefs.HasKey("CharacterPool"))
            {
                poolJson = PlayerPrefs.GetString("CharacterPool");
                Debug.Log("Loaded CharacterPool from PlayerPrefs");
            }
            else
            {
                // Fallback to Resources if no PlayerPrefs data exists
                TextAsset poolAsset = Resources.Load<TextAsset>("CharacterPool");
                if (poolAsset != null)
                {
                    poolJson = poolAsset.text;
                    Debug.Log("Loaded CharacterPool from Resources (no PlayerPrefs data found)");
                }
                else
                {
                    Debug.LogError("Failed to load CharacterPool from both PlayerPrefs and Resources");
                    return;
                }
            }

            // 3. Parse JSON
            CharacterDefinition[] allCharacters = JsonHelper.FromJson<CharacterDefinition>(listAsset.text);
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
                    if (newData.portrait == null) newData.portrait = Resources.Load<Sprite>(spriteName);

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

        /// <summary>
        /// 클래스 데이터 로드
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
                // Build positions data
                var tacticsData = new TacticsFileSaveData
                {
                    positions = new List<PositionSaveData>()
                };

                for (int i = 0; i < 6; i++)
                {
                    var posData = new PositionSaveData
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
                            var tacticData = new TacticsSaveData
                            {
                                characterClass = character.characterClass,
                                plan = new List<TacticRowSaveData>()
                            };

                            foreach (var row in plan.rows)
                            {
                                tacticData.plan.Add(new TacticRowSaveData
                                {
                                    skill = row.skillName,
                                    condition1 = row.condition1,
                                    condition2 = row.condition2
                                });
                            }

                            posData.tactics = new List<TacticsSaveData> { tacticData };
                        }
                    }

                    tacticsData.positions.Add(posData);
                }

                // Serialize to JSON with proper formatting
                string json = "{\n  \"positions\": [\n";

                for (int i = 0; i < tacticsData.positions.Count; i++)
                {
                    var pos = tacticsData.positions[i];
                    json += "    {\n";
                    json += $"      \"position\":\"{pos.position}\",\n";
                    json += $"      \"name\":\"";

                    if (!string.IsNullOrEmpty(pos.name))
                    {
                        json += pos.name.ToLower();
                    }
                    json += "\"";

                    // Add tactics if present
                    if (pos.tactics != null && pos.tactics.Count > 0)
                    {
                        json += ", \n      \"tactics\": [\n";
                        var tactics = pos.tactics[0];
                        json += "            {\n";
                        json += $"            \"class\": \"{tactics.characterClass}\",\n";
                        json += "            \"plan\": [\n";

                        for (int j = 0; j < tactics.plan.Count; j++)
                        {
                            var row = tactics.plan[j];
                            json += "                {\n";
                            json += $"                \"skill\": \"{row.skill}\",\n";
                            json += $"                \"condition1\": \"{row.condition1}\",\n";
                            json += $"                \"condition2\": \"{row.condition2}\"\n";
                            json += "                }";
                            if (j < tactics.plan.Count - 1) json += ",";
                            json += "\n";
                        }

                        json += "            ]\n";
                        json += "            }\n";
                        json += "        ]      ";
                    }

                    json += "\n    }";
                    if (i < tacticsData.positions.Count - 1) json += ",";
                    json += "\n";
                }

                json += "  ]\n}\n";

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
        /// CharacterPool 데이터를 PlayerPrefs에 저장
        /// </summary>
        public void SaveTacticsToFile(Dictionary<string, TacticsPlan> codingData)
        {
            try
            {
                // Build the save data structure
                var poolData = new List<CharacterPoolSaveData>();

                foreach (var character in availableCharacters)
                {
                    var saveData = new CharacterPoolSaveData
                    {
                        Name = character.characterName
                    };

                    // If this character has tactics data, save it
                    if (codingData.TryGetValue(character.id, out var plan))
                    {
                        var tacticData = new TacticsSaveData
                        {
                            characterClass = character.characterClass,
                            plan = new List<TacticRowSaveData>()
                        };

                        foreach (var row in plan.rows)
                        {
                            tacticData.plan.Add(new TacticRowSaveData
                            {
                                skill = row.skillName,
                                condition1 = row.condition1,
                                condition2 = row.condition2
                            });
                        }

                        saveData.tactics = new List<TacticsSaveData> { tacticData };
                    }

                    poolData.Add(saveData);
                }

                // Serialize to JSON
                string json = "[\n";
                for (int i = 0; i < poolData.Count; i++)
                {
                    json += "    {\n";
                    json += $"        \"Name\": \"{poolData[i].Name}\"";

                    if (poolData[i].tactics != null && poolData[i].tactics.Count > 0)
                    {
                        json += ",\n        \"tactics\": [\n";
                        var tactics = poolData[i].tactics[0];
                        json += "            {\n";
                        json += $"            \"class\": \"{tactics.characterClass}\",\n";
                        json += "            \"plan\": [\n";

                        for (int j = 0; j < tactics.plan.Count; j++)
                        {
                            var row = tactics.plan[j];
                            json += "                {\n";
                            json += $"                \"skill\": \"{row.skill}\",\n";
                            json += $"                \"condition1\": \"{row.condition1}\",\n";
                            json += $"                \"condition2\": \"{row.condition2}\"\n";
                            json += "                }";
                            if (j < tactics.plan.Count - 1) json += ",";
                            json += "\n";
                        }

                        json += "            ]\n";
                        json += "            }\n";
                        json += "        ]";
                    }

                    json += "\n    }";
                    if (i < poolData.Count - 1) json += ",";
                    json += "\n";
                }
                json += "]\n";

                // Save to PlayerPrefs
                PlayerPrefs.SetString("CharacterPool", json);
                PlayerPrefs.Save();
                Debug.Log("CharacterPool data saved to PlayerPrefs");

#if UNITY_EDITOR
                // Also save to Resources folder in editor for inspection
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

                TacticsFileLoadData tacticsData = JsonUtility.FromJson<TacticsFileLoadData>(json);
                if (tacticsData == null || tacticsData.positions == null)
                {
                    Debug.LogWarning("Failed to parse tactics.json");
                    return result;
                }

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

        [System.Serializable]
        public class TacticsSaveData
        {
            public string characterClass;
            public List<TacticRowSaveData> plan;
        }

        [System.Serializable]
        public class TacticRowSaveData
        {
            public string skill;
            public string condition1;
            public string condition2;
        }

        [System.Serializable]
        public class TacticsFileSaveData
        {
            public List<PositionSaveData> positions;
        }

        [System.Serializable]
        public class PositionSaveData
        {
            public string position;
            public string name;
            public List<TacticsSaveData> tactics;
        }

        [System.Serializable]
        public class TacticsFileLoadData
        {
            public PositionLoadData[] positions;
        }

        [System.Serializable]
        public class PositionLoadData
        {
            public string position;
            public string name;
            public TacticsLoadData[] tactics;
        }

        [System.Serializable]
        public class TacticsLoadData
        {
            public string @class;
            public TacticRowLoadData[] plan;
        }

        [System.Serializable]
        public class TacticRowLoadData
        {
            public string skill;
            public string condition1;
            public string condition2;
        }

        public class FormationLoadResult
        {
            public CharacterData[] unitSlots;
            public Dictionary<string, TacticsPlan> codingData;
        }

        [System.Serializable]
        public class CharacterPoolSaveData
        {
            public string Name;
            public List<TacticsSaveData> tactics;
        }

        #endregion
    }
}
