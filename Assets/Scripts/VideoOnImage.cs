using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoOnImage : MonoBehaviour
{
    public RawImage rawImage;
    public VideoPlayer videoPlayer;

    void Start()
    {
        // RenderTexture 생성
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);

        // VideoPlayer 설정
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;

        // RawImage에 연결
        rawImage.texture = renderTexture;

        // 비디오 재생
        videoPlayer.Play();
        
        // 비디오의 첫 프레임만 재생 후에 일단 Pause 한다.
        videoPlayer.Pause();
    }
}