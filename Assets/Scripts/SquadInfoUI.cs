using Arcana.Tactics.Data;
using Arcana.Tactics.UI;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SquadInfoUI : MonoBehaviour
{
    public FormationSlotUI[] formationSlots;
    public TextMeshProUGUI squadNameText;
    public Button tacticsButton;
    public Button returnButton;
    public bool isPlayerSquad = false;
    
    private RectTransform _rectTransform;
    private Vector2 _originalPosition;
    private bool _isInitialized = false;

    void Awake()
    {
        for(int i = 0; i < formationSlots.Length; i++)
        {
            formationSlots[i].Setup(null, i, false);
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tacticsButton.onClick.AddListener(OnTacticsButtonClicked);
        returnButton.onClick.AddListener(OnReturnButtonClicked);
        
        // RectTransform과 초기 위치 저장
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null)
        {
            _originalPosition = _rectTransform.anchoredPosition;
            _isInitialized = true;
        }
    }

    public void UpdateSquadInfo(string squadName, CharacterData[] unitSlots)
    {
        gameObject.SetActive(true);

        squadNameText.text = squadName;
        for(int i = 0; i < unitSlots.Length; i++)
        {
            formationSlots[i].UpdateState(unitSlots[i]);
        }
        
        // DOTween 슬라이드 애니메이션
        if (_isInitialized && _rectTransform != null)
        {
            // 현재 애니메이션 중이면 중지
            _rectTransform.DOKill();
            
            // 화면 왼쪽 바깥 위치 계산 (Canvas의 너비만큼 왼쪽으로 이동)
            Canvas canvas = GetComponentInParent<Canvas>();
            float screenWidth = canvas != null ? canvas.pixelRect.width : Screen.width;
            float xPos = isPlayerSquad ? _originalPosition.x - screenWidth : _originalPosition.x + screenWidth;
            Vector2 startPosition = new Vector2(xPos, _originalPosition.y);
            
            // 시작 위치로 설정
            _rectTransform.anchoredPosition = startPosition;
            
            // 원래 위치로 슬라이드 애니메이션 (0.5초)
            _rectTransform.DOAnchorPos(_originalPosition, 0.5f)
                .SetEase(Ease.OutCubic);
        }
    }

    void OnTacticsButtonClicked()
    {
        
    }

    void OnReturnButtonClicked()
    {
        BattleMapManager.Instance.ReturnSquad(squadNameText.text);
        gameObject.SetActive(false);
    }
}
