using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Arcana.Tactics.Data;
using static Arcana.Tactics.TacticsDataManager;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Arcana.Tactics.UI
{
    public class TacticsUIManager : MonoBehaviour
    {
        [Header("Data")]
        public List<CharacterData> availableCharacters;
        public int maxCost = 15;

        [Header("UI Containers")]
        public Transform characterPoolPanel;
        public Transform characterPoolContainer;
        public Transform formationGridContainer; // Should have 6 slots as children
        public Transform codingListContainer;

        [Header("UI Prefabs")]
        public GameObject characterCardPrefab;
        public GameObject tacticRowPrefab;

        [Header("UI Components")]
        public ConditionModalUI conditionModal;
        public SkillModal skillModal;
        public TextMeshProUGUI currentCostText;
        public TextMeshProUGUI codingPanelTitle;
        public GameObject characterDetailPanel;
        public Image detailPortrait;
        public TextMeshProUGUI detailCost;
        public TextMeshProUGUI detailName;
        public TextMeshProUGUI detailClass;
        public TextMeshProUGUI detailDesc;
        public TextMeshProUGUI uidText;

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
        public Button gotoGachaButton; 

        // State
        private CharacterData _selectedCharacter; // Currently selected (could be from pool or slot)
        private CharacterData[] _unitSlots = new CharacterData[6]; // 0-5
        private Dictionary<string, TacticsPlan> _codingData = new Dictionary<string, TacticsPlan>();

        // Modal State
        private string _modalTargetCharId;
        private int _modalTargetRowIndex;
        private int _modalTargetConditionNum; // 1 or 2

        private List<FormationSlotUI> _formationSlots = new List<FormationSlotUI>();
        private TacticsDataManager _dataManager;

        private IEnumerator Start()
        {
            AutoAssignReferences();
            InitializeUI();

            // TacticsDataManager의 데이터 로딩 완료 대기 (Firebase 비동기 로딩 포함)
            Debug.Log("TacticsUIManager: TacticsDataManager 데이터 로딩 대기 중...");
            yield return new WaitUntil(() => _dataManager != null && _dataManager.isDataLoaded);
            Debug.Log("TacticsUIManager: TacticsDataManager 데이터 로딩 완료!");

            // availableCharacters는 TacticsDataManager에서 이미 로드됨 (LoadCharactersFromWeb에서)
            availableCharacters = _dataManager.availableCharacters;

            // CharacterPool UI 초기화 (데이터 로드 완료 후)
            if (characterPoolContainer != null && characterCardPrefab != null)
            {
                // 모든 캐릭터 카드 생성 (배치 여부는 UpdatePoolUI에서 처리)
                foreach (var charData in availableCharacters)
                {
                    var go = Instantiate(characterCardPrefab, characterPoolContainer);
                    var card = go.GetComponent<CharacterCardUI>();
                    card.Setup(charData, this, false);
                }
            }

            // Load formation from TacticsDataManager (씬마다 독립적인 인스턴스)
            var loadResult = _dataManager.GetPlayerFormationLoadResult();
            if (loadResult != null)
            {
                _unitSlots = loadResult.unitSlots;
                _codingData = loadResult.codingData;
            }
            else
            {
                Debug.LogError("TacticsUIManager: Player formation load result is null!");
                _unitSlots = new CharacterData[6];
                _codingData = new Dictionary<string, TacticsPlan>();
            }

            UpdateAllUI();
        }

        private void AutoAssignReferences()
        {
            if (characterPoolContainer == null)
            {
                GameObject go = GameObject.Find("PoolScrollView");
                if (go != null)
                {
                    characterPoolPanel = go.transform.parent;
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
                if (detailClass == null)
                {
                    Transform t = RecursiveFind(characterDetailPanel.transform, "Class");
                    if (t != null) detailClass = t.GetComponent<TextMeshProUGUI>();
                }
                if (detailDesc == null)
                {
                    Transform t = RecursiveFind(characterDetailPanel.transform, "Description");
                    if (t != null) detailDesc = t.GetComponent<TextMeshProUGUI>();
                }

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
            if (gotoGachaButton == null) gotoGachaButton = GameObject.Find("GachaButton").GetComponent<Button>();
            if (characterCardPrefab == null) characterCardPrefab = Resources.Load<GameObject>("Prefabs/UI/CharacterCardPrefab");
            if (tacticRowPrefab == null) tacticRowPrefab = Resources.Load<GameObject>("Prefabs/UI/TacticRowPrefab");
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
            // Get data manager instance
            _dataManager = TacticsDataManager.Instance;
            if (_dataManager == null)
            {
                Debug.LogError("TacticsDataManager not found! Please add it to the scene.");
                return;
            }

            // availableCharacters는 Start()에서 데이터 로드 완료 후 설정됨
            // 여기서는 UI 구조만 초기화

            // Add DropHandler to the characterPoolContainer for dragging back to pool
            if (characterPoolPanel != null)
            {
                var dropHandler = characterPoolPanel.GetComponent<CharacterPoolDropHandler>();
                if (dropHandler == null) dropHandler = characterPoolPanel.gameObject.AddComponent<CharacterPoolDropHandler>();
                dropHandler.Setup(this);
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
            if (skillModal != null) skillModal.Setup(this);
            if (runBattleButton != null) runBattleButton.onClick.AddListener(OnRunBattleClicked);
            if (gotoGachaButton != null) gotoGachaButton.onClick.AddListener(OnGotoGachaClicked);
        }

        public void OnGotoGachaClicked()
        {
            SceneManager.LoadScene("GachaScene");
        }

        public void OnCharacterPoolCardClicked(CharacterData data)
        {
            _selectedCharacter = data;
            UpdateAllUI();
        }

        public void OnFormationSlotClicked(int slotIndex)
        {
            // Only select the character in the slot if any
            if (_unitSlots[slotIndex] != null)
            {
                _selectedCharacter = _unitSlots[slotIndex];
                UpdateAllUI();
            }
            else
            {
                // If empty slot clicked, maybe deselect? Or do nothing.
                // Let's deselect to clear detail panel
                _selectedCharacter = null;
                UpdateAllUI();
            }
        }

        public void OnConditionClicked(string charName, int rowIndex, int conditionNum)
        {
            // Use _selectedCharacter if it matches, otherwise find by name
            CharacterData targetCharacter = null;
            if (_selectedCharacter != null && _selectedCharacter.characterName == charName)
            {
                targetCharacter = _selectedCharacter;
            }
            else
            {
                targetCharacter = availableCharacters.Find(c => c.characterName == charName);
            }

            if (targetCharacter == null)
            {
                Debug.LogError($"TacticsUIManager: Character with name {charName} not found! Available characters: {string.Join(", ", availableCharacters.Select(c => c.characterName))}");
                return;
            }
            _modalTargetCharId = targetCharacter.id;
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
                _dataManager.SaveTacticsToFile(_codingData); // Save to CharacterPool.json
                _dataManager.SaveFormationToTacticsFile(_unitSlots, _codingData);
            }
            conditionModal.Close();
        }

        /// <summary>
        /// 스킬 이름 클릭 시 호출 (TacticRowUI에서)
        /// </summary>
        public void OnSkillNameClicked(string charName, int rowIndex)
        {
            _modalTargetRowIndex = rowIndex;

            // Use _selectedCharacter if it matches, otherwise find by name
            CharacterData targetCharacter = null;
            if (_selectedCharacter != null && _selectedCharacter.characterName == charName)
            {
                targetCharacter = _selectedCharacter;
            }
            else
            {
                targetCharacter = availableCharacters.Find(c => c.characterName == charName);
            }

            if (targetCharacter == null)
            {
                Debug.LogError($"TacticsUIManager: Character with name {charName} not found! Available characters: {string.Join(", ", availableCharacters.Select(c => c.characterName))}");
                return;
            }

            _modalTargetCharId = targetCharacter.id;

            if (skillModal == null)
            {
                Debug.LogError("TacticsUIManager: skillModal is not assigned!");
                return;
            }

            // 스킬 모달 열기
            skillModal.Open(targetCharacter, OnSkillSelected);
        }

        /// <summary>
        /// 스킬 선택 시 호출 (SkillModal에서 콜백)
        /// </summary>
        private void OnSkillSelected(SkillData selectedSkill)
        {
            if (_codingData.TryGetValue(_modalTargetCharId, out var plan))
            {
                if (_modalTargetRowIndex >= 0 && _modalTargetRowIndex < plan.rows.Count)
                {
                    var row = plan.rows[_modalTargetRowIndex];

                    // 스킬 이름 업데이트
                    row.skillName = selectedSkill.name;

                    // 스킬 타입 업데이트 (AP/PP)
                    row.skillType = selectedSkill.type; // "active" or "passive"

                    Debug.Log($"TacticsUIManager: Skill changed to {selectedSkill.name} for row {_modalTargetRowIndex}");

                    // UI 업데이트
                    UpdateCodingPanel();

                    // 데이터 저장
                    _dataManager.SaveTacticsToFile(_codingData); // CharacterPool.json에 저장
                    _dataManager.SaveFormationToTacticsFile(_unitSlots, _codingData); // tactics.json에 저장
                }
                else
                {
                    Debug.LogError($"TacticsUIManager: Invalid row index {_modalTargetRowIndex}");
                }
            }
            else
            {
                Debug.LogError($"TacticsUIManager: No coding data found for character {_modalTargetCharId}");
            }
        }

        public void OnRunBattleClicked()
        {
            // 로컬 파일에 저장
            _dataManager.SaveFormationToTacticsFile(_unitSlots, _codingData);

            // Firebase에 저장 (BattleScene으로 이동할 때만)
            if (FirebaseManager.Instance != null && FirebaseManager.Instance.isFirebaseInitialized)
            {
                string tacticsJson = _dataManager.GetTacticsJson(_unitSlots, _codingData);
                FirebaseManager.Instance.SaveTacticsToFirebase(tacticsJson, (success, key) =>
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
            else
            {
                Debug.LogWarning("Firebase가 초기화되지 않았습니다. 로컬 파일만 저장됩니다.");
            }

            // BattleScene으로 이동한다. 
            SceneManager.LoadScene("BattleScene");
        }

        private void UpdateAllUI()
        {
            UpdatePoolUI();
            UpdateFormationUI();
            UpdateDetailPanel();
            UpdateCodingPanel();
            UpdateCostDisplay();
            UpdateUserData();
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

                    // 배치된 캐릭터는 숨김 (CharacterCardUI.SetDeployed에서 처리)
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

        private void UpdateUserData() 
        {
            if (uidText != null && UserDataManager.Instance != null && UserDataManager.Instance.currentUserData != null)
            {
                uidText.text = "UID: " + UserDataManager.Instance.currentUserData.playerName;
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

            // Get description from ClassList.json based on character's class
            if (detailDesc != null)
            {
                string description = c.description; // Default fallback
                var classInfo = _dataManager.GetClassInfo(c.characterClass);
                if (classInfo != null)
                {
                    description = classInfo.description;
                }
                detailDesc.text = description;
            }

            // Update Stats
            var cInfo = _dataManager.GetClassInfo(c.characterClass);
            if (cInfo != null && cInfo.stats != null)
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


            // TacticsRowPrefab은 maxTacticsRow만큼 생성됩니다.
            // 저장된 plan이 그보다 적은 경우, 나머지는 기본 prefab으로 생성됩니다.

            // Tactics 데이터가 없으면 기본 plan 생성
            if (!_codingData.TryGetValue(_selectedCharacter.id, out var plan))
            {
                plan = _dataManager.CreateDefaultPlan(_selectedCharacter);
                _codingData[_selectedCharacter.id] = plan;
            }

            int maxTacticsRow = TacticsDatabase.MAX_TACTICS_ROW;
            for (int i = 0; i < maxTacticsRow; i++)
            {
                var go = Instantiate(tacticRowPrefab, codingListContainer);
                var rowUI = go.GetComponent<TacticRowUI>();

                if (i < plan.rows.Count)
                {
                    rowUI.Setup(this, _selectedCharacter.characterName, i, plan.rows[i]);
                }
                else
                {
                    rowUI.Setup(this, _selectedCharacter.characterName, i, null);
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




        public void OnCharacterDroppedOnSlot(CharacterData charData, int slotIndex)
        {
            // Check cost
            int currentTotalCost = CalculateTotalCost();
            int costDiff = charData.cost;
            if (_unitSlots[slotIndex] != null) costDiff -= _unitSlots[slotIndex].cost;

            // If moving from another slot (should be handled by OnSlotDroppedOnSlot, but just in case)
            int existingIndex = GetSlotIndex(charData);
            if (existingIndex != -1)
            {
                _unitSlots[existingIndex] = null;
                costDiff = 0;
            }

            if (currentTotalCost + costDiff > maxCost)
            {
                Debug.LogWarning("Cost Limit Exceeded!");
                return;
            }

            _unitSlots[slotIndex] = charData;

            // Initialize coding data if needed
            if (!_codingData.ContainsKey(charData.id))
            {
                _codingData[charData.id] = _dataManager.CreateDefaultPlan(charData);
            }

            _selectedCharacter = charData;
            StartCoroutine(UpdateAllUINextFrame());

            // tactics.json에 저장
            _dataManager.SaveFormationToTacticsFile(_unitSlots, _codingData);
        }

        IEnumerator UpdateAllUINextFrame()
        {
            yield return new WaitForEndOfFrame();
            UpdateAllUI();
        }

        public void OnSlotDroppedOnSlot(int sourceSlotIndex, int targetSlotIndex)
        {
            if (sourceSlotIndex == targetSlotIndex) return;

            // Swap or Move
            CharacterData sourceChar = _unitSlots[sourceSlotIndex];
            CharacterData targetChar = _unitSlots[targetSlotIndex];

            // Cost check is not needed for swap if both exist, or move if target is empty
            // But if target has char and source has char, cost sum remains same.
            // If target has char and source is empty (impossible in drag), ...

            _unitSlots[targetSlotIndex] = sourceChar;
            _unitSlots[sourceSlotIndex] = targetChar;

            StartCoroutine(UpdateAllUINextFrame());

            _dataManager.SaveFormationToTacticsFile(_unitSlots, _codingData);
        }

        public void OnSlotDroppedOnPool(int sourceSlotIndex)
        {
            if (_unitSlots[sourceSlotIndex] == null) return;

            _unitSlots[sourceSlotIndex] = null;
            // Optional: Clear data? No, keep it.
            StartCoroutine(UpdateAllUINextFrame());

            _dataManager.SaveFormationToTacticsFile(_unitSlots, _codingData);
        }
    }
}

