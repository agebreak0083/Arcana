using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Popup : MonoBehaviour
{
    public Button confirmBtn;
    public Button cancelBtn;
    public TextMeshProUGUI messageText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void SetMessage(string message)
    {
        messageText.text = message;
    }

    public void SetOnConfirm(System.Action onConfirm)
    {
        confirmBtn.onClick.AddListener(() => {
            onConfirm?.Invoke();
            Destroy(this.gameObject);
        });
    }

    public void SetOnCancel(System.Action onCancel)
    {
        cancelBtn.onClick.AddListener(() => {
            onCancel?.Invoke();
            Destroy(this.gameObject);
        });
    }
}
