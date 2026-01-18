using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class StoryBoardManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Button skipButton;
    public string nextSceneName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        skipButton.onClick.AddListener(OnSkipButtonClicked);
        videoPlayer.loopPointReached += OnVideoPlayerCompleted;

        // 영상 재생 동안 BGM 일시정지
        if (BGMPlayer.Instance != null)
            BGMPlayer.Instance.PauseBGM();
    }

    void OnSkipButtonClicked()
    {
        if (BGMPlayer.Instance != null)
            BGMPlayer.Instance.ResumeBGM();
        SceneManager.LoadScene(nextSceneName);
    }

    // VideoPlayer 재생 완료 시 호출되는 메서드
    void OnVideoPlayerCompleted(VideoPlayer vp)
    {
        if (BGMPlayer.Instance != null)
            BGMPlayer.Instance.ResumeBGM();
        SceneManager.LoadScene(nextSceneName);
    }
}
