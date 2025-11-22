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
        public TextMeshProUGUI detailName;
        public TextMeshProUGUI detailClass;
        public TextMeshProUGUI detailArcana;
        public TextMeshProUGUI detailSpeed;
        public TextMeshProUGUI detailDesc;
        public Button removeFromUnitBtn;

        // State
        private CharacterData _selectedCharacter; // Currently selected (could be from pool or slot)
        private CharacterData[] _unitSlots = new CharacterData[6]; // 0-5
        private Dictionary<string, TacticsPlan> _codingData = new Dictionary<string, TacticsPlan>();
        
        // Modal State
        private string _modalTargetCharId;
        private int _modalTargetRowIndex;
        private int _modalTargetConditionNum; // 1 or 2

        private void Start()
        {
            InitializeUI();
            UpdateAllUI();
        }

        private void InitializeUI()
        {
            // Clear placeholders
            foreach (Transform child in characterPoolContainer) Destroy(child.gameObject);
            
            // Instantiate Pool Cards
            foreach (var charData in availableCharacters)
            {
                var go = Instantiate(characterCardPrefab, characterPoolContainer);
                var card = go.GetComponent<CharacterCardUI>();
                card.Setup(charData, this, false);
            }

            // Initialize Slots (Assuming they are already children of formationGridContainer in order)
            for (int i = 0; i < 6; i++)
            {
                if (i < formationGridContainer.childCount)
                {
                    var slot = formationGridContainer.GetChild(i).GetComponent<FormationSlotUI>();
                    if (slot != null) slot.Setup(this, i);
                }
            }

            conditionModal.Setup(this);
            removeFromUnitBtn.onClick.AddListener(OnRemoveFromUnitClicked);
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
            int i = 0;
            foreach (Transform child in characterPoolContainer)
            {
                if (i >= availableCharacters.Count) break;
                var card = child.GetComponent<CharacterCardUI>();
                var data = availableCharacters[i];
                
                bool isDeployed = GetSlotIndex(data) != -1;
                bool isSelected = _selectedCharacter == data;

                card.SetDeployed(isDeployed);
                card.SetSelected(isSelected);
                i++;
            }
        }

        private void UpdateFormationUI()
        {
            for (int i = 0; i < 6; i++)
            {
                if (i < formationGridContainer.childCount)
                {
                    var slot = formationGridContainer.GetChild(i).GetComponent<FormationSlotUI>();
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
            if (_selectedCharacter == null)
            {
                characterDetailPanel.SetActive(false);
                return;
            }

            characterDetailPanel.SetActive(true);
            var c = _selectedCharacter;
            if (c.portrait != null) detailPortrait.sprite = c.portrait;
            detailName.text = c.characterName;
            detailClass.text = c.characterClass;
            detailArcana.text = c.arcana;
            detailSpeed.text = c.speed.ToString();
            detailDesc.text = c.description;

            bool isDeployed = GetSlotIndex(c) != -1;
            removeFromUnitBtn.gameObject.SetActive(isDeployed);
        }

        private void UpdateCodingPanel()
        {
            // Clear list
            foreach (Transform child in codingListContainer) Destroy(child.gameObject);

            if (_selectedCharacter == null)
            {
                codingPanelTitle.text = "캐릭터 선택 대기";
                return;
            }

            codingPanelTitle.text = $"{_selectedCharacter.characterName.Split(' ')[0]} - 작전 코딩";

            // If not deployed, maybe we don't show coding? Or show preview? 
            // The HTML implies coding is available when selected, but data is initialized on placement.
            // Let's show it if data exists, or empty if not.
            
            if (_codingData.TryGetValue(_selectedCharacter.id, out var plan))
            {
                for (int i = 0; i < plan.rows.Count; i++)
                {
                    var go = Instantiate(tacticRowPrefab, codingListContainer);
                    var rowUI = go.GetComponent<TacticRowUI>();
                    rowUI.Setup(this, _selectedCharacter.id, i, plan.rows[i]);
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
