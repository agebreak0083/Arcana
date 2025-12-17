using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{
    public TextMeshProUGUI versionText;
    public Button startButton;
    public Button gachaButton;
    public TMP_InputField idText; 
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(versionText != null)
        {
            versionText.text = "v.13";
        }

        SetId(UserDataManager.Instance.currentUserData.playerName);

        startButton.onClick.AddListener(OnStartButtonClicked);
        gachaButton.onClick.AddListener(OnGachaButtonClicked);
    }

    public void SetId(string id)
    {
        idText.text = id;
    }

    // Update is called once per frame
    void Update()
    {
        if(idText.text == "")
        {
            startButton.interactable = false;
            gachaButton.interactable = false;
        }
        else
        {
            startButton.interactable = true;
            gachaButton.interactable = true;
        }
    }

    void OnStartButtonClicked()
    {
        UserDataManager.Instance.currentUserData.playerName = idText.text;
        UserDataManager.Instance.SaveUserData();
        SceneManager.LoadScene("StoryBoardScene");
    }

    void OnGachaButtonClicked()
    {
        UserDataManager.Instance.currentUserData.playerName = idText.text;
        UserDataManager.Instance.SaveUserData();
        SceneManager.LoadScene("GachaScene");
    }
}
