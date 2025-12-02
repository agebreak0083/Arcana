using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Arcana.Tactics.Data;
using Arcana.Tactics.UI;

namespace Arcana.Tactics
{
    public class TacticsManager : MonoBehaviour
    {
        [Header("Data")]
        public List<CharacterData> availableCharacters;
        public int maxCost = 15;

        [Header("UI Containers")]
        public Transform characterPoolContainer;
        public Transform formationGridContainer; // Should have 6 slots as children
        public Transform codingListContainer;

        [Header("UI Prefabs")]
        public GameObject characterCardPrefab;
        public GameObject tacticRowPrefab;

        [Header("UI Components")]
        public ConditionModalUI conditionModal;
        public TextMeshProUGUI currentCostText;
        public TextMeshProUGUI codingPanelTitle;
        public GameObject characterDetailPanel;
        public Image detailPortrait;
        public TextMeshProUGUI detailCost;
        public TextMeshProUGUI detailName;
        public TextMeshProUGUI detailClass;
        public TextMeshProUGUI detailArcana;
        public TextMeshProUGUI detailSpeed;
        public TextMeshProUGUI detailDesc;
        public Button removeFromUnitBtn;

        [Header("Detail Stats")]
        public TextMeshProUGUI detailStatHP;
        public TextMeshProUGUI detailStatPhysAtk;
        public TextMeshProUGUI detailStatPhysDef;
        public TextMeshProUGUI detailStatMagAtk;
        public TextMeshProUGUI detailStatMagDef;
        public TextMeshProUGUI detailStatAccuracy;
        public TextMeshProUGUI detailStatEvasion;
        public TextMeshProUGUI detailStatCritRate;
        public TextMeshProUGUI detailStatGuardRate;
        public TextMeshProUGUI detailStatSpeed;

        [Header("Buttons")]
        public Button runBattleButton;

        // State
        private CharacterData _selectedCharacter; // Currently selected (could be from pool or slot)
        private CharacterData[] _unitSlots = new CharacterData[6]; // 0-5
        private Dictionary<string, TacticsPlan> _codingData = new Dictionary<string, TacticsPlan>();

        // Modal State
        private string _modalTargetCharId;
        private int _modalTargetRowIndex;
        private int _modalTargetConditionNum; // 1 or 2

        private List<FormationSlotUI> _formationSlots = new List<FormationSlotUI>();
        private Dictionary<string, ClassInfo> _classData = new Dictionary<string, ClassInfo>();
        private Dictionary<string, List<SkillData>> _skillMap = new Dictionary<string, List<SkillData>>();

        private void Start()
        {
            AutoAssignReferences();
            InitializeUI();
            LoadFormationFromTacticsFile();
            UpdateAllUI();
        }

        private void AutoAssignReferences()
        {
            if (characterPoolContainer == null)
            {
                GameObject go = GameObject.Find("PoolScrollView");
                if (go != null)
                {
                    Transform viewport = go.transform.Find("Viewport");
                    if (viewport != null) characterPoolContainer = viewport.Find("Content");
                }
            }

            if (formationGridContainer == null)
            {
                GameObject go = GameObject.Find("FormationGridPanel");
                if (go != null) formationGridContainer = go.transform;
            }

            if (codingListContainer == null)
            {
                GameObject go = GameObject.Find("CodingScrollView");
                if (go != null)
                {
                    Transform viewport = go.transform.Find("Viewport");
                    if (viewport != null) codingListContainer = viewport.Find("Content");
                }
            }

            if (conditionModal == null) conditionModal = FindFirstObjectByType<ConditionModalUI>(FindObjectsInactive.Include);

            if (currentCostText == null)
            {
                GameObject go = GameObject.Find("CostText");
                if (go != null) currentCostText = go.GetComponent<TextMeshProUGUI>();
            }

            if (codingPanelTitle == null)
            {
                GameObject header = GameObject.Find("CodingHeader");
                if (header != null)
                {
                    Transform title = header.transform.Find("Title");
                    if (title != null) codingPanelTitle = title.GetComponent<TextMeshProUGUI>();
                }
            }

            if (characterDetailPanel == null)
            {
                GameObject go = GameObject.Find("DetailPanel");
                if (go != null) characterDetailPanel = go;
            }

            if (characterDetailPanel != null)
            {
                if (detailPortrait == null)
                {
                    Transform t = RecursiveFind(characterDetailPanel.transform, "PortraitImage");
                    if (t != null) detailPortrait = t.GetComponent<Image>();
                }
                if (detailCost == null)
                {
                    Transform t = RecursiveFind(characterDetailPanel.transform, "CostText_Detail");
                    if (t != null) detailCost = t.GetComponent<TextMeshProUGUI>();
                }
                if (detailName == null)
                {
                    Transform t = RecursiveFind(characterDetailPanel.transform, "Name");
                    if (t != null) detailName = t.GetComponent<TextMeshProUGUI>();
                }
                if (detailDesc == null)
                {
                    Transform t = RecursiveFind(characterDetailPanel.transform, "Description");
                    if (t != null) detailDesc = t.GetComponent<TextMeshProUGUI>();
                }
                if (removeFromUnitBtn == null)
                {
                    Transform t = RecursiveFind(characterDetailPanel.transform, "RemoveButton");
                    if (t != null) removeFromUnitBtn = t.GetComponent<Button>();
                }

                if (detailClass == null) detailClass = FindInfoValue("클래스:");
                if (detailArcana == null) detailArcana = FindInfoValue("고유 아르카나:");
                if (detailSpeed == null) detailSpeed = FindInfoValue("행동 속도:");

                if (detailStatHP == null) detailStatHP = FindDetailStat("Value_HP");
                if (detailStatPhysAtk == null) detailStatPhysAtk = FindDetailStat("Value_PhysAtk");
                if (detailStatPhysDef == null) detailStatPhysDef = FindDetailStat("Value_PhysDef");
                if (detailStatMagAtk == null) detailStatMagAtk = FindDetailStat("Value_MagAtk");
                if (detailStatMagDef == null) detailStatMagDef = FindDetailStat("Value_MagDef");
                if (detailStatAccuracy == null) detailStatAccuracy = FindDetailStat("Value_Accuracy");
                if (detailStatEvasion == null) detailStatEvasion = FindDetailStat("Value_Evasion");
                if (detailStatCritRate == null) detailStatCritRate = FindDetailStat("Value_CritRate");
                if (detailStatGuardRate == null) detailStatGuardRate = FindDetailStat("Value_GuardRate");
                if (detailStatSpeed == null) detailStatSpeed = FindDetailStat("Value_Speed");
            }

            if (runBattleButton == null) runBattleButton = GameObject.Find("RunBattleButton").GetComponent<Button>();

            if (characterCardPrefab == null) characterCardPrefab = Resources.Load<GameObject>("Prefabs/UI/CharacterCardPrefab");
            if (tacticRowPrefab == null) tacticRowPrefab = Resources.Load<GameObject>("Prefabs/UI/TacticRowPrefab");
        }

        private TextMeshProUGUI FindInfoValue(string labelStart)
        {
            if (characterDetailPanel == null) return null;
            Transform infoArea = RecursiveFind(characterDetailPanel.transform, "InfoArea");
            if (infoArea == null) return null;

            foreach (Transform child in infoArea)
            {
                Transform labelObj = child.Find("Label");
                if (labelObj != null)
                {
                    var labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
                    if (labelTmp != null && labelTmp.text.StartsWith(labelStart))
                    {
                        Transform valueObj = child.Find("Value");
                        if (valueObj != null) return valueObj.GetComponent<TextMeshProUGUI>();
                    }
                }
            }
            return null;
        }

        private TextMeshProUGUI FindDetailStat(string objName)
        {
            if (characterDetailPanel == null) return null;
            Transform t = RecursiveFind(characterDetailPanel.transform, objName);
            if (t != null) return t.GetComponent<TextMeshProUGUI>();
            return null;
        }

        private Transform RecursiveFind(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform result = RecursiveFind(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private void InitializeUI()
        {
            LoadCharactersFromJSON();

            if (characterPoolContainer != null && characterCardPrefab != null)
            {
                foreach (var charData in availableCharacters)
                {
                    var go = Instantiate(characterCardPrefab, characterPoolContainer);
                    var card = go.GetComponent<CharacterCardUI>();
                    card.Setup(charData, this, false);
                }
            }

            _formationSlots.Clear();
            for (int i = 0; i < 6; i++)
            {
                GameObject slotObj = GameObject.Find($"Slot_{i}");
                if (slotObj != null)
                {
                    var slot = slotObj.GetComponent<FormationSlotUI>();
                    if (slot != null)
                    {
                        slot.Setup(this, i);
                        _formationSlots.Add(slot);
                    }
                    else _formationSlots.Add(null);
                }
                else _formationSlots.Add(null);
            }

            if (conditionModal != null) conditionModal.Setup(this);
            if (removeFromUnitBtn != null) removeFromUnitBtn.onClick.AddListener(OnRemoveFromUnitClicked);
            if (runBattleButton != null) runBattleButton.onClick.AddListener(OnRunBattleClicked);
        }

        private void LoadCharactersFromJSON()
        {
            availableCharacters = new List<CharacterData>();

            // 1. Load JSON files
            TextAsset listAsset = Resources.Load<TextAsset>("Table/CharacterList");
            TextAsset poolAsset = Resources.Load<TextAsset>("CharacterPool");

            if (listAsset == null || poolAsset == null)
            {
                Debug.LogError("Failed to load CharacterList.json or CharacterPool.json");
                return;
            }

            // 2. Parse JSON
            CharacterDefinition[] allCharacters = JsonHelper.FromJson<CharacterDefinition>(listAsset.text);
            CharacterPoolItem[] myPool = JsonHelper.FromJson<CharacterPoolItem>(poolAsset.text);

            // Load skill data first
            LoadSkillList();

            // 3. Match and Create Data
            foreach (var poolItem in myPool)
            {
                // Find matching definition
                CharacterDefinition def = System.Array.Find(allCharacters, c => c.Name == poolItem.Name);

                if (def != null)
                {
                    // 4. Create CharacterData
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

            // Load class data
            LoadClassList();
        }

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

        [System.Serializable]
        private class CharacterDefinition
        {
            public string Name;
            public string Portrait; // Matches JSON key
            public string Class;
            public int Cost;
        }

        [System.Serializable]
        private class CharacterPoolItem
        {
            public string Name;
        }

        // Helper for array JSONs
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

        [System.Serializable]
        private class ClassListWrapper
        {
            public ClassInfo[] classes;
        }

        [System.Serializable]
        private class ClassInfo
        {
            public string name;
            public string description;
            public ClassStats stats;
        }

        [System.Serializable]
        private class ClassStats
        {
            public string hp;
            public string physicalAttack;
            public string physicalDefense;
            public string magicalAttack;
            public string magicalDefense;
            public string accuracy;
            public string evasion;
            public string criticalRate;
            public string guardRate;
            public string actionSpeed;
        }

        public void OnCharacterPoolCardClicked(CharacterData data)
        {
            _selectedCharacter = data;
            UpdateAllUI();
        }

        public void OnFormationSlotClicked(int slotIndex)
        {
            if (_selectedCharacter != null)
            {
                // Try to place selected character
                CharacterData charToPlace = _selectedCharacter;

                // Check if already in this slot
                if (_unitSlots[slotIndex] == charToPlace)
                {
                    // Just select it (already selected)
                    return;
                }

                // Check cost
                int currentTotalCost = CalculateTotalCost();
                int costDiff = charToPlace.cost;
                if (_unitSlots[slotIndex] != null) costDiff -= _unitSlots[slotIndex].cost;

                // If placing a new char (not swapping from another slot, which is complex, let's assume pool -> slot only for now or simple overwrite)
                // If the character is already deployed elsewhere, remove it from there first?
                int existingIndex = GetSlotIndex(charToPlace);
                if (existingIndex != -1)
                {
                    // Moving within slots
                    _unitSlots[existingIndex] = null;
                    costDiff = 0; // Cost doesn't change if just moving
                }

                if (currentTotalCost + costDiff > maxCost)
                {
                    Debug.LogWarning("Cost Limit Exceeded!");
                    // Show warning UI
                    return;
                }

                _unitSlots[slotIndex] = charToPlace;

                // Initialize coding data if needed
                if (!_codingData.ContainsKey(charToPlace.id))
                {
                    _codingData[charToPlace.id] = CreateDefaultPlan(charToPlace);
                }

                _selectedCharacter = charToPlace; // Keep selected
                UpdateAllUI();
            }
            else
            {
                // Select the character in the slot if any
                if (_unitSlots[slotIndex] != null)
                {
                    _selectedCharacter = _unitSlots[slotIndex];
                    UpdateAllUI();
                }
            }
        }

        public void OnRemoveFromUnitClicked()
        {
            if (_selectedCharacter == null) return;

            int idx = GetSlotIndex(_selectedCharacter);
            if (idx != -1)
            {
                _unitSlots[idx] = null;
                _codingData.Remove(_selectedCharacter.id); // Optional: Clear data on remove
                _selectedCharacter = null;
                UpdateAllUI();
            }
        }

        public void OnConditionClicked(string charId, int rowIndex, int conditionNum)
        {
            _modalTargetCharId = charId;
            _modalTargetRowIndex = rowIndex;
            _modalTargetConditionNum = conditionNum;
            conditionModal.Open();
        }

        public void OnConditionSelected(string condition)
        {
            if (_codingData.TryGetValue(_modalTargetCharId, out var plan))
            {
                var row = plan.rows[_modalTargetRowIndex];
                if (_modalTargetConditionNum == 1) row.condition1 = condition;
                else row.condition2 = condition;

                UpdateCodingPanel(); // Just refresh coding panel
                SaveTacticsToFile(); // Save to CharacterPool.json
            }
            conditionModal.Close();
        }

        public void OnRunBattleClicked()
        {
            SaveFormationToTacticsFile();
        }

        private void UpdateAllUI()
        {
            UpdatePoolUI();
            UpdateFormationUI();
            UpdateDetailPanel();
            UpdateCodingPanel();
            UpdateCostDisplay();
        }

        private void UpdatePoolUI()
        {
            if (characterPoolContainer == null) return;
            int i = 0;
            foreach (Transform child in characterPoolContainer)
            {
                if (i >= availableCharacters.Count) break;
                var card = child.GetComponent<CharacterCardUI>();
                if (card != null)
                {
                    var data = availableCharacters[i];

                    bool isDeployed = GetSlotIndex(data) != -1;
                    bool isSelected = _selectedCharacter == data;

                    card.SetDeployed(isDeployed);
                    card.SetSelected(isSelected);
                }
                i++;
            }
        }

        private void UpdateFormationUI()
        {
            for (int i = 0; i < 6; i++)
            {
                if (i < _formationSlots.Count && _formationSlots[i] != null)
                {
                    var slot = _formationSlots[i];
                    slot.UpdateState(_unitSlots[i]);

                    bool isActive = false;
                    if (_selectedCharacter != null)
                    {
                        // Highlight if this slot contains the selected char
                        if (_unitSlots[i] == _selectedCharacter) isActive = true;
                        // OR if selected char is NOT deployed, highlight empty slots to suggest placement
                        else if (GetSlotIndex(_selectedCharacter) == -1 && _unitSlots[i] == null) isActive = true;
                    }
                    slot.SetActiveHighlight(isActive);
                }
            }
        }

        private void UpdateDetailPanel()
        {
            if (characterDetailPanel == null) return;

            if (_selectedCharacter == null)
            {
                characterDetailPanel.SetActive(false);
                return;
            }

            characterDetailPanel.SetActive(true);
            var c = _selectedCharacter;
            if (c.portrait != null && detailPortrait != null) detailPortrait.sprite = c.portrait;
            if (detailCost != null) detailCost.text = c.cost.ToString();
            if (detailName != null) detailName.text = c.characterName;
            if (detailClass != null) detailClass.text = c.characterClass;
            if (detailArcana != null) detailArcana.text = c.arcana;
            if (detailSpeed != null) detailSpeed.text = c.speed.ToString();

            // Get description from ClassList.json based on character's class
            if (detailDesc != null)
            {
                string description = c.description; // Default fallback
                if (_classData.TryGetValue(c.characterClass, out ClassInfo classInfo))
                {
                    description = classInfo.description;
                }
                detailDesc.text = description;
            }

            // Update Stats
            if (_classData.TryGetValue(c.characterClass, out ClassInfo cInfo) && cInfo.stats != null)
            {
                if (detailStatHP != null) detailStatHP.text = cInfo.stats.hp;
                if (detailStatPhysAtk != null) detailStatPhysAtk.text = cInfo.stats.physicalAttack;
                if (detailStatPhysDef != null) detailStatPhysDef.text = cInfo.stats.physicalDefense;
                if (detailStatMagAtk != null) detailStatMagAtk.text = cInfo.stats.magicalAttack;
                if (detailStatMagDef != null) detailStatMagDef.text = cInfo.stats.magicalDefense;
                if (detailStatAccuracy != null) detailStatAccuracy.text = cInfo.stats.accuracy;
                if (detailStatEvasion != null) detailStatEvasion.text = cInfo.stats.evasion;
                if (detailStatCritRate != null) detailStatCritRate.text = cInfo.stats.criticalRate;
                if (detailStatGuardRate != null) detailStatGuardRate.text = cInfo.stats.guardRate;
                if (detailStatSpeed != null) detailStatSpeed.text = cInfo.stats.actionSpeed;
            }

            bool isDeployed = GetSlotIndex(c) != -1;
            if (removeFromUnitBtn != null) removeFromUnitBtn.gameObject.SetActive(isDeployed);
        }

        private void UpdateCodingPanel()
        {
            if (codingListContainer == null) return;

            // Clear list
            foreach (Transform child in codingListContainer) Destroy(child.gameObject);

            if (_selectedCharacter == null)
            {
                if (codingPanelTitle != null) codingPanelTitle.text = "캐릭터 선택 대기";
                return;
            }

            if (codingPanelTitle != null) codingPanelTitle.text = $"{_selectedCharacter.characterName.Split(' ')[0]} - 작전 코딩";

            // If not deployed, maybe we don't show coding? Or show preview? 
            // The HTML implies coding is available when selected, but data is initialized on placement.
            // Let's show it if data exists, or empty if not.

            if (_codingData.TryGetValue(_selectedCharacter.id, out var plan))
            {
                if (tacticRowPrefab != null)
                {
                    for (int i = 0; i < plan.rows.Count; i++)
                    {
                        var go = Instantiate(tacticRowPrefab, codingListContainer);
                        var rowUI = go.GetComponent<TacticRowUI>();
                        rowUI.Setup(this, _selectedCharacter.id, i, plan.rows[i]);
                    }
                }
            }
            else
            {
                // Show default skills (preview)
                if (tacticRowPrefab != null && _selectedCharacter.skills != null)
                {
                    for (int i = 0; i < _selectedCharacter.skills.Count; i++)
                    {
                        var skill = _selectedCharacter.skills[i];
                        var go = Instantiate(tacticRowPrefab, codingListContainer);
                        var rowUI = go.GetComponent<TacticRowUI>();
                        // Create a temporary row for display
                        var tempRow = new TacticRow(skill.name, skill.skillType.ToString(), TacticsDatabase.DEFAULT_CONDITION, TacticsDatabase.DEFAULT_CONDITION);
                        rowUI.Setup(this, _selectedCharacter.id, i, tempRow);
                    }
                }
            }
        }

        private void UpdateCostDisplay()
        {
            if (currentCostText == null) return;
            int current = CalculateTotalCost();
            currentCostText.text = $"{current} / {maxCost}";
            currentCostText.color = current > maxCost ? Color.red : Color.cyan;
        }

        private int CalculateTotalCost()
        {
            int sum = 0;
            foreach (var c in _unitSlots)
            {
                if (c != null) sum += c.cost;
            }
            return sum;
        }

        private int GetSlotIndex(CharacterData data)
        {
            for (int i = 0; i < _unitSlots.Length; i++)
            {
                if (_unitSlots[i] == data) return i;
            }
            return -1;
        }

        private void SaveTacticsToFile()
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
                    if (_codingData.TryGetValue(character.id, out var plan))
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

                // Write to file
                string path = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Resources/CharacterPool.json");
                System.IO.File.WriteAllText(path, json);

                Debug.Log("Tactics saved to CharacterPool.json");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save tactics: {e.Message}");
            }
        }

        [System.Serializable]
        private class CharacterPoolSaveData
        {
            public string Name;
            public List<TacticsSaveData> tactics;
        }

        [System.Serializable]
        private class TacticsSaveData
        {
            public string characterClass;
            public List<TacticRowSaveData> plan;
        }

        [System.Serializable]
        private class TacticRowSaveData
        {
            public string skill;
            public string condition1;
            public string condition2;
        }

        private void SaveFormationToTacticsFile()
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
                    if (_unitSlots[i] != null)
                    {
                        var character = _unitSlots[i];
                        posData.name = character.characterName;

                        // If this character has tactics data, add it
                        if (_codingData.TryGetValue(character.id, out var plan))
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

                // Write to file
                string path = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Resources/tactics.json");
                System.IO.File.WriteAllText(path, json);

                Debug.Log("Formation and tactics saved to tactics.json");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save formation: {e.Message}");
            }
        }

        [System.Serializable]
        private class TacticsFileSaveData
        {
            public List<PositionSaveData> positions;
        }

        [System.Serializable]
        private class PositionSaveData
        {
            public string position;
            public string name;
            public List<TacticsSaveData> tactics;
        }

        private void LoadFormationFromTacticsFile()
        {
            try
            {
                TextAsset tacticsAsset = Resources.Load<TextAsset>("tactics");
                if (tacticsAsset == null)
                {
                    Debug.LogWarning("tactics.json not found, starting with empty formation");
                    return;
                }

                TacticsFileLoadData tacticsData = JsonUtility.FromJson<TacticsFileLoadData>(tacticsAsset.text);
                if (tacticsData == null || tacticsData.positions == null)
                {
                    Debug.LogWarning("Failed to parse tactics.json");
                    return;
                }

                // Clear current formation
                for (int i = 0; i < 6; i++)
                {
                    _unitSlots[i] = null;
                }
                _codingData.Clear();

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
                    _unitSlots[slotIndex] = character;

                    // Load tactics if present
                    if (posData.tactics != null && posData.tactics.Length > 0)
                    {
                        var tacticData = posData.tactics[0];
                        if (tacticData.plan != null && tacticData.plan.Length > 0)
                        {
                            var plan = new TacticsPlan(character.id);
                            plan.rows.Clear();

                            foreach (var rowData in tacticData.plan)
                            {
                                // Determine skill type from character's skills
                                string skillType = "AP";
                                var skill = character.skills.Find(s => s.name == rowData.skill);
                                if (skill != null)
                                {
                                    skillType = skill.skillType.ToString();
                                }

                                plan.rows.Add(new TacticRow(
                                    rowData.skill,
                                    skillType,
                                    rowData.condition1,
                                    rowData.condition2
                                ));
                            }

                            _codingData[character.id] = plan;
                        }
                    }
                    else
                    {
                        // No saved tactics, create default plan
                        if (!_codingData.ContainsKey(character.id))
                        {
                            _codingData[character.id] = CreateDefaultPlan(character);
                        }
                    }
                }

                Debug.Log("Formation loaded from tactics.json");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load formation: {e.Message}");
            }
        }

        [System.Serializable]
        private class TacticsFileLoadData
        {
            public PositionLoadData[] positions;
        }

        [System.Serializable]
        private class PositionLoadData
        {
            public string position;
            public string name;
            public TacticsLoadData[] tactics;
        }

        [System.Serializable]
        private class TacticsLoadData
        {
            public string @class;
            public TacticRowLoadData[] plan;
        }

        [System.Serializable]
        private class TacticRowLoadData
        {
            public string skill;
            public string condition1;
            public string condition2;
        }

        private TacticsPlan CreateDefaultPlan(CharacterData data)
        {
            var plan = new TacticsPlan(data.id);
            foreach (var skill in data.skills)
            {
                plan.rows.Add(new TacticRow(skill.name, skill.skillType.ToString(), TacticsDatabase.DEFAULT_CONDITION, TacticsDatabase.DEFAULT_CONDITION));
            }
            return plan;
        }
    }
}
