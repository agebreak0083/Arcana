using Arcana.Tactics.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{
    public TextMeshProUGUI versionText;
    public Button startButton;
    public Button gachaButton;
    public Button towerBattleButton;
    public Button battleMapButton;
    public TMP_InputField idText; 
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(versionText != null)
        {
            versionText.text = "v.30";
        }

        SetId(UserDataManager.Instance.currentUserData.playerName);

        startButton.onClick.AddListener(OnStartButtonClicked);
        gachaButton.onClick.AddListener(OnGachaButtonClicked);
        towerBattleButton.onClick.AddListener(OnTowerBattleButtonClicked);
        battleMapButton.onClick.AddListener(OnBattleMapButtonClicked);

        // 아리엘의 Welcome Message 표시
        IRISUIManager.Instance.ShowIrisUI(MessageToIRIS.WELCOME_MESSAGE, new WelcomeGameStatusData());
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
            towerBattleButton.interactable = false;
        }
        else
        {
            startButton.interactable = true;
            gachaButton.interactable = true;
            towerBattleButton.interactable = true;
        }
    }

    void OnStartButtonClicked()
    {
        UserDataManager.Instance.currentUserData.playerName = idText.text;
        UserDataManager.Instance.SaveUserData();

        BattleSetting.gameMode = GameMode.STORY_MODE;
        SceneManager.LoadScene("StoryBoardScene");
    }

    void OnGachaButtonClicked()
    {
        UserDataManager.Instance.currentUserData.playerName = idText.text;
        UserDataManager.Instance.SaveUserData();
        SceneManager.LoadScene("GachaScene");
    }

    void OnTowerBattleButtonClicked()
    {
        UserDataManager.Instance.currentUserData.playerName = idText.text;
        UserDataManager.Instance.SaveUserData();

        BattleSetting.gameMode = GameMode.TOWER_MODE;
        SceneManager.LoadScene("TacticsScene");
    }

    void OnBattleMapButtonClicked()
    {
        UserDataManager.Instance.currentUserData.playerName = idText.text;
        UserDataManager.Instance.SaveUserData();

        SceneManager.LoadScene("BattleMapScene");
    }
}
