using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{
    public TextMeshProUGUI versionText;
    public Button startButton;
    public Button gachaButton;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(versionText != null)
        {
            versionText.text = "v.10";
        }
        startButton.onClick.AddListener(OnStartButtonClicked);
        gachaButton.onClick.AddListener(OnGachaButtonClicked);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnStartButtonClicked()
    {
        SceneManager.LoadScene("TacticsScene");
    }

    void OnGachaButtonClicked()
    {
        SceneManager.LoadScene("GachaScene");
    }
}
