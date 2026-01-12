using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StoryBoardManager : MonoBehaviour
{
    public Button skipButton;
    public string nextSceneName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        skipButton.onClick.AddListener(OnSkipButtonClicked);
    }

    void OnSkipButtonClicked()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
