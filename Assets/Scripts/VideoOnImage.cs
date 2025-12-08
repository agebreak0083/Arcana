using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoOnImage : MonoBehaviour
{
    public string videoFileName = "gachaUI.mp4";
    public RawImage rawImage;
    public VideoPlayer videoPlayer;
    public bool isPause = false;

    IEnumerator Start()
    {
        // RenderTexture 생성
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);

        // VideoPlayer 설정
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;

        // RawImage에 연결
        rawImage.texture = renderTexture;

        // WebGL과 에디터에서 경로 처리 방식이 다름
        string videoPath;
        
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: Application.streamingAssetsPath는 이미 URL을 반환 (http:// 또는 https://)
        // 파일명을 직접 추가 (경로 구분자는 / 사용)
        string basePath = Application.streamingAssetsPath;
        if (!basePath.EndsWith("/"))
        {
            basePath += "/";
        }
        videoPath = basePath + videoFileName;
        Debug.Log($"WebGL Video Path: {videoPath}");
#else
        // 에디터/빌드: 파일 시스템 경로 사용
        videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
        
        // 에디터에서만 파일 존재 확인 (WebGL에서는 File.Exists가 작동하지 않음)
        if (!System.IO.File.Exists(videoPath))
        {
            Debug.LogError($"Video file not found: {videoPath}");
            yield break;
        }
        Debug.Log($"Video Path: {videoPath}");
#endif

        // VideoPlayer 설정 및 재생
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoPath;
        
        // WebGL에서는 Prepare가 실패할 수 있으므로 타임아웃 추가
        videoPlayer.Prepare();
        
        float prepareTimeout = 10f;
        float elapsed = 0f;
        while (!videoPlayer.isPrepared && elapsed < prepareTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (!videoPlayer.isPrepared)
        {
            Debug.LogError($"Video prepare timeout: {videoPath}");
            yield break;
        }

        videoPlayer.Play();

        // 비디오의 첫 프레임만 재생 후에 일단 Pause 한다.
        if (isPause)
        {
            videoPlayer.Pause();
        }
    }
}