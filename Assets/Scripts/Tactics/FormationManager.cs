using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Arcana.Tactics.Data;

namespace Arcana.Tactics
{
    /// <summary>
    /// 포메이션 저장/로드 관리
    /// </summary>
    public static class FormationManager
    {
        /// <summary>
        /// Tactics 데이터를 JSON 문자열로 변환
        /// </summary>
        public static string GetTacticsJson(CharacterData[] unitSlots, Dictionary<string, TacticsPlan> codingData)
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
        public static void SaveFormationToTacticsFile(CharacterData[] unitSlots, Dictionary<string, TacticsPlan> codingData, string fileName = "tactics")
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

                // Note: Firebase 저장은 BattleScene으로 이동할 때만 수행됨 (OnRunBattleClicked에서)
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save formation: {e.Message}");
            }
        }

        /// <summary>
        /// JSON에서 포메이션 로드 (공통 로직)
        /// </summary>
        public static FormationLoadResult LoadFormationFromJson(string json, List<CharacterData> availableCharacters, System.Func<CharacterData, TacticsPlan> createDefaultPlan)
        {
            FormationLoadResult result = new FormationLoadResult
            {
                unitSlots = new CharacterData[6],
                codingData = new Dictionary<string, TacticsPlan>()
            };

            try
            {
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
                                    rowData.skill,
                                    skillType,
                                    rowData.condition1,
                                    rowData.condition2
                                );
                            }

                            result.codingData[character.characterName] = plan;
                        }
                    }
                    else
                    {
                        // No saved tactics, create default plan
                        if (!result.codingData.ContainsKey(character.characterName))
                        {
                            result.codingData[character.characterName] = createDefaultPlan(character);
                        }
                    }
                }

                Debug.Log("Formation loaded successfully");
                return result;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load formation from json: {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// tactics.json에서 포메이션 로드
        /// </summary>
        public static FormationLoadResult LoadFormationFromTacticsFile(List<CharacterData> availableCharacters, System.Func<CharacterData, TacticsPlan> createDefaultPlan)
        {
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
                        return new FormationLoadResult
                        {
                            unitSlots = new CharacterData[6],
                            codingData = new Dictionary<string, TacticsPlan>()
                        };
                    }
#else
                    Debug.LogWarning("tactics.json not found in PlayerPrefs");
                    return new FormationLoadResult
                    {
                        unitSlots = new CharacterData[6],
                        codingData = new Dictionary<string, TacticsPlan>()
                    };
#endif
                }

                return LoadFormationFromJson(json, availableCharacters, createDefaultPlan);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load formation: {e.Message}");
            }

            return new FormationLoadResult
            {
                unitSlots = new CharacterData[6],
                codingData = new Dictionary<string, TacticsPlan>()
            };
        }

        /// <summary>
        /// CharacterPool 데이터를 파일에 저장
        /// </summary>
        public static void SaveTacticsToFile(List<CharacterData> availableCharacters, Dictionary<string, TacticsPlan> codingData)
        {
            try
            {
                // 1. 기존 CharacterPool 로드
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

                // 2. 기존 CharacterPool 데이터를 Dictionary로 변환 (캐릭터 이름을 키로 사용)
                Dictionary<string, CharacterPoolData> existingPoolDict = new Dictionary<string, CharacterPoolData>();
                if (!string.IsNullOrWhiteSpace(poolJson) && poolJson.Trim() != "")
                {
                    try
                    {
                        CharacterPoolData[] existingPoolData = JsonHelper.FromJson<CharacterPoolData>(poolJson);
                        if (existingPoolData != null)
                        {
                            foreach (var poolItem in existingPoolData)
                            {
                                if (!string.IsNullOrEmpty(poolItem.Name))
                                {
                                    existingPoolDict[poolItem.Name] = poolItem;
                                }
                            }
                        }
                    }
                    catch (System.Exception parseEx)
                    {
                        Debug.LogWarning($"기존 CharacterPool JSON 파싱 실패: {parseEx.Message}. 새로 시작합니다.");
                    }
                }

                // 3. 기존 CharacterPool을 기준으로, 패배 스쿼드(codingData)만 덮어쓰고 나머지 캐릭터 Tactics는 그대로 유지
                var poolData = new List<CharacterPoolData>();
                var availableMap = new Dictionary<string, CharacterData>();
                if (availableCharacters != null)
                {
                    foreach (var c in availableCharacters)
                    {
                        if (!string.IsNullOrEmpty(c.characterName))
                            availableMap[c.characterName] = c;
                    }
                }

                // 3a. 기존 CharacterPool 항목: codingData에 있으면 스쿼드 작전으로 갱신, 없으면 기존 tactics 유지
                foreach (var kv in existingPoolDict)
                {
                    var name = kv.Key;
                    var existingData = kv.Value;
                    CharacterPoolData saveData;

                    if (codingData != null && codingData.TryGetValue(name, out var plan) && availableMap.TryGetValue(name, out var character))
                    {
                        var tacticRowsList = new List<TacticRowData>();
                        foreach (var row in plan.rows)
                        {
                            tacticRowsList.Add(new TacticRowData { skill = row.skillName, condition1 = row.condition1, condition2 = row.condition2 });
                        }
                        saveData = new CharacterPoolData
                        {
                            Name = name,
                            tactics = new TacticsData[] { new TacticsData { characterClass = character.characterClass, plan = tacticRowsList.ToArray() } }
                        };
                    }
                    else
                    {
                        saveData = existingData; // 기존 Tactics 유지 (다른 캐릭터 Tactics 삭제 방지)
                    }
                    poolData.Add(saveData);
                }

                // 3b. 패배 스쿼드에만 있고 기존 Pool에 없는 캐릭터 추가
                if (codingData != null)
                {
                    foreach (var name in codingData.Keys)
                    {
                        if (string.IsNullOrEmpty(name) || existingPoolDict.ContainsKey(name)) continue;
                        if (!availableMap.TryGetValue(name, out var character)) continue;
                        if (!codingData.TryGetValue(name, out var plan)) continue;

                        var tacticRowsList = new List<TacticRowData>();
                        foreach (var row in plan.rows)
                        {
                            tacticRowsList.Add(new TacticRowData { skill = row.skillName, condition1 = row.condition1, condition2 = row.condition2 });
                        }
                        poolData.Add(new CharacterPoolData
                        {
                            Name = name,
                            tactics = new TacticsData[] { new TacticsData { characterClass = character.characterClass, plan = tacticRowsList.ToArray() } }
                        });
                    }
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
        /// CharacterPool에 새 캐릭터를 추가합니다 (가챠 시스템용)
        /// </summary>
        public static void AddCharacterToPool(string characterName)
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
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CharacterPool에 캐릭터 추가 실패: {e.Message}");
            }
        }
    }
}

