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

        private void Start()
        {
            AutoAssignReferences();
            InitializeUI();
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

            if (conditionModal == null) conditionModal = FindObjectOfType<ConditionModalUI>(true);

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

                    // Load Portrait (Assuming they are in Resources/Portraits or just Resources)
                    // Removing extension if present
                    string spriteName = System.IO.Path.GetFileNameWithoutExtension(def.Portrait);
                    newData.portrait = Resources.Load<Sprite>($"Portraits/{spriteName}");
                    if (newData.portrait == null) newData.portrait = Resources.Load<Sprite>(spriteName);

                    // Add default skills
                    newData.skills = new List<SkillData>
                    {
                        new SkillData { name = "Attack", type = SkillType.AP },
                        new SkillData { name = "Guard", type = SkillType.PP }
                    };

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
                    _classData[classInfo.id] = classInfo;
                }
                Debug.Log($"Loaded {_classData.Count} classes from ClassList.json");
            }
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
            public string id;
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
            }
            conditionModal.Close();
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
                // Show "Not Deployed" or similar message if we strictly follow "Init on placement"
                // For now, let's just say "Place character to edit tactics"
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

        private TacticsPlan CreateDefaultPlan(CharacterData data)
        {
            var plan = new TacticsPlan(data.id);
            foreach (var skill in data.skills)
            {
                plan.rows.Add(new TacticRow(skill.name, skill.type.ToString(), TacticsDatabase.DEFAULT_CONDITION, TacticsDatabase.DEFAULT_CONDITION));
            }
            return plan;
        }
    }
}
