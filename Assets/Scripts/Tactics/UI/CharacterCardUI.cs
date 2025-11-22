using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Arcana.Tactics.Data;

namespace Arcana.Tactics.UI
{
    public class CharacterCardUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI References")]
        public Image portraitImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI classText;
        public TextMeshProUGUI costText;
        public GameObject selectedHighlight;
        public GameObject deployedOverlay; // Makes it look dim if deployed

        private CharacterData _data;
        private TacticsManager _manager;
        private bool _isDeployed;

        public void Setup(CharacterData data, TacticsManager manager, bool isDeployed)
        {
            _data = data;
            _manager = manager;
            _isDeployed = isDeployed;

            // In a real app, we would load the sprite. For now, we might just set color or text if sprite is null.
            if (data.portrait != null) portraitImage.sprite = data.portrait;
            
            nameText.text = data.characterName.Split(' ')[0]; // Just first name for brevity
            classText.text = $"({data.characterClass.Split(' ')[0]})";
            costText.text = $"{data.cost}C";

            UpdateVisuals();
        }

        public void SetSelected(bool selected)
        {
            if (selectedHighlight != null) selectedHighlight.SetActive(selected);
        }

        public void SetDeployed(bool deployed)
        {
            _isDeployed = deployed;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (deployedOverlay != null) deployedOverlay.SetActive(_isDeployed);
            // Optional: Change border color based on cost or state
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_manager != null)
            {
                _manager.OnCharacterPoolCardClicked(_data);
            }
        }
    }
}
