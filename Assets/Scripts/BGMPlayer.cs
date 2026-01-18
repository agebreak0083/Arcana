using System.Collections;
using UnityEngine;

public class BGMPlayer : MonoBehaviour
{
    public AudioClip mainBGM;
    public AudioClip[] battleBGM;
    public AudioClip storyBGM;
    
    [Header("Fade Settings")]
    public float fadeDuration = 1.0f; // 페이드 시간 (초)

    private AudioSource audioSource;
    private float originalVolume;
    private int currentBattleBGMIndex = 0; // 현재 재생 중인 Battle BGM 인덱스 (순서대로 재생)
    public static BGMPlayer Instance;
    void Start()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        audioSource = GetComponent<AudioSource>();
        originalVolume = audioSource.volume;

        PlayMainBGM();
    }

    public void PlayMainBGM()
    {
        audioSource.clip = mainBGM;
        audioSource.Play();
    }

    public void PlayBattleBGM()
    {
        StartCoroutine(FadeToBattleBGM());
    }

    IEnumerator FadeToBattleBGM()
    {
        // 현재 BGM 페이드 아웃
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();

        // 새로운 Battle BGM 설정 및 재생 (순서대로 재생, 랜덤 제거)
        if (battleBGM != null && battleBGM.Length > 0)
        {
            audioSource.clip = battleBGM[currentBattleBGMIndex];
            audioSource.Play();
            
            // 다음 재생을 위해 인덱스 증가 (순환)
            currentBattleBGMIndex = (currentBattleBGMIndex + 1) % battleBGM.Length;
        }

        // 새로운 BGM 페이드 인
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, originalVolume, elapsedTime / fadeDuration);
            yield return null;
        }

        audioSource.volume = originalVolume;
    }

    public void PlayStoryBGM()
    {
        audioSource.clip = storyBGM;
        audioSource.Play();
    }    

    public void StopAllBGM()
    {
        audioSource.Stop();
    }

    /// <summary> BGM 일시정지 (영상 재생 등으로 인한 음소거 시 사용) </summary>
    public void PauseBGM()
    {
        if (audioSource != null)
            audioSource.Pause();
    }

    /// <summary> 일시정지된 BGM 재개 </summary>
    public void ResumeBGM()
    {
        if (audioSource != null)
            audioSource.UnPause();
    }
}
