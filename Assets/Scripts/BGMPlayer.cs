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

        // 새로운 Battle BGM 설정 및 재생
        audioSource.clip = battleBGM[Random.Range(0, battleBGM.Length)];
        audioSource.Play();

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
}
