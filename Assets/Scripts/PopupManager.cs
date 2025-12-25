using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupManager : MonoBehaviour
{
    public enum PopupType
    {
        None,
        Message,
        Confirm,        
    }
    public string popupPrefabPath = "Prefabs/UI/PopupPrefab";
    
    public static PopupManager Instance { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);            
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowPopup(PopupType popupType, string message, System.Action onConfirm)
    {
        // Canvas를 찾아서, popupObject를 생성
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found");
            return;
        }
        
        // 프리팹 로드
        GameObject popupPrefab = Resources.Load<GameObject>(popupPrefabPath);
        if (popupPrefab == null)
        {
            Debug.LogError($"Popup prefab not found at path: {popupPrefabPath}. Make sure the prefab is in Assets/Resources/{popupPrefabPath}.prefab");
            return;
        }
        
        GameObject popupObject = Instantiate(popupPrefab, canvas.transform);
        popupObject.SetActive(true);

        // Popup 컴포넌트 가져오기
        Popup popup = popupObject.GetComponent<Popup>();
        if (popup == null)
        {
            Debug.LogError("Popup component not found on PopupPrefab");
            Destroy(popupObject);
            return;
        }

        // 메시지 설정
        popup.SetMessage(message);

        // 확인 버튼 콜백 설정
        popup.SetOnConfirm(onConfirm);

        // 취소 버튼 콜백 설정 (기본 동작: 팝업 닫기)
        popup.SetOnCancel(null);

        // PopupType에 따라 버튼 표시/숨김
        switch (popupType)
        {
            case PopupType.Message:
                if (popup.confirmBtn != null)
                    popup.confirmBtn.gameObject.SetActive(true);
                if (popup.cancelBtn != null)
                    popup.cancelBtn.gameObject.SetActive(false);
                break;
            case PopupType.Confirm:
                if (popup.confirmBtn != null)
                    popup.confirmBtn.gameObject.SetActive(true);
                if (popup.cancelBtn != null)
                    popup.cancelBtn.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }
}
